import { bank, nav } from '../features/public/content.ts'
import { SiteLink } from './SiteLink.tsx'

function telephoneHref(telephone: string) {
  return `tel:${telephone.replace(/[^\d+]/g, '')}`
}

export function Footer() {
  return (
    <footer className="site-footer">
      <div className="footer-grid">
        <div>
          <SiteLink href="/" className="footer-mark">
            <img src="/brand/ambre-jules.png" alt={bank.name} />
          </SiteLink>
          <p className="footer-note">
            Private bankers since {bank.established}. Independently owned. Client assets are held with third-party
            custodians.
          </p>
        </div>
        <div>
          <p className="footer-label">Geneva</p>
          {bank.geneva.lines.map((line) => (
            <p key={line}>{line}</p>
          ))}
          <p>
            <a className="contact-link" href={telephoneHref(bank.geneva.telephone)}>
              {bank.geneva.telephone}
            </a>
          </p>
        </div>
        <div>
          <p className="footer-label">Zurich</p>
          {bank.zurich.lines.map((line) => (
            <p key={line}>{line}</p>
          ))}
          <p>
            <a className="contact-link" href={telephoneHref(bank.zurich.telephone)}>
              {bank.zurich.telephone}
            </a>
          </p>
        </div>
        <nav aria-label="Footer">
          <p className="footer-label">Index</p>
          <SiteLink href="/">Home</SiteLink>
          {nav.map((item) => (
            <SiteLink key={item.href} href={item.href}>
              {item.label}
            </SiteLink>
          ))}
          <SiteLink href="/login" className="client-access">
            Client access
          </SiteLink>
        </nav>
      </div>
      <div className="footer-base">
        <p>
          Correspondence:{' '}
          <a className="contact-link" href={`mailto:${bank.correspondence}`}>
            {bank.correspondence}
          </a>
        </p>
        <p>© {bank.established}–2026 {bank.name}. All rights reserved.</p>
      </div>
    </footer>
  )
}
