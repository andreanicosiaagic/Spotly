import { useBookingStore } from '../store/bookingStore'
import { useAuth } from '../hooks/useAuth'
import { Link } from 'react-router'

export default function DashboardPage() {
  const { user } = useAuth()
  const { selectedDate, setSelectedDate, parkingBooking, deskBooking, lunchBooking } = useBookingStore()

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold text-text">La mia giornata</h1>
        <p className="text-text-muted text-sm mt-1">Ciao, {user?.name ?? 'Utente'}</p>
      </div>

      <div className="flex items-center gap-3">
        <label className="text-sm font-medium text-text-muted">Data</label>
        <input
          type="date"
          value={selectedDate}
          onChange={e => setSelectedDate(e.target.value)}
          className="border border-border rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-primary"
        />
      </div>

      <div className="grid gap-4">
        <BookingCard
          icon="🚗"
          title="Parcheggio"
          to="/parking"
          booking={parkingBooking
            ? { label: `Posto ${parkingBooking.spotId}`, status: parkingBooking.status }
            : null}
        />
        <BookingCard
          icon="💼"
          title="Postazione"
          to="/desk"
          booking={deskBooking
            ? { label: `Desk ${deskBooking.deskId}`, status: deskBooking.status }
            : null}
        />
        <BookingCard
          icon="🍽"
          title="Pranzo"
          to="/lunch"
          booking={lunchBooking
            ? { label: lunchBooking.isLunchBox ? 'Lunch Box' : `Ristorante`, status: lunchBooking.status }
            : null}
        />
      </div>
    </div>
  )
}

interface BookingCardProps {
  icon: string
  title: string
  to: string
  booking: { label: string; status: string } | null
}

function BookingCard({ icon, title, to, booking }: BookingCardProps) {
  return (
    <Link
      to={to}
      className="bg-white rounded-xl border border-border p-4 flex items-center justify-between hover:border-primary transition-colors group"
    >
      <div className="flex items-center gap-3">
        <span className="text-2xl">{icon}</span>
        <div>
          <p className="font-medium text-text">{title}</p>
          {booking ? (
            <p className="text-sm text-success font-medium">{booking.label}</p>
          ) : (
            <p className="text-sm text-text-muted">Nessuna prenotazione</p>
          )}
        </div>
      </div>
      <span className="text-text-muted group-hover:text-primary transition-colors">→</span>
    </Link>
  )
}
