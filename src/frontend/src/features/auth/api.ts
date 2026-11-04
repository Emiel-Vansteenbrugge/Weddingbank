import { normalizePath } from '../../shared/routing.ts'

export type AuthUser = {
  id: string
  login: string
  displayName: string
  roles: string[]
}

export type AuditRow = {
  id: string
  occurredAt: string
  actorLogin: string | null
  eventType: string
  targetType: string | null
  targetId: string | null
}

export function visit(path: string) {
  const next = normalizePath(path)
  if (normalizePath(window.location.pathname) !== next) {
    window.history.pushState({}, '', next)
  }
  window.dispatchEvent(new PopStateEvent('popstate'))
}

export async function mutationHeaders() {
  const response = await fetch('/api/auth/csrf', { headers: { Accept: 'application/json' } })
  if (!response.ok) {
    throw new Error('The form could not be prepared.')
  }
  const body = (await response.json()) as { token?: string }
  if (!body.token) {
    throw new Error('The form could not be prepared.')
  }
  return {
    Accept: 'application/json',
    'Content-Type': 'application/json',
    'X-CSRF-TOKEN': body.token,
  }
}

export async function readFailure(response: Response) {
  try {
    const body = (await response.json()) as { title?: string; errors?: Record<string, string[]> }
    const errors: Record<string, string> = {}
    for (const [key, messages] of Object.entries(body.errors ?? {})) {
      const message = messages.find((item) => item.trim().length > 0)
      if (message) errors[key] = message
    }
    return { title: body.title?.trim() || null, errors }
  } catch {
    return { title: null, errors: {} }
  }
}

async function problemTitle(response: Response) {
  try {
    const body = (await response.json()) as { title?: string }
    return body.title?.trim() || null
  } catch {
    return null
  }
}

export async function fetchCurrentUser(): Promise<AuthUser | null> {
  const response = await fetch('/api/auth/me', { headers: { Accept: 'application/json' } })
  if (response.status === 401) return null
  if (!response.ok) throw new Error('The session could not be read.')
  return (await response.json()) as AuthUser
}

export async function signIn(login: string, password: string) {
  const response = await fetch('/api/auth/login', {
    method: 'POST',
    headers: await mutationHeaders(),
    body: JSON.stringify({ login, password }),
  })
  if (response.ok) return (await response.json()) as AuthUser
  if (response.status === 429) {
    throw new Error('Sign-in is temporarily unavailable. Please wait and try again.')
  }
  throw new Error((await problemTitle(response)) ?? 'The sign-in details are not recognised.')
}

export async function signOut() {
  const response = await fetch('/api/auth/logout', {
    method: 'POST',
    headers: await mutationHeaders(),
  })
  if (!response.ok) {
    throw new Error((await problemTitle(response)) ?? 'Sign-out did not complete.')
  }
}

export async function fetchAudit(): Promise<AuditRow[]> {
  const response = await fetch('/api/admin/audit', { headers: { Accept: 'application/json' } })
  if (!response.ok) {
    throw new Error((await problemTitle(response)) ?? 'The audit log could not be read.')
  }
  return (await response.json()) as AuditRow[]
}
