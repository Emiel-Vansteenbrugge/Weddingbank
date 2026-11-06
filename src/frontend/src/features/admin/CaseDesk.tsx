import { useEffect, useState, type FormEvent } from 'react'
import { SiteLink } from '../../shared/SiteLink.tsx'
import { visit } from '../auth/api.ts'
import {
  audienceLabel,
  createCase,
  createDocument,
  fetchCase,
  fetchCases,
  fetchDocument,
  reorderCases,
  reorderDocuments,
  setCaseStatus,
  setDocumentStatus,
  statusLabel,
  updateCase,
  updateDocument,
  type CaseDetail,
  type CaseSummary,
  type DocumentDetail,
} from '../cases/case-api.ts'

export function CaseList() {
  const [cases, setCases] = useState<CaseSummary[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [pending, setPending] = useState(false)

  useEffect(() => {
    let active = true
    fetchCases()
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

  async function move(index: number, direction: -1 | 1) {
    if (!cases || pending) return
    const target = index + direction
    if (target < 0 || target >= cases.length) return
    const next = cases.slice()
    const [row] = next.splice(index, 1)
    next.splice(target, 0, row)
    setPending(true)
    setError(null)
    try {
      setCases(await reorderCases(next.map((item) => item.id)))
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'The cases could not be reordered.')
    } finally {
      setPending(false)
    }
  }

  if (error && !cases) {
    return <p className="section-note" role="alert">{error}</p>
  }
  if (!cases) return <p className="section-note">Reading the cases…</p>

  return (
    <>
      <div className="ledger-head">
        <div>
          <p className="eyebrow">Cases</p>
          <h1>Cases</h1>
        </div>
        <SiteLink href="/admin/cases/new" className="text-link">
          New case
        </SiteLink>
      </div>
      <p className="section-note">Drafts and archived cases stay off the participants' desks.</p>
      {error ? <p className="form-status" role="alert">{error}</p> : null}
      {cases.length === 0 ? (
        <p className="section-note">No cases have been written.</p>
      ) : (
        <CaseCatalogue cases={cases} pending={pending} onMove={move} />
      )}
    </>
  )
}

export function CaseCatalogue({
  cases,
  pending = false,
  onMove,
}: {
  cases: CaseSummary[]
  pending?: boolean
  onMove?: (index: number, direction: -1 | 1) => void
}) {
  return (
    <div className="table-wrap">
      <table className="audit-table">
        <thead>
          <tr>
            <th>Reference</th>
            <th>Title</th>
            <th>Audience</th>
            <th>Status</th>
            <th>Documents</th>
            <th>Order</th>
          </tr>
        </thead>
        <tbody>
          {cases.map((item, index) => (
            <tr key={item.id}>
              <td>
                <SiteLink href={`/admin/cases/${item.id}`} className="text-link">
                  {item.reference}
                </SiteLink>
              </td>
              <td>{item.title}</td>
              <td>{audienceLabel(item.audience)}</td>
              <td>{statusLabel(item.status)}</td>
              <td>{item.documentCount}</td>
              <td>
                {onMove ? (
                  <>
                    <button className="quiet-button" type="button" disabled={pending || index === 0} onClick={() => onMove(index, -1)}>
                      Up
                    </button>
                    <button className="quiet-button" type="button" disabled={pending || index === cases.length - 1} onClick={() => onMove(index, 1)}>
                      Down
                    </button>
                  </>
                ) : null}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

export function CaseEditor({ caseId }: { caseId: string }) {
  const creating = caseId === 'new'
  const [record, setRecord] = useState<CaseDetail | null>(null)
  const [reference, setReference] = useState('')
  const [title, setTitle] = useState('')
  const [summary, setSummary] = useState('')
  const [introduction, setIntroduction] = useState('')
  const [audience, setAudience] = useState('BOTH')
  const [error, setError] = useState<string | null>(null)
  const [pending, setPending] = useState(false)
  const [ready, setReady] = useState(creating)

  useEffect(() => {
    if (creating) return
    let active = true
    fetchCase(caseId)
      .then((item) => {
        if (!active) return
        setRecord(item)
        setReference(item.reference)
        setTitle(item.title)
        setSummary(item.summary)
        setIntroduction(item.introduction ?? '')
        setAudience(item.audience)
        setReady(true)
      })
      .catch((caught: unknown) => {
        if (active) setError(caught instanceof Error ? caught.message : 'That case could not be read.')
      })
    return () => {
      active = false
    }
  }, [caseId, creating])

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (pending) return
    setPending(true)
    setError(null)
    const draft = { reference, title, summary, introduction, audience }
    try {
      const saved = creating ? await createCase(draft) : await updateCase(caseId, draft)
      if (creating) {
        visit(`/admin/cases/${saved.id}`)
        return
      }
      setRecord(saved)
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'The case could not be saved.')
    } finally {
      setPending(false)
    }
  }

  async function changeStatus(status: string) {
    if (pending || creating) return
    setPending(true)
    setError(null)
    try {
      setRecord(await setCaseStatus(caseId, status))
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'The case status could not be changed.')
    } finally {
      setPending(false)
    }
  }

  async function moveDocument(index: number, direction: -1 | 1) {
    if (!record || pending) return
    const target = index + direction
    if (target < 0 || target >= record.documents.length) return
    const next = record.documents.slice()
    const [row] = next.splice(index, 1)
    next.splice(target, 0, row)
    setPending(true)
    setError(null)
    try {
      setRecord(await reorderDocuments(caseId, next.map((item) => item.id)))
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'The documents could not be reordered.')
    } finally {
      setPending(false)
    }
  }

  if (error && !ready) return <p className="section-note" role="alert">{error}</p>
  if (!ready) return <p className="section-note">Reading the case…</p>

  return (
    <>
      <p className="eyebrow">Case</p>
      <h1>{creating ? 'New case' : title || 'Case'}</h1>
      <p className="section-note">
        <SiteLink href="/admin/cases" className="text-link">
          All cases
        </SiteLink>
      </p>
      {record ? <p className="section-note">Status: {statusLabel(record.status)}</p> : null}
      {error ? <p className="form-status" role="alert">{error}</p> : null}
      <form className="enquiry" onSubmit={save}>
        <label>
          Reference
          <input value={reference} onChange={(event) => setReference(event.target.value)} required />
        </label>
        <label>
          Title
          <input value={title} onChange={(event) => setTitle(event.target.value)} required />
        </label>
        <label>
          Summary
          <textarea value={summary} onChange={(event) => setSummary(event.target.value)} rows={3} required />
        </label>
        <label>
          Introduction
          <textarea value={introduction} onChange={(event) => setIntroduction(event.target.value)} rows={6} />
        </label>
        <label>
          Audience
          <select value={audience} onChange={(event) => setAudience(event.target.value)}>
            <option value="JUUL">Juul</option>
            <option value="AMBER">Amber</option>
            <option value="BOTH">Both</option>
          </select>
        </label>
        <button type="submit" disabled={pending}>
          {pending ? 'Saving' : 'Save'}
        </button>
      </form>
      {record ? (
        <>
          <div className="row-actions">
            {record.status !== 'PUBLISHED' ? (
              <button className="quiet-button" type="button" disabled={pending} onClick={() => void changeStatus('PUBLISHED')}>
                Publish
              </button>
            ) : null}
            {record.status !== 'DRAFT' ? (
              <button className="quiet-button" type="button" disabled={pending} onClick={() => void changeStatus('DRAFT')}>
                Return to draft
              </button>
            ) : null}
            {record.status !== 'ARCHIVED' ? (
              <button className="quiet-button" type="button" disabled={pending} onClick={() => void changeStatus('ARCHIVED')}>
                Archive
              </button>
            ) : null}
          </div>
          <div className="ledger-head">
            <h2>Documents</h2>
            <SiteLink href={`/admin/cases/${record.id}/documents/new`} className="text-link">
              New document
            </SiteLink>
          </div>
          {record.documents.length === 0 ? (
            <p className="section-note">No documents have been added to this case.</p>
          ) : (
            <div className="table-wrap">
              <table className="audit-table">
                <thead>
                  <tr>
                    <th>Reference</th>
                    <th>Title</th>
                    <th>Type</th>
                    <th>Audience</th>
                    <th>Status</th>
                    <th>Order</th>
                  </tr>
                </thead>
                <tbody>
                  {record.documents.map((item, index) => (
                    <tr key={item.id}>
                      <td>
                        <SiteLink href={`/admin/cases/${record.id}/documents/${item.id}`} className="text-link">
                          {item.reference}
                        </SiteLink>
                      </td>
                      <td>{item.title}</td>
                      <td>{item.category}</td>
                      <td>{audienceLabel(item.audience)}</td>
                      <td>{statusLabel(item.status)}</td>
                      <td>
                        <button className="quiet-button" type="button" disabled={pending || index === 0} onClick={() => void moveDocument(index, -1)}>
                          Up
                        </button>
                        <button className="quiet-button" type="button" disabled={pending || index === record.documents.length - 1} onClick={() => void moveDocument(index, 1)}>
                          Down
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </>
      ) : (
        <p className="section-note">Save the case before adding documents.</p>
      )}
    </>
  )
}

export function DocumentEditor({ caseId, documentId }: { caseId: string; documentId: string }) {
  const creating = documentId === 'new'
  const [reference, setReference] = useState('')
  const [title, setTitle] = useState('')
  const [category, setCategory] = useState('Memorandum')
  const [description, setDescription] = useState('')
  const [content, setContent] = useState('')
  const [audience, setAudience] = useState('BOTH')
  const [record, setRecord] = useState<DocumentDetail | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [pending, setPending] = useState(false)
  const [ready, setReady] = useState(creating)

  useEffect(() => {
    if (creating) return
    let active = true
    fetchDocument(caseId, documentId)
      .then((item) => {
        if (!active) return
        setRecord(item)
        setReference(item.reference)
        setTitle(item.title)
        setCategory(item.category)
        setDescription(item.description ?? '')
        setContent(item.content)
        setAudience(item.audience)
        setReady(true)
      })
      .catch((caught: unknown) => {
        if (active) setError(caught instanceof Error ? caught.message : 'That document could not be read.')
      })
    return () => {
      active = false
    }
  }, [caseId, documentId, creating])

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (pending) return
    setPending(true)
    setError(null)
    const draft = { reference, title, category, description, content, audience }
    try {
      const saved = creating ? await createDocument(caseId, draft) : await updateDocument(caseId, documentId, draft)
      if (creating) {
        visit(`/admin/cases/${caseId}/documents/${saved.id}`)
        return
      }
      setRecord(saved)
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'The document could not be saved.')
    } finally {
      setPending(false)
    }
  }

  async function changeStatus(status: string) {
    if (pending || !record) return
    setPending(true)
    setError(null)
    try {
      setRecord(await setDocumentStatus(caseId, record.id, status))
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'The document status could not be changed.')
    } finally {
      setPending(false)
    }
  }

  if (error && !ready) return <p className="section-note" role="alert">{error}</p>
  if (!ready) return <p className="section-note">Reading the document…</p>

  return (
    <>
      <p className="eyebrow">Document</p>
      <h1>{creating ? 'New document' : title || 'Document'}</h1>
      <p className="section-note">
        <SiteLink href={`/admin/cases/${caseId}`} className="text-link">
          Back to the case
        </SiteLink>
      </p>
      {record ? <p className="section-note">Status: {statusLabel(record.status)}</p> : null}
      {error ? <p className="form-status" role="alert">{error}</p> : null}
      <form className="enquiry" onSubmit={save}>
        <label>
          Reference
          <input value={reference} onChange={(event) => setReference(event.target.value)} required />
        </label>
        <label>
          Title
          <input value={title} onChange={(event) => setTitle(event.target.value)} required />
        </label>
        <label>
          Type
          <input value={category} onChange={(event) => setCategory(event.target.value)} required />
        </label>
        <label>
          Description
          <textarea value={description} onChange={(event) => setDescription(event.target.value)} rows={3} />
        </label>
        <label>
          Text
          <textarea value={content} onChange={(event) => setContent(event.target.value)} rows={14} required />
        </label>
        <label>
          Audience
          <select value={audience} onChange={(event) => setAudience(event.target.value)}>
            <option value="JUUL">Juul</option>
            <option value="AMBER">Amber</option>
            <option value="BOTH">Both</option>
          </select>
        </label>
        <button type="submit" disabled={pending}>
          {pending ? 'Saving' : 'Save'}
        </button>
      </form>
      {record ? (
        <div className="row-actions">
          {record.status !== 'PUBLISHED' ? (
            <button className="quiet-button" type="button" disabled={pending} onClick={() => void changeStatus('PUBLISHED')}>
              Publish
            </button>
          ) : (
            <button className="quiet-button" type="button" disabled={pending} onClick={() => void changeStatus('DRAFT')}>
              Return to draft
            </button>
          )}
        </div>
      ) : (
        <p className="section-note">The document stays a draft until it is published.</p>
      )}
    </>
  )
}
