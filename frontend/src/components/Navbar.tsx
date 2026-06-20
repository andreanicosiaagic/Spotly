import { NavLink } from 'react-router'
import { useAuth } from '../hooks/useAuth'
import { AppIcon } from './AppIcon'

const navigation = [
  { to: '/', label: 'Oggi', icon: 'home' },
  { to: '/parking', label: 'Parcheggio', icon: 'directions_car' },
  { to: '/desk', label: 'Postazioni', icon: 'chair' },
  { to: '/lunch', label: 'Pranzo', icon: 'restaurant' },
]

export function Navbar() {
  const { user } = useAuth()
  const initials = user?.name.split(' ').map(part => part[0]).slice(0, 2).join('') ?? 'SP'
  return <>
    <aside className="desktop-sidebar">
      <NavLink to="/" className="brand-lockup"><img src="/spotly-logo.png" alt="Spotly" /></NavLink>
      <nav className="sidebar-nav" aria-label="Navigazione principale">
        {navigation.map(item => <NavItem key={item.to} {...item} />)}
      </nav>
      <div className="sidebar-user">
        <div className="avatar">{initials}</div><div><strong>{user?.name}</strong><span>{user?.roles[0]} · Team Product</span></div>
      </div>
    </aside>
    <header className="mobile-header">
      <NavLink to="/" className="mobile-brand"><img src="/spotly-logo.png" alt="Spotly" /></NavLink>
      <div className="avatar">{initials}</div>
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
