import { useEffect } from "react";
import { Link } from "react-router-dom";
import heroBottle from "@/assets/hero-bottle.jpg";
import { perfumes, marcas } from "@/data/perfumes";
import { ProductCard } from "@/components/product-card";

const estacoesCards = [
  { nome: "Primavera", desc: "Floralidade leve, néroli, chá branco." },
  { nome: "Verão", desc: "Cítricos solares, sal, frescor mineral." },
  { nome: "Outono", desc: "Especiarias quentes, madeiras secas." },
  { nome: "Inverno", desc: "Resinas, baunilha rara, âmbar." },
];

const ocasioesCards = [
  { nome: "Trabalho", desc: "Discrição calibrada, rastro próximo." },
  { nome: "Encontro", desc: "Calor, magnetismo, pele." },
  { nome: "Casamento", desc: "Floralidade nobre, luz." },
  { nome: "Dia a Dia", desc: "Assinatura silenciosa." },
];

export default function HomePage() {
  const novidades = perfumes.slice(0, 4);
  const maisProcurados = perfumes.slice(2, 6);

  useEffect(() => {
    document.title = "Maryar — A arquitetura do invisível";
  }, []);

  return (
    <>
      <section className="relative h-[88vh] min-h-[640px] flex items-center justify-center overflow-hidden">
        <div className="absolute inset-0 z-0">
          <img src={heroBottle} alt="Frasco editorial Maryar" className="w-full h-full object-cover" />
          <div className="absolute inset-0 bg-background/30" />
        </div>
        <div className="relative z-10 text-center max-w-4xl px-6 animate-reveal">
          <p className="text-[10px] uppercase tracking-[0.4em] text-foreground/70 mb-6">
            Edição de Inverno · Maryar Atelier
          </p>
          <h1 className="font-serif text-5xl md:text-7xl lg:text-8xl mb-8 leading-[0.95] text-balance tracking-tighter">
            A arquitetura do <span className="italic">invisível</span>.
          </h1>
          <p className="text-sm md:text-base tracking-wide max-w-md mx-auto leading-relaxed text-muted-foreground mb-10">
            Uma curadoria de fragrâncias que operam no silêncio entre a pele e o ar. Minimalismo perolado em cada nota.
          </p>
          <div className="flex flex-wrap gap-3 justify-center">
            <Link
              to="/catalogo"
              className="px-10 py-4 bg-foreground text-background text-[10px] uppercase tracking-[0.3em] hover:bg-muted-foreground transition-all duration-500"
            >
              Explorar Perfumes
            </Link>
            <a
              href="#estacoes"
              className="px-10 py-4 border border-foreground/30 text-foreground text-[10px] uppercase tracking-[0.3em] hover:bg-foreground hover:text-background transition-all duration-500"
            >
              Por Estação
            </a>
          </div>
        </div>
      </section>

      <section className="max-w-7xl mx-auto px-6 py-24 md:py-32">
        <div className="flex items-end justify-between mb-16">
          <div>
            <p className="text-[10px] uppercase tracking-[0.3em] text-muted-foreground mb-4">Edições Recentes</p>
            <h2 className="font-serif text-4xl md:text-5xl italic">Novidades.</h2>
          </div>
          <Link to="/catalogo" className="text-[10px] uppercase tracking-[0.2em] border-b border-foreground/30 pb-1 hover:border-foreground transition-colors">
            Ver tudo
          </Link>
        </div>
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-x-8 gap-y-16">
          {novidades.map((p) => <ProductCard key={p.slug} perfume={p} />)}
        </div>
      </section>

      <section className="bg-paper py-24 md:py-32">
        <div className="max-w-7xl mx-auto px-6">
          <div className="flex items-end justify-between mb-16">
            <h2 className="font-serif text-4xl md:text-5xl italic">Mais procurados.</h2>
            <Link to="/catalogo" className="text-[10px] uppercase tracking-[0.2em] border-b border-foreground/30 pb-1 hover:border-foreground transition-colors">
              Ver mais
            </Link>
          </div>
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-x-8 gap-y-16">
            {maisProcurados.map((p) => <ProductCard key={p.slug} perfume={p} />)}
          </div>
        </div>
      </section>

      <section id="estacoes" className="max-w-7xl mx-auto px-6 py-24 md:py-32">
        <div className="mb-16 max-w-2xl">
          <p className="text-[10px] uppercase tracking-[0.3em] text-muted-foreground mb-4">Curadoria por estação</p>
          <h2 className="font-serif text-4xl md:text-5xl italic mb-4">O ano em quatro respirações.</h2>
          <p className="text-sm text-muted-foreground leading-relaxed">
            Cada estação pede uma matéria-prima diferente. Encontre fragrâncias calibradas para o clima e a luz.
          </p>
        </div>
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-px bg-foreground/5">
          {estacoesCards.map((e) => (
            <Link
              key={e.nome}
              to={`/catalogo?estacao=${encodeURIComponent(e.nome)}`}
              className="group bg-background p-10 min-h-[260px] flex flex-col justify-between hover:bg-pearl transition-colors duration-500"
            >
              <span className="text-[10px] uppercase tracking-[0.3em] text-muted-foreground">Estação</span>
              <div>
                <h3 className="font-serif text-3xl italic mb-3">{e.nome}</h3>
                <p className="text-xs text-muted-foreground leading-relaxed">{e.desc}</p>
              </div>
            </Link>
          ))}
        </div>
      </section>

      <section className="bg-paper py-24 md:py-32">
        <div className="max-w-7xl mx-auto px-6">
          <div className="mb-16 max-w-2xl">
            <p className="text-[10px] uppercase tracking-[0.3em] text-muted-foreground mb-4">Curadoria por ocasião</p>
            <h2 className="font-serif text-4xl md:text-5xl italic">Um perfume para cada momento.</h2>
          </div>
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-px bg-foreground/5">
            {ocasioesCards.map((o) => (
              <Link
                key={o.nome}
                to={`/catalogo?ocasiao=${encodeURIComponent(o.nome)}`}
                className="bg-paper p-10 min-h-[220px] flex flex-col justify-between hover:bg-background transition-colors duration-500"
              >
                <span className="text-[10px] uppercase tracking-[0.3em] text-muted-foreground">Ocasião</span>
                <div>
                  <h3 className="font-serif text-2xl italic mb-2">{o.nome}</h3>
                  <p className="text-xs text-muted-foreground leading-relaxed">{o.desc}</p>
                </div>
              </Link>
            ))}
          </div>
        </div>
      </section>

      <section className="max-w-7xl mx-auto px-6 py-24 md:py-32 border-t border-foreground/5">
        <p className="text-[10px] uppercase tracking-[0.3em] text-muted-foreground mb-12 text-center">Marcas em destaque</p>
        <div className="grid grid-cols-2 md:grid-cols-5 gap-8 items-center justify-items-center text-muted-foreground/80">
          {marcas.map((m) => (
            <span key={m} className="font-serif italic text-xl text-center hover:text-foreground transition-colors">{m}</span>
          ))}
        </div>
      </section>
    </>
  );
}
