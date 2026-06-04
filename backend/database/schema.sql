-- =====================================================================
-- Maryar - MySQL 8.x schema
-- Charset: utf8mb4 / Collation: utf8mb4_0900_ai_ci
-- IDs em CHAR(36) (UUID em texto) para casar com Guid.ToString() do .NET
-- =====================================================================

CREATE DATABASE IF NOT EXISTS maryar
  CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
USE maryar;

-- Usuário de aplicação (rode UMA VEZ, troque a senha):
-- CREATE USER 'maryar_app'@'%' IDENTIFIED BY 'TROQUE_AQUI';
-- GRANT SELECT, INSERT, UPDATE, DELETE ON maryar.* TO 'maryar_app'@'%';
-- FLUSH PRIVILEGES;

-- ---------- Marcas ----------
CREATE TABLE IF NOT EXISTS brands (
  id           CHAR(36)      NOT NULL PRIMARY KEY,
  slug         VARCHAR(120)  NOT NULL UNIQUE,
  name         VARCHAR(160)  NOT NULL,
  description  TEXT          NULL,
  created_at   TIMESTAMP     NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB;

-- ---------- Famílias olfativas ----------
CREATE TABLE IF NOT EXISTS fragrance_families (
  id     CHAR(36)     NOT NULL PRIMARY KEY,
  slug   VARCHAR(120) NOT NULL UNIQUE,
  name   VARCHAR(160) NOT NULL
) ENGINE=InnoDB;

-- ---------- Produtos ----------
CREATE TABLE IF NOT EXISTS products (
  id                CHAR(36)        NOT NULL PRIMARY KEY,
  slug              VARCHAR(160)    NOT NULL UNIQUE,
  name              VARCHAR(200)    NOT NULL,
  brand_id          CHAR(36)        NOT NULL,
  family_id         CHAR(36)        NULL,
  concentration     VARCHAR(60)     NULL,
  volume_ml         INT             NOT NULL DEFAULT 0,
  price             DECIMAL(10,2)   NOT NULL DEFAULT 0,
  stock_qty         INT             NOT NULL DEFAULT 0,
  description       TEXT            NULL,
  image_url         VARCHAR(500)    NULL,
  detail_image_url  VARCHAR(500)    NULL,
  active            TINYINT(1)      NOT NULL DEFAULT 1,
  created_at        TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at        TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  INDEX ix_products_brand   (brand_id),
  INDEX ix_products_family  (family_id),
  INDEX ix_products_active  (active),
  CONSTRAINT fk_products_brand  FOREIGN KEY (brand_id)  REFERENCES brands(id),
  CONSTRAINT fk_products_family FOREIGN KEY (family_id) REFERENCES fragrance_families(id)
) ENGINE=InnoDB;

-- ---------- Detalhes olfativos (1:1 com products) ----------
CREATE TABLE IF NOT EXISTS perfume_details (
  product_id     CHAR(36)     NOT NULL PRIMARY KEY,
  notas_topo     JSON         NULL,   -- ["Bergamota","Pimenta Rosa"]
  notas_coracao  JSON         NULL,
  notas_base     JSON         NULL,
  estacao        JSON         NULL,   -- {"Primavera":8,"Verão":6,"Outono":9,"Inverno":7}
  periodo        JSON         NULL,   -- {"Dia":7,"Noite":9}
  ocasiao        JSON         NULL,   -- {"Trabalho":8,"Encontro":9,...}
  fixacao        TINYINT      NOT NULL DEFAULT 0,
  projecao       TINYINT      NOT NULL DEFAULT 0,
  duracao_horas  VARCHAR(40)  NULL,
  similares      JSON         NULL,   -- ["slug1","slug2"]
  CONSTRAINT fk_details_product FOREIGN KEY (product_id) REFERENCES products(id) ON DELETE CASCADE
) ENGINE=InnoDB;

-- ---------- Usuários ----------
CREATE TABLE IF NOT EXISTS users (
  id            CHAR(36)      NOT NULL PRIMARY KEY,
  email         VARCHAR(200)  NOT NULL UNIQUE,
  name          VARCHAR(160)  NOT NULL,
  password_hash VARCHAR(500)  NOT NULL,
  role          VARCHAR(20)   NOT NULL DEFAULT 'customer',
  created_at    TIMESTAMP     NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB;

-- ---------- Carrinhos ----------
CREATE TABLE IF NOT EXISTS carts (
  id               CHAR(36)      NOT NULL PRIMARY KEY,
  user_id          CHAR(36)      NULL,
  anonymous_token  VARCHAR(80)   NULL,
  created_at       TIMESTAMP     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at       TIMESTAMP     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  UNIQUE KEY ux_carts_user  (user_id),
  UNIQUE KEY ux_carts_anon  (anonymous_token),
  CONSTRAINT fk_carts_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE SET NULL
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS cart_items (
  id          CHAR(36)      NOT NULL PRIMARY KEY,
  cart_id     CHAR(36)      NOT NULL,
  product_id  CHAR(36)      NOT NULL,
  quantity    INT           NOT NULL DEFAULT 1,
  unit_price  DECIMAL(10,2) NOT NULL,
  created_at  TIMESTAMP     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE KEY ux_cart_product (cart_id, product_id),
  CONSTRAINT fk_ci_cart    FOREIGN KEY (cart_id)    REFERENCES carts(id)    ON DELETE CASCADE,
  CONSTRAINT fk_ci_product FOREIGN KEY (product_id) REFERENCES products(id)
) ENGINE=InnoDB;

-- ---------- Pedidos ----------
CREATE TABLE IF NOT EXISTS orders (
  id                       CHAR(36)      NOT NULL PRIMARY KEY,
  order_number             VARCHAR(40)   NOT NULL UNIQUE,
  user_id                  CHAR(36)      NULL,
  customer_name            VARCHAR(200)  NOT NULL,
  customer_email           VARCHAR(200)  NOT NULL,
  customer_document        VARCHAR(20)   NULL,
  customer_phone           VARCHAR(40)   NULL,
  shipping_zip             VARCHAR(20)   NOT NULL,
  shipping_street          VARCHAR(200)  NOT NULL,
  shipping_number          VARCHAR(20)   NOT NULL,
  shipping_complement      VARCHAR(200)  NULL,
  shipping_neighborhood    VARCHAR(160)  NOT NULL,
  shipping_city            VARCHAR(160)  NOT NULL,
  shipping_state           CHAR(2)       NOT NULL,
  subtotal                 DECIMAL(10,2) NOT NULL,
  shipping_fee             DECIMAL(10,2) NOT NULL DEFAULT 0,
  discount                 DECIMAL(10,2) NOT NULL DEFAULT 0,
  total                    DECIMAL(10,2) NOT NULL,
  payment_method           VARCHAR(20)   NOT NULL,        -- pix | credit_card
  payment_status           VARCHAR(20)   NOT NULL DEFAULT 'pending',
  order_status             VARCHAR(20)   NOT NULL DEFAULT 'created',
  infinitepay_charge_id    VARCHAR(120)  NULL,
  pix_qr_code              TEXT          NULL,
  pix_copy_paste           TEXT          NULL,
  created_at               TIMESTAMP     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at               TIMESTAMP     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  INDEX ix_orders_charge   (infinitepay_charge_id),
  INDEX ix_orders_email    (customer_email),
  INDEX ix_orders_status   (order_status),
  CONSTRAINT fk_orders_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE SET NULL
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS order_items (
  id            CHAR(36)      NOT NULL PRIMARY KEY,
  order_id      CHAR(36)      NOT NULL,
  product_id    CHAR(36)      NOT NULL,
  product_slug  VARCHAR(160)  NOT NULL,
  product_name  VARCHAR(200)  NOT NULL,
  brand_name    VARCHAR(160)  NOT NULL,
  quantity      INT           NOT NULL,
  unit_price    DECIMAL(10,2) NOT NULL,
  line_total    DECIMAL(10,2) NOT NULL,
  CONSTRAINT fk_oi_order   FOREIGN KEY (order_id)   REFERENCES orders(id) ON DELETE CASCADE,
  CONSTRAINT fk_oi_product FOREIGN KEY (product_id) REFERENCES products(id)
) ENGINE=InnoDB;

-- ---------- Eventos de pagamento (idempotência de webhook) ----------
CREATE TABLE IF NOT EXISTS payment_events (
  id          CHAR(36)      NOT NULL PRIMARY KEY,
  event_id    VARCHAR(120)  NOT NULL UNIQUE,
  order_id    CHAR(36)      NULL,
  event_type  VARCHAR(80)   NOT NULL,
  payload     JSON          NOT NULL,
  received_at TIMESTAMP     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  INDEX ix_pe_order (order_id)
) ENGINE=InnoDB;
