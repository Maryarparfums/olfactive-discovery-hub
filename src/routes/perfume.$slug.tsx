import { createFileRoute, notFound, Link } from "@tanstack/react-router";
import {
  getPerfume,
  formatPreco,
  perfumes,
  estacoes,
  periodos,
  type Perfume,
} from "@/data/perfumes";
import { ScoreBar } from "@/components/score-bar";
import { ProductCard } from "@/components/product-card";

export const Route = createFileRoute("/perfume/$slug")({
  loader: ({ params }) => {
    const perfume = getPerfume(params.slug);
    if (!perfume) throw notFound();
    return perfume;
  },
  head: ({ loaderData, params }) => {
    if (!loaderData) return { meta: [] };
    return {
      meta: [
        { title: `${loaderData.nome} — ${loaderData.marca} | Maryar` },
        { name: "description", content: loaderData.descricao },
        { property: "og:title", content: `${loaderData.nome} — ${loaderData.marca}` },
        { property: "og:description", content: loaderData.descricao },
        { property: "og:type", content: "product" },
        { property: "og:url", content: `/perfume/${params.slug}` },
        { property: "og:image", content: loaderData.imagem },
      ],
      links: [{ rel: "canonical", href: `/perfume/${params.slug}` }],
      scripts: [
        {
          type: "application/ld+json",
          children: JSON.stringify({
            "@context": "https://schema.org",
            "@type": "Product",
            name: loaderData.nome,
            description: loaderData.descricao,
            brand: { "@type": "Brand", name: loaderData.marca },
            image: loaderData.imagem,
            offers: {
              "@type": "Offer",
              priceCurrency: "BRL",
              price: loaderData.preco,
              availability: "https://schema.org/InStock",
            },
          }),
        },
      ],
    };
  },
  component: PerfumePage,
  notFoundComponent: () => (
    <div className="max-w-3xl mx-auto px-6 py-32 text-center">
      <h1 className="font-serif text-4xl italic mb-4">Perfume não encontrado</h1>
      <Link
        to="/catalogo"
        className="text-[10px] uppercase tracking-[0.3em] border-b border-foreground pb-1"
      >
        Ver catálogo
      </Link>
    </div>
  ),
});

