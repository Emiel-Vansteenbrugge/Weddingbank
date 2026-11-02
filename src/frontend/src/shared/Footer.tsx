import { bank, nav } from '../features/public/content.ts'
import { SiteLink } from './SiteLink.tsx'

export function Footer() {
  return (
    <footer className="site-footer">
      <div className="footer-grid">
        <div>
          <p className="footer-name">{bank.name}</p>
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
          <p>{bank.geneva.telephone}</p>
        </div>
        <div>
          <p className="footer-label">Zurich</p>
          {bank.zurich.lines.map((line) => (
            <p key={line}>{line}</p>
          ))}
          <p>{bank.zurich.telephone}</p>
        </div>
        <nav aria-label="Footer">
          <p className="footer-label">Index</p>
          <SiteLink href="/">Home</SiteLink>
          {nav.map((item) => (
            <SiteLink key={item.href} href={item.href}>
              {item.label}
            </SiteLink>
          ))}
          <SiteLink href="/client-access">Client access</SiteLink>
        </nav>
      </div>
      <div className="footer-base">
        <p>Correspondence: {bank.correspondence}</p>
        <p>© {bank.established}–2026 {bank.name}. All rights reserved.</p>
      </div>
    </footer>
  )
}
