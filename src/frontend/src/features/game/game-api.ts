export type ParticipantCard = {
  login: string
  displayName: string
  stage: string
  completedTasks: number
  availableTasks: number
  hintsSent: number
  lastActivity: string | null
}

export type GameOverview = {
  status: string
  juul: ParticipantCard
  amber: ParticipantCard
  shared: {
    stage: string
    completedTasks: number
    availableTasks: number
    hintsSent: number
  }
  outstandingSubmissions: number
  pendingPhoneRequests: number
}

export type ParticipantDesk = {
  login: string
  displayName: string
  stage: string
  account: {
    holder: string
    title: string
    note: string
  }
  completedTasks: number
  availableTasks: number
  sharedCompletedTasks: number
  sharedOpenTasks: number
  hintsWaiting: number
}

export async function fetchOverview() {
  const response = await fetch('/api/admin/game', { headers: { Accept: 'application/json' } })
  if (!response.ok) throw new Error('The game could not be read.')
  return (await response.json()) as GameOverview
}

export async function fetchParticipant(login: string) {
  const response = await fetch(`/api/admin/game/participants/${login}`, { headers: { Accept: 'application/json' } })
  if (response.status === 404) throw new Error('That participant is not part of the game.')
  if (!response.ok) throw new Error('The participant could not be read.')
  return (await response.json()) as ParticipantDesk
}

export async function fetchDesk() {
  const response = await fetch('/api/client/desk', { headers: { Accept: 'application/json' } })
  if (!response.ok) throw new Error('Your record could not be read.')
  return (await response.json()) as ParticipantDesk
}
