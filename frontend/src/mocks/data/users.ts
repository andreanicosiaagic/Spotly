import type { User } from '../../types'

export const SEED_USERS: User[] = [
  { oid: 'u1', name: 'Giulia Romano',  email: 'giulia@spotly.test', roles: ['Dipendente'] },
  { oid: 'u2', name: 'Marco Bianchi',  email: 'marco@spotly.test',  roles: ['Manager'] },
  { oid: 'u3', name: 'Sara Conti',     email: 'sara@spotly.test',   roles: ['Facility'] },
  { oid: 'u4', name: 'Admin Spotly',   email: 'admin@spotly.test',  roles: ['Admin'] },
]

// Default user for local dev (simulates Easy Auth header)
export const DEV_USER = SEED_USERS[0]
