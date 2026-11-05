import { expect, test } from 'vitest'
import { matchRoute } from './routes.ts'

test('public rooms keep their existing addresses', () => {
  expect(matchRoute('/')).toEqual({ area: 'public', page: 'home' })
  expect(matchRoute('/about')).toEqual({ area: 'public', page: 'about' })
  expect(matchRoute('/services')).toEqual({ area: 'public', page: 'services' })
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
  expect(matchRoute('/client-access')).toEqual({ area: 'login' })
  expect(matchRoute('/client/accounts')).toEqual({ area: 'client', page: 'accounts' })
  expect(matchRoute('/client/transactions')).toEqual({ area: 'client', page: 'not-found' })
  expect(matchRoute('/client/archive')).toEqual({ area: 'client', page: 'archive' })
  expect(matchRoute('/admin/phone-requests')).toEqual({ area: 'admin', page: 'phone-requests' })
  expect(matchRoute('/admin/hints')).toEqual({ area: 'admin', page: 'hints' })
  expect(matchRoute('/admin/participants/Juul-V')).toEqual({ area: 'admin', page: 'participant', participant: 'Juul-V' })
  expect(matchRoute('/admin/participants/juul')).toEqual({ area: 'admin', page: 'participant', participant: 'Juul-V' })
  expect(matchRoute('/admin/participants/Amber-D')).toEqual({ area: 'admin', page: 'participant', participant: 'Amber-D' })
  expect(matchRoute('/admin/participants/new')).toEqual({ area: 'admin', page: 'not-found' })
  expect(matchRoute('/admin/cases')).toEqual({ area: 'admin', page: 'cases' })
  expect(matchRoute('/admin/cases/new')).toEqual({ area: 'admin', page: 'case', caseId: 'new' })
  expect(matchRoute('/admin/cases/11111111-1111-1111-1111-111111111111')).toEqual({
    area: 'admin',
    page: 'case',
    caseId: '11111111-1111-1111-1111-111111111111',
  })
  expect(matchRoute('/admin/cases/11111111-1111-1111-1111-111111111111/documents/new')).toEqual({
    area: 'admin',
    page: 'case-document',
    caseId: '11111111-1111-1111-1111-111111111111',
    documentId: 'new',
  })
  expect(matchRoute('/client/cases/11111111-1111-1111-1111-111111111111')).toEqual({
    area: 'client',
    page: 'case',
    caseId: '11111111-1111-1111-1111-111111111111',
  })
  expect(matchRoute('/client/documents/22222222-2222-2222-2222-222222222222')).toEqual({
    area: 'client',
    page: 'document',
    documentId: '22222222-2222-2222-2222-222222222222',
  })
  expect(matchRoute('/client/tasks')).toEqual({ area: 'client', page: 'tasks' })
  expect(matchRoute('/client/tasks/33333333-3333-3333-3333-333333333333')).toEqual({
    area: 'client',
    page: 'quest',
    questId: '33333333-3333-3333-3333-333333333333',
  })
  expect(matchRoute('/admin/tasks')).toEqual({ area: 'admin', page: 'tasks' })
  expect(matchRoute('/admin/tasks/new')).toEqual({ area: 'admin', page: 'quest', questId: 'new' })
  expect(matchRoute('/admin/tasks/33333333-3333-3333-3333-333333333333')).toEqual({
    area: 'admin',
    page: 'quest',
    questId: '33333333-3333-3333-3333-333333333333',
  })
  expect(matchRoute('/admin/tasks/33333333-3333-3333-3333-333333333333/tasks/new')).toEqual({
    area: 'admin',
    page: 'quest-task',
    questId: '33333333-3333-3333-3333-333333333333',
    taskId: 'new',
  })
})
