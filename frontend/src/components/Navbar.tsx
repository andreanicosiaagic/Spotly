import { NavLink } from 'react-router'
import { useAuth } from '../hooks/useAuth'
import { useLogoClickEasterEgg } from '../hooks/use-logo-click-easter-egg'
import { DEMO_PROFILES } from '../mocks/data/users'
import { AppIcon } from './AppIcon'
import { ThemeToggle } from './ThemeToggle'

const navigation = [
  { to: '/', label: 'Oggi', icon: 'home' },
  { to: '/parking', label: 'Parcheggio', icon: 'directions_car' },
  { to: '/desk', label: 'Postazioni', icon: 'chair' },
  { to: '/lunch', label: 'Pranzo', icon: 'restaurant' },
]

interface NavbarProps {
  onLogoActivate: () => void
}

export function Navbar({ onLogoActivate }: NavbarProps) {
  const { profile, login, logout, user } = useAuth()
  const handleLogoClick = useLogoClickEasterEgg(onLogoActivate)
  const initials = user?.name.split(' ').map(part => part[0]).slice(0, 2).join('') ?? 'SP'
  return <>
    <aside className="desktop-sidebar">
      <NavLink to="/" onClick={handleLogoClick} className="brand-lockup"><img src="/spotly-logo.png" alt="Spotly" /></NavLink>
      <nav className="sidebar-nav" aria-label="Navigazione principale">
        {navigation.map(item => <NavItem key={item.to} {...item} />)}
      </nav>
      <div className="mb-3 flex items-center gap-2 px-2 text-[12px] font-semibold text-text-muted">
        <ThemeToggle /><span>Tema chiaro / scuro</span>
      </div>
      <div className="sidebar-user">
        <div className="avatar">{initials}</div><div className="min-w-0 flex-1"><strong>{user?.name}</strong><span>{user?.roles[0]} · Team Product</span>
          <label className="mt-2 block text-[10px] font-semibold text-[var(--c-7b7266)]">
            Profilo demo
            <select value={profile.id} onChange={(event) => login(event.target.value)} className="mt-1 block w-full rounded-[10px] border border-border bg-surface px-2 py-2 text-[12px] text-text">
              {DEMO_PROFILES.map((option) => <option key={option.id} value={option.id}>{option.roles[0]} · {option.name}</option>)}
            </select>
          </label>
        </div>
        <button type="button" onClick={logout} aria-label="Esci" title="Esci"
          className="grid h-8 w-8 flex-none place-items-center self-start rounded-[10px] border border-border bg-surface text-text-muted transition hover:text-text"><AppIcon name="logout" /></button>
      </div>
    </aside>
    <header className="mobile-header">
      <NavLink to="/" onClick={handleLogoClick} className="mobile-brand"><img src="/spotly-logo.png" alt="Spotly" /></NavLink>
      <div className="flex items-center gap-2">
        <ThemeToggle />
        <select aria-label="Profilo demo" value={profile.id} onChange={(event) => login(event.target.value)} className="max-w-[140px] rounded-[10px] border border-border bg-surface px-2 py-2 text-[12px] text-text">
          {DEMO_PROFILES.map((option) => <option key={option.id} value={option.id}>{option.roles[0]}</option>)}
        </select>
        <button type="button" onClick={logout} aria-label="Esci" title="Esci" className="grid h-9 w-9 flex-none place-items-center rounded-[11px] border border-border bg-surface text-text-muted"><AppIcon name="logout" /></button>
        <div className="avatar">{initials}</div>
      </div>
    </header>
    <nav className="mobile-bottom-nav" aria-label="Navigazione principale">
      {navigation.map(item => <NavItem key={item.to} {...item} />)}
    </nav>
  </>
}

function NavItem({ to, label, icon }: { to: string; label: string; icon: string }) {
  return <NavLink to={to} end={to === '/'} className={({ isActive }) => `app-nav-item ${isActive ? 'app-nav-item-active' : ''}`}>
    <AppIcon name={icon} filled /><span>{label}</span>
  </NavLink>
}
