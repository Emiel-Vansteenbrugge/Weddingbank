import { useContext } from 'react'
import { SessionContext } from './session-context.ts'

export function useSession() {
  const session = useContext(SessionContext)
  if (!session) {
    throw new Error('Session is only available inside the private house.')
  }
  return session
}
