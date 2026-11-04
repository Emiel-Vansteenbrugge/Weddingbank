import { Page } from './Page.tsx'
import { SiteLink } from '../../shared/SiteLink.tsx'

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
