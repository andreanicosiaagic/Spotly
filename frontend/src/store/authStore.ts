import { create } from 'zustand'
import type { User } from '../types'
import { DEMO_PROFILES, type DemoProfile } from '../mocks/data/users'
import { getDemoProfile, readStoredDemoProfileId, storeDemoProfileId } from '../lib/demo-auth'

interface AuthState {
  profile: DemoProfile
  user: User | null
  initialized: boolean
  setInitialized: (value: boolean) => void
  setUser: (user: User | null) => void
  setProfileId: (profileId: string) => void
}

export const useAuthStore = create<AuthState>((set) => ({
  profile: getDemoProfile(readStoredDemoProfileId() ?? DEMO_PROFILES[1]?.id ?? DEMO_PROFILES[0].id),
  user: null,
  initialized: false,
  setInitialized: (initialized) => set({ initialized }),
  setUser: (user) => set({ user }),
  setProfileId: (profileId) => set((state) => {
    const profile = getDemoProfile(profileId)
    storeDemoProfileId(profile.id)
    return {
      ...state,
      profile,
      initialized: false,
      user: null,
    }
  }),
}))
