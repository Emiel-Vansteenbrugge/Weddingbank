import { renderToStaticMarkup } from 'react-dom/server'
import { expect, test } from 'vitest'
import { HeistEntry } from './HeistEntry.tsx'

test('the room is the shared entrance and not a player sign-in', () => {
  const html = renderToStaticMarkup(<HeistEntry path="/heist" />)
  expect(html).toContain('The Great Heist')
  expect(html).toContain('One shared game')
  expect(html).toContain('no player sign-in')
  expect(html).toContain('physical safe holding €250')
  expect(html).toContain('combination is not available')
  expect(html).toContain('Reading the shared record')
  expect(html).not.toContain('Password')
  expect(html).not.toContain('Client access')
  expect(html).not.toContain('Sign in')
})
