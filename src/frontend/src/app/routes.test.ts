import { expect, test } from 'vitest'
import { matchRoute } from './routes.ts'

test('public rooms keep their existing addresses', () => {
  expect(matchRoute('/')).toEqual({ area: 'public', page: 'home' })
  expect(matchRoute('/about')).toEqual({ area: 'public', page: 'about' })
  expect(matchRoute('/services')).toEqual({ area: 'public', page: 'services' })
  expect(matchRoute('/client-access')).toEqual({ area: 'public', page: 'client-access' })
  expect(matchRoute('/insights/patience-and-the-private-mandate')).toEqual({
    area: 'public',
    page: 'article',
    slug: 'patience-and-the-private-mandate',
  })
})

test('unknown addresses are refused in the house they belong to', () => {
  expect(matchRoute('/not-a-room')).toEqual({ area: 'public', page: 'not-found' })
  expect(matchRoute('/insights/missing')).toEqual({ area: 'public', page: 'not-found' })
  expect(matchRoute('/client/vault')).toEqual({ area: 'client', page: 'not-found' })
  expect(matchRoute('/admin/vault')).toEqual({ area: 'admin', page: 'not-found' })
  expect(matchRoute('/login')).toEqual({ area: 'login' })
  expect(matchRoute('/client/accounts')).toEqual({ area: 'client', page: 'accounts' })
  expect(matchRoute('/client/transactions')).toEqual({ area: 'client', page: 'not-found' })
  expect(matchRoute('/client/archive')).toEqual({ area: 'client', page: 'archive' })
  expect(matchRoute('/admin/phone-requests')).toEqual({ area: 'admin', page: 'phone-requests' })
  expect(matchRoute('/admin/hints')).toEqual({ area: 'admin', page: 'hints' })
  expect(matchRoute('/admin/participants/juul')).toEqual({ area: 'admin', page: 'participant', participant: 'juul' })
  expect(matchRoute('/admin/participants/new')).toEqual({ area: 'admin', page: 'not-found' })
})
