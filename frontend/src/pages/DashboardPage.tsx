import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import { useBookingStore } from '../store/bookingStore'
import { useAuth } from '../hooks/useAuth'
import type { CalendarAvailability, TeamAvailabilityMatch, TeamMemberMatch } from '../types'

const API = import.meta.env.VITE_API_URL ?? ''

export default function DashboardPage() {
  const { user } = useAuth()
  const { selectedDate, setSelectedDate, parkingBooking, deskBooking, lunchBooking } = useBookingStore()
  const canSeeTeam = user?.roles.some(role => ['Manager', 'Facility', 'Admin'].includes(role)) ?? false
  const teamQuery = useQuery<TeamAvailabilityMatch>({
    queryKey: ['team-availability', selectedDate],
    queryFn: async () => {
      const response = await fetch(`${API}/api/collaboration/team-match?date=${selectedDate}`)
      if (!response.ok) throw new Error('Disponibilità del team non disponibile')
      return response.json()
    },
    enabled: canSeeTeam,
  })

  return (
    <div className="space-y-6">
      <header>
        <p className="text-xs font-bold uppercase tracking-[0.16em] text-primary">Giornata in ufficio</p>
        <h1 className="mt-1 text-2xl font-semibold text-text">Ciao, {user?.name ?? 'Utente'}</h1>
      </header>

      <div className="flex items-center gap-3">
        <label htmlFor="dashboard-date" className="text-sm font-medium text-text-muted">Data</label>
        <input id="dashboard-date" type="date" value={selectedDate} onChange={event => setSelectedDate(event.target.value)}
          className="rounded-lg border border-border px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-primary" />
      </div>

      {canSeeTeam && <TeamAvailabilityPanel data={teamQuery.data} loading={teamQuery.isLoading} error={teamQuery.isError} />}

      <div className="grid gap-4">
        <BookingCard icon="🚗" title="Parcheggio" to="/parking" booking={parkingBooking ? { label: `Posto ${parkingBooking.spotId}`, status: parkingBooking.status } : null} />
        <BookingCard icon="💼" title="Postazione" to="/desk" booking={deskBooking ? { label: `Desk ${deskBooking.deskId}`, status: deskBooking.status } : null} />
        <BookingCard icon="🍽" title="Pranzo" to="/lunch" booking={lunchBooking ? { label: lunchBooking.isLunchBox ? 'Lunch Box' : 'Ristorante', status: lunchBooking.status } : null} />
      </div>
    </div>
  )
}

function TeamAvailabilityPanel({ data, loading, error }: { data?: TeamAvailabilityMatch; loading: boolean; error: boolean }) {
  return <section aria-labelledby="team-availability" className="overflow-hidden rounded-2xl border border-indigo-100 bg-white shadow-sm">
    <div className="bg-indigo-600 px-5 py-4 text-white">
      <div className="flex items-start justify-between gap-4">
        <div><p className="text-[11px] font-bold uppercase tracking-[0.16em] text-indigo-200">Work location Teams</p>
          <h2 id="team-availability" className="mt-1 text-lg font-semibold">{data?.currentLocationLabel ?? 'Team in sede'}</h2></div>
        {data && <div className="rounded-xl bg-white/15 px-3 py-2 text-center"><strong className="block text-2xl leading-none">{data.matchingMembers}</strong><span className="text-[10px] font-bold uppercase tracking-wide">match</span></div>}
      </div>
    </div>
    <div className="p-4">
      {loading && <p className="text-sm text-text-muted">Confronto sede Teams e calendario…</p>}
      {error && <p role="alert" className="text-sm text-danger">Non è possibile leggere la disponibilità mock del team.</p>}
      {data && <ul className="space-y-2">{data.members.map(member => <li key={member.userId} className="flex items-center gap-3 rounded-xl bg-slate-50 px-3 py-2.5">
        <span className={`h-2.5 w-2.5 rounded-full ${member.isMatch ? 'bg-success' : statusColor(member.calendarStatus)}`} aria-hidden="true" />
        <div className="min-w-0 flex-1"><p className="truncate text-sm font-semibold text-text">{member.displayName}</p><p className="truncate text-xs text-text-muted">{member.reason}</p></div>
        <span className={`rounded-full px-2 py-1 text-[10px] font-bold uppercase tracking-wide ${member.isMatch ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-200 text-slate-600'}`}>{memberBadge(member)}</span>
      </li>)}</ul>}
      <p className="mt-3 text-[11px] leading-4 text-text-muted">Spotly usa solo sede lavorativa e free/busy. Titoli e partecipanti degli eventi non vengono letti.</p>
    </div>
  </section>
}

function statusColor(status: CalendarAvailability) {
  return status === 'busy' || status === 'outOfOffice' ? 'bg-red-400' : status === 'tentative' ? 'bg-warning' : 'bg-slate-400'
}

function calendarLabel(status: CalendarAvailability) {
  return ({ free: 'Libero', tentative: 'Forse', busy: 'Occupato', outOfOffice: 'Assente', unknown: 'N/D' } as const)[status]
}

function memberBadge(member: TeamMemberMatch) {
  if (member.isMatch) return 'Match'
  if (member.workMode === 'remote') return 'Remoto'
  if (member.reason === 'Sede Teams diversa') return 'Altra sede'
  return calendarLabel(member.calendarStatus)
}

interface BookingCardProps { icon: string; title: string; to: string; booking: { label: string; status: string } | null }

function BookingCard({ icon, title, to, booking }: BookingCardProps) {
  return <Link to={to} className="group flex items-center justify-between rounded-xl border border-border bg-white p-4 transition-colors hover:border-primary">
    <div className="flex items-center gap-3"><span className="text-2xl">{icon}</span><div><p className="font-medium text-text">{title}</p>
      {booking ? <p className="text-sm font-medium text-success">{booking.label}</p> : <p className="text-sm text-text-muted">Nessuna prenotazione</p>}</div></div>
    <span className="text-text-muted transition-colors group-hover:text-primary">→</span>
  </Link>
}
