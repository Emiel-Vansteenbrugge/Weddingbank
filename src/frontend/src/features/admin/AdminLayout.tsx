import { useState, type ReactNode } from 'react'
import { bank } from '../public/content.ts'
import { SiteLink } from '../../shared/SiteLink.tsx'
import { isCurrentPage } from '../../app/routes.ts'

const links = [
  { href: '/admin', label: 'Dashboard' },
  { href: '/admin/participants', label: 'Participants' },
  { href: '/admin/cases', label: 'Cases' },
  { href: '/admin/tasks', label: 'Tasks' },
  { href: '/admin/hints', label: 'Hints & messages' },
  { href: '/admin/phone-requests', label: 'Phone requests' },
  { href: '/admin/events', label: 'Events' },
  { href: '/admin/audit-log', label: 'Audit log' },
  { href: '/admin/settings', label: 'Settings' },
] as const

type AdminLayoutProps = {
  path: string
  displayName: string
  onLogout: () => Promise<void>
  children: ReactNode
}

export function AdminLayout({ path, displayName, onLogout, children }: AdminLayoutProps) {
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
    <div className="desk">
      <a className="skip-link" href="#main">
        Skip to content
      </a>
      <aside className="desk-side">
        <SiteLink href="/admin" className="desk-mark">
          <img src="/brand/ambre-jules.png" alt={bank.name} />
        </SiteLink>
        <p className="desk-kicker">Game</p>
        <nav className="desk-nav" aria-label="Game">
          {links.map((item) => (
            <SiteLink key={item.href} href={item.href} current={isCurrentPage(path, item.href)}>
              {item.label}
            </SiteLink>
          ))}
        </nav>
      </aside>
      <div className="desk-body">
        <header className="desk-bar">
          <p className="desk-identity">{displayName}</p>
          <button className="quiet-button" type="button" onClick={logout} disabled={pending}>
            {pending ? 'Signing out' : 'Sign out'}
          </button>
        </header>
        <main id="main" className="desk-main">
          {error ? (
            <p className="form-status" role="alert">
              {error}
            </p>
          ) : null}
          {children}
        </main>
      </div>
    </div>
  )
}
