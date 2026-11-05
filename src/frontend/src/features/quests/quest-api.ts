import { mutationHeaders, readFailure } from '../auth/api.ts'
import { audienceLabel, statusLabel } from '../cases/case-api.ts'

export type QuestSummary = {
  id: string
  reference: string
  title: string
  status: string
  audience: string
  kind: string
  caseId: string
  caseReference: string
  displayOrder: number
  taskCount: number
  completedAssignments: number
  openAssignments: number
  updatedAt: string
}

export type UnlockDetail = {
  documentId: string
  reference: string
  title: string
  released: boolean
}

export type TaskDetail = {
  id: string
  reference: string
  title: string
  instructions: string
  taskType: string
  status: string
  scope: string
  participantLogin: string | null
  ignoreCase: boolean
  trimWhitespace: boolean
  successMessage: string | null
  displayOrder: number
  completed: boolean
  answers: string[]
  unlocks: UnlockDetail[]
  updatedAt: string
}

export type QuestDetail = {
  id: string
  reference: string
  title: string
  description: string
  instructions: string
  caseId: string
  caseReference: string
  status: string
  audience: string
  kind: string
  displayOrder: number
  createdAt: string
  updatedAt: string
  tasks: TaskDetail[]
}

export type SubmissionRecord = {
  id: string
  taskId: string
  taskReference: string
  scope: string
  assignedTo: string | null
  submittedBy: string
  answer: string
  outcome: string
  submittedAt: string
}

export type ReadingQuestSummary = {
  id: string
  reference: string
  title: string
  description: string
  openTasks: number
  completedTasks: number
}

export type ReadingTask = {
  id: string
  reference: string
  title: string
  instructions: string
  shared: boolean
  completed: boolean
  successMessage: string | null
}

export type ReadingQuest = {
  id: string
  reference: string
  title: string
  description: string
  instructions: string
  caseId: string
  caseReference: string
  caseTitle: string
  sharedCompleted: number
  sharedOpen: number
  tasks: ReadingTask[]
}

export type AnswerOutcome = {
  accepted: boolean
  completed: boolean
  message: string
}

export type QuestDraft = {
  reference: string
  title: string
  description: string
  instructions: string
  caseId: string
  audience: string
  kind: string
}

export type TaskDraft = {
  reference: string
  title: string
  instructions: string
  taskType: string
  scope: string
  participant: string
  answers: string[]
  ignoreCase: boolean
  trimWhitespace: boolean
  successMessage: string
  unlockDocumentIds: string[]
}

export function kindLabel(kind: string) {
  return kind === 'SIDE' ? 'Side' : 'Main'
}

export function scopeLabel(scope: string, participant?: string | null) {
  if (scope === 'INDIVIDUAL') return audienceLabel(participant === 'Amber-D' ? 'AMBER' : 'JUUL')
  return 'Shared'
}

export function outcomeLabel(outcome: string) {
  if (outcome === 'ACCEPTED') return 'Accepted'
  if (outcome === 'REFUSED') return 'Rejected'
  return 'Waiting'
}

export { statusLabel }

async function readBody<T>(response: Response, fallback: string) {
  if (response.ok) return (await response.json()) as T
  const failure = await readFailure(response)
  const detail = Object.values(failure.errors)[0]
  throw new Error(detail ?? failure.title ?? fallback)
}

export async function fetchQuests() {
  const response = await fetch('/api/admin/quests', { headers: { Accept: 'application/json' } })
  return readBody<QuestSummary[]>(response, 'The quests could not be read.')
}

export async function fetchQuest(id: string) {
  const response = await fetch(`/api/admin/quests/${id}`, { headers: { Accept: 'application/json' } })
  return readBody<QuestDetail>(response, 'That quest could not be read.')
}

export async function createQuest(draft: QuestDraft) {
  const response = await fetch('/api/admin/quests', {
    method: 'POST',
    headers: await mutationHeaders(),
    body: JSON.stringify(draft),
  })
  return readBody<QuestDetail>(response, 'The quest could not be saved.')
}

export async function updateQuest(id: string, draft: QuestDraft) {
  const response = await fetch(`/api/admin/quests/${id}`, {
    method: 'PUT',
    headers: await mutationHeaders(),
    body: JSON.stringify(draft),
  })
  return readBody<QuestDetail>(response, 'The quest could not be saved.')
}

export async function setQuestStatus(id: string, status: string) {
  const response = await fetch(`/api/admin/quests/${id}/status`, {
    method: 'POST',
    headers: await mutationHeaders(),
    body: JSON.stringify({ status }),
  })
  return readBody<QuestDetail>(response, 'The quest status could not be changed.')
}

export async function createTask(questId: string, draft: TaskDraft) {
  const response = await fetch(`/api/admin/quests/${questId}/tasks`, {
    method: 'POST',
    headers: await mutationHeaders(),
    body: JSON.stringify(draft),
  })
  return readBody<TaskDetail>(response, 'The task could not be saved.')
}

export async function updateTask(questId: string, taskId: string, draft: TaskDraft) {
  const response = await fetch(`/api/admin/quests/${questId}/tasks/${taskId}`, {
    method: 'PUT',
    headers: await mutationHeaders(),
    body: JSON.stringify(draft),
  })
  return readBody<TaskDetail>(response, 'The task could not be saved.')
}

export async function setTaskStatus(questId: string, taskId: string, status: string) {
  const response = await fetch(`/api/admin/quests/${questId}/tasks/${taskId}/status`, {
    method: 'POST',
    headers: await mutationHeaders(),
    body: JSON.stringify({ status }),
  })
  return readBody<TaskDetail>(response, 'The task status could not be changed.')
}

export async function fetchSubmissions(questId: string) {
  const response = await fetch(`/api/admin/quests/${questId}/submissions`, { headers: { Accept: 'application/json' } })
  return readBody<SubmissionRecord[]>(response, 'The submissions could not be read.')
}

export async function fetchReadingQuests() {
  const response = await fetch('/api/client/quests', { headers: { Accept: 'application/json' } })
  return readBody<ReadingQuestSummary[]>(response, 'The quests could not be read.')
}

export async function fetchReadingQuest(id: string) {
  const response = await fetch(`/api/client/quests/${id}`, { headers: { Accept: 'application/json' } })
  return readBody<ReadingQuest>(response, 'That quest is not available.')
}

export async function submitAnswer(taskId: string, answer: string) {
  const response = await fetch(`/api/client/quests/tasks/${taskId}/answers`, {
    method: 'POST',
    headers: await mutationHeaders(),
    body: JSON.stringify({ answer }),
  })
  return readBody<AnswerOutcome>(response, 'The answer could not be submitted.')
}
