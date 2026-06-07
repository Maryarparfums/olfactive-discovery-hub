# Compilar o backend ASP.NET no GitHub Actions

Guia passo a passo, para quem nunca usou GitHub Actions. No fim você terá um
arquivo `maryar-api-deploy.zip` pronto para enviar por FTP para a Locaweb,
**sem precisar instalar Visual Studio nem nada no seu computador**.

---

## 1. Visão geral (o que vai acontecer)

```
Você edita código no Lovable
        ↓
Lovable sincroniza com o GitHub
        ↓
GitHub Actions liga uma máquina Windows
        ↓
Restaura NuGet  → compila com MSBuild  → publica em ./publish
        ↓
Gera maryar-api-deploy.zip (com bin/, Web.config, Global.asax, ...)
        ↓
Você baixa o ZIP, descompacta e sobe por FTP em api.maryar.com.br
```

O arquivo `.github/workflows/build-backend.yml` já está criado no repositório.
Ele faz tudo isso sozinho.

---

## 2. Conectar o projeto Lovable ao GitHub (faz uma única vez)

1. No editor do Lovable, clique no botão **+** (canto inferior esquerdo do chat).
2. Escolha **GitHub → Connect project**.
3. Autorize o **Lovable GitHub App** na sua conta GitHub.
4. Selecione a conta/organização onde o repositório será criado.
5. Clique em **Create Repository**. O Lovable empurra todo o código (inclusive
   `backend/` e `.github/workflows/build-backend.yml`) para o novo repo.

A partir daí, toda alteração feita no Lovable vai pro GitHub automaticamente.

---

## 3. Rodar a compilação pela primeira vez

Existem duas formas — qualquer push em `backend/` já dispara o build, mas você
também pode rodar manualmente:

### 3.1 Rodar manualmente

1. Abra seu repositório no GitHub (ex.: `https://github.com/seu-usuario/maryar`).
2. Clique na aba **Actions** (no topo da página do repo).
3. Na barra lateral esquerda, clique no workflow **Build Backend (ASP.NET 4.5.2)**.
4. À direita, clique no botão **Run workflow** → escolha a branch `main` →
   **Run workflow** (verde).
5. Aguarde ~3 a 6 minutos. Quando aparecer ✅ verde, terminou.

### 3.2 Rodar via push (automático)

Qualquer mudança em arquivos dentro da pasta `backend/` que você fizer no
Lovable já dispara o workflow. Não precisa fazer nada extra.

---

## 4. Baixar o ZIP compilado

1. Ainda na aba **Actions**, clique na **execução mais recente** (a linha do
   topo com ✅ verde).
2. Role a página até o fim, até a seção **Artifacts**.
3. Você verá `maryar-api-deploy` — clique no nome para baixar.
4. O navegador baixa um arquivo `maryar-api-deploy.zip`.
   Dentro dele já tem **um segundo ZIP** chamado também `maryar-api-deploy.zip`
   (é o jeito que o GitHub Actions empacota artifacts). Extraia até chegar na
   pasta com:

```
bin/
  Maryar.Api.dll
  MySql.Data.dll
  Newtonsoft.Json.dll
  System.Web.Http.dll
  ... (mais umas 8-10 DLLs)
Global.asax
Web.config
```

Essa é a sua **publicação pronta para FTP**.

---

## 5. Preencher o Web.config antes de subir

Abra `Web.config` no Bloco de Notas (ou Notepad++) e troque os valores:

### 5.1 Banco MySQL (Locaweb)
```xml
<add name="MaryarDb"
     connectionString="server=SEU_HOST_MYSQL_LOCAWEB;port=3306;database=NOME_DO_BANCO;uid=USUARIO;password=SENHA;SslMode=Required;CharSet=utf8mb4;AllowPublicKeyRetrieval=true;"
     providerName="MySql.Data.MySqlClient" />
```
Os valores `SEU_HOST_MYSQL_LOCAWEB`, `NOME_DO_BANCO`, `USUARIO` e `SENHA`
vêm do **Painel Locaweb → MySQL** (após criar o banco).

### 5.2 JWT (segredo da sessão de login)
```xml
<add key="Maryar:JwtSecret" value="COLE_AQUI_UMA_STRING_LONGA_E_ALEATORIA_COM_PELO_MENOS_32_CARACTERES" />
```
Gere uma em https://www.random.org/strings/ (50 chars, alfanumérico).

