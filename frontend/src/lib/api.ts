import { useAuthStore } from '../store/authStore'
import type { User } from '../types'
import { toDevAccessToken } from './demo-auth'

const API = import.meta.env.DEV && import.meta.env.VITE_USE_DIRECT_API !== 'true'
  ? ''
  : (import.meta.env.VITE_API_URL ?? '')

export function apiUrl(path: string): string {
  return `${API}${path}`
}

export function getAuthToken(): string | null {
  const { profile } = useAuthStore.getState()
  return profile ? toDevAccessToken(profile) : null
}

export function buildAuthHeaders(init?: HeadersInit): Headers {
  const headers = new Headers(init)
  const token = getAuthToken()
  if (token) headers.set('Authorization', `Bearer ${token}`)
  return headers
}

export async function apiFetch(input: string, init?: RequestInit): Promise<Response> {
  return fetch(apiUrl(input), {
    ...init,
    headers: buildAuthHeaders(init?.headers),
  })
}

export async function readJson<T>(response: Response): Promise<T> {
  if (response.status === 204) return undefined as T

  const text = await response.text()
  const data = text ? JSON.parse(text) as unknown : undefined
  if (!response.ok) throw new Error(readErrorMessage(data))
  return data as T
}

export async function getJson<T>(input: string): Promise<T> {
  const response = await apiFetch(input)
  return readJson<T>(response)
}

export async function getOptionalJson<T>(input: string): Promise<T | null> {
  const response = await apiFetch(input)
  if (response.status === 404) return null
  return readJson<T>(response)
}

export async function postJson<T>(input: string, body?: unknown, init?: Omit<RequestInit, 'body' | 'method'>): Promise<T> {
  const response = await apiFetch(input, {
    ...init,
    method: 'POST',
    headers: buildAuthHeaders({ 'Content-Type': 'application/json', ...(init?.headers ?? {}) }),
    body: body === undefined ? undefined : JSON.stringify(body),
  })
  return readJson<T>(response)
}

export async function deleteJson<T>(input: string): Promise<T> {
  const response = await apiFetch(input, { method: 'DELETE' })
  return readJson<T>(response)
}

export async function fetchCurrentUser(): Promise<User> {
  return getJson<User>('/api/me')
}

function readErrorMessage(data: unknown): string {
  if (!data || typeof data !== 'object') return 'Operazione non riuscita'
  const record = data as Record<string, unknown>
  if (typeof record.detail === 'string') return record.detail
  if (typeof record.error === 'string') return record.error
  if (typeof record.title === 'string') return record.title
  if (record.errors && typeof record.errors === 'object') {
    const firstError = Object.values(record.errors as Record<string, unknown>)
      .flatMap(value => Array.isArray(value) ? value : [])
      .find(value => typeof value === 'string')
    if (typeof firstError === 'string') return firstError
  }
  return 'Operazione non riuscita'
}
