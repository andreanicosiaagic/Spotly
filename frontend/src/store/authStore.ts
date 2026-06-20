import { create } from 'zustand'
import type { User } from '../types'
import { DEMO_PROFILES, type DemoProfile } from '../mocks/data/users'
import { getDemoProfile, readStoredDemoProfileId, storeDemoProfileId, readDemoSession, storeDemoSession } from '../lib/demo-auth'

interface AuthState {
  profile: DemoProfile
  user: User | null
  initialized: boolean
  setInitialized: (value: boolean) => void
  setUser: (user: User | null) => void
  setProfileId: (profileId: string) => void
  /** Fake "Entra" login: enter the app as the given demo profile. */
  login: (profileId: string) => void
  /** Return to the login screen. */
  logout: () => void
}

const initialProfile = getDemoProfile(readStoredDemoProfileId() ?? DEMO_PROFILES[1]?.id ?? DEMO_PROFILES[0].id)
const restoredSession = readDemoSession()

export const useAuthStore = create<AuthState>((set) => ({
  profile: initialProfile,
  user: restoredSession ? initialProfile : null,
  initialized: restoredSession,
  setInitialized: (initialized) => set({ initialized }),
  setUser: (user) => set({ user }),
  setProfileId: (profileId) => set(() => {
    const profile = getDemoProfile(profileId)
    storeDemoProfileId(profile.id)
    return { profile, initialized: false, user: null }
  }),
  login: (profileId) => set(() => {
    const profile = getDemoProfile(profileId)
    storeDemoProfileId(profile.id)
    storeDemoSession(true)
    return { profile, user: profile, initialized: true }
  }),
  logout: () => set(() => {
    storeDemoSession(false)
    return { user: null, initialized: false }
  }),
}))
