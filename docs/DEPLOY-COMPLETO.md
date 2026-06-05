# Maryar — Guia Completo de Deploy (Frontend + Backend)

> Documento escrito passo a passo, em linguagem simples. Siga na ordem.

Última atualização: junho/2026

---

## 1. Visão geral do projeto

O sistema é dividido em **3 partes independentes**:

| Parte         | Tecnologia                                     | Onde fica hospedado                             |
| ------------- | ---------------------------------------------- | ----------------------------------------------- |
| **Frontend**  | React + Vite (SPA pura, só HTML/CSS/JS)        | Hospedagem compartilhada Windows da Locaweb     |
| **Backend**   | ASP.NET 4.5.2 + Web API 2 + C# + ADO.NET       | Hospedagem Windows com IIS (Locaweb ou VPS)     |
| **Banco**     | MySQL 8.x                                      | MySQL da Locaweb (ou outro provedor)            |

O frontend é um site **estático**: só arquivos `.html`, `.css`, `.js`.
Ele chama a API .NET por HTTPS para mostrar produtos, carrinho, login, etc.
A API .NET conversa com o MySQL e com a InfinitePay (PIX/cartão).

```
[Navegador do cliente]
        │
        │  (1) baixa o site estático
        ▼
[Locaweb Windows: httpdocs/]   ← frontend (dist/)
        │
        │  (2) chama https://api.maryar.com.br/api/...
        ▼
[IIS: Maryar.Api]              ← backend ASP.NET
        │
        ├── MySQL  (banco de dados)
        └── InfinitePay (pagamentos)
```

---

## 2. O que você precisa antes de começar

