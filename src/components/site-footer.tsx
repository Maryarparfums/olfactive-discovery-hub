export function SiteFooter() {
  return (
    <footer className="bg-background py-24 border-t border-foreground/5">
      <div className="max-w-7xl mx-auto px-6 grid grid-cols-1 md:grid-cols-4 gap-12">
        <div className="col-span-1 md:col-span-2">
          <h2 className="font-serif italic text-3xl mb-8">Maryar</h2>
          <p className="text-sm text-muted-foreground max-w-sm leading-relaxed">
            Fragrâncias e rituais de cuidado curados com silêncio. Uma
            experiência de luxo consciente, feita no Brasil.
          </p>
        </div>
        <div className="space-y-4">
          <h4 className="text-[10px] uppercase tracking-widest font-semibold">
            Serviço
          </h4>
          <ul className="text-[13px] text-muted-foreground space-y-2">
            <li className="hover:text-foreground cursor-pointer">Envio & Devoluções</li>
            <li className="hover:text-foreground cursor-pointer">Rastreamento</li>
            <li className="hover:text-foreground cursor-pointer">Perguntas Frequentes</li>
          </ul>
        </div>
        <div className="space-y-4">
          <h4 className="text-[10px] uppercase tracking-widest font-semibold">
            Newsletter
          </h4>
          <form
            className="flex border-b border-foreground/20 pb-2"
            onSubmit={(e) => e.preventDefault()}
          >
            <input
              type="email"
              placeholder="E-mail"
              className="bg-transparent text-sm w-full outline-none py-1"
              aria-label="Seu e-mail"
            />
            <button className="text-[10px] uppercase tracking-widest font-bold ml-4">
              Assinar
            </button>
          </form>
        </div>
      </div>
      <div className="max-w-7xl mx-auto px-6 mt-24 text-[10px] text-muted-foreground/60 uppercase tracking-[0.2em] flex justify-between">
        <span>© 2026 Maryar Perfumaria</span>
        <span>Feito com silêncio</span>
      </div>
    </footer>
  );
}
