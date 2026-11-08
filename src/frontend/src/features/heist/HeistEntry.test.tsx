import { renderToStaticMarkup } from 'react-dom/server'
import { expect, test } from 'vitest'
import { Entrance, HeistEntry, HeistStage, Interior } from './HeistEntry.tsx'
import type { HeistRecord } from './heist-api.ts'

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

function record(stage: string): HeistRecord {
  return {
    title: 'Wedding Bank – The Great Heist',
    status: 'CLOSED',
    stage,
    updatedAt: '2026-11-06T22:00:00.000Z',
    safe: { present: true, amountEuros: 250, currency: 'EUR', combinationRevealed: false },
  }
}

test('the entrance is its own screen with one entry button', () => {
  const html = renderToStaticMarkup(
    <HeistStage record={record('entrance')} pending={false} entryError={null} onEnter={() => {}} />,
  )
  expect(html).toContain('class="heist"')
  expect(html).toContain('The Great Heist')
  expect(html).toContain('Enter the bank')
  expect(html).not.toContain('class="interior"')
})

test('the interior is a separate empty screen', () => {
  const html = renderToStaticMarkup(<Interior />)
  expect(html).toContain('class="interior"')
  expect(html).toContain('aria-label="Interior"')
  expect(html).toContain('Shut until the €250 is recorded')
  expect(html).toContain('class="interior-floor"')
  expect(html).not.toContain('The Great Heist')
  expect(html).not.toContain('<button')
  expect(html).not.toContain('Enter the bank')
  expect(renderToStaticMarkup(
    <Entrance notice={null} pending={false} entryError={null} onEnter={() => {}} />,
  )).not.toContain('class="interior"')
})
