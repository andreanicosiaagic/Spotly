import { http, HttpResponse } from 'msw'

const BASE = import.meta.env.DEV && import.meta.env.VITE_USE_DIRECT_API !== 'true'
  ? ''
  : (import.meta.env.VITE_API_URL ?? '')

export const collaborationHandlers = [
  http.get(`${BASE}/api/collaboration/team-match`, ({ request }) => {
    const date = new URL(request.url).searchParams.get('date') ?? new Date().toISOString().split('T')[0]
    return HttpResponse.json({
      date,
      windowStartUtc: '09:00:00',
      windowEndUtc: '17:00:00',
      currentLocationId: 'HQ',
      currentLocationLabel: 'Milano HQ',
      matchingMembers: 2,
      members: [
        { userId: 'u1', displayName: 'Giulia Romano', workMode: 'office', locationId: 'HQ', locationLabel: 'Milano HQ', calendarStatus: 'free', isMatch: true, reason: 'Stessa sede e calendario libero' },
        { userId: 'u6', displayName: 'Paolo Riva', workMode: 'office', locationId: 'HQ', locationLabel: 'Milano HQ', calendarStatus: 'tentative', isMatch: true, reason: 'Stessa sede, disponibilità provvisoria' },
        { userId: 'u4', displayName: 'Luca Ferri', workMode: 'office', locationId: 'HQ', locationLabel: 'Milano HQ', calendarStatus: 'busy', isMatch: false, reason: 'Calendario occupato' },
        { userId: 'u3', displayName: 'Sara Conti', workMode: 'remote', locationLabel: 'Da remoto', calendarStatus: 'free', isMatch: false, reason: 'Non lavora in sede' },
        { userId: 'u5', displayName: 'Elena Greco', workMode: 'office', locationId: 'ROMA', locationLabel: 'Roma EUR', calendarStatus: 'free', isMatch: false, reason: 'Sede Teams diversa' },
      ],
    })
  }),
]
