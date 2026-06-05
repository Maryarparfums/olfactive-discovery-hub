import { useEffect, useMemo } from "react";
import { useSearchParams } from "react-router-dom";
import {
  perfumes,
  familias,
  type Perfume,
  type Estacao,
  type Periodo,
  type Ocasiao,
  type FamiliaOlfativa,
} from "@/data/perfumes";
import { ProductCard } from "@/components/product-card";
import { FilterSidebar, type CatalogFilters } from "@/components/filter-sidebar";

function matchesFilters(p: Perfume, f: CatalogFilters): boolean {
  if (f.familia && p.familia !== f.familia) return false;
  if (f.estacao && (p.estacao[f.estacao] ?? 0) < 7) return false;
  if (f.periodo && (p.periodo[f.periodo] ?? 0) < 7) return false;
  if (f.ocasiao && (p.ocasiao[f.ocasiao] ?? 0) < 7) return false;
  if (f.nota) {
    const all = [...p.notas.topo, ...p.notas.coracao, ...p.notas.base].join(" ").toLowerCase();
    if (!all.includes(f.nota.toLowerCase())) return false;
  }
  return true;
}

export default function CatalogPage() {
  const [searchParams, setSearchParams] = useSearchParams();

  useEffect(() => {
    document.title = "Catálogo — Maryar";
  }, []);

  const filters: CatalogFilters = useMemo(
    () => ({
      familia: (searchParams.get("familia") as FamiliaOlfativa) || undefined,
      estacao: (searchParams.get("estacao") as Estacao) || undefined,
      periodo: (searchParams.get("periodo") as Periodo) || undefined,
      ocasiao: (searchParams.get("ocasiao") as Ocasiao) || undefined,
      nota: searchParams.get("nota") || undefined,
    }),
    [searchParams],
  );

  const onChange = (next: CatalogFilters) => {
    const params = new URLSearchParams();
    Object.entries(next).forEach(([k, v]) => {
      if (v) params.set(k, String(v));
    });
    setSearchParams(params, { replace: true });
  };

  const filtered = useMemo(() => perfumes.filter((p) => matchesFilters(p, filters)), [filters]);

  const counts = useMemo(() => {
    const c: Record<string, number> = {};
    for (const f of familias) {
      c[`familia:${f}`] = perfumes.filter((p) => p.familia === f).length;
    }
    return c;
  }, []);

  return (
    <>
      <header className="max-w-7xl mx-auto px-6 pt-16 md:pt-24 pb-10">
        <p className="text-[10px] uppercase tracking-[0.3em] text-muted-foreground mb-4">Catálogo</p>
        <h1 className="font-serif text-4xl md:text-6xl italic max-w-3xl leading-[1.05]">
          A coleção inteira, organizada por <span>matéria.</span>
        </h1>
        <p className="mt-6 text-sm text-muted-foreground max-w-xl leading-relaxed">
          {filtered.length} {filtered.length === 1 ? "fragrância" : "fragrâncias"} na seleção atual.
        </p>
      </header>

      <section className="max-w-7xl mx-auto px-6 pb-24 grid grid-cols-12 gap-12">
        <aside className="col-span-12 lg:col-span-3 lg:sticky lg:top-24 h-fit">
          <FilterSidebar filters={filters} counts={counts} onChange={onChange} />
        </aside>

        <main className="col-span-12 lg:col-span-9">
          {filtered.length === 0 ? (
            <div className="border border-foreground/10 p-16 text-center">
              <p className="font-serif italic text-2xl mb-3">Nada corresponde a esta seleção.</p>
              <p className="text-sm text-muted-foreground mb-6">Tente afrouxar um dos filtros.</p>
              <button
                onClick={() => onChange({})}
                className="text-[10px] uppercase tracking-[0.2em] border-b border-foreground pb-1"
              >
                Limpar filtros
              </button>
            </div>
          ) : (
            <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-x-8 gap-y-16">
              {filtered.map((p) => <ProductCard key={p.slug} perfume={p} />)}
            </div>
          )}
        </main>
      </section>
    </>
  );
}
