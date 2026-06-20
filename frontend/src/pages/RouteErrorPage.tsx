import { isRouteErrorResponse, Link, useRouteError } from 'react-router'

export default function RouteErrorPage() {
  const error = useRouteError()
  const title = isRouteErrorResponse(error)
    ? `${error.status} · ${error.statusText || 'Errore di navigazione'}`
    : 'Errore applicativo'
  const detail = isRouteErrorResponse(error)
    ? (typeof error.data === 'string' ? error.data : 'La route richiesta non è stata caricata correttamente.')
    : error instanceof Error
      ? error.message
      : 'Si è verificato un errore inatteso durante il rendering della pagina.'

  return <div className="spotly-card p-8 text-center">
    <p className="m-0 text-[11px] font-extrabold uppercase tracking-[.08em] text-[#A89E92]">Spotly</p>
    <h1 className="mt-2 mb-2 text-2xl font-bold text-text">{title}</h1>
    <p className="mb-5 text-sm text-text-muted">{detail}</p>
    <Link to="/" className="inline-flex rounded-[14px] bg-[#2B2622] px-5 py-3 text-sm font-bold text-white no-underline">Torna alla dashboard</Link>
  </div>
}