### 5.3 InfinitePay (quando tiver as chaves)
```xml
<add key="InfinitePay:ApiKey"         value="..." />
<add key="InfinitePay:WebhookSecret"  value="..." />
<add key="InfinitePay:MerchantHandle" value="..." />
```

### 5.4 CORS (domínios do frontend autorizados)
```xml
<add key="Maryar:CorsOrigins" value="https://maryar.com.br,https://www.maryar.com.br" />
```

Salve o arquivo.

---

## 6. Subir para a Locaweb por FTP

1. Antes: no painel Locaweb, rode em **MySQL → phpMyAdmin** os scripts:
   - `backend/database/schema.sql`
   - `backend/database/seeds.sql`

2. Abra o FileZilla (ou WinSCP) com os dados de FTP do site
   `api.maryar.com.br` (a Locaweb envia por e-mail ao contratar).

3. Navegue até `httpdocs/` (raiz do site `api.maryar.com.br`). Apague tudo que
   estiver lá (se for um site novo, normalmente só tem um `index.html`
   placeholder).

4. Arraste **todo o conteúdo da pasta extraída** (a pasta `bin/`, o
   `Global.asax` e o `Web.config`) para dentro de `httpdocs/`. A estrutura final
   no servidor deve ficar:

```
httpdocs/
├── bin/
│   ├── Maryar.Api.dll
│   ├── MySql.Data.dll
│   └── ... (demais DLLs)
├── Global.asax
└── Web.config
```

5. No painel Locaweb, libere o IP do servidor Web para acessar o MySQL
   (geralmente em **MySQL → Liberar IP**). Se o MySQL estiver no mesmo
   plano/host, costuma ser `localhost`.

---

## 7. Testar

Abra no navegador:

- `https://api.maryar.com.br/api/brands` → deve retornar um JSON com marcas.
- `https://api.maryar.com.br/api/products` → deve retornar lista de produtos.

Se der erro 500, ative temporariamente em `Web.config`:
```xml
<customErrors mode="Off" />
```
e abra novamente — o IIS vai mostrar a mensagem de erro real (quase sempre
algum valor errado no `connectionStrings`).

---

## 8. Atualizações futuras

Sempre que mudar algo no backend:

1. Edite no Lovable normalmente.
2. Aguarde o workflow rodar no GitHub (~5 min) — ou rode manualmente em
   **Actions → Run workflow**.
3. Baixe o novo `maryar-api-deploy.zip` em **Artifacts**.
4. Substitua os arquivos no FTP (geralmente basta sobrescrever a pasta `bin/`
   e o `Global.asax` — **não sobrescreva o `Web.config`** se já estiver
   preenchido, ou faça backup antes).

---

## 9. Problemas comuns

| Sintoma                                        | Causa provável                                | Solução                                                                              |
| ---------------------------------------------- | --------------------------------------------- | ------------------------------------------------------------------------------------ |
| Workflow falha em "Restore"                    | Sem acesso ao NuGet                           | Reexecute (botão **Re-run jobs**). 99% das vezes é instabilidade momentânea.         |
| Workflow falha em "Build" com erro de Reference | Faltou algum pacote                          | Verifique `packages.config` — se editou manualmente, confira nome/versão.            |
| Site retorna 403/404 ao abrir `/api/brands`    | Falta `Global.asax` ou pasta `bin/` no FTP    | Reenvie esses arquivos para `httpdocs/`.                                             |
| Erro 500 "Could not load file MySql.Data"      | DLL do MySQL não foi para o servidor          | Verifique se `bin/MySql.Data.dll` está no FTP.                                       |
| Erro 500 ao chamar endpoint                    | Connection string errada                      | Coloque `customErrors mode="Off"` e leia a mensagem real.                            |
| CORS bloqueado no navegador                    | Frontend não está em `Maryar:CorsOrigins`     | Adicione o domínio do frontend, separado por vírgula. Reenvie `Web.config` por FTP.  |

---

## 10. Resumo de comandos (zero — tudo é clique)

Você **não precisa** rodar nada localmente. O fluxo todo é:

1. Editar no Lovable → 2. Aguardar Actions ✅ → 3. Baixar Artifact →
4. Extrair → 5. Editar `Web.config` → 6. FTP → 7. Testar.

Pronto.