- [ ] Conta na Locaweb com **Hospedagem Windows** ativa
- [ ] Banco **MySQL** criado no painel da Locaweb (anote host, nome do banco, usuário e senha)
- [ ] Domínio apontado (`maryar.com.br`) e subdomínio para a API (`api.maryar.com.br`)
- [ ] Certificado SSL ativo nos dois domínios (a Locaweb fornece Let's Encrypt grátis)
- [ ] **Visual Studio 2019 ou 2022** (Community serve) instalado na sua máquina, para compilar o backend
- [ ] Um cliente FTP (FileZilla, WinSCP) para enviar arquivos
- [ ] As credenciais da **InfinitePay** (pode preencher depois)

---

## 3. Preparar o frontend (no seu computador)

### 3.1. Instalar dependências (só uma vez)

Abra o terminal na pasta do projeto e rode:

```bash
bun install
```

Se não tiver o `bun`, instale primeiro: <https://bun.sh>
Pode também usar `npm install`, mas o projeto usa `bun.lock`.

### 3.2. Configurar o endereço do backend

Crie um arquivo chamado `.env` na raiz do projeto (mesma pasta do `package.json`) com este conteúdo:

```
VITE_API_BASE_URL=https://api.maryar.com.br
```

> Se ainda não tiver o subdomínio da API funcionando, use o IP/URL temporário
> que a Locaweb fornece. Você pode alterar e gerar o build de novo depois.

### 3.3. Gerar o build de produção

```bash
bun run build
```

Isso cria uma pasta chamada **`dist/`** com tudo que precisa ir pro servidor:

```
dist/
├── index.html
├── web.config          ← regras de URL pra IIS (já vem pronto)
└── assets/
    ├── index-[hash].js
    ├── index-[hash].css
    └── ... (imagens, fontes)
```

> Esse `web.config` é **diferente** do `web.config` do backend. Não misture os dois.

### 3.4. Enviar para a Locaweb (via FTP)

1. Abra o FileZilla e conecte com os dados FTP da hospedagem Windows.
2. Entre na pasta pública. Na Locaweb costuma se chamar **`httpdocs/`** (pode ser `www/` ou `web/` dependendo do plano — verifique no painel).
3. **Apague** todos os arquivos antigos que estiverem lá (faça um backup antes!).
4. Arraste **todo o conteúdo de dentro da pasta `dist/`** para o `httpdocs/`.
   > Importante: arraste o **conteúdo** da pasta, não a pasta `dist` inteira.
   > No final, o `index.html` precisa estar em `httpdocs/index.html`.

Pronto. Abra `https://maryar.com.br` no navegador — o site deve carregar.

> Se ao atualizar uma página interna (ex.: `/catalogo`) der erro 404, é porque
> o `web.config` não foi enviado. Reenvie ele e dê um refresh.

---

## 4. Preparar o banco MySQL

### 4.1. Criar o banco no painel da Locaweb

1. Painel Locaweb → **Bancos de Dados → MySQL → Criar**.
2. Anote:
   - **Host** (ex.: `mysql.maryar.com.br` ou um IP)
   - **Nome do banco** (ex.: `maryar`)
   - **Usuário**
   - **Senha**

### 4.2. Rodar os scripts SQL

Use o **MySQL Workbench** (grátis) ou o **phpMyAdmin** do painel da Locaweb.

1. Conecte com os dados acima.
2. Selecione o banco `maryar`.
3. Abra o arquivo `backend/database/schema.sql` (está no projeto) e execute tudo.
   Isso cria as tabelas (`products`, `orders`, `users`, etc.).
4. Em seguida, execute `backend/database/seeds.sql`. Isso cadastra marcas, famílias e produtos de exemplo.

Verifique se as tabelas apareceram. Se sim, banco pronto.

---

## 5. Compilar o backend ASP.NET

### 5.1. Abrir a solution no Visual Studio

1. Abra o **Visual Studio**.
2. **Arquivo → Abrir → Projeto/Solução** e selecione `backend/Maryar.Api.sln`.
3. Clique com o botão direito na solution → **Restore NuGet Packages**.
   Isso baixa as bibliotecas (`MySql.Data`, `Newtonsoft.Json`, `JWT`, etc.).

### 5.2. Preencher o `Web.config`

Abra `backend/Maryar.Api/Web.config` no Visual Studio e ajuste:

```xml
<connectionStrings>
  <add name="MaryarDb"
       connectionString="server=mysql.maryar.com.br;port=3306;database=maryar;uid=SEU_USUARIO;password=SUA_SENHA;SslMode=Required;CharSet=utf8mb4;AllowPublicKeyRetrieval=true;"
       providerName="MySql.Data.MySqlClient" />
</connectionStrings>

<appSettings>
  <add key="Maryar:JwtSecret"   value="COLE_AQUI_UMA_STRING_ALEATORIA_LONGA_DE_NO_MINIMO_32_CARACTERES" />
  <add key="Maryar:CorsOrigins" value="https://maryar.com.br,https://www.maryar.com.br" />

  <add key="InfinitePay:ApiKey"         value="PREENCHER_QUANDO_RECEBER" />
  <add key="InfinitePay:WebhookSecret"  value="PREENCHER_QUANDO_RECEBER" />
  <add key="InfinitePay:MerchantHandle" value="PREENCHER_QUANDO_RECEBER" />
</appSettings>
```

> Para gerar uma `JwtSecret` aleatória, use: <https://generate-secret.vercel.app/64>

### 5.3. Publicar o build

1. No Visual Studio, clique com o botão direito no projeto `Maryar.Api` → **Publish (Publicar)**.
2. Escolha o destino: **Folder (Pasta)**.
3. Aponte para uma pasta local, por ex.: `C:\publish\Maryar.Api\`.
4. Configuration = **Release**.
5. Clique em **Publish**.

A pasta resultante terá esta cara:

```
C:\publish\Maryar.Api\
├── bin\               ← DLLs (Maryar.Api.dll, MySql.Data.dll, etc.)
├── Global.asax
├── Web.config         ← já com seus dados preenchidos
└── (outros arquivos)
```

---

## 6. Subir o backend para o IIS da Locaweb

> A Locaweb Windows compartilhada permite hospedar aplicações ASP.NET.
> Se o seu plano não permitir, precisará de um **VPS Windows**.

### 6.1. Criar o site/aplicativo no painel

1. Painel Locaweb → **Sites → Adicionar aplicação ASP.NET** (ou similar).
2. Aponte o subdomínio `api.maryar.com.br` para a nova aplicação.
3. Versão do .NET: **4.5** (ou superior — qualquer 4.x serve).
4. Modo do Application Pool: **Integrated** (padrão).

### 6.2. Enviar os arquivos via FTP

1. Conecte no FTP da hospedagem da API.
2. Vá para a pasta raiz da aplicação (geralmente `httpdocs/` do subdomínio).
3. Apague o que estiver lá.
4. Arraste **todo o conteúdo** de `C:\publish\Maryar.Api\` para essa pasta.
   No final, o servidor deve ter:
   ```
   httpdocs/
   ├── bin/
   ├── Global.asax
   └── Web.config
   ```

### 6.3. Permitir o IP do servidor no MySQL

No painel do MySQL da Locaweb, libere o IP da hospedagem Windows para
acessar o banco (ou marque "qualquer IP" se for o caso). Senão a API vai
dar erro de conexão.

### 6.4. Testar

Abra no navegador:

- `https://api.maryar.com.br/api/brands` → deve retornar uma lista JSON de marcas.
- `https://api.maryar.com.br/api/products` → deve retornar produtos.

Se der erro 500, acesse os logs no painel da Locaweb ou ative o
`<customErrors mode="Off" />` no `Web.config` para ver o stack trace.

---

## 7. Configurar a InfinitePay (quando tiver as credenciais)

1. Entre no painel da InfinitePay.
2. Pegue a **API Key** e o **Webhook Secret**.
3. Coloque os valores no `Web.config` (seção `appSettings`).
4. Cadastre o webhook apontando para:
   ```
   https://api.maryar.com.br/api/public/webhooks/infinitepay
   ```
5. Reenvie o `Web.config` atualizado por FTP (não precisa recompilar — o IIS recarrega sozinho).

---

## 8. Atualizando o site no futuro

### Mudou só o frontend (cores, textos, páginas):
1. `bun run build`
2. FTP → apague o conteúdo de `httpdocs/` do site principal e suba o novo `dist/`.

> Dica: na verdade você não precisa apagar tudo. Pode sobrescrever.
> Mas se tiver dúvida, apagar e subir de novo é mais seguro.

### Mudou o backend (código C#):
1. Visual Studio → **Publish** novamente para a pasta local.
2. FTP → sobrescreva os arquivos da pasta `httpdocs/` do subdomínio da API.
3. O IIS detecta a mudança e reinicia a aplicação automaticamente.

### Mudou o banco (novas tabelas, colunas):
1. Crie um arquivo `migration-XXXX.sql` em `backend/database/`.
2. Rode no MySQL Workbench / phpMyAdmin.

---

## 9. Estrutura do projeto (referência rápida)

```
maryar/
├── src/                       ← código React (frontend)
│   ├── pages/                 ← Home, Catalog, Perfume, NotFound
│   ├── components/            ← componentes reutilizáveis
│   ├── data/                  ← dados mock (serão substituídos pela API)
│   └── main.tsx               ← entry point
├── public/web.config          ← regras IIS pro frontend (copiado pro dist)
├── index.html                 ← entry HTML
├── vite.config.ts             ← config do build
├── package.json
│
├── backend/
│   ├── Maryar.Api.sln
│   ├── Maryar.Api/            ← projeto ASP.NET Web API
│   │   ├── Controllers/       ← endpoints REST
│   │   ├── Repositories/      ← acesso a dados (ADO.NET puro)
│   │   ├── Services/          ← JWT, InfinitePay, hash de senha
│   │   ├── Models/  Dtos/
│   │   ├── Infrastructure/
│   │   ├── Global.asax(.cs)
│   │   └── Web.config         ← preencha antes de publicar!
│   ├── database/
│   │   ├── schema.sql         ← rode 1º
│   │   └── seeds.sql          ← rode 2º
│   └── legacy/                ← scripts ASP Clássico de apoio
│
└── docs/
    ├── DEPLOY-COMPLETO.md            ← este arquivo
    └── INTEGRACAO-APIS-INFINITEPAY.md
```

---

## 10. Endpoints da API (resumo)

Base: `https://api.maryar.com.br/api`

| Método | Rota                              | O que faz                       |
| ------ | --------------------------------- | ------------------------------- |
| GET    | `/products?familia=&marca=&...`   | Lista catálogo com filtros      |
| GET    | `/products/{slug}`                | Detalhe de um perfume           |
| GET    | `/brands`                         | Marcas                          |
| GET    | `/families`                       | Famílias olfativas              |
| GET    | `/cart`                           | Carrinho atual                  |
| POST   | `/cart/items`                     | Adiciona item                   |
| PUT    | `/cart/items/{id}`                | Atualiza quantidade             |
| DELETE | `/cart/items/{id}`                | Remove item                     |
| POST   | `/auth/signup`                    | Cadastrar usuário               |
| POST   | `/auth/signin`                    | Login (retorna JWT)             |
| POST   | `/checkout`                       | Cria pedido + cobrança          |
| GET    | `/payments/status/{orderId}`      | Checar status de pagamento      |
| POST   | `/public/webhooks/infinitepay`    | Webhook da InfinitePay          |

---

## 11. Problemas comuns

| Sintoma                                              | Causa provável                          | Solução                                                      |
| ---------------------------------------------------- | --------------------------------------- | ------------------------------------------------------------ |
| Site abre, mas refresh em `/catalogo` dá 404         | `web.config` não foi enviado pro IIS    | Reenvie `dist/web.config` para `httpdocs/`                   |
| Frontend abre mas dados não carregam                 | URL da API errada no `.env`             | Conferir `VITE_API_BASE_URL` e gerar build de novo           |
| Erro CORS no navegador                               | Domínio não está em `Maryar:CorsOrigins`| Adicione no `Web.config` do backend e salve no servidor      |
| API retorna erro 500 ao chamar `/api/brands`         | Conexão MySQL falhando                  | Verificar string de conexão + liberar IP no painel MySQL     |
| Pagamentos PIX não geram QR Code                     | `InfinitePay:ApiKey` vazia ou inválida  | Conferir credenciais no `Web.config`                         |
| Webhook da InfinitePay sempre 401                    | `WebhookSecret` errado                  | Conferir secret no painel e no `Web.config`                  |

---

## 12. Checklist final antes de ir ao ar

- [ ] `bun run build` rodou sem erros
- [ ] `dist/` enviado para `httpdocs/` do site principal
- [ ] `index.html`, `web.config` e pasta `assets/` aparecem na raiz pública
- [ ] Banco MySQL criado, `schema.sql` e `seeds.sql` executados
- [ ] `Web.config` do backend com connection string e JwtSecret reais
- [ ] Backend publicado e enviado para `httpdocs/` do subdomínio `api.`
- [ ] IP do servidor liberado no MySQL
- [ ] HTTPS funcionando nos dois domínios
- [ ] `GET https://api.maryar.com.br/api/brands` retorna JSON
- [ ] Site abre, mostra catálogo e responde nos links
- [ ] (Opcional) InfinitePay configurada e webhook cadastrado

Quando tudo isso estiver ✅, está no ar!
