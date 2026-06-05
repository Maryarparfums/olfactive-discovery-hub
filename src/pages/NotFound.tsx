import { Link } from "react-router-dom";
import { useEffect } from "react";

export default function NotFoundPage() {
  useEffect(() => {
    document.title = "Página não encontrada — Maryar";
  }, []);
  return (
    <div className="flex min-h-screen items-center justify-center bg-background px-6">
      <div className="max-w-md text-center">
        <p className="text-[10px] uppercase tracking-[0.3em] text-muted-foreground mb-6">
          Erro 404
        </p>
        <h1 className="font-serif text-5xl italic mb-4">Página não encontrada</h1>
        <p className="text-sm text-muted-foreground mb-8">
          A fragrância que você procurava se dispersou. Volte ao início.
        </p>
        <Link
          to="/"
          className="inline-block bg-foreground text-background text-[10px] uppercase tracking-[0.3em] px-8 py-3 hover:bg-muted-foreground transition-colors"
        >
          Voltar
        </Link>
      </div>
    </div>
  );
}