function PerfumePage() {
  const perfume = Route.useLoaderData() as Perfume;
  const similares: Perfume[] = perfume.similares
    .map((s: string) => perfumes.find((p) => p.slug === s))
    .filter((p): p is Perfume => Boolean(p));

  return (
    <article>
      {/* Breadcrumb */}
      <nav className="max-w-7xl mx-auto px-6 pt-10 text-[10px] uppercase tracking-[0.25em] text-muted-foreground">
        <Link to="/" className="hover:text-foreground">Início</Link>
        <span className="mx-3 opacity-40">/</span>
        <Link to="/catalogo" className="hover:text-foreground">Catálogo</Link>
        <span className="mx-3 opacity-40">/</span>
        <span className="text-foreground">{perfume.nome}</span>
      </nav>

      {/* Hero PDP */}
      <section className="max-w-7xl mx-auto px-6 pt-12 pb-24 grid grid-cols-1 lg:grid-cols-2 gap-16 items-start">
        <div className="lg:sticky lg:top-24 space-y-4">
          <div className="aspect-[4/5] bg-pearl overflow-hidden">
            <img
              src={perfume.imagem}
              alt={`Frasco de ${perfume.nome}, ${perfume.marca}`}
              className="w-full h-full object-cover"
            />
          </div>
          {perfume.imagemDetalhe && (
            <div className="grid grid-cols-2 gap-4">
              <div className="aspect-square bg-pearl overflow-hidden">
                <img
                  src={perfume.imagemDetalhe}
                  alt="Detalhe macro do frasco"
                  className="w-full h-full object-cover"
                />
              </div>
              <div className="aspect-square bg-paper" />
            </div>
          )}
        </div>

        <div>
          <p className="text-[10px] uppercase tracking-[0.3em] text-muted-foreground mb-4">
            {perfume.marca} · {perfume.familia}
          </p>
          <h1 className="font-serif text-5xl md:text-6xl italic leading-[1.02] mb-6">
            {perfume.nome}
          </h1>
          <div className="flex items-baseline gap-6 mb-8">
            <span className="text-2xl font-mono">{formatPreco(perfume.preco)}</span>
            <span className="text-[11px] uppercase tracking-widest text-muted-foreground">
              {perfume.volumeMl} ml · {perfume.concentracao}
            </span>
          </div>

          <p className="text-base text-muted-foreground leading-relaxed mb-10 max-w-prose">
            {perfume.descricao}
          </p>

          <div className="flex gap-3 mb-12">
            <button className="px-10 py-4 bg-foreground text-background text-[10px] uppercase tracking-[0.3em] hover:bg-muted-foreground transition-colors">
              Adicionar à sacola
            </button>
            <button className="px-10 py-4 border border-foreground/20 text-[10px] uppercase tracking-[0.3em] hover:border-foreground transition-colors">
              Favoritar
            </button>
          </div>

          {/* Pyramid */}
          <div className="mb-16">
            <h2 className="text-[10px] uppercase tracking-[0.3em] font-semibold mb-8 border-b border-foreground/10 pb-3">
              Pirâmide Olfativa
            </h2>
            <div className="space-y-4">
              {[
                { label: "Notas de Topo", notas: perfume.notas.topo },
                { label: "Notas de Coração", notas: perfume.notas.coracao },
                { label: "Notas de Base", notas: perfume.notas.base },
              ].map((n) => (
                <div
                  key={n.label}
                  className="p-6 border border-foreground/10 hover:border-foreground/30 hover:bg-pearl/50 transition-all group"
                >
                  <div className="flex justify-between items-center mb-2">
                    <span className="text-[10px] uppercase tracking-widest text-muted-foreground">
                      {n.label}
                    </span>
                    <div className="size-1 bg-accent group-hover:scale-150 transition-transform" />
                  </div>
                  <p className="text-lg font-serif">{n.notas.join(", ")}</p>
                </div>
              ))}
            </div>
          </div>

          {/* Performance */}
          <div className="mb-16">
            <h2 className="text-[10px] uppercase tracking-[0.3em] font-semibold mb-8 border-b border-foreground/10 pb-3">
              Performance
            </h2>
            <div className="grid grid-cols-2 gap-x-12 gap-y-8">
              <ScoreBar label="Fixação" value={perfume.performance.fixacao} />
              <ScoreBar label="Projeção" value={perfume.performance.projecao} />
              <div className="col-span-2">
                <ScoreBar
                  label="Duração média"
                  value={Math.round(
                    (perfume.performance.fixacao +
                      perfume.performance.projecao) /
                      2,
                  )}
                  caption={perfume.performance.duracaoHoras}
                />
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Adequação — Estação / Período / Ocasião */}
      <section className="bg-paper py-24">
        <div className="max-w-7xl mx-auto px-6">
          <p className="text-[10px] uppercase tracking-[0.3em] text-muted-foreground mb-4">
            Quando usar
          </p>
          <h2 className="font-serif text-3xl md:text-4xl italic mb-16">
            Adequação calibrada.
          </h2>

          <div className="grid md:grid-cols-3 gap-16">
            <div>
              <h3 className="text-[10px] uppercase tracking-[0.3em] font-semibold mb-6">
                Por Estação
              </h3>
              <div className="space-y-6">
                {estacoes.map((e) => (
                  <ScoreBar key={e} label={e} value={perfume.estacao[e]} />
                ))}
              </div>
            </div>

            <div>
              <h3 className="text-[10px] uppercase tracking-[0.3em] font-semibold mb-6">
                Por Período
              </h3>
              <div className="space-y-6">
                {periodos.map((p) => (
                  <ScoreBar key={p} label={p} value={perfume.periodo[p]} />
                ))}
              </div>
            </div>

            <div>
              <h3 className="text-[10px] uppercase tracking-[0.3em] font-semibold mb-6">
                Por Ocasião
              </h3>
              <div className="space-y-6">
                {Object.entries(perfume.ocasiao).map(([k, v]) => (
                  <ScoreBar key={k} label={k} value={v as number} />
                ))}
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Similares */}
      {similares.length > 0 && (
        <section className="max-w-7xl mx-auto px-6 py-24 md:py-32">
          <div className="flex items-end justify-between mb-16">
            <div>
              <p className="text-[10px] uppercase tracking-[0.3em] text-muted-foreground mb-4">
                Você também pode gostar
              </p>
              <h2 className="font-serif text-3xl md:text-4xl italic">
                Fragrâncias próximas.
              </h2>
            </div>
            <Link
              to="/catalogo"
              className="text-[10px] uppercase tracking-[0.2em] border-b border-foreground/30 pb-1 hover:border-foreground"
            >
              Ver catálogo
            </Link>
          </div>
          <div className="grid grid-cols-2 lg:grid-cols-3 gap-x-8 gap-y-16">
            {similares.map((s: Perfume) => (
              <ProductCard key={s.slug} perfume={s} />
            ))}
          </div>
        </section>
      )}
    </article>
  );
}
