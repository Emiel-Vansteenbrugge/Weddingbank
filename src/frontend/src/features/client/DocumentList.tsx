import { SiteLink } from '../../shared/SiteLink.tsx'
import type { ReadingDocument } from '../cases/case-api.ts'

export function DocumentList({ documents }: { documents: ReadingDocument[] }) {
  return (
    <div className="document-list">
      {documents.map((item) => (
        <SiteLink key={item.id} href={`/client/documents/${item.id}`} className="document-row">
          <DocumentIcon />
          <span className="document-copy">
            <span className="document-title">{item.title}</span>
            <span className="document-category">{item.category}</span>
          </span>
          {item.isNew ? <span className="new-mark">New</span> : <span className="new-mark" aria-hidden="true" />}
          <Chevron />
        </SiteLink>
      ))}
    </div>
  )
}

function DocumentIcon() {
  return (
    <svg className="document-icon" viewBox="0 0 24 24" aria-hidden="true">
      <path d="M7 3.5h7.1L19 8.3V20.5H7z" />
      <path d="M14 3.6V8.4h4.8" />
    </svg>
  )
}

function Chevron() {
  return (
    <svg className="document-chevron" viewBox="0 0 16 16" aria-hidden="true">
      <path d="M6 3.5 11 8l-5 4.5" />
    </svg>
  )
}
