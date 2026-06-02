import { Link } from "@tanstack/react-router";
import type { Perfume } from "@/data/perfumes";
import { formatPreco } from "@/data/perfumes";

interface Props {
  perfume: Perfume;
}

export function ProductCard({ perfume }: Props) {
  return (
    <Link
      to="/perfume/$slug"
      params={{ slug: perfume.slug }}
      className="group block"
    >
      <div className="w-full aspect-[4/5] bg-pearl group-hover:bg-accent/30 transition-colors duration-700 overflow-hidden mb-6">
        <img
          src={perfume.imagem}
          alt={`${perfume.nome} — ${perfume.marca}`}
          loading="lazy"
          className="w-full h-full object-cover group-hover:scale-[1.02] transition-transform duration-1000 ease-[cubic-bezier(0.19,1,0.22,1)]"
        />
      </div>
      <div className="flex justify-between items-start gap-4">
        <div>
          <h3 className="font-serif text-xl leading-tight">{perfume.nome}</h3>
          <p className="text-[11px] text-muted-foreground uppercase tracking-widest mt-1">
            {perfume.familia}
          </p>
        </div>
        <span className="text-sm font-mono whitespace-nowrap">
          {formatPreco(perfume.preco)}
        </span>
      </div>
    </Link>
  );
}
