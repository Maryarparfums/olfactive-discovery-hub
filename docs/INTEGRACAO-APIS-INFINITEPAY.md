# Maryar — Guia de Integração de APIs e InfinitePay

Documento de referência técnica para evolução do projeto Maryar (frontend
TanStack Start já entregue na Fase 1) em direção a um e-commerce completo
com backend, autenticação, checkout e integração de pagamentos via
**InfinitePay** (PIX e Cartão de Crédito).

> **Status atual do projeto:** apenas frontend (mocks em `src/data/perfumes.ts`).
> Este documento descreve o caminho recomendado para habilitar backend, APIs
> internas e o fluxo de pagamento real.

---

## 1. Arquitetura recomendada

```
┌──────────────────────────────┐
│  Browser (React 19 + TSR)   │
│  - Vitrine / Catálogo / PDP │
│  - Checkout multi-step      │
└──────────────┬──────────────┘
               │  fetch / useServerFn
               ▼
┌──────────────────────────────┐
│  TanStack Start (Worker)    │
│  - createServerFn (RPC)     │
│  - /api/public/* (webhooks) │
└──────────────┬──────────────┘
               │
        ┌──────┴──────┐
        ▼             ▼
┌────────────┐  ┌──────────────────────┐
│ Lovable    │  │ InfinitePay API REST │
│ Cloud (DB) │  │ - charges (cartão)   │
│ Postgres   │  │ - pix                │
│ + Auth     │  │ - webhooks           │
└────────────┘  └──────────────────────┘
```

Componentes do stack atual e o que precisa ser adicionado:

| Camada            | Tecnologia                             | Status      |
| ----------------- | -------------------------------------- | ----------- |
| Frontend          | TanStack Start v1 + React 19           | Implementado|
| Routing/SSR       | TanStack Router (file-based)           | Implementado|
| Estilo            | Tailwind v4 + tokens em `styles.css`   | Implementado|
| Dados de produto  | Mock em `src/data/perfumes.ts`         | Mock        |
| Backend / DB      | Lovable Cloud (Postgres + Auth)        | **A ativar**|
| RPC servidor      | `createServerFn`                       | A criar     |
| Endpoints públicos| Server routes em `src/routes/api/public/*` | A criar  |
| Pagamentos        | InfinitePay (PIX + Cartão)             | A integrar  |

---

## 2. Modelagem de dados (Lovable Cloud / Postgres)

Equivalente moderno ao MySQL originalmente solicitado. Use migrations
Postgres (Lovable Cloud as gerencia). Todas as tabelas em `public` precisam
de `GRANT` e RLS.

### 2.1 Tabelas principais

