# Maryar — Backend (ASP.NET 4.5.2 + Web API 2 + ADO.NET + MySQL)

Backend pronto para a loja **maryar.com.br**, com endpoints REST para
catálogo, carrinho, autenticação JWT, checkout e webhook InfinitePay,
seguindo **Repository Pattern + ADO.NET puro (MySqlCommand)**.
Coexiste com **ASP Clássico** em `backend/legacy/` para tarefas internas.

---

## Stack

| Camada            | Tecnologia                                            |
|-------------------|-------------------------------------------------------|
| Runtime           | .NET Framework 4.5.2                                  |
| API               | ASP.NET Web API 2 (5.2.9) hospedado em IIS            |
| Acesso a dados    | ADO.NET puro + `MySql.Data` (Connector/NET)           |
| Padrão            | Repository Pattern (interfaces + implementações MySQL)|
| Auth              | JWT HS256 (`System.IdentityModel.Tokens.Jwt`)         |
| Senhas            | PBKDF2 (Rfc2898) 100k iterações                       |
| Pagamento         | InfinitePay (PIX + cartão tokenizado) + Webhook HMAC  |
| Legado            | ASP Clássico (VBScript + ADODB)                       |
| Banco             | MySQL 8.x (utf8mb4)                                   |

---

## Estrutura

```
backend/
├─ Maryar.Api.sln
├─ Maryar.Api/
│  ├─ App_Start/WebApiConfig.cs
│  ├─ Controllers/         ← ProductsController, CartController, CheckoutController, ...
│  ├─ Repositories/
│  │   ├─ Interfaces/      ← IProductRepository, ICartRepository, ...
│  │   └─ MySql/           ← Implementações com MySqlCommand puro
│  ├─ Services/            ← JwtService, PasswordHasher, HmacValidator,
│  │                          PricingService, InfinitePayClient
│  ├─ Models/ Dtos/ Infrastructure/
│  ├─ Global.asax(.cs)
│  ├─ Web.config
│  └─ packages.config
├─ database/
│  ├─ schema.sql           ← rode primeiro
│  └─ seeds.sql            ← marcas/famílias/produto exemplo
└─ legacy/
   ├─ health.asp
   └─ pix-status.asp
```

---

## Endpoints

Base local: `https://localhost:44300/api`

| Método | Rota                                  | Descrição                       |
|--------|---------------------------------------|---------------------------------|
| GET    | `/products?familia=&marca=&nota=...`  | Catálogo paginado + filtros     |
| GET    | `/products/{slug}`                    | PDP completo (pirâmide + scores)|
| GET    | `/brands`                             | Marcas                          |
| GET    | `/families`                           | Famílias olfativas              |
| GET    | `/cart`                               | Carrinho (cookie ou JWT)        |
| POST   | `/cart/items`                         | Adiciona item                   |
| PUT    | `/cart/items/{itemId}`                | Atualiza quantidade (0 = remove)|
| DELETE | `/cart/items/{itemId}`                | Remove item                     |
| DELETE | `/cart`                               | Esvazia                         |
| POST   | `/auth/signup`                        | Cadastro → JWT                  |
| POST   | `/auth/signin`                        | Login → JWT                     |
| POST   | `/checkout`                           | Cria pedido + cobrança          |
| GET    | `/checkout/orders/{id}`               | Detalhe do pedido               |
| GET    | `/payments/status/{orderId}`          | Polling de status               |
| POST   | `/public/webhooks/infinitepay`        | Webhook InfinitePay (HMAC)      |

---

## Passo a passo de deploy

### 1. Banco
```bash
mysql -u root -p < backend/database/schema.sql
mysql -u root -p < backend/database/seeds.sql
# Crie o usuário de aplicação:
mysql -u root -p -e "CREATE USER 'maryar_app'@'%' IDENTIFIED BY 'TROCAR'; \
  GRANT SELECT, INSERT, UPDATE, DELETE ON maryar.* TO 'maryar_app'@'%'; FLUSH PRIVILEGES;"
```

### 2. Build (Visual Studio 2019/2022 ou MSBuild)
```bash
# Restore NuGet:
nuget restore backend/Maryar.Api.sln
# Build:
msbuild backend/Maryar.Api.sln /p:Configuration=Release
```

### 3. `Web.config` — preencher antes do publish

- `connectionStrings/MaryarDb` → seu MySQL (server, db, user, password)
- `Maryar:JwtSecret` → string aleatória ≥ 32 chars
- `Maryar:CorsOrigins` → `https://maryar.com.br,https://www.maryar.com.br`
- `InfinitePay:ApiKey` → quando receber da InfinitePay
- `InfinitePay:WebhookSecret` → segredo configurado no painel da InfinitePay
- `InfinitePay:MerchantHandle` → handle/conta da InfinitePay

