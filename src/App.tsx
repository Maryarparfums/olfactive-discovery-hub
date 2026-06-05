import { Routes, Route } from "react-router-dom";
import { SiteHeader } from "@/components/site-header";
import { SiteFooter } from "@/components/site-footer";
import HomePage from "@/pages/Home";
import CatalogPage from "@/pages/Catalog";
import PerfumePage from "@/pages/Perfume";
import NotFoundPage from "@/pages/NotFound";

export default function App() {
  return (
    <>
      <SiteHeader />
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/catalogo" element={<CatalogPage />} />
        <Route path="/perfume/:slug" element={<PerfumePage />} />
        <Route path="*" element={<NotFoundPage />} />
      </Routes>
      <SiteFooter />
    </>
  );
}
