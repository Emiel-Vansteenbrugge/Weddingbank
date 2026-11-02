import type { ReactNode } from 'react'
import { followLink } from './routing.ts'

type SiteLinkProps = {
  href: string
  className?: string
  current?: boolean
  children: ReactNode
  onNavigate?: () => void
}

export function SiteLink({ href, className, current = false, children, onNavigate }: SiteLinkProps) {
  return (
    <a
      href={href}
      className={className}
      aria-current={current ? 'page' : undefined}
      onClick={(event) => {
        followLink(event, href)
        onNavigate?.()
      }}
    >
      {children}
    </a>
  )
}
