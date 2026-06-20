import { create } from 'zustand'
import type { User } from '../types'
import { DEV_USER } from '../mocks/data/users'

interface AuthState {
  user: User | null
  setUser: (user: User | null) => void
}

export const useAuthStore = create<AuthState>((set) => ({
  // In dev, use the seed user; in prod Easy Auth provides the identity
  user: DEV_USER,
  setUser: (user) => set({ user }),
}))
