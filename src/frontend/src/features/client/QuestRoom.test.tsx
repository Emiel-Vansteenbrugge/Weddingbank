import { renderToStaticMarkup } from 'react-dom/server'
import { expect, test } from 'vitest'
import { QuestCatalogue, SubmissionTable } from '../admin/QuestDesk.tsx'
import { QuestReading } from './QuestRoom.tsx'

const quest = {
  id: '33333333-3333-3333-3333-333333333333',
  reference: 'CH-I-QUEST-001',
  title: 'The Missing Record',
  description: 'An inconsistency has been identified.',
  instructions: 'Use the October 2026 Closing Register.',
  caseId: '11111111-1111-1111-1111-111111111111',
  caseReference: 'CH-I-001',
  caseTitle: 'The Account',
  sharedCompleted: 0,
  sharedOpen: 1,
  tasks: [
    {
      id: '44444444-4444-4444-4444-444444444444',
      reference: 'CH-I-TASK-001',
      title: 'Identify the Registry Entry',
      instructions: 'Submit the reference number of the registry entry.',
      shared: true,
      completed: false,
      successMessage: null,
    },
  ],
}

test('a participant quest shows the task and an answer field without the accepted answer', () => {
  const html = renderToStaticMarkup(<QuestReading quest={quest} onSubmit={() => {}} />)
  expect(html).toContain('The Missing Record')
  expect(html).toContain('Identify the Registry Entry')
  expect(html).toContain('Held for both account holders.')
  expect(html).toContain('Shared progress: 0 completed, 1 open.')
  expect(html).toContain('name="answer"')
  expect(html).not.toContain('REG-1026-17')
  expect(html).not.toContain('MAIN')
  expect(html).not.toContain('SIDE')
})

test('a completed shared task shows the confirmation and no further answer field', () => {
  const html = renderToStaticMarkup(
    <QuestReading
      quest={{
        ...quest,
        sharedCompleted: 1,
        sharedOpen: 0,
        tasks: [
          {
            ...quest.tasks[0],
            completed: true,
            successMessage: 'The registry entry has been identified. The associated record is now available for examination.',
          },
        ],
      }}
    />,
  )
  expect(html).toContain('The registry entry has been identified.')
  expect(html).toContain('Completed')
  expect(html).not.toContain('name="answer"')
  expect(html).not.toContain('REG-1026-17')
})

test('the quest desk shows scope, status and separate progress', () => {
  const html = renderToStaticMarkup(
    <QuestCatalogue
      quests={[
        {
          id: '33333333-3333-3333-3333-333333333333',
          reference: 'CH-I-QUEST-001',
          title: 'The Missing Record',
          status: 'PUBLISHED',
          audience: 'BOTH',
          kind: 'MAIN',
          caseId: '11111111-1111-1111-1111-111111111111',
          caseReference: 'CH-I-001',
          displayOrder: 0,
          taskCount: 1,
          completedAssignments: 0,
          openAssignments: 1,
          updatedAt: '2026-11-05T00:00:00Z',
        },
      ]}
    />,
  )
  expect(html).toContain('CH-I-QUEST-001')
  expect(html).toContain('Both')
  expect(html).toContain('Main')
  expect(html).toContain('Published')
  expect(html).toContain('0 completed, 1 open')
  expect(html).toContain('href="/admin/tasks/33333333-3333-3333-3333-333333333333"')
})

test('submission history names the participant, the answer and the outcome', () => {
  const html = renderToStaticMarkup(
    <SubmissionTable
      rows={[
        {
          id: '55555555-5555-5555-5555-555555555555',
          taskId: '44444444-4444-4444-4444-444444444444',
          taskReference: 'CH-I-TASK-001',
          scope: 'SHARED',
          assignedTo: null,
          submittedBy: 'Juul-V',
          answer: 'REG-1026-14',
          outcome: 'REFUSED',
          submittedAt: '2026-11-05T12:00:00Z',
        },
      ]}
    />,
  )
  expect(html).toContain('CH-I-TASK-001')
  expect(html).toContain('Shared')
  expect(html).toContain('Juul-V')
  expect(html).toContain('REG-1026-14')
  expect(html).toContain('Rejected')
})
