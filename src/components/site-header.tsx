import { Link } from "react-router-dom";

const left = [
  { to: "/catalogo", label: "Coleções" },
  { to: "/catalogo?familia=Amadeirado", label: "Aromas" },
];

export function SiteHeader() {
  return (
    <header className="sticky top-0 z-50 w-full bg-background/80 backdrop-blur-md border-b border-foreground/5">
      <div className="max-w-7xl mx-auto px-6 h-16 flex items-center justify-between">
        <nav className="flex gap-8 text-[11px] uppercase tracking-[0.2em] font-medium">
          {left.map((l) => (
            <Link key={l.label} to={l.to} className="hover:text-muted-foreground transition-colors">
              {l.label}
            </Link>
          ))}
        </nav>

        <Link
          to="/"
          className="font-serif italic text-2xl tracking-tighter absolute left-1/2 -translate-x-1/2"
          aria-label="Maryar — Home"
        >
          Maryar
        </Link>

        <nav className="flex gap-8 text-[11px] uppercase tracking-[0.2em] font-medium">
          <a href="#perfil" className="hover:text-muted-foreground transition-colors">Perfil</a>
          <a href="#sacola" className="hover:text-muted-foreground transition-colors">Sacola (0)</a>
        </nav>
      </div>
    </header>
  );
}
