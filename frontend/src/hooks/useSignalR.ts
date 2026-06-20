import { useEffect, useRef, useState } from 'react'
import * as signalR from '@microsoft/signalr'
import { apiUrl, getAuthToken } from '../lib/api'
import type {
  AvailabilityUpdate,
  RealtimeStatus,
  RestaurantAvailabilityUpdate,
  RestaurantMessageEvent,
} from '../types'

export function useSignalR(
  locationId: string,
  date: string,
  onUpdate: (update: AvailabilityUpdate) => void,
) {
  const connectionRef = useRef<signalR.HubConnection | null>(null)
  const [status, setStatus] = useState<RealtimeStatus>('connecting')

  useEffect(() => {
    const connection = buildConnection()
    connectionRef.current = connection
    connection.on('ResourceStatusChanged', onUpdate)
    connection.onreconnecting(() => setStatus('reconnecting'))
    connection.onreconnected(async () => {
      setStatus('connected')
      await connection.invoke('JoinGroup', locationId, date)
    })
    connection.onclose(() => setStatus('offline'))

    void connection
      .start()
      .then(async () => {
        setStatus('connected')
        await connection.invoke('JoinGroup', locationId, date)
      })
      .catch(() => setStatus('offline'))

    return () => {
      void connection.stop()
    }
  }, [date, locationId, onUpdate])

  return { connectionRef, status }
}

export function useRestaurantSignalR(
  date: string,
  onAvailability: (update: RestaurantAvailabilityUpdate) => void,
  onMessage: (event: RestaurantMessageEvent) => void,
) {
  const [status, setStatus] = useState<RealtimeStatus>('connecting')

  useEffect(() => {
    const connection = buildConnection()
    connection.on('RestaurantAvailabilityChanged', (update: RestaurantAvailabilityUpdate) => {
      if (update.bookingDate === date) onAvailability(update)
    })
    connection.on('RestaurantMessageReceived', (event: RestaurantMessageEvent) => {
      if (event.bookingDate === date) onMessage(event)
    })
    connection.onreconnecting(() => setStatus('reconnecting'))
    connection.onreconnected(async () => {
      setStatus('connected')
      await connection.invoke('JoinGroup', 'HQ', date)
    })
    connection.onclose(() => setStatus('offline'))

    void connection
      .start()
      .then(async () => {
        setStatus('connected')
        await connection.invoke('JoinGroup', 'HQ', date)
      })
      .catch(() => setStatus('offline'))

    return () => {
      void connection.stop()
    }
  }, [date, onAvailability, onMessage])

  return status
}

function buildConnection() {
  return new signalR.HubConnectionBuilder()
    .withUrl(apiUrl('/availability'), {
      accessTokenFactory: () => getAuthToken() ?? '',
    })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Warning)
    .build()
}
