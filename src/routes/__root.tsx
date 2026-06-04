import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
  Outlet,
  Link,
  createRootRouteWithContext,
  useRouter,
  HeadContent,
  Scripts,
} from "@tanstack/react-router";
import { useEffect, type ReactNode } from "react";

import appCss from "../styles.css?url";
import { reportLovableError } from "../lib/lovable-error-reporting";
import { SiteHeader } from "@/components/site-header";
import { SiteFooter } from "@/components/site-footer";

function NotFoundComponent() {
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

function ErrorComponent({ error, reset }: { error: Error; reset: () => void }) {
  console.error(error);
  const router = useRouter();
  useEffect(() => {
    reportLovableError(error, { boundary: "tanstack_root_error_component" });
  }, [error]);

  return (
    <div className="flex min-h-screen items-center justify-center bg-background px-6">
      <div className="max-w-md text-center">
        <p className="text-[10px] uppercase tracking-[0.3em] text-muted-foreground mb-6">
          Algo se quebrou
        </p>
        <h1 className="font-serif text-4xl italic mb-4">
          Esta página não carregou
        </h1>
        <p className="text-sm text-muted-foreground mb-8">
          Tente novamente ou volte ao início.
        </p>
        <div className="flex justify-center gap-3">
          <button
            onClick={() => {
              router.invalidate();
              reset();
            }}
            className="bg-foreground text-background text-[10px] uppercase tracking-[0.3em] px-8 py-3"
          >
            Tentar de novo
          </button>
          <a
            href="/"
            className="border border-foreground/20 text-[10px] uppercase tracking-[0.3em] px-8 py-3"
          >
            Início
          </a>
        </div>
      </div>
    </div>
  );
}

export const Route = createRootRouteWithContext<{ queryClient: QueryClient }>()({
  head: () => ({
    meta: [
      { charSet: "utf-8" },
      { name: "viewport", content: "width=device-width, initial-scale=1" },
      { title: "Maryar — Perfumaria de nicho e rituais de cuidado" },
      {
        name: "description",
        content:
          "Maryar: curadoria de perfumes, skincare e maquiagem premium. Descubra fragrâncias por família olfativa, estação e ocasião.",
      },
      { property: "og:title", content: "Maryar — Perfumaria de nicho e rituais de cuidado" },
      {
        property: "og:description",
        content:
          "Curadoria de fragrâncias e rituais de cuidado. Encontre o perfume que combina com você.",
      },
      { property: "og:type", content: "website" },
      { property: "og:site_name", content: "Maryar" },
      { name: "twitter:card", content: "summary_large_image" },
      { name: "twitter:title", content: "Maryar — Perfumaria de nicho e rituais de cuidado" },
      { name: "description", content: "Maryar is a premium e-commerce platform for perfumes and beauty products, offering advanced olfactory discovery." },
      { property: "og:description", content: "Maryar is a premium e-commerce platform for perfumes and beauty products, offering advanced olfactory discovery." },
      { name: "twitter:description", content: "Maryar is a premium e-commerce platform for perfumes and beauty products, offering advanced olfactory discovery." },
      { property: "og:image", content: "https://pub-bb2e103a32db4e198524a2e9ed8f35b4.r2.dev/32aee6ac-9221-44d3-8190-3b1a6f3a92d4/id-preview-93e74019--448b0e5e-a531-460d-a1cb-ab7432c53675.lovable.app-1780594478762.png" },
      { name: "twitter:image", content: "https://pub-bb2e103a32db4e198524a2e9ed8f35b4.r2.dev/32aee6ac-9221-44d3-8190-3b1a6f3a92d4/id-preview-93e74019--448b0e5e-a531-460d-a1cb-ab7432c53675.lovable.app-1780594478762.png" },
    ],
    links: [
      { rel: "stylesheet", href: appCss },
      { rel: "preconnect", href: "https://fonts.googleapis.com" },
      {
        rel: "preconnect",
        href: "https://fonts.gstatic.com",
        crossOrigin: "anonymous",
      },
      {
        rel: "stylesheet",
        href: "https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600&family=Playfair+Display:ital,wght@0,500;0,600;1,500;1,600&family=JetBrains+Mono:wght@400&display=swap",
      },
    ],
    scripts: [
      {
        type: "application/ld+json",
        children: JSON.stringify({
          "@context": "https://schema.org",
          "@type": "Organization",
          name: "Maryar",
          description:
            "E-commerce premium de perfumaria de nicho, skincare e maquiagem.",
        }),
      },
    ],
  }),
  shellComponent: RootShell,
  component: RootComponent,
  notFoundComponent: NotFoundComponent,
  errorComponent: ErrorComponent,
});

function RootShell({ children }: { children: ReactNode }) {
  return (
    <html lang="pt-BR">
      <head>
        <HeadContent />
      </head>
      <body>
        {children}
        <Scripts />
      </body>
    </html>
  );
}

function RootComponent() {
  const { queryClient } = Route.useRouteContext();

  return (
    <QueryClientProvider client={queryClient}>
      <SiteHeader />
      <Outlet />
      <SiteFooter />
    </QueryClientProvider>
  );
}
