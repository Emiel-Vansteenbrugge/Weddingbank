import { useState, type FormEvent } from 'react'
import { bank } from './content.ts'
import { Page } from './Page.tsx'

export function ContactPage() {
  const [submitted, setSubmitted] = useState(false)

  function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSubmitted(true)
  }

  return (
    <Page title="Contact">
      <header className="page-head">
        <p className="eyebrow">Contact</p>
        <h1>The client office receives correspondence in Geneva.</h1>
        <p className="lede">
          Meetings are arranged in Geneva or Zurich. Please write with the nature of your enquiry. We reply when we
          are able to be of service.
        </p>
      </header>
      <div className="contact-layout">
        <div className="offices">
          <section>
            <h2>Geneva</h2>
            {bank.geneva.lines.map((line) => (
              <p key={line}>{line}</p>
            ))}
            <p>
              <a href={`tel:${bank.geneva.telephone.replace(/\s/g, '')}`}>{bank.geneva.telephone}</a>
            </p>
          </section>
          <section>
            <h2>Zurich</h2>
            {bank.zurich.lines.map((line) => (
              <p key={line}>{line}</p>
            ))}
            <p>
              <a href={`tel:${bank.zurich.telephone.replace(/\s/g, '')}`}>{bank.zurich.telephone}</a>
            </p>
          </section>
          <section>
            <h2>Correspondence</h2>
            <p>
              <a href={`mailto:${bank.correspondence}`}>{bank.correspondence}</a>
            </p>
            <p>Monday to Friday, by appointment.</p>
          </section>
        </div>
        <form className="enquiry" onSubmit={onSubmit}>
          <h2>Enquiry</h2>
          {submitted ? (
            <p className="form-status" role="status">
              Thank you. Please address your enquiry by letter or to {bank.correspondence}. The client office
              corresponds directly.
            </p>
          ) : (
            <>
              <label>
                Name
                <input name="name" type="text" autoComplete="name" required />
              </label>
              <label>
                Email
                <input name="email" type="email" autoComplete="email" required />
              </label>
              <label>
                Telephone
                <input name="telephone" type="tel" autoComplete="tel" />
              </label>
              <label>
                Subject
                <input name="subject" type="text" required />
              </label>
              <label>
                Message
                <textarea name="message" rows={6} required />
              </label>
              <button type="submit">Submit enquiry</button>
              <p className="form-note">
                Please do not include account numbers or confidential documents in this form.
              </p>
            </>
          )}
        </form>
      </div>
    </Page>
  )
}
