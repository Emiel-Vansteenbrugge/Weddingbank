import { useEffect, useState } from 'react'
import { SiteLink } from '../../shared/SiteLink.tsx'
import { DocumentList } from './DocumentList.tsx'
import { PaperBody } from './PaperBody.tsx'
import {
  fetchPaper,
  fetchPapers,
  fetchReadingCase,
  fetchReadingCases,
  type ReadingCase,
  type ReadingDocument,
  type ReadingPaper,
} from '../cases/case-api.ts'

export function ParticipantCases() {
  const [cases, setCases] = useState<ReadingCase[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let active = true
    fetchReadingCases()
      .then((rows) => {
        if (active) setCases(rows)
      })
      .catch((caught: unknown) => {
        if (active) setError(caught instanceof Error ? caught.message : 'The cases could not be read.')
      })
    return () => {
      active = false
    }
  }, [])

  if (error) return <p className="section-note" role="alert">{error}</p>
  if (!cases) return <p className="section-note">Opening the cases…</p>

  return (
    <>
      <p className="eyebrow">Cases</p>
      <h1>Cases</h1>
      {cases.length === 0 ? (
        <p className="section-note">No dossier is open in your name.</p>
      ) : (
        <div className="empty-list">
          {cases.map((item) => (
            <section key={item.id}>
              <p className="status-mark">{item.reference}</p>
              <h2>
                <SiteLink href={`/client/cases/${item.id}`} className="text-link">
                  {item.title}
                </SiteLink>
              </h2>
              <p>{item.summary}</p>
            </section>
          ))}
        </div>
      )}
    </>
  )
}

export function ParticipantCase({ caseId }: { caseId: string }) {
  const [record, setRecord] = useState<ReadingCase | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let active = true
    fetchReadingCase(caseId)
      .then((item) => {
        if (active) setRecord(item)
      })
      .catch((caught: unknown) => {
        if (active) setError(caught instanceof Error ? caught.message : 'That case is not available.')
      })
    return () => {
      active = false
    }
  }, [caseId])

  if (error) return <p className="section-note" role="alert">{error}</p>
  if (!record) return <p className="section-note">Opening the case…</p>

  return (
    <>
      <p className="eyebrow">{record.reference}</p>
      <h1>{record.title}</h1>
      <p className="section-note">{record.summary}</p>
      {record.introduction ? <p className="paper">{record.introduction}</p> : null}
      <h2>Documents</h2>
      {record.documents.length === 0 ? (
        <p className="section-note">No papers are held in this dossier.</p>
      ) : (
        <DocumentList documents={record.documents} />
      )}
    </>
  )
}

export function ParticipantDocuments() {
  const [papers, setPapers] = useState<ReadingDocument[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let active = true
    fetchPapers()
      .then((rows) => {
        if (active) setPapers(rows)
      })
      .catch((caught: unknown) => {
        if (active) setError(caught instanceof Error ? caught.message : 'The documents could not be read.')
      })
    return () => {
      active = false
    }
  }, [])

  if (error) return <p className="section-note" role="alert">{error}</p>
  if (!papers) return <p className="section-note">Opening the documents…</p>

  return (
    <>
      <p className="eyebrow">Documents</p>
      <h1>Documents</h1>
      {papers.length === 0 ? (
        <p className="section-note">No papers are held for you.</p>
      ) : (
        <DocumentList documents={papers} />
      )}
    </>
  )
}

export function ParticipantDocument({ documentId }: { documentId: string }) {
  const [paper, setPaper] = useState<ReadingPaper | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let active = true
    fetchPaper(documentId)
      .then((item) => {
        if (active) setPaper(item)
      })
      .catch((caught: unknown) => {
        if (active) setError(caught instanceof Error ? caught.message : 'That document is not available.')
      })
    return () => {
      active = false
    }
  }, [documentId])

  if (error) return <p className="section-note" role="alert">{error}</p>
  if (!paper) return <p className="section-note">Opening the document…</p>

  return (
    <>
      <p className="eyebrow">{paper.category}</p>
      <h1>{paper.title}</h1>
      <p className="section-note">
        {paper.reference}
        {' · '}
        <SiteLink href={`/client/cases/${paper.caseId}`} className="text-link">
          {paper.caseTitle}
        </SiteLink>
      </p>
      {paper.description ? <p className="section-note">{paper.description}</p> : null}
      <PaperBody content={paper.content} />
    </>
  )
}

