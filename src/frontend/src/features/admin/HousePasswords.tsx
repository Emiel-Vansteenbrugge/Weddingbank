import { useEffect, useState, type FormEvent } from 'react'
import { mutationHeaders, readFailure } from '../auth/api.ts'

const houseLogins = ['juul', 'amber', 'administrator'] as const

type HouseAccount = {
  login: string
  displayName: string
}

export function HousePasswords() {
  const [accounts, setAccounts] = useState<HouseAccount[]>(
    houseLogins.map((login) => ({ login, displayName: labelFor(login) })),
  )
  const [message, setMessage] = useState<string | null>(null)

  useEffect(() => {
    let active = true
    fetch('/api/admin/house-accounts', { headers: { Accept: 'application/json' } })
      .then(async (response) => {
        if (!response.ok || !active) return
        const body = (await response.json()) as HouseAccount[]
        if (!active) return
        setAccounts(houseLogins.map((login) => body.find((account) => account.login === login) ?? { login, displayName: labelFor(login) }))
      })
      .catch(() => {
        if (active) setMessage('The house accounts could not be read.')
      })
    return () => {
      active = false
    }
  }, [])

  return (
    <>
      <p className="eyebrow">Settings</p>
      <h1>House sign-ins</h1>
      <p className="section-note">
        Set the password for Juul, Amber, or the administrator. The new password replaces the old one, and that account is signed out.
      </p>
      {message ? (
        <p className="form-status" role="alert">
          {message}
        </p>
      ) : null}
      <div className="empty-list">
        {accounts.map((account) => (
          <PasswordForm key={account.login} account={account} />
        ))}
      </div>
    </>
  )
}

function PasswordForm({ account }: { account: HouseAccount }) {
  const [password, setPassword] = useState('')
  const [confirmation, setConfirmation] = useState('')
  const [pending, setPending] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (pending) return
    if (password.length < 12) {
      setSaved(false)
      setError('Use at least 12 characters.')
      return
    }
    if (password !== confirmation) {
      setSaved(false)
      setError('Enter the same password again.')
      return
    }
    setPending(true)
    setError(null)
    setSaved(false)
    try {
      const response = await fetch(`/api/admin/house-accounts/${account.login}/password`, {
        method: 'POST',
        headers: await mutationHeaders(),
        body: JSON.stringify({ password, confirmation }),
      })
      if (!response.ok) {
        const failure = await readFailure(response)
        setError(failure.errors.password ?? failure.errors.confirmation ?? failure.title ?? 'The password could not be changed.')
        return
      }
      setPassword('')
      setConfirmation('')
      setSaved(true)
    } catch {
      setError('The password could not be changed.')
    } finally {
      setPending(false)
    }
  }

  return (
    <section>
      <h2>{account.displayName}</h2>
      <p className="section-note">{account.login}</p>
      <form className="enquiry" onSubmit={onSubmit}>
        <label>
          New password
          <input
            name={`${account.login}-password`}
            type="password"
            autoComplete="new-password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
          />
        </label>
        <label>
          Confirm password
          <input
            name={`${account.login}-confirmation`}
            type="password"
            autoComplete="new-password"
            value={confirmation}
            onChange={(event) => setConfirmation(event.target.value)}
          />
        </label>
        {error ? <p className="form-status">{error}</p> : null}
        {saved ? <p className="section-note">The password is updated. That account will need to sign in again.</p> : null}
        <button type="submit" disabled={pending}>
          {pending ? 'Saving' : 'Set password'}
        </button>
      </form>
    </section>
  )
}

function labelFor(login: string) {
  if (login === 'administrator') return 'Administrator'
  return login.slice(0, 1).toUpperCase() + login.slice(1)
}
