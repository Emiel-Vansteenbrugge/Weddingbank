import { useEffect, useState, type ReactNode } from 'react'
import { fetchCurrentUser, signOut as postSignOut, type AuthUser } from './api.ts'
import { SessionContext, type SessionState } from './session-context.ts'

export function SessionProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<SessionState>({ status: 'loading' })
  const [attempt, setAttempt] = useState(0)

  useEffect(() => {
    let active = true
    fetchCurrentUser()
      .then((user) => {
        if (!active) return
        setState(user ? { status: 'ready', user } : { status: 'anonymous' })
      })
      .catch(() => {
        if (active) setState({ status: 'error' })
      })
    return () => {
      active = false
    }
  }, [attempt])

  return (
    <SessionContext
      value={{
        state,
        establish: (user: AuthUser) => setState({ status: 'ready', user }),
        signOut: async () => {
          await postSignOut()
          setState({ status: 'anonymous' })
        },
        retry: () => {
          setState({ status: 'loading' })
          setAttempt((current) => current + 1)
        },
      }}
    >
      {children}
    </SessionContext>
  )
}
