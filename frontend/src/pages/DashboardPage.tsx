import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import { AppIcon } from '../components/AppIcon'
import { DateStrip } from '../components/DateStrip'
import { getJson } from '../lib/api'
import { formatDateLabel } from '../lib/date'
import { useBookingStore } from '../store/bookingStore'
import { useAuth } from '../hooks/useAuth'
import { useDeskBooking, useLunchBooking, useParkingBooking } from '../hooks/use-bookings'
import type { CalendarAvailability, TeamAvailabilityMatch, TeamMemberMatch } from '../types'

export default function DashboardPage() {
  const { user } = useAuth()
  const { selectedDate } = useBookingStore()
  const parkingBooking = useParkingBooking(selectedDate)
  const deskBooking = useDeskBooking(selectedDate)
  const lunchBooking = useLunchBooking(selectedDate)
  const canSeeTeam = user?.roles.some(role => ['Manager', 'Facility', 'Admin'].includes(role)) ?? false
  const teamQuery = useQuery<TeamAvailabilityMatch>({
    queryKey: ['team-availability', selectedDate],
    queryFn: () => getJson<TeamAvailabilityMatch>(`/api/collaboration/team-match?date=${selectedDate}`),
    enabled: canSeeTeam,
  })

  return <div>
    <div className="page-mobile-heading">
      <p className="!mb-1 !text-[var(--c-a89e92)]">{formatDateLabel(selectedDate, { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })}</p>
      <h1>Ciao, {user?.name.split(' ')[0] ?? 'Utente'}</h1>
    </div>
    <div className="mb-5 lg:hidden"><DateStrip /></div>

    <div className="mb-5 flex items-center justify-between rounded-[20px] bg-[var(--c-2b2622)] px-[18px] py-4 text-white lg:hidden">
      <div><p className="m-0 text-[12px] font-semibold text-[var(--c-c9bdab)]">La tua giornata</p><strong className="font-display mt-0.5 block text-[19px]">Organizza le prenotazioni</strong></div>
      <div className="flex gap-1.5"><i className="h-2.5 w-2.5 rounded-full bg-[var(--c-ec6a4d)]" /><i className="h-2.5 w-2.5 rounded-full bg-[var(--c-665f58)]" /><i className="h-2.5 w-2.5 rounded-full bg-[var(--c-665f58)]" /></div>
    </div>

    <div className="grid items-start gap-[22px] lg:grid-cols-[1fr_340px]">
      <div>
        <p className="section-label lg:hidden">PRENOTAZIONI</p>
        <div className="grid gap-3 lg:grid-cols-3 lg:gap-4">
          <BookingCard icon="directions_car" tone="blue" title="Parcheggio" to="/parking"
            booking={parkingBooking.data ? `Posto ${parkingBooking.data.spotId}` : null} />
          <BookingCard icon="chair" tone="green" title="Postazione" to="/desk"
            booking={deskBooking.data ? `Desk ${deskBooking.data.deskId}` : null} />
          <BookingCard icon="restaurant" tone="coral" title="Pranzo" to="/lunch"
            booking={lunchBooking.data ? (lunchBooking.data.isLunchBox ? 'Lunch Box' : 'Ristorante') : null} />
        </div>
        {!parkingBooking.data && <div className="spotly-alert mt-4 flex items-center gap-2.5 border border-[var(--c-f3c9bc)] bg-[var(--c-fbe7e1)] text-[var(--c-a8432c)]">
          <AppIcon name="error" className="text-[21px]" /><span>Prenota il parcheggio in anticipo per assicurarti un posto.</span>
        </div>}
      </div>
      {canSeeTeam && <TeamPanel data={teamQuery.data} loading={teamQuery.isLoading} error={teamQuery.isError} />}
    </div>
  </div>
}

