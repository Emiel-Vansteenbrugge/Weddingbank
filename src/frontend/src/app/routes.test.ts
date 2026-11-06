import { expect, test } from 'vitest'
import { heistEntryPath, matchRoute } from './routes.ts'

test('the QR entry and the old public addresses open the shared room', () => {
  expect(heistEntryPath).toBe('/heist')
  expect(matchRoute('/')).toEqual({ area: 'heist' })
  expect(matchRoute('/heist')).toEqual({ area: 'heist' })
  expect(matchRoute('/about')).toEqual({ area: 'heist' })
  expect(matchRoute('/services')).toEqual({ area: 'heist' })
  expect(matchRoute('/insights/patience-and-the-private-mandate')).toEqual({ area: 'heist' })
  expect(matchRoute('/client-access')).toEqual({ area: 'heist' })
  expect(matchRoute('/client/accounts')).toEqual({ area: 'heist' })
  expect(matchRoute('/client/cases/11111111-1111-1111-1111-111111111111')).toEqual({ area: 'heist' })
})

test('administrator routes stay on the desk', () => {
  expect(matchRoute('/login')).toEqual({ area: 'login' })
  expect(matchRoute('/admin')).toEqual({ area: 'admin', page: 'overview' })
  expect(matchRoute('/admin/vault')).toEqual({ area: 'admin', page: 'not-found' })
  expect(matchRoute('/admin/settings')).toEqual({ area: 'admin', page: 'settings' })
  expect(matchRoute('/admin/audit-log')).toEqual({ area: 'admin', page: 'audit-log' })
  expect(matchRoute('/admin/participants/Juul-V')).toEqual({
    area: 'admin',
    page: 'participant',
    participant: 'Juul-V',
  })
  expect(matchRoute('/admin/cases/11111111-1111-1111-1111-111111111111/documents/new')).toEqual({
    area: 'admin',
    page: 'case-document',
    caseId: '11111111-1111-1111-1111-111111111111',
    documentId: 'new',
  })
})
