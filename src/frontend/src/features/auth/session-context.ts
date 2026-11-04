import { createContext } from 'react'
import type { AuthUser } from './api.ts'

export type SessionState =
  | { status: 'loading' }
  | { status: 'anonymous' }
  | { status: 'error' }
  | { status: 'ready'; user: AuthUser }

export type SessionContextValue = {
  state: SessionState
  establish: (user: AuthUser) => void
  signOut: () => Promise<void>
  retry: () => void
}

export const SessionContext = createContext<SessionContextValue | null>(null)
