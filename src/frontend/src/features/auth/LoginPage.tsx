import { useEffect, useState, type FormEvent } from 'react'
import { bank } from '../public/content.ts'
import { SiteLink } from '../../shared/SiteLink.tsx'
import { homeFor } from '../../app/routes.ts'
import { signIn, visit } from './api.ts'
import { useSession } from './useSession.ts'

export function LoginPage() {
  const session = useSession()
  const [error, setError] = useState<string | null>(null)
  const [pending, setPending] = useState(false)

  useEffect(() => {
    document.title = `Sign in — ${bank.name}`
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

  return (
    <main id="main" className="gate">
      <SiteLink href="/" className="gate-mark">
        <img src="/brand/ambre-jules.png" alt={bank.name} />
      </SiteLink>
      <p className="eyebrow">Client access</p>
      <h1>Sign in</h1>
      <p className="lede">Access is issued by the house. There is no registration.</p>
      <form className="enquiry gate-form" onSubmit={onSubmit}>
        <label>
          Login
          <input name="login" autoComplete="username" required />
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
      <SiteLink href="/client-access" className="text-link">
        Return to client access
      </SiteLink>
    </main>
  )
}
