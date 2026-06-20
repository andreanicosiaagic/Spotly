import { useEffect, useRef } from 'react'
import * as signalR from '@microsoft/signalr'
import type { AvailabilityUpdate, RestaurantAvailabilityUpdate, RestaurantMessageEvent } from '../types'

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

export function useRestaurantSignalR(
  date: string,
  onAvailability: (update: RestaurantAvailabilityUpdate) => void,
  onMessage: (event: RestaurantMessageEvent) => void,
) {
  useEffect(() => {
    const apiUrl = import.meta.env.VITE_API_URL ?? ''
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${apiUrl}/availability`)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build()
    connection.on('RestaurantAvailabilityChanged', onAvailability)
    connection.on('RestaurantMessageReceived', onMessage)
    connection.start()
      .then(() => connection.invoke('JoinGroup', 'HQ', date))
      .catch(() => undefined)
    return () => { void connection.stop() }
  }, [date, onAvailability, onMessage])
}
