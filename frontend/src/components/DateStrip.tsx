import { useBookingStore } from '../store/bookingStore'

const dayLabels = ['DOM', 'LUN', 'MAR', 'MER', 'GIO', 'VEN', 'SAB']

export function DateStrip() {
  const { selectedDate, setSelectedDate } = useBookingStore()
  const center = new Date(`${selectedDate}T12:00:00`)
  const days = Array.from({ length: 7 }, (_, index) => {
    const date = new Date(center)
    date.setDate(center.getDate() + index - 2)
    return date
  })
  return <div className="date-strip" aria-label="Seleziona giorno">
    {days.map(date => {
      const value = date.toISOString().split('T')[0]
      const active = value === selectedDate
      return <button key={value} onClick={() => setSelectedDate(value)} aria-pressed={active}
        className={`date-chip ${active ? 'date-chip-active' : ''}`}>
        <span>{dayLabels[date.getDay()]}</span><strong>{date.getDate()}</strong>
      </button>
    })}
  </div>
}