```sql
-- Marcas
create table public.brands (
  id          uuid primary key default gen_random_uuid(),
  slug        text unique not null,
  nome        text not null,
  descricao   text,
  created_at  timestamptz default now()
);

-- Famílias olfativas (enum-like, mas como tabela para flexibilidade)
create table public.fragrance_families (
  slug  text primary key,         -- 'amadeirado', 'floral-branco', ...
  nome  text not null
);

-- Produtos (perfumes, skincare, makeup) — começamos por perfumes
create table public.products (
  id              uuid primary key default gen_random_uuid(),
  slug            text unique not null,
  brand_id        uuid references public.brands(id),
  categoria       text not null,           -- 'perfume' | 'skincare' | 'makeup'
  nome            text not null,
  descricao       text,
  preco_centavos  integer not null check (preco_centavos > 0),
  estoque         integer not null default 0,
  imagem_url      text,
  ativo           boolean not null default true,
  created_at      timestamptz default now(),
  updated_at      timestamptz default now()
);

-- Detalhes olfativos (1-to-1 com products quando categoria='perfume')
create table public.perfume_details (
  product_id        uuid primary key references public.products(id) on delete cascade,
  familia_slug      text references public.fragrance_families(slug),
  concentracao      text,                  -- 'EDP', 'EDT', 'Extrait'
  volume_ml         integer,
  notas_topo        text[] default '{}',
  notas_coracao     text[] default '{}',
  notas_base        text[] default '{}',
  fixacao           smallint check (fixacao between 0 and 10),
  projecao          smallint check (projecao between 0 and 10),
  duracao_horas     text,
  estacao_scores    jsonb,                 -- { primavera: 8, verao: 6, ... }
  periodo_scores    jsonb,                 -- { dia: 7, noite: 9 }
  ocasiao_scores    jsonb
);

-- Carrinho (persistido por usuário OU por session_token anônimo)
create table public.carts (
  id            uuid primary key default gen_random_uuid(),
  user_id       uuid references auth.users(id) on delete cascade,
  session_token text,                       -- para guests
  created_at    timestamptz default now(),
  updated_at    timestamptz default now()
);

create table public.cart_items (
  id                uuid primary key default gen_random_uuid(),
  cart_id           uuid not null references public.carts(id) on delete cascade,
  product_id        uuid not null references public.products(id),
  quantidade        integer not null check (quantidade > 0),
  preco_centavos    integer not null,       -- snapshot do preço
  unique (cart_id, product_id)
);

-- Pedidos
create type public.order_status as enum (
  'pending', 'awaiting_payment', 'paid', 'shipped', 'delivered',
  'cancelled', 'refunded', 'failed'
);

create table public.orders (
  id                  uuid primary key default gen_random_uuid(),
  user_id             uuid references auth.users(id),
  email               text not null,
  status              public.order_status not null default 'pending',
  total_centavos      integer not null,
  endereco_entrega    jsonb not null,
  metodo_pagamento    text not null,        -- 'pix' | 'credit_card'
  infinitepay_charge_id text,
  pix_qrcode          text,
  pix_copia_e_cola    text,
  created_at          timestamptz default now(),
  updated_at          timestamptz default now()
);

create table public.order_items (
  id              uuid primary key default gen_random_uuid(),
  order_id        uuid not null references public.orders(id) on delete cascade,
  product_id      uuid not null references public.products(id),
  nome            text not null,
  preco_centavos  integer not null,
  quantidade      integer not null
);

-- Log de webhooks (auditoria + idempotência)
create table public.payment_events (
  id              uuid primary key default gen_random_uuid(),
  order_id        uuid references public.orders(id),
  provider        text not null,            -- 'infinitepay'
  event_id        text unique,              -- id do evento do provider
  event_type      text not null,
  payload         jsonb not null,
  received_at     timestamptz default now()
);
```

### 2.2 GRANTs e RLS (obrigatório)

```sql
-- Catálogo: leitura pública
grant select on public.brands, public.fragrance_families,
  public.products, public.perfume_details to anon, authenticated;
grant all on public.brands, public.fragrance_families,
  public.products, public.perfume_details to service_role;

alter table public.products enable row level security;
create policy "produtos ativos visíveis" on public.products
  for select to anon, authenticated using (ativo = true);

-- Carrinho: só dono ou dono do session_token
grant select, insert, update, delete on public.carts, public.cart_items to authenticated;
grant all on public.carts, public.cart_items to service_role;

alter table public.carts enable row level security;
create policy "meu carrinho" on public.carts
  for all to authenticated using (user_id = auth.uid());

-- Orders: usuário só vê os próprios
grant select, insert on public.orders, public.order_items to authenticated;
grant all on public.orders, public.order_items to service_role;

alter table public.orders enable row level security;
create policy "meus pedidos" on public.orders
  for select to authenticated using (user_id = auth.uid());

-- payment_events: apenas service_role (servidor)
grant all on public.payment_events to service_role;
alter table public.payment_events enable row level security;
```

### 2.3 Papéis (admin)

Siga o padrão obrigatório do Lovable (tabela separada `user_roles` +
função `has_role` security definer). **Nunca** armazene `is_admin` em
`profiles`.

---

## 3. APIs internas (createServerFn)

Use `createServerFn` para tudo que é consumido pelo próprio frontend.

Convenções:
- Arquivos em `src/lib/<dominio>.functions.ts` (cliente-safe).
- Helpers com lógica privada em `src/lib/<dominio>.server.ts`.
- Validar **toda** entrada com Zod.
- Retornar DTOs serializáveis (sem instâncias de classe).

### 3.1 Catálogo

