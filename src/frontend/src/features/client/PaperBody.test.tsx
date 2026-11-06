import { renderToStaticMarkup } from 'react-dom/server'
import { expect, test } from 'vitest'
import { PaperBody } from './PaperBody.tsx'

const transfer = [
  'WEDDINGBANK',
  'Private Banking · Records Administration',
  '',
  'DEPARTMENTAL TRANSFER REGISTER',
  '',
  'Reference | Routing Category | Receiving Department | Subject',
  '',
  'TR-1030-A | A | Account Services | Account review',
  'TR-1030-B | B | Compliance | Supporting documentation',
  'TR-1030-C | C | Legal Affairs | Supporting documentation',
  'TR-1031-B | B | Records Administration | Correspondence',
  '',
  'Internal administrative record.',
].join('\n')

test('a register keeps its letterhead and draws the rows as a table', () => {
  const html = renderToStaticMarkup(<PaperBody content={transfer} />)
  expect(html).toContain('DEPARTMENTAL TRANSFER REGISTER')
  expect(html).toContain('Internal administrative record.')
  expect(html).toContain('<th scope="col">Receiving Department</th>')
  expect(html).toContain('<td>TR-1030-B</td>')
  expect(html).toContain('<td>Compliance</td>')
  expect(html).not.toContain('TR-1030-B |')
})

test('the closing register is a table without surrounding prose', () => {
  const html = renderToStaticMarkup(
    <PaperBody content={'Reference | Department | Date | Subject\nREG-1026-17 | Records Administration | 29/10/2026 | Supporting documentation'} />,
  )
  expect(html).toContain('<th scope="col">Department</th>')
  expect(html).toContain('<td>REG-1026-17</td>')
  expect(html).not.toContain('<p')
})

test('a letter without columns stays plain text', () => {
  const html = renderToStaticMarkup(<PaperBody content={'WEDDINGBANK\n\nThe original receipt was processed by Records Administration.'} />)
  expect(html).not.toContain('<table')
  expect(html).toContain('Records Administration.')
})
