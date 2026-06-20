import { setupServer } from 'msw/node'
import { parkingHandlers } from './handlers/parking.handlers'
import { deskHandlers } from './handlers/desk.handlers'
import { lunchHandlers } from './handlers/lunch.handlers'

export const server = setupServer(
  ...parkingHandlers,
  ...deskHandlers,
  ...lunchHandlers
)
