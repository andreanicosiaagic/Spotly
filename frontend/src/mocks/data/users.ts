import type { User } from '../../types'

export interface DemoProfile extends User {
  id: string
  department?: string
  parkingEligibility?: string[]
}

export const DEMO_PROFILES: DemoProfile[] = [
  { id: 'employee', oid: 'u1', name: 'Giulia Romano', email: 'giulia@spotly.test', roles: ['Dipendente'], department: 'Product' },
  { id: 'manager', oid: 'u2', name: 'Marco Bianchi', email: 'marco@spotly.test', roles: ['Manager'], department: 'Engineering' },
  { id: 'facility', oid: 'u3', name: 'Sara Conti', email: 'sara@spotly.test', roles: ['Facility'], department: 'Facility', parkingEligibility: ['guest', 'ev'] },
  { id: 'admin', oid: 'u4', name: 'Admin Spotly', email: 'admin@spotly.test', roles: ['Admin'], department: 'Operations', parkingEligibility: ['guest', 'ev', 'disabled'] },
]
