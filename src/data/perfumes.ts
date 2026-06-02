import eter from "@/assets/perfume-eter.jpg";
import sandalo from "@/assets/perfume-sandalo.jpg";
import ambar from "@/assets/perfume-ambar.jpg";
import citrus from "@/assets/perfume-citrus.jpg";
import floral from "@/assets/perfume-floral.jpg";
import pdpDetail from "@/assets/pdp-detail.jpg";

export type FamiliaOlfativa =
  | "Amadeirado"
  | "Floral Branco"
  | "Cítrico Aromático"
  | "Oriental"
  | "Gourmand"
  | "Chipre"
  | "Aquático";

export type Estacao = "Primavera" | "Verão" | "Outono" | "Inverno";
export type Periodo = "Dia" | "Noite";
export type Ocasiao =
  | "Trabalho"
  | "Encontro"
  | "Casamento"
  | "Balada"
  | "Dia a Dia"
  | "Academia"
  | "Evento Formal";

export interface Perfume {
  slug: string;
  nome: string;
  marca: string;
  concentracao: string;
  volumeMl: number;
  preco: number;
  imagem: string;
  imagemDetalhe?: string;
  familia: FamiliaOlfativa;
  descricao: string;
  notas: {
    topo: string[];
    coracao: string[];
    base: string[];
  };
  estacao: Record<Estacao, number>;
  periodo: Record<Periodo, number>;
  ocasiao: Partial<Record<Ocasiao, number>>;
  performance: {
    fixacao: number; // 0-10
    projecao: number; // 0-10
    duracaoHoras: string;
  };
  similares: string[];
}

export const marcas = [
  "Lumière Noir",
  "Atelier Maryar",
  "Maison M",
  "Essence",
  "Parfum D'Or",
];

export const familias: FamiliaOlfativa[] = [
  "Amadeirado",
  "Floral Branco",
  "Cítrico Aromático",
  "Oriental",
  "Gourmand",
  "Chipre",
  "Aquático",
];

export const estacoes: Estacao[] = ["Primavera", "Verão", "Outono", "Inverno"];
export const periodos: Periodo[] = ["Dia", "Noite"];
export const ocasioes: Ocasiao[] = [
  "Trabalho",
  "Encontro",
  "Casamento",
  "Balada",
  "Dia a Dia",
  "Academia",
  "Evento Formal",
];

export const notasComuns = [
  "Bergamota",
  "Pimenta Rosa",
  "Cardamomo",
  "Íris",
  "Cedro",
  "Sândalo",
  "Âmbar",
  "Baunilha",
  "Musk",
  "Vetiver",
  "Jasmim",
  "Rosa",
  "Couro",
  "Tabaco",
  "Café",
  "Lavanda",
];