> **Dica de segurança**: para produção, use o `aspnet_regiis -pe` para
> criptografar `<connectionStrings>` e `<appSettings>` no servidor.

### 4. IIS

1. Crie um **Application Pool**: .NET CLR v4.0, Managed Pipeline = **Integrated**, Identity = `ApplicationPoolIdentity`.
2. Adicione **Web Site** apontando para a pasta publicada (com `bin/`, `Global.asax`, `Web.config`).
3. Binding HTTPS com certificado (TLS 1.2+ obrigatório para InfinitePay).
4. Configure o webhook na InfinitePay para:
   `https://api.maryar.com.br/api/public/webhooks/infinitepay`

### 5. Conferir
- `GET https://api.maryar.com.br/api/brands` → lista marcas
- `GET https://api.maryar.com.br/api/products` → lista catálogo
- `POST .../api/auth/signup` → cria usuário + retorna JWT

---

## Repository Pattern — visão rápida

```csharp
// Interface (Repositories/Interfaces/IProductRepository.cs)
public interface IProductRepository {
    IEnumerable<Product> Query(ProductQueryDto q, out int total);
    Product GetBySlug(string slug);
    PerfumeDetails GetDetailsByProductId(Guid productId);
}

// Implementação (Repositories/MySql/ProductRepository.cs)
public class ProductRepository : IProductRepository {
    private readonly IConnectionFactory _factory;
    public ProductRepository() : this(new MySqlConnectionFactory()) { }
    public ProductRepository(IConnectionFactory factory) { _factory = factory; }
    // ... MySqlCommand + parâmetros nomeados (@x) ...
}

// Uso no Controller (Controllers/ProductsController.cs)
public ProductsController() : this(new ProductRepository()) { }
public ProductsController(IProductRepository repo) { _repo = repo; }
```

Os controllers têm **dois construtores** (default + injeção). Isso permite
substituir o repositório por mocks em testes sem amarrar a um container DI.
Para introduzir Unity/Autofac/SimpleInjector depois, basta registrar
`IProductRepository → ProductRepository` e remover o construtor default.

---

## Segurança aplicada

- **TLS 1.2** forçado em `Global.asax.cs` para chamadas de saída.
- **Parâmetros nomeados** em 100% das queries (zero concatenação).
- **PBKDF2 100k** para senhas + verificação em **tempo constante**.
- **JWT HS256** com `iss/aud/exp` validados em `JwtAuthAttribute`.
- **HMAC-SHA256** no webhook + comparação em tempo constante.
- **Idempotência** do webhook via `payment_events.event_id UNIQUE`.
- **Recálculo de totais no servidor** (`PricingService`), nunca confiando no cliente.
- **Tokenização do cartão** apenas no navegador (SDK InfinitePay). O backend
  recebe somente o `card_token`, jamais o PAN.
- **Headers**: `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`.
- **Cookies** do carrinho marcados `HttpOnly` + `Secure` (em HTTPS).

---

## InfinitePay — pontos de atenção

O `InfinitePayClient` foi escrito contra um contrato genérico
(`/v1/charges` com `payment_method = pix | credit_card`). **Confira contra
a documentação oficial** ao receber suas credenciais e ajuste:

- URL exata do endpoint de cobrança;
- Nome do header de assinatura do webhook (assumido: `X-Infinitepay-Signature`);
- Campos exatos do payload de PIX (`pix.qr_code`, `pix.copy_paste` — o cliente
  usa `JToken.SelectToken` com fallbacks para reduzir retrabalho);
- Forma da assinatura HMAC (hex puro ou prefixado `sha256=`).

Quando souber os valores, preencha em `Web.config`:

```xml
<add key="InfinitePay:ApiKey"          value="..." />
<add key="InfinitePay:WebhookSecret"   value="..." />
<add key="InfinitePay:MerchantHandle"  value="..." />
```

---

## Integração com o frontend (TanStack Start)

No frontend, crie `src/lib/api.ts`:

```ts
const BASE = import.meta.env.VITE_API_BASE_URL; // ex.: https://api.maryar.com.br

export async function api<T>(path: string, init: RequestInit = {}): Promise<T> {
  const token = localStorage.getItem("maryar_token");
  const res = await fetch(`${BASE}${path}`, {
    credentials: "include", // envia cookie do carrinho
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(init.headers || {}),
    },
    ...init,
  });
  if (!res.ok) throw new Error(`API ${res.status}`);
  return res.json();
}
```

E configure `VITE_API_BASE_URL` em produção apontando para o IIS.

---

## O que falta (quando você quiser)

- Painel admin (CRUD produtos/marcas) — pode ser feito como SPA usando os mesmos endpoints + role `admin`.
- Cálculo real de frete (Correios/Melhor Envio).
- Cupons e descontos.
- Quiz olfativo e recomendações por similaridade.

Tudo já cabe no esqueleto atual: novo repositório → novo controller → consumir do front.
