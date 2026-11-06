import { renderToStaticMarkup } from 'react-dom/server'
import { expect, test } from 'vitest'
import type { ReadingDocument } from '../cases/case-api.ts'
import { DocumentList } from './DocumentList.tsx'

const paper = (title: string, isNew: boolean): ReadingDocument => ({
  id: '11111111-1111-1111-1111-111111111111',
  caseId: '22222222-2222-2222-2222-222222222222',
  caseReference: 'CH-I-001',
  caseTitle: 'The Account',
  reference: 'CH-I-DOC-010',
  title,
  category: 'Register',
  description: null,
  displayOrder: 9,
  isNew,
})

test('documents are one clickable row, with New only while unread', () => {
  const html = renderToStaticMarkup(
    <DocumentList documents={[paper('Departmental Transfer Register', true), { ...paper('October 2026 Closing Register', false), id: '33333333-3333-3333-3333-333333333333' }]} />,
  )
  expect(html).toContain('class="document-list"')
  expect(html).not.toContain('empty-list')
  expect(html).toContain('href="/client/documents/11111111-1111-1111-1111-111111111111"')
  expect(html).toContain('Departmental Transfer Register')
  expect(html).toContain('Register')
  expect(html).toContain('>New<')
  expect(html.match(/document-row/g)?.length).toBe(2)
  expect(html.match(/>New</g)?.length).toBe(1)
})
