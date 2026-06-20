import { Outlet, useLocation } from 'react-router'
import { Navbar } from './Navbar'
import { DateStrip } from './DateStrip'

const pageMeta: Record<string, { title: string; subtitle: string }> = {
  '/': { title: 'La mia giornata', subtitle: 'Organizza la tua presenza in ufficio' },
  '/parking': { title: 'Parcheggio', subtitle: 'Scegli un posto disponibile' },
  '/desk': { title: 'Postazioni', subtitle: 'Trova la postazione giusta per te' },
  '/lunch': { title: 'Pranzo', subtitle: 'Prenota un locale o scegli un lunch box' },
}

interface LayoutProps {
  onLogoActivate: () => void
}

export function Layout({ onLogoActivate }: LayoutProps) {
  const location = useLocation()
  const meta = pageMeta[location.pathname] ?? pageMeta['/']
  return <div className="app-shell">
    <Navbar onLogoActivate={onLogoActivate} />
    <main className="app-main">
      <div className="app-content">
        <div className="desktop-page-header">
          <div><h1>{meta.title}</h1><p>{meta.subtitle}</p></div><DateStrip />
        </div>
        <Outlet />
      </div>
    </main>
  </div>
}
