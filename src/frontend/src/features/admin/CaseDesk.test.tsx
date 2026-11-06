import { renderToStaticMarkup } from 'react-dom/server'
import { expect, test } from 'vitest'
import { CaseCatalogue } from './CaseDesk.tsx'

test('the case desk shows reference, audience and status', () => {
  const html = renderToStaticMarkup(
    <CaseCatalogue
      cases={[
        {
          id: '11111111-1111-1111-1111-111111111111',
          reference: 'ARCHIVE-2019-047',
          title: 'The vault',
          summary: 'A closed file.',
          status: 'DRAFT',
          audience: 'JUUL',
          displayOrder: 0,
          documentCount: 2,
          updatedAt: '2026-11-04T00:00:00Z',
        },
      ]}
    />,
  )
  expect(html).toContain('ARCHIVE-2019-047')
  expect(html).toContain('Juul')
  expect(html).toContain('Draft')
  expect(html).toContain('href="/admin/cases/11111111-1111-1111-1111-111111111111"')
})