function BookingCard({ icon, tone, title, to, booking }: { icon: string; tone: 'blue' | 'green' | 'coral'; title: string; to: string; booking: string | null }) {
  const tones = { blue: 'bg-[var(--c-e9f0fb)] text-[var(--c-3e6bb0)]', green: 'bg-[var(--c-e7f3ec)] text-[var(--c-2f8a5c)]', coral: 'bg-[var(--c-fcede7)] text-[var(--c-ec6a4d)]' }
  return <Link to={to} className="spotly-card group flex items-center gap-3 p-[15px] no-underline transition hover:-translate-y-0.5 hover:shadow-md lg:block lg:p-5">
    <div className={`grid h-12 w-12 flex-none place-items-center rounded-[14px] lg:h-[52px] lg:w-[52px] lg:rounded-[15px] ${tones[tone]}`}><AppIcon name={icon} filled className="text-[27px]" /></div>
    <div className="min-w-0 flex-1 lg:mt-3"><h2 className="m-0 text-[15px] font-bold text-text lg:text-base">{title}</h2>
      <p className={`mt-1 mb-0 truncate text-[12px] lg:min-h-9 lg:whitespace-normal lg:text-[13px] ${booking ? 'font-semibold text-success' : 'text-[var(--c-726a60)]'}`}>{booking ?? 'Nessuna prenotazione'}</p>
      <span className={`mt-2 hidden rounded-lg px-2.5 py-1 text-[10px] font-extrabold lg:inline-block ${booking ? 'bg-[var(--c-e7f3ec)] text-[var(--c-2f8a5c)]' : 'bg-[var(--c-f2ede4)] text-[var(--c-928879)]'}`}>{booking ? 'CONFERMATA' : 'DA PRENOTARE'}</span>
    </div><AppIcon name="chevron_right" className="text-[22px] text-[var(--c-c9bdab)] lg:hidden" />
  </Link>
}

function TeamPanel({ data, loading, error }: { data?: TeamAvailabilityMatch; loading: boolean; error: boolean }) {
  const matching = data?.members.filter(member => member.isMatch) ?? []
  return <aside aria-label="Team Product">
    <div className="rounded-[20px] border border-[var(--c-f6dfd3)] bg-[var(--c-fff7f2)] p-5">
      <div className="flex items-center justify-between"><h2 className="m-0 text-base font-bold">Team Product</h2>
        {data && <span className="rounded-lg bg-[var(--c-e7f3ec)] px-2.5 py-1 text-[11px] font-bold text-[var(--c-2f8a5c)]">{data.matchingMembers} in sede</span>}
      </div>
      {loading && <p className="text-sm text-text-muted">Confronto Teams e calendario…</p>}
      {error && <p className="text-sm text-danger">Disponibilità team non disponibile.</p>}
      {data && <>
        <div className="my-4 flex pl-2">{matching.map((member, index) => <div key={member.userId} title={member.displayName}
          className="-ml-2.5 grid h-10 w-10 place-items-center rounded-full border-[2.5px] border-[var(--c-fff7f2)] bg-[var(--c-ec6a4d)] text-xs font-bold text-white"
          style={{ background: ['var(--c-ec6a4d)', 'var(--c-3e6bb0)', 'var(--c-2f8a5c)'][index % 3] }}>{initials(member.displayName)}</div>)}</div>
        <div className="space-y-2">{data.members.slice(0, 4).map(member => <div key={member.userId} className="flex items-center gap-2 text-xs">
          <i className={`h-2 w-2 rounded-full ${member.isMatch ? 'bg-[var(--c-2f8a5c)]' : 'bg-[var(--c-c9bdab)]'}`} /><span className="font-semibold">{member.displayName}</span><span className="ml-auto text-text-muted">{memberBadge(member)}</span>
        </div>)}</div>
        <button disabled className="mt-4 flex w-full items-center justify-center gap-2 rounded-[13px] border-0 bg-[var(--c-d7d2c7)] px-3 py-3 text-sm font-bold text-[var(--c-6f6659)]"><AppIcon name="groups" />Prenota per il team · demo futura</button>
      </>}
    </div>
    <div className="mt-4 flex gap-2.5 rounded-2xl bg-[var(--c-f2ede4)] p-4 text-[13px] leading-5 text-[var(--c-5c544a)]"><AppIcon name="tips_and_updates" className="text-[22px] text-[var(--c-b07d22)]" /><span>Spotly combina la sede impostata su Teams con il calendario free/busy.</span></div>
  </aside>
}

function initials(name: string) { return name.split(' ').map(part => part[0]).slice(0, 2).join('') }
function calendarLabel(status: CalendarAvailability) { return ({ free: 'Libero', tentative: 'Forse', busy: 'Occupato', outOfOffice: 'Assente', unknown: 'N/D' } as const)[status] }
function memberBadge(member: TeamMemberMatch) { if (member.isMatch) return 'Match'; if (member.workMode === 'remote') return 'Remoto'; if (member.reason === 'Sede Teams diversa') return 'Altra sede'; return calendarLabel(member.calendarStatus) }
