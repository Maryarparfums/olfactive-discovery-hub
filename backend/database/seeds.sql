-- Seeds iniciais para Maryar
USE maryar;

-- Famílias
INSERT IGNORE INTO fragrance_families (id, slug, name) VALUES
  (UUID(), 'amadeirado',         'Amadeirado'),
  (UUID(), 'floral-branco',      'Floral Branco'),
  (UUID(), 'citrico-aromatico',  'Cítrico Aromático'),
  (UUID(), 'oriental',           'Oriental'),
  (UUID(), 'gourmand',           'Gourmand'),
  (UUID(), 'chipre',             'Chipre'),
  (UUID(), 'aquatico',           'Aquático');

-- Marcas
INSERT IGNORE INTO brands (id, slug, name, description) VALUES
  (UUID(), 'lumiere-noir',    'Lumière Noir',    'Maison contemporânea de matérias raras.'),
  (UUID(), 'atelier-maryar',  'Atelier Maryar',  'Linha própria, edições limitadas.'),
  (UUID(), 'maison-m',        'Maison M',        'Composições orientais e amadeiradas.'),
  (UUID(), 'essence',         'Essence',         'Frescores cítricos e aquáticos.'),
  (UUID(), 'parfum-dor',      'Parfum D''Or',    'Florais brancos solares.');

-- Exemplo de produto (Éter n. 03). Copie/adapte para os demais.
SET @brand_atelier := (SELECT id FROM brands WHERE slug = 'atelier-maryar');
SET @fam_floral    := (SELECT id FROM fragrance_families WHERE slug = 'floral-branco');
SET @pid           := UUID();

INSERT IGNORE INTO products
  (id, slug, name, brand_id, family_id, concentration, volume_ml, price, stock_qty,
   description, image_url, detail_image_url, active)
VALUES
  (@pid, 'eter-n-03', 'Éter n. 03', @brand_atelier, @fam_floral, 'Extrait de Parfum',
   50, 840.00, 25,
   'Composição que flutua entre o concreto e o etéreo. Cedro branco, íris e almíscar mineral.',
   '/assets/perfume-eter.jpg', '/assets/pdp-detail.jpg', 1);

INSERT IGNORE INTO perfume_details
  (product_id, notas_topo, notas_coracao, notas_base, estacao, periodo, ocasiao,
   fixacao, projecao, duracao_horas, similares)
VALUES (
  @pid,
  JSON_ARRAY('Bergamota','Pimenta Branca','Cardamomo'),
  JSON_ARRAY('Íris','Cedro da Virgínia','Violeta'),
  JSON_ARRAY('Âmbar Gris','Musk Branco','Vetiver'),
  JSON_OBJECT('Primavera',8,'Verão',6,'Outono',9,'Inverno',7),
  JSON_OBJECT('Dia',7,'Noite',9),
  JSON_OBJECT('Trabalho',8,'Encontro',9,'Dia a Dia',7,'Evento Formal',9,'Casamento',8),
  9, 7, '10h – 12h',
  JSON_ARRAY('sandalo-solido','ambar-gris','floral-blanc')
);
