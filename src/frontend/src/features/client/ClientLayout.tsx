import { useState, type ReactNode } from 'react'
import { bank } from '../public/content.ts'
import { SiteLink } from '../../shared/SiteLink.tsx'
import { isCurrentPage } from '../../app/routes.ts'

const links = [
  { href: '/client', label: 'Dashboard' },
  { href: '/client/accounts', label: 'Accounts' },
  { href: '/client/cases', label: 'Cases' },
  { href: '/client/tasks', label: 'Tasks' },
  { href: '/client/documents', label: 'Documents' },
  { href: '/client/correspondence', label: 'Correspondence' },
  { href: '/client/archive', label: 'Archive' },
] as const

type ClientLayoutProps = {
  path: string
  displayName: string
  onLogout: () => Promise<void>
  children: ReactNode
}

export function ClientLayout({ path, displayName, onLogout, children }: ClientLayoutProps) {
  const [pending, setPending] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function logout() {
    if (pending) return
    setPending(true)
    setError(null)
    try {
      await onLogout()
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Sign-out did not complete.')
    } finally {
      setPending(false)
    }
  }

  return (
    <div className="house">
      <a className="skip-link" href="#main">
        Skip to content
      </a>
      <header className="house-bar">
        <SiteLink href="/client" className="house-mark">
          <img src="/brand/ambre-jules.png" alt={bank.name} />
        </SiteLink>
        <p className="house-identity">{displayName}</p>
        <nav className="house-nav" aria-label="Client">
          {links.map((item) => (
            <SiteLink key={item.href} href={item.href} current={isCurrentPage(path, item.href)}>
              {item.label}
            </SiteLink>
          ))}
        </nav>
        <button className="quiet-button" type="button" onClick={logout} disabled={pending}>
          {pending ? 'Signing out' : 'Sign out'}
        </button>
      </header>
      <main id="main" className="house-main">
        {error ? (
          <p className="form-status" role="alert">
            {error}
          </p>
        ) : null}
        {children}
      </main>
    </div>
  )
}
