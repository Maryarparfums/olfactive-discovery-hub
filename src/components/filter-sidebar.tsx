import {
  familias,
  estacoes,
  periodos,
  ocasioes,
  notasComuns,
  type FamiliaOlfativa,
  type Estacao,
  type Periodo,
  type Ocasiao,
} from "@/data/perfumes";

export interface CatalogFilters {
  familia?: FamiliaOlfativa;
  estacao?: Estacao;
  periodo?: Periodo;
  ocasiao?: Ocasiao;
  nota?: string;
}

interface Props {
  filters: CatalogFilters;
  counts: Record<string, number>;
  onChange: (f: CatalogFilters) => void;
}

function Section({
  title,
  children,
}: {
  title: string;
  children: React.ReactNode;
}) {
  return (
    <div>
      <h3 className="text-[10px] uppercase tracking-[0.2em] font-semibold mb-6 border-b border-foreground/10 pb-2">
        {title}
      </h3>
      {children}
    </div>
  );
}

export function FilterSidebar({ filters, counts, onChange }: Props) {
  const toggle = <K extends keyof CatalogFilters>(
    key: K,
    value: CatalogFilters[K],
  ) => {
    onChange({
      ...filters,
      [key]: filters[key] === value ? undefined : value,
    });
  };

  const isActive = <K extends keyof CatalogFilters>(
    key: K,
    value: CatalogFilters[K],
  ) => filters[key] === value;

  return (
    <div className="space-y-10">
      <Section title="Famílias Olfativas">
        <ul className="space-y-3 text-[13px]">
          {familias.map((f) => {
            const c = counts[`familia:${f}`] ?? 0;
            const active = isActive("familia", f);
            return (
              <li
                key={f}
                className={`flex justify-between cursor-pointer transition-colors ${
                  active
                    ? "text-foreground font-medium"
                    : "text-muted-foreground hover:text-foreground"
                }`}
                onClick={() => toggle("familia", f)}
              >
                <span>{f}</span>
                <span className="font-mono text-[11px]">
                  {String(c).padStart(2, "0")}
                </span>
              </li>
            );
          })}
        </ul>
      </Section>

      <Section title="Estação">
        <div className="grid grid-cols-2 gap-2">
          {estacoes.map((e) => {
            const active = isActive("estacao", e);
            return (
              <button
                key={e}
                onClick={() => toggle("estacao", e)}
                className={`px-3 py-2 text-[10px] uppercase tracking-tighter border transition-colors ${
                  active
                    ? "bg-foreground text-background border-foreground"
                    : "border-foreground/10 hover:border-foreground/40"
                }`}
              >
                {e}
              </button>
            );
          })}
        </div>
      </Section>

      <Section title="Período">
        <div className="flex gap-2">
          {periodos.map((p) => {
            const active = isActive("periodo", p);
            return (
              <button
                key={p}
                onClick={() => toggle("periodo", p)}
                className={`px-4 py-2 text-[10px] uppercase tracking-widest border transition-colors ${
                  active
                    ? "bg-foreground text-background border-foreground"
                    : "border-foreground/10 hover:border-foreground/40"
                }`}
              >
                {p}
              </button>
            );
          })}
        </div>
      </Section>

      <Section title="Ocasião">
        <ul className="space-y-3">
          {ocasioes.map((o) => {
            const active = isActive("ocasiao", o);
            return (
              <li
                key={o}
                className="flex gap-3 items-center text-xs cursor-pointer"
                onClick={() => toggle("ocasiao", o)}
              >
                <span
                  className={`w-3 h-3 border border-foreground transition-colors ${
                    active ? "bg-foreground" : "bg-transparent"
                  }`}
                />
                <span className={active ? "text-foreground" : "text-muted-foreground"}>
                  {o}
                </span>
              </li>
            );
          })}
        </ul>
      </Section>

      <Section title="Notas">
        <div className="flex flex-wrap gap-2">
          {notasComuns.slice(0, 10).map((n) => {
            const active = isActive("nota", n);
            return (
              <button
                key={n}
                onClick={() => toggle("nota", n)}
                className={`px-3 py-1 text-[10px] uppercase tracking-widest border transition-colors ${
                  active
                    ? "bg-foreground text-background border-foreground"
                    : "border-foreground/10 hover:border-foreground/40"
                }`}
              >
                {n}
              </button>
            );
          })}
        </div>
      </Section>

      {Object.values(filters).some(Boolean) && (
        <button
          onClick={() => onChange({})}
          className="text-[10px] uppercase tracking-[0.2em] font-medium border-b border-foreground pb-1 hover:opacity-60 transition-opacity"
        >
          Limpar filtros
        </button>
      )}
    </div>
  );
}
