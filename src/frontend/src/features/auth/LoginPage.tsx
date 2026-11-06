import { useEffect, useState, type FormEvent, type KeyboardEvent } from 'react'
import { SiteLink } from '../../shared/SiteLink.tsx'
import { homeFor } from '../../app/routes.ts'
import { signIn, visit } from './api.ts'
import { useSession } from './useSession.ts'

function submitOnEnter(event: KeyboardEvent<HTMLFormElement>) {
  if (event.key !== 'Enter' || event.nativeEvent.isComposing) return
  const target = event.target
  if (target instanceof HTMLButtonElement || target instanceof HTMLTextAreaElement) return
  const submitter = event.currentTarget.querySelector<HTMLButtonElement>('button[type="submit"]')
  if (!submitter || submitter.disabled) return
  event.preventDefault()
  event.currentTarget.requestSubmit(submitter)
}

export function LoginPage() {
  const session = useSession()
  const [error, setError] = useState<string | null>(null)
  const [pending, setPending] = useState(false)

  useEffect(() => {
    document.title = 'Administrator — Wedding Bank'
  }, [])

  useEffect(() => {
    if (session.state.status !== 'ready') return
    const home = homeFor(session.state.user.roles)
    if (home) visit(home)
  }, [session.state])

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (pending) return
    const data = new FormData(event.currentTarget)
    setPending(true)
    setError(null)
    try {
      const user = await signIn(String(data.get('login') ?? ''), String(data.get('password') ?? ''))
      session.establish(user)
      const home = homeFor(user.roles)
      if (home) visit(home)
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'The sign-in details are not recognised.')
    } finally {
      setPending(false)
    }
  }

  if (session.state.status === 'loading') {
    return (
      <main id="main" className="panel-status">
        <p>Opening the record…</p>
      </main>
    )
  }

  if (session.state.status === 'ready' && !homeFor(session.state.user.roles)) {
    return (
      <main id="main" className="gate">
        <p className="eyebrow">Administrator</p>
        <h1>This sign-in does not open the game.</h1>
        <p className="lede">Players do not have accounts. The room is shared.</p>
        <button className="quiet-button" type="button" onClick={() => void session.signOut()}>
          Sign out
        </button>
      </main>
    )
  }

  return (
    <main id="main" className="gate">
      <p className="eyebrow">Administrator</p>
      <h1>Sign in</h1>
      <p className="lede">This entrance is for the administrator. Players do not sign in.</p>
      <form className="enquiry gate-form" onSubmit={onSubmit} onKeyDown={submitOnEnter}>
        <label>
          Login
          <input name="login" type="text" autoComplete="username" required />
        </label>
        <label>
          Password
          <input name="password" type="password" autoComplete="current-password" required />
        </label>
        {error ? (
          <p className="form-status" role="alert">
            {error}
          </p>
        ) : null}
        <button type="submit" disabled={pending}>
          {pending ? 'Signing in' : 'Sign in'}
        </button>
      </form>
      <SiteLink href="/heist" className="text-link">
        Return to the room
      </SiteLink>
    </main>
  )
}
