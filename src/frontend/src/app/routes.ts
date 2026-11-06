export type ClientPageId =
  | 'dashboard'
  | 'accounts'
  | 'cases'
  | 'case'
  | 'tasks'
  | 'quest'
  | 'documents'
  | 'document'
  | 'correspondence'
  | 'archive'
  | 'profile'
  | 'not-found'

export type AdminPageId =
  | 'overview'
  | 'participants'
  | 'participant'
  | 'cases'
  | 'case'
  | 'case-document'
  | 'tasks'
  | 'quest'
  | 'quest-task'
  | 'hints'
  | 'phone-requests'
  | 'events'
  | 'audit-log'
  | 'settings'
  | 'not-found'

export type AppRoute =
  | { area: 'heist' }
  | { area: 'login' }
  | { area: 'admin'; page: AdminPageId; participant?: string; caseId?: string; documentId?: string; questId?: string; taskId?: string }

const adminPages: Record<string, AdminPageId> = {
  '/admin': 'overview',
  '/admin/participants': 'participants',
  '/admin/cases': 'cases',
  '/admin/tasks': 'tasks',
  '/admin/hints': 'hints',
  '/admin/phone-requests': 'phone-requests',
  '/admin/events': 'events',
  '/admin/audit-log': 'audit-log',
  '/admin/settings': 'settings',
}

export const heistEntryPath = '/heist'

export function matchRoute(path: string): AppRoute {
  if (path === '/login') return { area: 'login' }
  if (path === '/admin' || path.startsWith('/admin/')) {
    if (path.startsWith('/admin/participants/')) {
      const participant = path.slice('/admin/participants/'.length).toLowerCase()
      if (participant === 'juul' || participant === 'juul-v') {
        return { area: 'admin', page: 'participant', participant: 'Juul-V' }
      }
      if (participant === 'amber' || participant === 'amber-d') {
        return { area: 'admin', page: 'participant', participant: 'Amber-D' }
      }
    }
    if (path === '/admin/tasks/new') return { area: 'admin', page: 'quest', questId: 'new' }
    const adminQuestTask = /^\/admin\/tasks\/([0-9a-f-]{36})\/tasks\/(new|[0-9a-f-]{36})$/i.exec(path)
    if (adminQuestTask) return { area: 'admin', page: 'quest-task', questId: adminQuestTask[1], taskId: adminQuestTask[2] }
    const adminQuest = /^\/admin\/tasks\/([0-9a-f-]{36})$/i.exec(path)
    if (adminQuest) return { area: 'admin', page: 'quest', questId: adminQuest[1] }
    if (path === '/admin/cases/new') return { area: 'admin', page: 'case', caseId: 'new' }
    const adminDocument = /^\/admin\/cases\/([0-9a-f-]{36})\/documents\/(new|[0-9a-f-]{36})$/i.exec(path)
    if (adminDocument) {
      return { area: 'admin', page: 'case-document', caseId: adminDocument[1], documentId: adminDocument[2] }
    }
    const adminCase = /^\/admin\/cases\/([0-9a-f-]{36})$/i.exec(path)
    if (adminCase) return { area: 'admin', page: 'case', caseId: adminCase[1] }
    return { area: 'admin', page: adminPages[path] ?? 'not-found' }
  }
  return { area: 'heist' }
}

export function isCurrentPage(path: string, href: string) {
  if (href === '/admin') return path === href
  return path === href || path.startsWith(`${href}/`)
}

export function homeFor(roles: readonly string[]) {
  if (roles.includes('ADMIN')) return '/admin'
  return null
}