export const perfumes: Perfume[] = [
  {
    slug: "eter-n-03",
    nome: "Éter n. 03",
    marca: "Atelier Maryar",
    concentracao: "Extrait de Parfum",
    volumeMl: 50,
    preco: 840,
    imagem: eter,
    imagemDetalhe: pdpDetail,
    familia: "Floral Branco",
    descricao:
      "Uma composição que flutua entre o concreto e o etéreo. Notas de papel úmido, cedro branco e a sutileza do almíscar mineral compõem um rastro discreto, quase imaterial.",
    notas: {
      topo: ["Bergamota", "Pimenta Branca", "Cardamomo"],
      coracao: ["Íris", "Cedro da Virgínia", "Violeta"],
      base: ["Âmbar Gris", "Musk Branco", "Vetiver"],
    },
    estacao: { Primavera: 8, Verão: 6, Outono: 9, Inverno: 7 },
    periodo: { Dia: 7, Noite: 9 },
    ocasiao: {
      Trabalho: 8,
      Encontro: 9,
      "Dia a Dia": 7,
      "Evento Formal": 9,
      Casamento: 8,
    },
    performance: { fixacao: 9, projecao: 7, duracaoHoras: "10h – 12h" },
    similares: ["sandalo-solido", "ambar-gris", "floral-blanc"],
  },
  {
    slug: "sandalo-solido",
    nome: "Sândalo Sólido",
    marca: "Lumière Noir",
    concentracao: "Eau de Parfum",
    volumeMl: 100,
    preco: 720,
    imagem: sandalo,
    familia: "Amadeirado",
    descricao:
      "Madeira nua, resina morna e fumaça distante. Uma fragrância de pele, construída sobre sândalo Mysore reinterpretado com matérias contemporâneas.",
    notas: {
      topo: ["Cardamomo", "Pimenta Rosa"],
      coracao: ["Cedro", "Sândalo Mysore"],
      base: ["Vetiver", "Couro Suave", "Musk"],
    },
    estacao: { Primavera: 5, Verão: 3, Outono: 9, Inverno: 10 },
    periodo: { Dia: 6, Noite: 9 },
    ocasiao: {
      Trabalho: 7,
      Encontro: 9,
      "Dia a Dia": 6,
      "Evento Formal": 8,
    },
    performance: { fixacao: 10, projecao: 8, duracaoHoras: "12h+" },
    similares: ["eter-n-03", "tabac-noir", "ambar-gris"],
  },
  {
    slug: "ambar-gris",
    nome: "Âmbar Gris",
    marca: "Maison M",
    concentracao: "Extrait de Parfum",
    volumeMl: 50,
    preco: 1180,
    imagem: ambar,
    familia: "Oriental",
    descricao:
      "Calor mineral. Âmbar fóssil, baunilha rara e resinas envelhecidas em torno de um coração de íris. Um perfume que se instala, sem pressa.",
    notas: {
      topo: ["Açafrão", "Bergamota"],
      coracao: ["Rosa Búlgara", "Íris", "Benjoim"],
      base: ["Âmbar", "Baunilha de Madagascar", "Olíbano"],
    },
    estacao: { Primavera: 4, Verão: 2, Outono: 9, Inverno: 10 },
    periodo: { Dia: 4, Noite: 10 },
    ocasiao: {
      Encontro: 10,
      "Evento Formal": 10,
      Casamento: 9,
      Balada: 7,
    },
    performance: { fixacao: 10, projecao: 9, duracaoHoras: "14h+" },
    similares: ["sandalo-solido", "tabac-noir", "eter-n-03"],
  },
  {
    slug: "citrus-blanc",
    nome: "Citrus Blanc",
    marca: "Essence",
    concentracao: "Eau de Toilette",
    volumeMl: 100,
    preco: 480,
    imagem: citrus,
    familia: "Cítrico Aromático",
    descricao:
      "Luz mediterrânea engarrafada. Limão siciliano, manjericão e uma assinatura de vetiver branco que prolonga o frescor por horas.",
    notas: {
      topo: ["Limão Siciliano", "Bergamota", "Manjericão"],
      coracao: ["Néroli", "Chá Verde", "Petitgrain"],
      base: ["Vetiver Branco", "Musk", "Cedro Claro"],
    },
    estacao: { Primavera: 10, Verão: 10, Outono: 5, Inverno: 3 },
    periodo: { Dia: 10, Noite: 5 },
    ocasiao: {
      Trabalho: 9,
      "Dia a Dia": 10,
      Academia: 8,
      Encontro: 7,
    },
    performance: { fixacao: 6, projecao: 6, duracaoHoras: "6h – 8h" },
    similares: ["floral-blanc", "eter-n-03"],
  },
  {
    slug: "floral-blanc",
    nome: "Floral Blanc",
    marca: "Parfum D'Or",
    concentracao: "Eau de Parfum",
    volumeMl: 75,
    preco: 690,
    imagem: floral,
    familia: "Floral Branco",
    descricao:
      "Jasmim ao crepúsculo, tuberosa e uma gota de mel branco. Floral solar com um final de almíscar limpo.",
    notas: {
      topo: ["Pera Branca", "Bergamota"],
      coracao: ["Jasmim Sambac", "Tuberosa", "Magnólia"],
      base: ["Musk Branco", "Sândalo", "Cedro"],
    },
    estacao: { Primavera: 10, Verão: 8, Outono: 6, Inverno: 4 },
    periodo: { Dia: 8, Noite: 8 },
    ocasiao: {
      Encontro: 9,
      Casamento: 10,
      "Dia a Dia": 7,
      "Evento Formal": 8,
    },
    performance: { fixacao: 7, projecao: 7, duracaoHoras: "8h – 10h" },
    similares: ["eter-n-03", "citrus-blanc"],
  },
  {
    slug: "tabac-noir",
    nome: "Tabac Noir",
    marca: "Lumière Noir",
    concentracao: "Extrait de Parfum",
    volumeMl: 50,
    preco: 980,
    imagem: sandalo,
    familia: "Oriental",
    descricao:
      "Folhas de tabaco curadas, mel escuro e couro envelhecido. Uma fragrância noturna que conversa baixo.",
    notas: {
      topo: ["Pimenta Preta", "Bergamota"],
      coracao: ["Tabaco", "Mel", "Canela"],
      base: ["Couro", "Vetiver", "Baunilha Fumada"],
    },
    estacao: { Primavera: 3, Verão: 2, Outono: 9, Inverno: 10 },
    periodo: { Dia: 3, Noite: 10 },
    ocasiao: {
      Encontro: 9,
      "Evento Formal": 9,
      Balada: 8,
    },
    performance: { fixacao: 10, projecao: 9, duracaoHoras: "12h+" },
    similares: ["sandalo-solido", "ambar-gris"],
  },
  {
    slug: "aqua-mineral",
    nome: "Aqua Mineral",
    marca: "Essence",
    concentracao: "Eau de Toilette",
    volumeMl: 100,
    preco: 420,
    imagem: citrus,
    familia: "Aquático",
    descricao:
      "Pedra molhada, sal marinho e algas brancas. Frescor mineral, quase abstrato.",
    notas: {
      topo: ["Notas Salinas", "Limão", "Hortelã"],
      coracao: ["Algas", "Lírio do Vale", "Sálvia"],
      base: ["Âmbar Branco", "Almíscar", "Madeiras Claras"],
    },
    estacao: { Primavera: 9, Verão: 10, Outono: 5, Inverno: 3 },
    periodo: { Dia: 10, Noite: 5 },
    ocasiao: {
      Trabalho: 9,
      "Dia a Dia": 9,
      Academia: 9,
    },
    performance: { fixacao: 6, projecao: 7, duracaoHoras: "6h – 8h" },
    similares: ["citrus-blanc", "floral-blanc"],
  },
  {
    slug: "iris-poudre",
    nome: "Íris Poudré",
    marca: "Maison M",
    concentracao: "Eau de Parfum",
    volumeMl: 75,
    preco: 860,
    imagem: floral,
    familia: "Chipre",
    descricao:
      "Íris empoeirada sobre musgo de carvalho e couro suave. Elegância de outra década.",
    notas: {
      topo: ["Aldeídos", "Bergamota"],
      coracao: ["Íris", "Violeta", "Rosa"],
      base: ["Musgo de Carvalho", "Couro", "Patchouli"],
    },
    estacao: { Primavera: 7, Verão: 4, Outono: 9, Inverno: 9 },
    periodo: { Dia: 7, Noite: 9 },
    ocasiao: {
      Trabalho: 9,
      Encontro: 8,
      "Evento Formal": 10,
      Casamento: 9,
    },
    performance: { fixacao: 9, projecao: 7, duracaoHoras: "10h – 12h" },
    similares: ["eter-n-03", "ambar-gris", "sandalo-solido"],
  },
];

export const getPerfume = (slug: string) =>
  perfumes.find((p) => p.slug === slug);

export const formatPreco = (preco: number) =>
  new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: "BRL",
    minimumFractionDigits: 0,
  }).format(preco);
