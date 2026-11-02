import { bank, history, values } from './content.ts'
import { Page } from './Page.tsx'

export function AboutPage() {
  return (
    <Page title="About us">
      <header className="page-head">
        <p className="eyebrow">About us</p>
        <h1>An independent partnership, still privately held.</h1>
        <p className="lede">
          {bank.name} was founded in Geneva in {bank.established}. We have one purpose: the long custody of a family’s
          financial affairs, conducted with discretion and in writing.
        </p>
      </header>

      <section className="prose-grid">
        <img
          src="/images/stone-stair.jpg"
          alt="A worn limestone staircase with a thin black iron rail inside a historic townhouse."
        />
        <div>
          <h2>History</h2>
          <p>
            The partnership was formed to serve entrepreneurial families who wanted a banker they could name, rather
            than an institution that would change its character with its ownership. That condition has been kept. The
            partnership has never been listed and has never been sold.
          </p>
          <p>
            Geneva remains the seat. Zurich was added when clients’ lives required a second, equally quiet room. Both
            offices work from the same policies.
          </p>
        </div>
      </section>

      <section className="band">
        <div className="band-intro">
          <p className="eyebrow">Chronology</p>
          <h2>A short institutional record.</h2>
        </div>
        <ol className="timeline">
          {history.map((entry) => (
            <li key={entry.year}>
              <span>{entry.year}</span>
              <p>{entry.text}</p>
            </li>
          ))}
        </ol>
      </section>

      <section className="band band-dark">
        <div className="band-intro">
          <p className="eyebrow">Mission</p>
          <h2>To keep capital intelligible to the people it must serve.</h2>
        </div>
        <div className="band-copy">
          <p>
            Our work is to understand a family’s obligations, to write an investment policy that a later reader can
            follow, and to see that the policy is carried out with third-party custodians. We are not organised to
            gather assets for their own sake.
          </p>
          <p>
            International families are ordinary in our practice. Residence, currency, and succession differ from one
            household to the next. The method does not: a named relationship, a written policy, and advisers who remain
            independent of the partnership.
          </p>
        </div>
      </section>

      <section className="band">
        <div className="band-intro">
          <p className="eyebrow">Values</p>
          <h2>What we will and will not do.</h2>
        </div>
        <ul className="value-list">
          {values.map((value) => (
            <li key={value.title}>
              <h3>{value.title}</h3>
              <p>{value.text}</p>
            </li>
          ))}
        </ul>
      </section>
    </Page>
  )
}
