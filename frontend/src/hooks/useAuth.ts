import { useAuthStore } from '../store/authStore'
import type { UserRole } from '../types'

export function useAuth() {
  const { user, setUser } = useAuthStore()

  const hasRole = (role: UserRole) => user?.roles.includes(role) ?? false
  const isManager = () => hasRole('Manager') || hasRole('Facility') || hasRole('Admin')
  const isFacility = () => hasRole('Facility') || hasRole('Admin')
  const isAdmin = () => hasRole('Admin')

  return { user, setUser, hasRole, isManager, isFacility, isAdmin }
}
