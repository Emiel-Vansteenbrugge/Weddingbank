import { services } from './content.ts'
import { Page } from './Page.tsx'
import { SiteLink } from '../../shared/SiteLink.tsx'

export function ServicesPage() {
  return (
    <Page title="Our services">
      <header className="page-head">
        <p className="eyebrow">Our services</p>
        <h1>The mandate is agreed before any portfolio is discussed.</h1>
        <p className="lede">
          Every service sits inside one relationship. We do not offer a catalogue. We accept a defined responsibility
          and we record its limits.
        </p>
      </header>
      <div className="service-list">
        {services.map((service) => (
          <article key={service.title} className="service-entry">
            <h2>{service.title}</h2>
            <p className="service-summary">{service.summary}</p>
            <p>{service.detail}</p>
          </article>
        ))}
      </div>
      <section className="closing closing-plain">
        <div>
          <h2>Suitability is discussed before an account is opened.</h2>
          <p>If you wish to understand whether a conversation is appropriate, write to the client office.</p>
          <SiteLink href="/contact" className="text-link">
            Contact the house
          </SiteLink>
        </div>
      </section>
    </Page>
  )
}
