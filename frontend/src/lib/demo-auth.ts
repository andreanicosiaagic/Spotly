import { DEMO_PROFILES, type DemoProfile } from '../mocks/data/users'

export const DEMO_PROFILE_STORAGE_KEY = 'spotly.demo-profile'

export function getDemoProfile(profileId?: string | null): DemoProfile {
  return DEMO_PROFILES.find(profile => profile.id === profileId) ?? DEMO_PROFILES[0]
}

export function toDevAccessToken(profile: DemoProfile): string {
  const payload = {
    sub: profile.oid,
    name: profile.name,
    role: profile.roles[0],
    department: profile.department,
    eligibility: profile.parkingEligibility ?? [],
  }
  const encoded = btoa(JSON.stringify(payload)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/u, '')
  return `dev.${encoded}`
}

export function readStoredDemoProfileId(): string | null {
  return typeof window === 'undefined' ? null : window.localStorage.getItem(DEMO_PROFILE_STORAGE_KEY)
}

export function storeDemoProfileId(profileId: string) {
  if (typeof window !== 'undefined') window.localStorage.setItem(DEMO_PROFILE_STORAGE_KEY, profileId)
}
