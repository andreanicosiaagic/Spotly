import { NavLink } from 'react-router'
import { useAuth } from '../hooks/useAuth'

export function Navbar() {
  const { user } = useAuth()

  return (
    <nav className="bg-white border-b border-border sticky top-0 z-50">
      <div className="max-w-2xl mx-auto px-4 flex items-center justify-between h-14">
        <NavLink to="/" className="flex items-center gap-2 font-semibold text-primary text-lg select-none">
          <span className="text-2xl">🅿</span>
          <span>Spotly</span>
        </NavLink>

        <div className="flex items-center gap-1">
          <NavLink
            to="/parking"
            className={({ isActive }) =>
              `px-3 py-1.5 rounded-md text-sm font-medium transition-colors ${
                isActive ? 'bg-primary text-white' : 'text-text-muted hover:bg-surface-alt'
              }`
            }
          >
            🚗 Parcheggio
          </NavLink>
          <NavLink
            to="/desk"
            className={({ isActive }) =>
              `px-3 py-1.5 rounded-md text-sm font-medium transition-colors ${
                isActive ? 'bg-primary text-white' : 'text-text-muted hover:bg-surface-alt'
              }`
            }
          >
            💼 Postazione
          </NavLink>
          <NavLink
            to="/lunch"
            className={({ isActive }) =>
              `px-3 py-1.5 rounded-md text-sm font-medium transition-colors ${
                isActive ? 'bg-primary text-white' : 'text-text-muted hover:bg-surface-alt'
              }`
            }
          >
            🍽 Pranzo
          </NavLink>
        </div>

        {user && (
          <div className="text-xs text-text-muted hidden sm:block">
            {user.name} · <span className="text-primary font-medium">{user.roles[0]}</span>
          </div>
        )}
      </div>
    </nav>
  )
}
