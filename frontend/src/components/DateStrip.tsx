import { formatDateKey, formatDateLabel, parseDateKey } from '../lib/date'
import { useBookingStore } from '../store/bookingStore'

const dayLabels = ['DOM', 'LUN', 'MAR', 'MER', 'GIO', 'VEN', 'SAB']

export function DateStrip() {
  const { selectedDate, setSelectedDate } = useBookingStore()
  const center = parseDateKey(selectedDate)
  const days = Array.from({ length: 7 }, (_, index) => {
    const date = new Date(center)
    date.setDate(center.getDate() + index - 2)
    return date
  })
  return <div className="date-strip" aria-label="Seleziona giorno">
    {days.map(date => {
      const value = formatDateKey(date)
      const active = value === selectedDate
      return <button key={value} onClick={() => setSelectedDate(value)} aria-pressed={active}
        aria-label={formatDateLabel(value, { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })}
        data-date={value}
        className={`date-chip ${active ? 'date-chip-active' : ''}`}>
        <span>{dayLabels[date.getDay()]}</span><strong>{date.getDate()}</strong>
      </button>
    })}
  </div>
}
