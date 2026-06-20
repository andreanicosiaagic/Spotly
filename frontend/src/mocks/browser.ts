import { setupWorker } from 'msw/browser'
import { parkingHandlers } from './handlers/parking.handlers'
import { deskHandlers } from './handlers/desk.handlers'
import { lunchHandlers } from './handlers/lunch.handlers'

export const worker = setupWorker(
  ...parkingHandlers,
  ...deskHandlers,
  ...lunchHandlers
)
