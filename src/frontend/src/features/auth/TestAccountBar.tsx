import { useEffect, useState } from 'react'
import { homeFor } from '../../app/routes.ts'
import { mutationHeaders, readFailure, visit, type AuthUser } from './api.ts'

// Temporary. Remove this toolbar, its stylesheet, and the test-account endpoints before the wedding.

type TestAccount = {
  login: string
  label: string
}

export function TestAccountBar({
  activeLogin,
  onSwitched,
}: {
  activeLogin: string | null
  onSwitched: (user: AuthUser) => void
}) {
  const [accounts, setAccounts] = useState<TestAccount[] | null>(null)
  const [pending, setPending] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let active = true
    fetch('/api/auth/test-accounts', { headers: { Accept: 'application/json' } })
      .then(async (response) => {
        if (!active || response.status === 404) return
        if (!response.ok) return
        const body = (await response.json()) as TestAccount[]
        if (active) setAccounts(body)
      })
      .catch(() => {
        if (active) setAccounts(null)
      })
    return () => {
      active = false
    }
  }, [])

  async function choose(login: string) {
    if (pending || login === activeLogin) return
    setPending(login)
    setError(null)
    try {
      const response = await fetch(`/api/auth/test-accounts/${encodeURIComponent(login)}/session`, {
        method: 'POST',
        headers: await mutationHeaders(),
      })
      if (!response.ok) {
        const failure = await readFailure(response)
        throw new Error(failure.title ?? 'That account could not be opened.')
      }
      const user = (await response.json()) as AuthUser
      onSwitched(user)
      const home = homeFor(user.roles)
      if (home) visit(home)
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'That account could not be opened.')
    } finally {
      setPending(null)
    }
  }

  if (!accounts || accounts.length === 0) return null

  return (
    <TestAccountBarView
      accounts={accounts}
      activeLogin={activeLogin}
      pending={pending}
      error={error}
      onChoose={(login) => void choose(login)}
    />
  )
}

export function TestAccountBarView({
  accounts,
  activeLogin,
  pending,
  error,
  onChoose,
}: {
  accounts: TestAccount[]
  activeLogin: string | null
  pending: string | null
  error: string | null
  onChoose: (login: string) => void
}) {
  return (
    <div className="test-account-bar" data-temporary="test-accounts">
      <p>Temporary test accounts</p>
      {accounts.map((account) => (
        <button
          key={account.login}
          type="button"
          aria-current={account.login === activeLogin ? 'true' : undefined}
          disabled={pending !== null}
          onClick={() => onChoose(account.login)}
        >
          {account.label}
        </button>
      ))}
      {error ? <span role="alert">{error}</span> : null}
    </div>
  )
}
