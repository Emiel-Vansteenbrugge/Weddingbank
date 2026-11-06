import { useState } from 'react'
import { bank, nav } from '../features/public/content.ts'
import { SiteLink } from './SiteLink.tsx'

type HeaderProps = {
  path: string
}

export function Header({ path }: HeaderProps) {
  const [open, setOpen] = useState(false)
  const close = () => setOpen(false)

  return (
    <header className="site-header">
      <a className="skip-link" href="#main">
        Skip to content
      </a>
      <div className="header-bar">
        <SiteLink href="/" className="wordmark" onNavigate={close}>
          <img src="/brand/ambre-jules.png" alt={`${bank.name}, ${bank.descriptor}`} />
        </SiteLink>
        <button
          className="menu-button"
          type="button"
          aria-expanded={open}
          aria-controls="site-navigation"
          onClick={() => setOpen((value) => !value)}
        >
          {open ? 'Close' : 'Menu'}
        </button>
        <nav id="site-navigation" className={open ? 'site-nav is-open' : 'site-nav'} aria-label="Primary">
          {nav.map((item) => (
            <SiteLink
              key={item.href}
              href={item.href}
              current={path === item.href || (item.href === '/insights' && path.startsWith('/insights/'))}
              onNavigate={close}
            >
              {item.label}
            </SiteLink>
          ))}
          <SiteLink
            href="/login"
            className="client-access"
            current={path === '/login'}
            onNavigate={close}
          >
            Client access
          </SiteLink>
        </nav>
      </div>
    </header>
  )
}