```ts
// src/lib/catalog.functions.ts
import { createServerFn } from "@tanstack/react-start";
import { z } from "zod";
import { listProducts, getProductBySlug } from "./catalog.server";

export const listProductsFn = createServerFn({ method: "GET" })
  .inputValidator(z.object({
    familia: z.string().optional(),
    estacao: z.enum(["primavera","verao","outono","inverno"]).optional(),
    page: z.number().int().min(1).default(1),
    pageSize: z.number().int().min(1).max(48).default(24),
  }))
  .handler(async ({ data }) => listProducts(data));

export const getProductFn = createServerFn({ method: "GET" })
  .inputValidator(z.object({ slug: z.string().min(1).max(120) }))
  .handler(async ({ data }) => getProductBySlug(data.slug));
```

### 3.2 Carrinho

| Função              | Método | Descrição                              |
| ------------------- | ------ | -------------------------------------- |
| `getCart`           | GET    | Retorna carrinho atual                 |
| `addCartItem`       | POST   | Adiciona item                          |
| `updateCartItem`    | POST   | Atualiza quantidade                    |
| `removeCartItem`    | POST   | Remove item                            |
| `clearCart`         | POST   | Esvazia                                |

### 3.3 Checkout

| Função                  | Método | Descrição                                                |
| ----------------------- | ------ | -------------------------------------------------------- |
| `createOrderFn`         | POST   | Cria order `pending` com snapshot do carrinho            |
| `createPixChargeFn`     | POST   | Chama InfinitePay PIX, retorna `qr_code` + `copia_cola`  |
| `createCardChargeFn`    | POST   | Chama InfinitePay Cartão (token gerado no browser)       |
| `getOrderStatusFn`      | GET    | Polling de status (PIX)                                  |

---

## 4. Integração InfinitePay

> InfinitePay (CloudWalk) expõe uma API REST para charges (cartão de
> crédito) e PIX. Confira sempre os endpoints e payloads atuais em
> <https://developers.infinitepay.io>. Este guia descreve o **shape** da
> integração; ajuste os paths/campos conforme a versão vigente da API.

### 4.1 Credenciais (Secrets)

Adicione via ferramenta de Secrets do Lovable (nunca no código):

| Secret                       | Onde usar                       |
| ---------------------------- | ------------------------------- |
| `INFINITEPAY_API_KEY`        | Authorization bearer do servidor|
| `INFINITEPAY_WEBHOOK_SECRET` | Validar assinatura de webhook   |
| `INFINITEPAY_HANDLE`         | Identificador da conta (se aplicável) |

Ambiente: use credenciais de **sandbox** durante desenvolvimento e troque
para produção apenas quando o fluxo end-to-end estiver validado.

### 4.2 Cliente HTTP do servidor

```ts
// src/lib/infinitepay.server.ts
const BASE_URL = process.env.INFINITEPAY_BASE_URL
  ?? "https://api.infinitepay.io"; // confira o host correto na doc oficial

export async function infinitePayFetch<T>(
  path: string,
  init: RequestInit = {},
): Promise<T> {
  const apiKey = process.env.INFINITEPAY_API_KEY;
  if (!apiKey) throw new Error("INFINITEPAY_API_KEY ausente");

  const res = await fetch(`${BASE_URL}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      "Accept": "application/json",
      "Authorization": `Bearer ${apiKey}`,
      ...(init.headers ?? {}),
    },
  });

  if (!res.ok) {
    const body = await res.text();
    throw new Error(`InfinitePay ${res.status}: ${body}`);
  }
  return res.json() as Promise<T>;
}
```

### 4.3 Cobrança PIX

Fluxo:
1. Frontend chama `createPixChargeFn(orderId)`.
2. Servidor cria a charge no InfinitePay com `payment_method: "pix"`.
3. Salva `pix_qrcode`, `pix_copia_e_cola`, `infinitepay_charge_id` na
   order e marca status `awaiting_payment`.
4. Frontend exibe QR Code + botão "copiar código".
5. Confirmação chega via **webhook** (preferido) ou polling.

```ts
// src/lib/payments.functions.ts
import { createServerFn } from "@tanstack/react-start";
import { z } from "zod";
import { infinitePayFetch } from "./infinitepay.server";
import { supabaseAdmin } from "@/integrations/supabase/client.server";

