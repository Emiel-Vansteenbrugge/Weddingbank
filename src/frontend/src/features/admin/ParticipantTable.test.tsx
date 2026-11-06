import { renderToStaticMarkup } from 'react-dom/server'
import { expect, test } from 'vitest'
import { ParticipantTable } from './AdminPages.tsx'

const game = {
  status: 'RUNNING',
  juul: {
    login: 'Juul-V',
    displayName: 'Juul',
    stage: 'Not started',
    completedTasks: 0,
    availableTasks: 0,
    hintsSent: 0,
    lastActivity: null,
  },
  amber: {
    login: 'Amber-D',
    displayName: 'Amber',
    stage: 'Not started',
    completedTasks: 0,
    availableTasks: 0,
    hintsSent: 0,
    lastActivity: null,
  },
  shared: { stage: 'In progress', completedTasks: 0, availableTasks: 1, hintsSent: 0 },
  outstandingSubmissions: 0,
  pendingPhoneRequests: 0,
}

test('each participant can be opened from the administrator list', () => {
  const html = renderToStaticMarkup(<ParticipantTable game={game} onOpen={() => {}} />)
  expect(html).toContain('Open as Juul')
  expect(html).toContain('Open as Amber')
  expect(html).not.toContain('password')
})
