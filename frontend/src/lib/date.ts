export function formatDateKey(date: Date): string {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

export function getTodayDateKey(): string {
  return formatDateKey(new Date())
}

export function parseDateKey(dateKey: string): Date {
  const [year, month, day] = dateKey.split('-').map(Number)
  return new Date(year, month - 1, day, 12, 0, 0, 0)
}

export function shiftDateKey(dateKey: string, offsetDays: number): string {
  const value = parseDateKey(dateKey)
  value.setDate(value.getDate() + offsetDays)
  return formatDateKey(value)
}

export function formatDateLabel(dateKey: string, options: Intl.DateTimeFormatOptions): string {
  return parseDateKey(dateKey).toLocaleDateString('it-IT', options)
}

export function isToday(dateKey: string): boolean {
  return dateKey === getTodayDateKey()
}