export const createPixChargeFn = createServerFn({ method: "POST" })
  .inputValidator(z.object({ orderId: z.string().uuid() }))
  .handler(async ({ data }) => {
    const { data: order } = await supabaseAdmin
      .from("orders").select("*").eq("id", data.orderId).single();
    if (!order) throw new Error("Pedido não encontrado");

    const charge = await infinitePayFetch<{
      id: string;
      pix: { qr_code: string; qr_code_base64: string; copy_paste: string };
    }>("/v1/charges", {
      method: "POST",
      body: JSON.stringify({
        amount: order.total_centavos,
        currency: "BRL",
        payment_method: "pix",
        customer: { email: order.email },
        metadata: { order_id: order.id },
      }),
    });

    await supabaseAdmin.from("orders").update({
      status: "awaiting_payment",
      infinitepay_charge_id: charge.id,
      pix_qrcode: charge.pix.qr_code_base64,
      pix_copia_e_cola: charge.pix.copy_paste,
    }).eq("id", order.id);

    return {
      qrCodeBase64: charge.pix.qr_code_base64,
      copiaECola: charge.pix.copy_paste,
    };
  });
```

### 4.4 Cobrança Cartão de Crédito

**Nunca** envie PAN/CVV pelo seu servidor. Use a SDK JS oficial da
InfinitePay no browser para gerar um **token** do cartão e envie só o
token para o seu servidor.

Fluxo:
1. Frontend tokeniza cartão via SDK do InfinitePay.
2. `createCardChargeFn({ orderId, cardToken, parcelas, holderName })`.
3. Servidor cria charge `payment_method: "credit_card"`.
4. Atualiza order para `paid` (ou `failed`).

```ts
export const createCardChargeFn = createServerFn({ method: "POST" })
  .inputValidator(z.object({
    orderId:    z.string().uuid(),
    cardToken:  z.string().min(10).max(500),
    parcelas:   z.number().int().min(1).max(12),
    holderName: z.string().min(2).max(120),
  }))
  .handler(async ({ data }) => {
    const { data: order } = await supabaseAdmin
      .from("orders").select("*").eq("id", data.orderId).single();
    if (!order) throw new Error("Pedido não encontrado");

    const charge = await infinitePayFetch<{ id: string; status: string }>(
      "/v1/charges",
      {
        method: "POST",
        body: JSON.stringify({
          amount: order.total_centavos,
          currency: "BRL",
          payment_method: "credit_card",
          card: { token: data.cardToken, holder_name: data.holderName },
          installments: data.parcelas,
          customer: { email: order.email },
          metadata: { order_id: order.id },
        }),
      },
    );

    const novoStatus = charge.status === "paid" ? "paid" : "failed";
    await supabaseAdmin.from("orders").update({
      status: novoStatus,
      infinitepay_charge_id: charge.id,
    }).eq("id", order.id);

    return { status: novoStatus, chargeId: charge.id };
  });
