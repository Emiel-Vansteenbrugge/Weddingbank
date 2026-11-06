import { mutationHeaders, readFailure } from '../auth/api.ts'

export type CaseSummary = {
  id: string
  reference: string
  title: string
  summary: string
  status: string
  audience: string
  displayOrder: number
  documentCount: number
  updatedAt: string
}

export type DocumentSummary = {
  id: string
  reference: string
  title: string
  category: string
  description: string | null
  status: string
  audience: string
  displayOrder: number
  updatedAt: string
}

export type CaseDetail = CaseSummary & {
  introduction: string | null
  createdAt: string
  documents: DocumentSummary[]
}

export type DocumentDetail = DocumentSummary & {
  caseId: string
  content: string
  createdAt: string
}

export type ReadingCase = {
  id: string
  reference: string
  title: string
  summary: string
  introduction: string | null
  documents: ReadingDocument[]
}

export type ReadingDocument = {
  id: string
  caseId: string
  caseReference: string
  caseTitle: string
  reference: string
  title: string
  category: string
  description: string | null
  displayOrder: number
  isNew: boolean
}

export type ReadingPaper = ReadingDocument & {
  content: string
}

export type CaseDraft = {
  reference: string
  title: string
  summary: string
  introduction: string
  audience: string
}

export type DocumentDraft = {
  reference: string
  title: string
  category: string
  description: string
  content: string
  audience: string
}

export function audienceLabel(audience: string) {
  if (audience === 'JUUL') return 'Juul'
  if (audience === 'AMBER') return 'Amber'
  return 'Both'
}

export function statusLabel(status: string) {
  if (status === 'PUBLISHED') return 'Published'
  if (status === 'ARCHIVED') return 'Archived'
  return 'Draft'
}

async function readBody<T>(response: Response, fallback: string) {
  if (response.ok) return (await response.json()) as T
  const failure = await readFailure(response)
  const detail = Object.values(failure.errors)[0]
  throw new Error(detail ?? failure.title ?? fallback)
}

export async function fetchCases() {
  const response = await fetch('/api/admin/cases', { headers: { Accept: 'application/json' } })
  return readBody<CaseSummary[]>(response, 'The cases could not be read.')
}

export async function fetchCase(id: string) {
  const response = await fetch(`/api/admin/cases/${id}`, { headers: { Accept: 'application/json' } })
  return readBody<CaseDetail>(response, 'That case could not be read.')
}

export async function createCase(draft: CaseDraft) {
  const response = await fetch('/api/admin/cases', {
    method: 'POST',
    headers: await mutationHeaders(),
    body: JSON.stringify(draft),
  })
  return readBody<CaseDetail>(response, 'The case could not be saved.')
}

export async function updateCase(id: string, draft: CaseDraft) {
  const response = await fetch(`/api/admin/cases/${id}`, {
    method: 'PUT',
    headers: await mutationHeaders(),
    body: JSON.stringify(draft),
  })
  return readBody<CaseDetail>(response, 'The case could not be saved.')
}

export async function setCaseStatus(id: string, status: string) {
  const response = await fetch(`/api/admin/cases/${id}/status`, {
    method: 'POST',
    headers: await mutationHeaders(),
    body: JSON.stringify({ status }),
  })
  return readBody<CaseDetail>(response, 'The case status could not be changed.')
}

export async function reorderCases(ids: string[]) {
  const response = await fetch('/api/admin/cases/order', {
    method: 'POST',
    headers: await mutationHeaders(),
    body: JSON.stringify({ ids }),
  })
  return readBody<CaseSummary[]>(response, 'The cases could not be reordered.')
}

export async function fetchDocument(caseId: string, documentId: string) {
  const response = await fetch(`/api/admin/cases/${caseId}/documents/${documentId}`, { headers: { Accept: 'application/json' } })
  return readBody<DocumentDetail>(response, 'That document could not be read.')
}

export async function createDocument(caseId: string, draft: DocumentDraft) {
  const response = await fetch(`/api/admin/cases/${caseId}/documents`, {
    method: 'POST',
    headers: await mutationHeaders(),
    body: JSON.stringify(draft),
  })
  return readBody<DocumentDetail>(response, 'The document could not be saved.')
}

export async function updateDocument(caseId: string, documentId: string, draft: DocumentDraft) {
  const response = await fetch(`/api/admin/cases/${caseId}/documents/${documentId}`, {
    method: 'PUT',
    headers: await mutationHeaders(),
    body: JSON.stringify(draft),
  })
  return readBody<DocumentDetail>(response, 'The document could not be saved.')
}

export async function setDocumentStatus(caseId: string, documentId: string, status: string) {
  const response = await fetch(`/api/admin/cases/${caseId}/documents/${documentId}/status`, {
    method: 'POST',
    headers: await mutationHeaders(),
    body: JSON.stringify({ status }),
  })
  return readBody<DocumentDetail>(response, 'The document status could not be changed.')
}

export async function reorderDocuments(caseId: string, ids: string[]) {
  const response = await fetch(`/api/admin/cases/${caseId}/documents/order`, {
    method: 'POST',
    headers: await mutationHeaders(),
    body: JSON.stringify({ ids }),
  })
  return readBody<CaseDetail>(response, 'The documents could not be reordered.')
}

export async function fetchReadingCases() {
  const response = await fetch('/api/client/cases', { headers: { Accept: 'application/json' } })
  return readBody<ReadingCase[]>(response, 'The cases could not be read.')
}

export async function fetchReadingCase(id: string) {
  const response = await fetch(`/api/client/cases/${id}`, { headers: { Accept: 'application/json' } })
  return readBody<ReadingCase>(response, 'That case is not available.')
}

export async function fetchPapers() {
  const response = await fetch('/api/client/documents', { headers: { Accept: 'application/json' } })
  return readBody<ReadingDocument[]>(response, 'The documents could not be read.')
}

export async function fetchPaper(id: string) {
  const response = await fetch(`/api/client/documents/${id}`, { headers: { Accept: 'application/json' } })
  return readBody<ReadingPaper>(response, 'That document is not available.')
}
