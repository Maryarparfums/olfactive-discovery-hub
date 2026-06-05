# Maryar — Frontend SPA (React + Vite)

Aplicação 100% client-side. Sem SSR, sem TanStack Start, sem Nitro.
O backend é separado em **ASP.NET 4.5.2 Web API** (pasta `backend/`).

## Stack

- React 19 + TypeScript
- Vite 7 (bundler)
- React Router DOM 7 (BrowserRouter)
- TanStack Query (cache de dados)
- Tailwind CSS 4 + shadcn/ui
- Zod (validação)

## Scripts

```bash
bun install      # instala dependências
bun run dev      # dev server (porta 8080)
bun run build    # gera ./dist pronto para produção
bun run preview  # serve o build localmente para teste
```

## Estrutura de rotas

| URL                    | Componente              |
| ---------------------- | ----------------------- |
| `/`                    | `src/pages/Home.tsx`    |
| `/catalogo`            | `src/pages/Catalog.tsx` |
| `/perfume/:slug`       | `src/pages/Perfume.tsx` |
| `*` (404)              | `src/pages/NotFound.tsx`|

Filtros do catálogo são propagados via query string
(`?familia=...&estacao=...&periodo=...&ocasiao=...&nota=...`).

## Deploy na Locaweb (Hospedagem Windows compartilhada)

1. Rodar `bun run build` localmente. A saída fica em `./dist`:
   ```
   dist/
   ├── index.html
   ├── web.config        ← copiado de public/web.config
   └── assets/
       ├── index-[hash].js
       ├── index-[hash].css
       └── ...
   ```
2. Enviar **todo** o conteúdo de `dist/` para a raiz pública do site
   (geralmente `httpdocs/` ou `www/`) via FTP.
3. O `web.config` já está configurado para:
   - reescrever toda rota não-arquivo para `/index.html`
     (necessário para o React Router funcionar em refresh / deep link);
   - servir os MIME types modernos (`.js`, `.mjs`, `.webp`, `.woff2`);
   - aplicar cache de 30 dias em assets estáticos;
   - habilitar compressão estática e dinâmica.
4. Não é necessário Node.js no servidor — é tudo HTML/CSS/JS estático.

## Integração com a API .NET

Configure o endpoint base do backend em `.env`:

```
VITE_API_BASE_URL=https://api.seudominio.com.br
```

E consuma via `fetch` ou TanStack Query nas páginas:

```ts
const API = import.meta.env.VITE_API_BASE_URL;
fetch(`${API}/api/products`).then(r => r.json());
```

O backend ASP.NET 4.5.2 fica em `backend/Maryar.Api/` (ver
`backend/README.md` para deploy no IIS).
