
# Maryar — Guia de Integração de APIs (ASP Clássico + ASP.NET 4.5.2) e InfinitePay

Documento de referência técnica para o backend do Maryar. O **frontend**
(TanStack Start + React 19) entregue na Fase 1 é apenas a camada de
apresentação e consome **APIs REST hospedadas em IIS**.

Stack alvo definida com o cliente:

| Camada              | Tecnologia                                                       |
| ------------------- | ---------------------------------------------------------------- |
| Frontend            | TanStack Start v1 + React 19 (Lovable)                           |
| Backend principal   | **ASP.NET Framework 4.5.2 + Web API 2 (C#)** — APIs JSON         |
| Backend legado/auxiliar | **ASP Clássico (VBScript)** — páginas utilitárias e endpoints simples |
| Acesso a dados      | **ADO.NET puro** (`MySqlConnection` + `MySqlCommand`)            |
| Banco               | **MySQL 8** (InnoDB, utf8mb4)                                    |
| Hospedagem          | IIS 8.5+ (Windows Server)                                        |
| Pagamentos          | InfinitePay (PIX + Cartão de Crédito) via API REST              |

> Importante: **Lovable Cloud / Supabase / `createServerFn` / `supabaseAdmin` NÃO se aplicam aqui.**
> O Lovable cuida apenas do frontend; toda a lógica de negócio, persistência
> e integração com InfinitePay vive no seu servidor IIS.

---

## 1. Arquitetura

```
┌──────────────────────────────────────────┐
│ Browser (React 19 + TanStack Start)      │
│ Vitrine • Catálogo • PDP • Checkout      │
└───────────────┬──────────────────────────┘
                │ HTTPS + CORS (fetch)
                ▼
┌──────────────────────────────────────────┐
│ IIS  —  https://api.maryar.com.br        │
│ ┌──────────────────────────────────────┐ │
│ │ ASP.NET 4.5.2 Web API 2 (C#)         │ │
│ │  /api/products, /api/cart,           │ │
│ │  /api/orders, /api/payments,         │ │
│ │  /api/webhooks/infinitepay           │ │
│ └──────────────────────────────────────┘ │
│ ┌──────────────────────────────────────┐ │
│ │ ASP Clássico (.asp)                  │ │
│ │  páginas auxiliares / endpoints      │ │
│ │  simples / relatórios legados        │ │
│ └──────────────────────────────────────┘ │
└──────┬───────────────────┬───────────────┘
       │                   │
       ▼                   ▼
┌──────────────┐   ┌──────────────────────┐
│  MySQL 8     │   │ InfinitePay API REST │
│  (ADO.NET)   │   │  charges (PIX/cartão)│
└──────────────┘   └──────────────────────┘
```

Divisão recomendada entre ASP Clássico e ASP.NET:

| Função                                       | Stack recomendada      |
| -------------------------------------------- | ---------------------- |
| Catálogo, carrinho, pedidos, pagamentos, webhook InfinitePay | **ASP.NET 4.5.2 (Web API 2)** — JSON, HMAC, HttpClient, async |
| Páginas de relatório interno, exportações CSV, áreas administrativas legadas | **ASP Clássico** — quando já existir base ou para evitar deploy completo |
| Endpoints públicos críticos (pagamento, autenticação) | **Nunca** em ASP Clássico — sem suporte nativo a JSON, HMAC time-safe, TLS moderno controlado |

> ASP Clássico fica como **complemento**; o caminho de pagamento permanece
> 100% em ASP.NET por questão de segurança (HMAC, comparação time-safe,
> TLS 1.2+, `HttpClient` com timeouts).

---

## 2. Banco de dados MySQL

Charset `utf8mb4`, engine `InnoDB`, timestamps UTC.

```sql
CREATE DATABASE maryar
  CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;

USE maryar;

CREATE TABLE brands (
  id           CHAR(36)     NOT NULL PRIMARY KEY,
  slug         VARCHAR(120) NOT NULL UNIQUE,
  nome         VARCHAR(160) NOT NULL,
  descricao    TEXT NULL,
  created_at   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB;

CREATE TABLE fragrance_families (
  slug VARCHAR(60)  NOT NULL PRIMARY KEY,
  nome VARCHAR(120) NOT NULL
) ENGINE=InnoDB;

CREATE TABLE products (
  id              CHAR(36)     NOT NULL PRIMARY KEY,
  slug            VARCHAR(160) NOT NULL UNIQUE,
  brand_id        CHAR(36)     NULL,
  categoria       VARCHAR(30)  NOT NULL,            -- perfume|skincare|makeup
  nome            VARCHAR(200) NOT NULL,
  descricao       TEXT         NULL,
  preco_centavos  INT          NOT NULL,
  estoque         INT          NOT NULL DEFAULT 0,
  imagem_url      VARCHAR(500) NULL,
  ativo           TINYINT(1)   NOT NULL DEFAULT 1,
  created_at      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
                                ON UPDATE CURRENT_TIMESTAMP,
  CONSTRAINT fk_products_brand FOREIGN KEY (brand_id) REFERENCES brands(id),
  INDEX ix_products_categoria (categoria),
  INDEX ix_products_ativo     (ativo)
) ENGINE=InnoDB;

CREATE TABLE perfume_details (
  product_id     CHAR(36)    NOT NULL PRIMARY KEY,
  familia_slug   VARCHAR(60) NULL,
  concentracao   VARCHAR(20) NULL,                  -- EDP|EDT|Extrait
  volume_ml      SMALLINT    NULL,
  notas_topo     JSON        NULL,                  -- ["bergamota","limão"]
  notas_coracao  JSON        NULL,
  notas_base     JSON        NULL,
  fixacao        TINYINT     NULL,                  -- 0..10
  projecao       TINYINT     NULL,
  duracao_horas  VARCHAR(20) NULL,
  estacao_scores JSON        NULL,                  -- {primavera:8,...}
  periodo_scores JSON        NULL,
  ocasiao_scores JSON        NULL,
  CONSTRAINT fk_perfdet_product
     FOREIGN KEY (product_id) REFERENCES products(id) ON DELETE CASCADE,
  CONSTRAINT fk_perfdet_familia
     FOREIGN KEY (familia_slug) REFERENCES fragrance_families(slug)
) ENGINE=InnoDB;

CREATE TABLE customers (
  id            CHAR(36)     NOT NULL PRIMARY KEY,
  email         VARCHAR(180) NOT NULL UNIQUE,
  nome          VARCHAR(160) NOT NULL,
  senha_hash    VARCHAR(255) NULL,                  -- bcrypt/PBKDF2
  created_at    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB;

CREATE TABLE carts (
  id            CHAR(36)     NOT NULL PRIMARY KEY,
  customer_id   CHAR(36)     NULL,
  session_token VARCHAR(80)  NULL,                  -- guests
  created_at    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
                              ON UPDATE CURRENT_TIMESTAMP,
  CONSTRAINT fk_carts_customer FOREIGN KEY (customer_id) REFERENCES customers(id),
  INDEX ix_carts_session (session_token)
) ENGINE=InnoDB;

CREATE TABLE cart_items (
  id              CHAR(36) NOT NULL PRIMARY KEY,
  cart_id         CHAR(36) NOT NULL,
  product_id      CHAR(36) NOT NULL,
  quantidade      INT      NOT NULL,
  preco_centavos  INT      NOT NULL,                -- snapshot
  CONSTRAINT fk_cartitems_cart    FOREIGN KEY (cart_id) REFERENCES carts(id) ON DELETE CASCADE,
  CONSTRAINT fk_cartitems_product FOREIGN KEY (product_id) REFERENCES products(id),
  UNIQUE KEY uk_cart_product (cart_id, product_id)
) ENGINE=InnoDB;

CREATE TABLE orders (
  id                     CHAR(36)     NOT NULL PRIMARY KEY,
  customer_id            CHAR(36)     NULL,
  email                  VARCHAR(180) NOT NULL,
  status                 VARCHAR(30)  NOT NULL DEFAULT 'pending',
  total_centavos         INT          NOT NULL,
  endereco_entrega_json  JSON         NOT NULL,
  metodo_pagamento       VARCHAR(20)  NOT NULL,     -- pix|credit_card
  infinitepay_charge_id  VARCHAR(120) NULL,
  pix_qrcode             MEDIUMTEXT   NULL,         -- base64
  pix_copia_e_cola       TEXT         NULL,
  created_at             DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at             DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
                                       ON UPDATE CURRENT_TIMESTAMP,
  CONSTRAINT fk_orders_customer FOREIGN KEY (customer_id) REFERENCES customers(id),
  INDEX ix_orders_status (status),
  INDEX ix_orders_charge (infinitepay_charge_id)
) ENGINE=InnoDB;

CREATE TABLE order_items (
  id              CHAR(36)     NOT NULL PRIMARY KEY,
  order_id        CHAR(36)     NOT NULL,
  product_id      CHAR(36)     NOT NULL,
  nome            VARCHAR(200) NOT NULL,
  preco_centavos  INT          NOT NULL,
  quantidade      INT          NOT NULL,
  CONSTRAINT fk_oitems_order   FOREIGN KEY (order_id)   REFERENCES orders(id) ON DELETE CASCADE,
  CONSTRAINT fk_oitems_product FOREIGN KEY (product_id) REFERENCES products(id)
) ENGINE=InnoDB;

CREATE TABLE payment_events (
  id           CHAR(36)     NOT NULL PRIMARY KEY,
  order_id     CHAR(36)     NULL,
  provider     VARCHAR(40)  NOT NULL,               -- infinitepay
  event_id     VARCHAR(120) NOT NULL UNIQUE,        -- idempotência
  event_type   VARCHAR(80)  NOT NULL,
  payload      JSON         NOT NULL,
  received_at  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT fk_pevents_order FOREIGN KEY (order_id) REFERENCES orders(id)
) ENGINE=InnoDB;
```

Status do pedido (string controlada na aplicação):
`pending → awaiting_payment → paid → shipped → delivered`
estados terminais alternativos: `failed`, `cancelled`, `refunded`.

---

## 3. Estrutura do projeto ASP.NET 4.5.2

```
Maryar.Api/
├── Maryar.Api.csproj
├── Web.config
├── packages.config
├── Global.asax / Global.asax.cs
├── App_Start/
│   ├── WebApiConfig.cs
│   ├── CorsConfig.cs
│   └── FilterConfig.cs
├── Controllers/
│   ├── ProductsController.cs
│   ├── CartController.cs
│   ├── OrdersController.cs
│   ├── PaymentsController.cs
│   └── WebhooksController.cs
├── Models/
│   ├── Dtos/ (ProductDto, CartDto, OrderDto, ...)
│   └── Entities/ (Product, Order, ...)
├── Data/
│   ├── Db.cs                  // helper de conexão MySql
│   ├── ProductRepository.cs
│   ├── CartRepository.cs
│   └── OrderRepository.cs
├── Services/
│   ├── InfinitePayClient.cs
│   ├── PricingService.cs
│   └── WebhookVerifier.cs
└── Infrastructure/
    ├── JsonFormatterConfig.cs
    └── ErrorHandler.cs
```

### 3.1 Pacotes NuGet obrigatórios

```
Microsoft.AspNet.WebApi              5.2.x
Microsoft.AspNet.WebApi.Cors         5.2.x
Microsoft.AspNet.WebApi.WebHost      5.2.x
Newtonsoft.Json                      13.x
MySql.Data                           8.x          (driver oficial Oracle)
Microsoft.Owin.Host.SystemWeb        4.x          (se usar JWT/OWIN)
Microsoft.Owin.Security.Jwt          4.x          (auth opcional)
```

### 3.2 `Web.config` (trechos essenciais)

```xml
<configuration>
  <appSettings>
    <add key="MySqlConnectionString"
         value="Server=localhost;Port=3306;Database=maryar;
                Uid=maryar_app;Pwd=__SET_IN_DEPLOY__;
                SslMode=Required;DefaultCommandTimeout=30;" />
    <add key="InfinitePay:BaseUrl"      value="https://api.infinitepay.io" />
    <add key="InfinitePay:ApiKey"       value="__SET_IN_DEPLOY__" />
    <add key="InfinitePay:WebhookSecret" value="__SET_IN_DEPLOY__" />
    <add key="Cors:AllowedOrigins"
         value="https://www.maryar.com.br,https://maryar.com.br" />
  </appSettings>

  <system.web>
    <compilation debug="false" targetFramework="4.5.2" />
    <httpRuntime targetFramework="4.5.2" enableVersionHeader="false" />
    <customErrors mode="RemoteOnly" />
  </system.web>

  <system.webServer>
    <security>
      <requestFiltering>
        <requestLimits maxAllowedContentLength="2097152" /> <!-- 2 MB -->
      </requestFiltering>
    </security>
    <httpProtocol>
      <customHeaders>
        <remove name="X-Powered-By" />
        <add name="Strict-Transport-Security"
             value="max-age=31536000; includeSubDomains" />
      </customHeaders>
    </httpProtocol>
  </system.webServer>
</configuration>
```

> Em produção, mantenha segredos fora do `Web.config`. Use **arquivos
> `.config` externos** (`configSource="secrets.config"`) ou **DPAPI**
> (`aspnet_regiis -pe "appSettings"`). Nunca commitar valores reais.

### 3.3 Bootstrap do TLS

ASP.NET 4.5.2 **não habilita TLS 1.2 por padrão**. No `Global.asax.cs`:

```csharp
protected void Application_Start()
{
    ServicePointManager.SecurityProtocol =
        SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11;
    GlobalConfiguration.Configure(WebApiConfig.Register);
}
```

### 3.4 CORS

```csharp
// App_Start/WebApiConfig.cs
public static class WebApiConfig
{
    public static void Register(HttpConfiguration config)
    {
        var origins = ConfigurationManager.AppSettings["Cors:AllowedOrigins"];
        var cors = new EnableCorsAttribute(origins, "*", "GET,POST,PUT,DELETE,OPTIONS")
        {
            SupportsCredentials = false
        };
        config.EnableCors(cors);

        config.MapHttpAttributeRoutes();
        config.Routes.MapHttpRoute(
            name: "DefaultApi",
            routeTemplate: "api/{controller}/{id}",
            defaults: new { id = RouteParameter.Optional });

        // JSON em camelCase + ignora nulls
        var json = config.Formatters.JsonFormatter;
        json.SerializerSettings.ContractResolver =
            new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
        json.SerializerSettings.NullValueHandling =
            Newtonsoft.Json.NullValueHandling.Ignore;
    }
}
```

---

## 4. Acesso a MySQL com ADO.NET puro

Helper único para abrir conexão e parametrizar comandos.

```csharp
// Data/Db.cs
using MySql.Data.MySqlClient;
using System.Configuration;

public static class Db
{
    public static string ConnectionString =>
        ConfigurationManager.AppSettings["MySqlConnectionString"];

    public static MySqlConnection Open()
    {
        var cn = new MySqlConnection(ConnectionString);
        cn.Open();
        return cn;
    }
}
```

Repositório com **parâmetros nomeados** (jamais concatenar SQL):

```csharp
// Data/ProductRepository.cs
public class ProductRepository
{
    public ProductDto GetBySlug(string slug)
    {
        const string sql = @"
            SELECT p.id, p.slug, p.nome, p.descricao, p.preco_centavos,
                   p.imagem_url, p.estoque,
                   pd.familia_slug, pd.concentracao, pd.volume_ml,
                   pd.notas_topo, pd.notas_coracao, pd.notas_base,
                   pd.fixacao, pd.projecao, pd.duracao_horas
            FROM products p
            LEFT JOIN perfume_details pd ON pd.product_id = p.id
            WHERE p.slug = @slug AND p.ativo = 1
            LIMIT 1;";

        using (var cn = Db.Open())
        using (var cmd = new MySqlCommand(sql, cn))
        {
            cmd.Parameters.AddWithValue("@slug", slug);
            using (var r = cmd.ExecuteReader())
            {
                if (!r.Read()) return null;
                return new ProductDto
                {
                    Id            = r.GetString("id"),
                    Slug          = r.GetString("slug"),
                    Nome          = r.GetString("nome"),
                    Descricao     = r.IsDBNull(r.GetOrdinal("descricao")) ? null : r.GetString("descricao"),
                    PrecoCentavos = r.GetInt32("preco_centavos"),
                    ImagemUrl     = r.IsDBNull(r.GetOrdinal("imagem_url")) ? null : r.GetString("imagem_url"),
                    Estoque       = r.GetInt32("estoque"),
                    Olfativo      = MapOlfativo(r)
                };
            }
        }
    }
}
```

Transações para operações que mexem em várias tabelas (criação de pedido):

```csharp
using (var cn = Db.Open())
using (var tx = cn.BeginTransaction())
{
    try
    {
        // 1) INSERT INTO orders ...
        // 2) INSERT INTO order_items ... (loop)
        // 3) UPDATE products SET estoque = estoque - @q WHERE id = @id AND estoque >= @q
        tx.Commit();
    }
    catch { tx.Rollback(); throw; }
}
```

---

## 5. Endpoints REST

Convenção: **JSON camelCase**, datas ISO-8601 UTC, valores monetários
sempre em **centavos** (`int`), nunca `decimal` no transporte.

### 5.1 Catálogo

| Método | Rota                          | Descrição                          |
| ------ | ----------------------------- | ---------------------------------- |
| GET    | `/api/products`               | Lista paginada (filtros via query) |
| GET    | `/api/products/{slug}`        | Detalhe + pirâmide olfativa        |
| GET    | `/api/families`               | Lista famílias olfativas           |

### 5.2 Carrinho

| Método | Rota                                  | Descrição              |
| ------ | ------------------------------------- | ---------------------- |
| GET    | `/api/cart`                           | Carrinho do session    |
| POST   | `/api/cart/items`                     | Adicionar item         |
| PUT    | `/api/cart/items/{productId}`         | Atualizar quantidade   |
| DELETE | `/api/cart/items/{productId}`         | Remover item           |
| DELETE | `/api/cart`                           | Esvaziar               |

### 5.3 Pedidos e pagamento

| Método | Rota                                  | Descrição                              |
| ------ | ------------------------------------- | -------------------------------------- |
| POST   | `/api/orders`                         | Cria pedido `pending` (snapshot do carrinho) |
| GET    | `/api/orders/{id}`                    | Detalhe / status                       |
| POST   | `/api/payments/pix`                   | Cria charge PIX, retorna QR + copia-cola |
| POST   | `/api/payments/card`                  | Cria charge cartão (token gerado no browser) |
| POST   | `/api/webhooks/infinitepay`           | **Webhook público** com assinatura HMAC |

Identificação do carrinho de visitante: header `X-Session-Token` (UUID gerado
e armazenado no `localStorage` do frontend) **OU** cookie HttpOnly emitido
pelo servidor. Usuário logado: header `Authorization: Bearer <jwt>`.

---

## 6. Integração InfinitePay (C#)

> A InfinitePay (CloudWalk) expõe API REST para charges (PIX e cartão).
> Confira sempre os endpoints e payloads atuais em
> <https://developers.infinitepay.io>. Os exemplos abaixo descrevem o
> **shape** da integração; ajuste paths/campos para a versão vigente.

### 6.1 Segredos

| Chave                          | Onde                              |
| ------------------------------ | --------------------------------- |
| `InfinitePay:ApiKey`           | `Authorization: Bearer ...`       |
| `InfinitePay:WebhookSecret`    | Validação HMAC do webhook         |
| `InfinitePay:BaseUrl`          | `https://api.infinitepay.io`      |

Use sandbox até validar end-to-end. Nunca commitar credenciais reais.

### 6.2 Cliente HttpClient

```csharp
// Services/InfinitePayClient.cs
public class InfinitePayClient
{
    private static readonly HttpClient _http = CreateHttpClient();

    private static HttpClient CreateHttpClient()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        c.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        return c;
    }

    private static string BaseUrl =>
        ConfigurationManager.AppSettings["InfinitePay:BaseUrl"];
    private static string ApiKey =>
        ConfigurationManager.AppSettings["InfinitePay:ApiKey"];

    public async Task<JObject> PostAsync(string path, object body)
    {
        using (var req = new HttpRequestMessage(HttpMethod.Post, BaseUrl + path))
        {
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
            req.Content = new StringContent(
                JsonConvert.SerializeObject(body),
                Encoding.UTF8, "application/json");

            using (var resp = await _http.SendAsync(req))
            {
                var text = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode)
                    throw new InvalidOperationException(
                        "InfinitePay " + (int)resp.StatusCode + ": " + text);
                return JObject.Parse(text);
            }
        }
    }
}
```

### 6.3 PIX

```csharp
// Controllers/PaymentsController.cs
[RoutePrefix("api/payments")]
public class PaymentsController : ApiController
{
    [HttpPost, Route("pix")]
    public async Task<IHttpActionResult> CreatePix([FromBody] CreatePixRequest req)
    {
        if (req == null || req.OrderId == Guid.Empty)
            return BadRequest("orderId obrigatório");

        var order = new OrderRepository().GetById(req.OrderId)
                    ?? throw new HttpResponseException(HttpStatusCode.NotFound);

        var client = new InfinitePayClient();
        var charge = await client.PostAsync("/v1/charges", new {
            amount         = order.TotalCentavos,           // total recalculado no servidor
            currency       = "BRL",
            payment_method = "pix",
            customer       = new { email = order.Email },
            metadata       = new { order_id = order.Id.ToString() }
        });

        var pixQr    = (string)charge["pix"]["qr_code_base64"];
        var copiaCola = (string)charge["pix"]["copy_paste"];
        var chargeId  = (string)charge["id"];

        new OrderRepository().MarkAwaitingPix(order.Id, chargeId, pixQr, copiaCola);

        return Ok(new { qrCodeBase64 = pixQr, copiaECola = copiaCola });
    }
}
```

### 6.4 Cartão de Crédito

**Tokenização SEMPRE no browser** via SDK JS oficial do InfinitePay. O
servidor só recebe um **token opaco** — nunca PAN/CVV.

```csharp
[HttpPost, Route("card")]
public async Task<IHttpActionResult> CreateCard([FromBody] CreateCardRequest req)
{
    var order = new OrderRepository().GetById(req.OrderId)
                ?? throw new HttpResponseException(HttpStatusCode.NotFound);

    var charge = await new InfinitePayClient().PostAsync("/v1/charges", new {
        amount         = order.TotalCentavos,
        currency       = "BRL",
        payment_method = "credit_card",
        card           = new { token = req.CardToken, holder_name = req.HolderName },
        installments   = req.Parcelas,
        customer       = new { email = order.Email },
        metadata       = new { order_id = order.Id.ToString() }
    });

    var status   = (string)charge["status"];
    var chargeId = (string)charge["id"];
    var novo     = status == "paid" ? "paid" : "failed";
    new OrderRepository().UpdateStatus(order.Id, novo, chargeId);

    return Ok(new { status = novo, chargeId });
}
```

### 6.5 Webhook InfinitePay

Endpoint **público** (sem auth de usuário) — defendido por HMAC + idempotência.

```csharp
[RoutePrefix("api/webhooks")]
public class WebhooksController : ApiController
{
    [HttpPost, Route("infinitepay")]
    public async Task<HttpResponseMessage> InfinitePay()
    {
        var body = await Request.Content.ReadAsStringAsync();
        var signature = Request.Headers.TryGetValues("X-InfinitePay-Signature", out var v)
                        ? v.FirstOrDefault() : null;

        var secret = ConfigurationManager.AppSettings["InfinitePay:WebhookSecret"];
        if (!WebhookVerifier.IsValid(body, signature, secret))
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);

        var evt = JObject.Parse(body);
        var eventId   = (string)evt["id"];
        var eventType = (string)evt["type"];
        var orderId   = (string)evt["data"]?["metadata"]?["order_id"];

        var repo = new OrderRepository();

        // Idempotência via UNIQUE em payment_events.event_id
        if (!repo.TrySavePaymentEvent(eventId, eventType, orderId, body))
            return new HttpResponseMessage(HttpStatusCode.OK); // já processado

        string newStatus = null;
        switch (eventType)
        {
            case "charge.paid":      newStatus = "paid"; break;
            case "charge.failed":    newStatus = "failed"; break;
            case "charge.refunded":  newStatus = "refunded"; break;
            case "charge.cancelled": newStatus = "cancelled"; break;
        }
        if (newStatus != null && !string.IsNullOrEmpty(orderId))
            repo.UpdateStatus(Guid.Parse(orderId), newStatus, null);

        return new HttpResponseMessage(HttpStatusCode.OK);
    }
}
```

Verificação HMAC com **comparação time-safe**:

```csharp
// Services/WebhookVerifier.cs
public static class WebhookVerifier
{
    public static bool IsValid(string body, string signatureHex, string secret)
    {
        if (string.IsNullOrEmpty(signatureHex)) return false;

        using (var h = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
        {
            var expected = h.ComputeHash(Encoding.UTF8.GetBytes(body));
            var received = HexToBytes(signatureHex);
            if (expected.Length != received.Length) return false;

            int diff = 0;
            for (int i = 0; i < expected.Length; i++)
                diff |= expected[i] ^ received[i];
            return diff == 0;
        }
    }

    private static byte[] HexToBytes(string hex)
    {
        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        return bytes;
    }
}
```

URL pública para configurar no painel InfinitePay:
`https://api.maryar.com.br/api/webhooks/infinitepay`

---

## 7. ASP Clássico — quando usar e como conviver

ASP Clássico (`.asp`, VBScript) pode permanecer para **utilidades internas**:
- páginas de relatório legadas, exportações CSV simples;
- formulários administrativos antigos;
- ferramentas internas que já existem na sua operação.

Regras de convivência no mesmo site IIS:

- **App pool separado** para `/api` (ASP.NET 4.5.2 Integrated) e para `.asp`
  (Classic mode, .NET CLR `No Managed Code`) — evita conflitos de pipeline.
- ASP Clássico **não** deve emitir endpoints públicos críticos (pagamento,
  autenticação, webhook). Falta JSON nativo, falta HMAC time-safe robusto,
  falta cliente HTTPS moderno. Tudo isso fica no Web API.
- Conexão MySQL no ASP Clássico via **ODBC**:
  ```vbscript
  <%
  Dim cn
  Set cn = Server.CreateObject("ADODB.Connection")
  cn.Open "DRIVER={MySQL ODBC 8.0 Unicode Driver};" & _
          "SERVER=localhost;DATABASE=maryar;" & _
          "UID=maryar_ro;PWD=" & Application("MysqlRoPwd") & ";" & _
          "OPTION=3;SSLMODE=REQUIRED;"
  %>
  ```
- Sempre usar `ADODB.Command` com parâmetros — **nunca** concatenar SQL
  (risco crítico de SQL Injection).
- Senhas/segredos nunca hardcoded no `.asp`; carregue de `Web.config`
  externo via `Application` no `global.asa` durante deploy.

---

## 8. Frontend (Lovable) — consumindo as APIs

Centralizar todas as chamadas em um único cliente HTTP:

```ts
// src/lib/api.ts
const BASE = import.meta.env.VITE_API_BASE_URL; // ex.: https://api.maryar.com.br

function sessionToken() {
  let t = localStorage.getItem("maryar.session");
  if (!t) {
    t = crypto.randomUUID();
    localStorage.setItem("maryar.session", t);
  }
  return t;
}

export async function api<T>(path: string, init: RequestInit = {}): Promise<T> {
  const res = await fetch(`${BASE}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      "X-Session-Token": sessionToken(),
      ...(init.headers ?? {}),
    },
  });
  if (!res.ok) throw new Error(`API ${res.status}`);
  return res.json() as Promise<T>;
}
```

Variável pública em `.env`:
```
VITE_API_BASE_URL=https://api.maryar.com.br
```

Tokenização do cartão acontece no **browser** com a SDK JS do InfinitePay;
o frontend envia para `/api/payments/card` apenas:
```json
{ "orderId": "...", "cardToken": "tok_xxx", "parcelas": 3, "holderName": "..." }
```

---

## 9. Segurança — checklist obrigatório

- [ ] TLS 1.2+ habilitado no `Global.asax` e exigido no IIS.
- [ ] HSTS, `X-Content-Type-Options: nosniff`, `Referrer-Policy`.
- [ ] Segredos via `configSource` externo ou DPAPI, **fora** do repositório.
- [ ] Toda query SQL com parâmetros nomeados (`@x`), **zero** concatenação.
- [ ] Usuário MySQL da aplicação com privilégios mínimos (sem `GRANT ALL`).
- [ ] Webhook valida HMAC com comparação **time-safe** e checa idempotência por `payment_events.event_id` UNIQUE.
- [ ] `total_centavos` SEMPRE recalculado no servidor a partir dos preços atuais — nunca aceitar total enviado pelo frontend.
- [ ] PAN/CVV **jamais** trafegam pelo seu servidor — só token da SDK.
- [ ] Logs sem dados sensíveis (sem token, sem PAN, sem CVV).
- [ ] CORS restrito aos domínios oficiais do frontend.
- [ ] Rate limiting (IIS Dynamic IP Restrictions ou middleware OWIN) em `/api/orders` e `/api/payments/*`.
- [ ] App pools separados (ASP.NET vs ASP Clássico); contas de serviço dedicadas.

---

## 10. Testes manuais

1. **PIX feliz**: criar pedido → `/api/payments/pix` → simular pagamento no sandbox → webhook altera status para `paid`.
2. **PIX não pago**: status permanece `awaiting_payment` (job futuro de expiração).
3. **Cartão aprovado**: token válido → `paid`.
4. **Cartão recusado**: token de teste recusado → `failed` + mensagem amigável no UI.
5. **Webhook replay**: reenviar mesmo evento → segunda chamada não duplica (idempotência).
6. **Assinatura adulterada**: webhook com HMAC inválida → 401.
7. **Total adulterado**: frontend envia `total` divergente → servidor ignora e recalcula.

---

## 11. Roadmap por fases

| Fase | Entregáveis                                                                          |
| ---- | ------------------------------------------------------------------------------------ |
| 1    | (Concluída) Frontend Home + Catálogo + PDP com mocks                                 |
| 2    | Criar banco MySQL, projeto ASP.NET 4.5.2 com `ProductsController` + `Db.cs`          |
| 3    | Migrar mocks (`src/data/perfumes.ts`) para `products` + `perfume_details`            |
| 4    | Carrinho persistente + `OrdersController` + endereço + frete fixo                    |
| 5    | InfinitePay PIX (sandbox) + tela de pagamento PIX no frontend                        |
| 6    | InfinitePay Cartão + parcelamento + tokenização SDK no browser                       |
| 7    | Webhook + tela "Meus Pedidos" + emails transacionais                                 |
| 8    | Área administrativa (CRUD produtos, pedidos, estoque) — pode usar ASP Clássico legado |
| 9    | Quiz olfativo, blog, recomendações personalizadas                                    |

---

## 12. Referências

- ASP.NET Web API 2: <https://learn.microsoft.com/aspnet/web-api>
- MySQL Connector/NET: <https://dev.mysql.com/doc/connector-net/en/>
- InfinitePay Developers: <https://developers.infinitepay.io>
- Padrões OWASP para Web API: <https://owasp.org/www-project-api-security/>

> Edite este arquivo em `docs/INTEGRACAO-APIS-INFINITEPAY.md` conforme a
> arquitetura evoluir.
