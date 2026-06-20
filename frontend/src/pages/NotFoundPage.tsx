import { Link } from 'react-router'

export default function NotFoundPage() {
  return <div className="spotly-card p-8 text-center">
    <p className="m-0 text-[11px] font-extrabold uppercase tracking-[.08em] text-[#A89E92]">404</p>
    <h1 className="mt-2 mb-2 text-2xl font-bold text-text">Pagina non trovata</h1>
    <p className="mb-5 text-sm text-text-muted">Il percorso richiesto non esiste nella demo corrente.</p>
    <Link to="/" className="inline-flex rounded-[14px] bg-[#2B2622] px-5 py-3 text-sm font-bold text-white no-underline">Torna alla dashboard</Link>
  </div>
}
