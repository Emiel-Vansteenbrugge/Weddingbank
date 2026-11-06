import { renderToStaticMarkup } from 'react-dom/server'
import { expect, test } from 'vitest'
import { TestAccountBarView } from './TestAccountBar.tsx'

test('the temporary toolbar names the three house accounts and the active one', () => {
  const html = renderToStaticMarkup(
    <TestAccountBarView
      accounts={[
        { login: 'Juul-V', label: 'Juul' },
        { login: 'Amber-D', label: 'Amber' },
        { login: 'administrator', label: 'Administrator' },
      ]}
      activeLogin="Juul-V"
      pending={null}
      error={null}
      onChoose={() => {}}
    />,
  )

  expect(html).toContain('Temporary test accounts')
  expect(html).toContain('Juul')
  expect(html).toContain('Amber')
  expect(html).toContain('Administrator')
  expect(html).toContain('aria-current="true"')
  expect(html).not.toContain('password')
})
