import { useState } from 'react'
import { DEMO_PROFILES } from '../mocks/data/users'
import { useAuth } from '../hooks/useAuth'
import { ThemeToggle } from '../components/ThemeToggle'

// Accesso dimostrativo (login finta): nessun Entra ID, l'utente sceglie un
// profilo demo ed entra nell'app con dati simulati.
export default function LoginPage() {
  const { login, profile } = useAuth()
  const [selected, setSelected] = useState(profile.id)

  return (
    <div className="relative grid min-h-screen place-items-center bg-[var(--app-bg)] px-5 py-10">
      <div className="absolute right-4 top-4"><ThemeToggle /></div>

      <div className="spotly-card w-full max-w-[420px] p-7 text-center">
        <img src="/spotly-logo.png" alt="Spotly" className="mx-auto mb-4 h-[68px] w-[68px] rounded-[18px] object-cover" />
        <h1 className="font-display m-0 text-[30px] font-extrabold tracking-[-.02em] text-text">Spotly</h1>
        <p className="mt-1.5 mb-0 text-sm text-text-muted">Smart Office Booking · accesso dimostrativo</p>

        <p className="mt-6 mb-2 text-left text-[11px] font-extrabold uppercase tracking-[.08em] text-text-muted">Entra come</p>
        <div className="grid grid-cols-2 gap-2">
          {DEMO_PROFILES.map((option) => {
            const active = option.id === selected
            return (
              <button key={option.id} type="button" onClick={() => setSelected(option.id)} aria-pressed={active}
                className={`rounded-[14px] border-[1.5px] p-3 text-left transition ${active ? 'border-primary bg-[var(--c-fcede7)]' : 'border-border bg-surface hover:border-[var(--color-text-muted)]'}`}>
                <span className="block text-[13px] font-extrabold text-text">{option.roles[0]}</span>
                <span className="mt-0.5 block truncate text-[11px] text-text-muted">{option.name}</span>
              </button>
            )
          })}
        </div>

        <button type="button" onClick={() => login(selected)}
          className="mt-5 flex w-full items-center justify-center gap-2 rounded-[14px] border-0 bg-primary px-5 py-3.5 text-sm font-bold text-white shadow-[0_6px_16px_rgba(236,106,77,.28)] transition hover:brightness-105">
          <AppIcon name="login" filled className="text-[20px]" />Accedi
        </button>
        <p className="mt-3 mb-0 text-[11px] text-text-muted">Dati simulati · nessuna autenticazione reale</p>
      </div>
    </div>
  )
}
