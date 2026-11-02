import { articles, bank, services } from './content.ts'
import { Page } from './Page.tsx'
import { SiteLink } from '../../shared/SiteLink.tsx'

export function HomePage() {
  return (
    <Page title="Home">
      <section className="hero">
        <img
          src="/images/hero-facade.jpg"
          alt="Pale stone facade of a quiet Geneva townhouse, with tall windows and a black iron balcony."
        />
        <div className="hero-copy">
          <p className="eyebrow">Geneva · Zurich · Since {bank.established}</p>
          <h1>Stewardship for families who intend to remain.</h1>
          <p className="lede">
            {bank.name} is a private partnership. We advise a limited number of families on the preservation,
            organisation, and transfer of their capital.
          </p>
          <div className="hero-actions">
            <SiteLink href="/contact" className="text-link">
              Request a conversation
            </SiteLink>
            <SiteLink href="/services" className="text-link text-link-quiet">
              Our services
            </SiteLink>
          </div>
        </div>
      </section>

      <section className="band">
        <div className="band-intro">
          <p className="eyebrow">The partnership</p>
          <h2>A private bank is a responsibility, not a display.</h2>
        </div>
        <div className="band-copy">
          <p>
            We were established in Geneva in {bank.established} and remain independently owned. The work is personal,
            documented, and slow by design. A relationship begins only when we understand the family and the family
            understands how we work.
          </p>
          <p>
            Advice, custody, and the client conversation are kept distinct. Assets are held with third-party
            custodians. The partnership does not manufacture investment products.
          </p>
          <SiteLink href="/about" className="text-link">
            About the house
          </SiteLink>
        </div>
      </section>

      <section className="band band-dark">
        <div className="band-intro">
          <p className="eyebrow">Services</p>
          <h2>Five responsibilities, one relationship.</h2>
        </div>
        <ol className="index-list">
          {services.map((service, index) => (
            <li key={service.title}>
              <span>0{index + 1}</span>
              <div>
                <h3>{service.title}</h3>
                <p>{service.summary}</p>
              </div>
            </li>
          ))}
        </ol>
        <SiteLink href="/services" className="text-link">
          Read the services
        </SiteLink>
      </section>

      <section className="split">
        <img
          src="/images/private-salon.jpg"
          alt="An empty private meeting room with an oak table, linen chairs, and daylight from a tall window."
        />
        <div>
          <p className="eyebrow">Expertise</p>
          <h2>The house is organised around the work, not around a product.</h2>
          <p>
            The investment office, wealth planning, family governance, and the client office each have a written
            mandate. Clients know which desk is responsible for a question.
          </p>
          <SiteLink href="/expertise" className="text-link">
            The departments
          </SiteLink>
        </div>
      </section>

      <section className="band">
        <div className="band-intro">
          <p className="eyebrow">Insights</p>
          <h2>Notes from the house.</h2>
          <p>Institutional commentary for clients and correspondents. Not a market bulletin.</p>
        </div>
        <ul className="article-index">
          {articles.slice(0, 3).map((article) => (
            <li key={article.slug}>
              <p>
                {article.category}
                <span>{article.date}</span>
              </p>
              <h3>
                <SiteLink href={`/insights/${article.slug}`}>{article.title}</SiteLink>
              </h3>
              <p>{article.standfirst}</p>
            </li>
          ))}
        </ul>
      </section>

      <section className="closing">
        <img
          src="/images/lake-quay.jpg"
          alt="A pale stone building set back from a calm lakeside under an overcast sky."
        />
        <div>
          <h2>A conversation begins in Geneva or Zurich.</h2>
          <p>
            New relationships are accepted selectively. If you wish to be introduced, write to the client office.
          </p>
          <SiteLink href="/contact" className="text-link">
            Contact the house
          </SiteLink>
        </div>
      </section>
    </Page>
  )
}
