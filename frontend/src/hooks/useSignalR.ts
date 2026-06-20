import { useEffect, useRef } from 'react'
import * as signalR from '@microsoft/signalr'
import type { AvailabilityUpdate } from '../types'

export function useSignalR(
  sedeId: string,
  date: string,
  onUpdate: (update: AvailabilityUpdate) => void
) {
  const connectionRef = useRef<signalR.HubConnection | null>(null)

  useEffect(() => {
    const apiUrl = import.meta.env.VITE_API_URL ?? ''
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${apiUrl}/availability`)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    connectionRef.current = connection

    connection.on('ResourceStatusChanged', (update: AvailabilityUpdate) => {
      onUpdate(update)
    })

    connection
      .start()
      .then(() => connection.invoke('JoinGroup', sedeId, date))
      .catch(() => {
        // SignalR not available in dev with MSW — silent fail
      })

    return () => {
      connection.stop()
    }
  }, [sedeId, date, onUpdate])

  return connectionRef
}
