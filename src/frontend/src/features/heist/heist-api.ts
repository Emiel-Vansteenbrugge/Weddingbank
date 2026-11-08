import { mutationHeaders, readFailure } from '../auth/api.ts'

export type HeistRecord = {
  title: string
  status: 'CLOSED' | 'OPEN'
  stage: string
  updatedAt: string
  safe: {
    present: boolean
    amountEuros: number
    currency: string
    combinationRevealed: boolean
  }
}

export async function enterHeist(): Promise<HeistRecord> {
  const response = await fetch('/api/heist/enter', {
    method: 'POST',
    headers: { Accept: 'application/json' },
  })
  if (!response.ok) {
    throw new Error('The entrance could not be left.')
  }
  return (await response.json()) as HeistRecord
}

export async function fetchHeist(): Promise<HeistRecord> {
  const response = await fetch('/api/heist', { headers: { Accept: 'application/json' } })
  if (!response.ok) {
    throw new Error('The shared record could not be read.')
  }
  return (await response.json()) as HeistRecord
}

export async function resetHeist(): Promise<HeistRecord> {
  const response = await fetch('/api/admin/heist/reset', {
    method: 'POST',
    headers: await mutationHeaders(),
  })
  if (response.ok) return (await response.json()) as HeistRecord
  const failure = await readFailure(response)
  throw new Error(failure.title ?? 'The room could not be reset.')
}

export async function setHeistOpen(open: boolean): Promise<HeistRecord> {
  const response = await fetch(open ? '/api/admin/heist/open' : '/api/admin/heist/close', {
    method: 'POST',
    headers: await mutationHeaders(),
  })
  if (response.ok) return (await response.json()) as HeistRecord
  const failure = await readFailure(response)
  throw new Error(failure.title ?? 'The room could not be changed.')
}
