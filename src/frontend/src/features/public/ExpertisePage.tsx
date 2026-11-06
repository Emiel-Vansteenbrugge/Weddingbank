import { departments } from './content.ts'
import { Page } from './Page.tsx'

export function ExpertisePage() {
  return (
    <Page title="Expertise">
      <header className="page-head">
        <p className="eyebrow">Expertise</p>
        <h1>Each question has a desk, and each desk has a mandate.</h1>
        <p className="lede">
          The partnership is small. The organisation is deliberate, so that investment judgement, family planning, and
          the administration of the relationship are not collapsed into a single conversation.
        </p>
      </header>
      <section className="split split-expertise">
        <img
          src="/images/private-salon.jpg"
          alt="A formal meeting salon with an oak table and daylight, prepared for a private appointment."
        />
        <div>
          <h2>How a matter proceeds</h2>
          <p>
            A new enquiry is received by the client office. If a meeting is appropriate, it is held in Geneva or
            Zurich. Only after the family’s circumstances are understood does the investment office draft a policy.
          </p>
          <p>
            Legal, tax, and accounting questions are referred to the family’s own counsel. We coordinate the financial
            information those advisers require. We do not replace them.
          </p>
        </div>
      </section>
      <section className="band">
        <div className="band-intro">
          <p className="eyebrow">Departments</p>
          <h2>The standing organisation of the house.</h2>
        </div>
        <div className="department-list">
          {departments.map((department) => (
            <article key={department.name}>
              <h3>{department.name}</h3>
              <p>{department.mandate}</p>
            </article>
          ))}
        </div>
      </section>
    </Page>
  )
}