```

### 4.5 Webhook (confirmação assíncrona)

Coloque em `/api/public/*` para bypassar auth do site publicado, mas
**sempre** valide assinatura.

```ts
// src/routes/api/public/infinitepay-webhook.ts
import { createFileRoute } from "@tanstack/react-router";
import { createHmac, timingSafeEqual } from "node:crypto";
import { supabaseAdmin } from "@/integrations/supabase/client.server";

export const Route = createFileRoute("/api/public/infinitepay-webhook")({
  server: {
    handlers: {
      POST: async ({ request }) => {
        const signature = request.headers.get("x-infinitepay-signature") ?? "";
        const body = await request.text();

        const secret = process.env.INFINITEPAY_WEBHOOK_SECRET!;
        const expected = createHmac("sha256", secret).update(body).digest("hex");

        const a = Buffer.from(signature);
        const b = Buffer.from(expected);
        if (a.length !== b.length || !timingSafeEqual(a, b)) {
          return new Response("invalid signature", { status: 401 });
        }

        const event = JSON.parse(body) as {
          id: string;
          type: string;        // 'charge.paid' | 'charge.failed' | ...
          data: { id: string; status: string; metadata?: { order_id?: string } };
        };

        // Idempotência
        const { error: dupErr } = await supabaseAdmin
          .from("payment_events")
          .insert({
            provider: "infinitepay",
            event_id: event.id,
            event_type: event.type,
            payload: event,
            order_id: event.data.metadata?.order_id ?? null,
          });
        if (dupErr && dupErr.code === "23505") {
          return new Response("ok"); // já processado
        }

        const orderId = event.data.metadata?.order_id;
        if (!orderId) return new Response("no order metadata", { status: 200 });

        const statusMap: Record<string, string> = {
          "charge.paid": "paid",
          "charge.failed": "failed",
          "charge.refunded": "refunded",
          "charge.cancelled": "cancelled",
        };
        const newStatus = statusMap[event.type];
        if (newStatus) {
          await supabaseAdmin
            .from("orders")
            .update({ status: newStatus, updated_at: new Date().toISOString() })
            .eq("id", orderId);
        }

        return new Response("ok");
      },
    },
  },
});
```

URL estável para configurar no painel do InfinitePay:
`https://project--<project-id>.lovable.app/api/public/infinitepay-webhook`

### 4.6 Estados do pedido

```
pending ──► awaiting_payment ──► paid ──► shipped ──► delivered
                  │                  │
                  ▼                  ▼
                failed            refunded
                  │
                  ▼
              cancelled
```

---

## 5. Segurança — checklist obrigatório

- [ ] `INFINITEPAY_API_KEY` e `INFINITEPAY_WEBHOOK_SECRET` apenas em Secrets.
- [ ] Validar **assinatura HMAC** de todo webhook com `timingSafeEqual`.
- [ ] Verificar **idempotência** via `payment_events.event_id` UNIQUE.
- [ ] Nunca confiar em status enviado pelo cliente — fonte da verdade é
      a resposta direta do InfinitePay + webhook.
- [ ] Recalcular `total_centavos` no servidor a partir dos produtos
      (nunca aceitar total enviado pelo frontend).
- [ ] RLS habilitada em **todas** as tabelas com dados de usuário.
- [ ] Tokenização de cartão **apenas no browser** via SDK oficial.
- [ ] Logs sem dados sensíveis (sem PAN, sem CVV, sem token bruto).
- [ ] Rate limit no endpoint de criação de pedido.

---

## 6. Testes manuais sugeridos

1. **PIX feliz**: criar pedido → gerar QR → simular pagamento no
   sandbox → confirmar que webhook altera status para `paid`.
2. **PIX expirado**: não pagar → status permanece `awaiting_payment` →
   verificar job de limpeza (futuro).
3. **Cartão aprovado**: token válido → `paid`.
4. **Cartão recusado**: token de teste recusado → `failed` e mensagem
   amigável no UI.
5. **Webhook replay**: reenviar mesmo evento → segunda chamada não duplica
   atualização (idempotência via `event_id`).
6. **Assinatura inválida**: enviar webhook com `x-infinitepay-signature`
   adulterada → 401.

---

## 7. Roadmap por fases

| Fase | Entregáveis                                                              |
| ---- | ------------------------------------------------------------------------ |
| 1    | (Concluída) Home + Catálogo + PDP com mocks                              |
| 2    | Ativar Lovable Cloud, criar schema, migrar mocks para DB, auth de cliente|
| 3    | Carrinho persistente + Checkout (endereço, frete fixo, resumo)           |
| 4    | Integração InfinitePay PIX (sandbox)                                     |
| 5    | Integração InfinitePay Cartão + parcelamento                             |
| 6    | Webhook + painel "Meus Pedidos"                                          |
| 7    | Área administrativa (CRUD produtos, pedidos, estoque)                    |
| 8    | Quiz olfativo, blog, recomendações personalizadas                        |

---

## 8. Referências

- TanStack Start — server functions: <https://tanstack.com/start>
- InfinitePay Developers: <https://developers.infinitepay.io>
- Lovable Cloud: <https://docs.lovable.dev/features/cloud>
- Padrão de papéis (user_roles) — ver instruções internas do projeto.

> Dúvidas ou ajustes: edite este arquivo em `docs/INTEGRACAO-APIS-INFINITEPAY.md`.
