import { Page } from './Page.tsx'
import { SiteLink } from '../../shared/SiteLink.tsx'

export function ClientAccessPage() {
  return (
    <Page title="Client access">
      <header className="page-head">
        <p className="eyebrow">Client access</p>
        <h1>Reporting is opened for retained clients only.</h1>
        <p className="lede">
          Access is arranged individually, after a relationship has been accepted, and is provided through the
          relationship manager. There is no public registration.
        </p>
        <div className="access-links">
          <SiteLink href="/login" className="text-link">
            Sign in
          </SiteLink>
          <SiteLink href="/contact" className="text-link">
            Write to the client office
          </SiteLink>
        </div>
      </header>
    </Page>
  )
}

export function NotFoundPage() {
  return (
    <Page title="Page not found">
      <header className="page-head">
        <p className="eyebrow">404</p>
        <h1>This page is not part of the house.</h1>
        <SiteLink href="/" className="text-link">
          Return to the entrance
        </SiteLink>
      </header>
    </Page>
  )
}
