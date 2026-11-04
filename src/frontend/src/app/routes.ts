import { articles } from '../features/public/content.ts'

export type PublicPage =
  | 'home'
  | 'about'
  | 'services'
  | 'expertise'
  | 'insights'
  | 'article'
  | 'contact'
  | 'client-access'
  | 'not-found'

export type ClientPageId =
  | 'dashboard'
  | 'accounts'
  | 'cases'
  | 'tasks'
  | 'documents'
  | 'correspondence'
  | 'archive'
  | 'profile'
  | 'not-found'

export type AdminPageId =
  | 'overview'
  | 'participants'
  | 'participant'
  | 'cases'
  | 'tasks'
  | 'hints'
  | 'phone-requests'
  | 'events'
  | 'audit-log'
  | 'settings'
  | 'not-found'

export type AppRoute =
  | { area: 'public'; page: PublicPage; slug?: string }
  | { area: 'login' }
  | { area: 'client'; page: ClientPageId }
  | { area: 'admin'; page: AdminPageId; participant?: string }

const clientPages: Record<string, ClientPageId> = {
  '/client': 'dashboard',
  '/client/accounts': 'accounts',
  '/client/cases': 'cases',
  '/client/tasks': 'tasks',
  '/client/documents': 'documents',
  '/client/correspondence': 'correspondence',
  '/client/archive': 'archive',
  '/client/profile': 'profile',
}

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

export function matchRoute(path: string): AppRoute {
  if (path === '/login') return { area: 'login' }
  if (path === '/client' || path.startsWith('/client/')) {
    return { area: 'client', page: clientPages[path] ?? 'not-found' }
  }
  if (path === '/admin' || path.startsWith('/admin/')) {
    if (path === '/admin/participants/juul' || path === '/admin/participants/amber') {
      return { area: 'admin', page: 'participant', participant: path.slice('/admin/participants/'.length) }
    }
    return { area: 'admin', page: adminPages[path] ?? 'not-found' }
  }
  if (path === '/') return { area: 'public', page: 'home' }
  if (path === '/about') return { area: 'public', page: 'about' }
  if (path === '/services') return { area: 'public', page: 'services' }
  if (path === '/expertise') return { area: 'public', page: 'expertise' }
  if (path === '/insights') return { area: 'public', page: 'insights' }
  if (path === '/contact') return { area: 'public', page: 'contact' }
  if (path === '/client-access') return { area: 'public', page: 'client-access' }
  if (path.startsWith('/insights/')) {
    const slug = path.slice('/insights/'.length)
    if (articles.some((article) => article.slug === slug)) {
      return { area: 'public', page: 'article', slug }
    }
  }
  return { area: 'public', page: 'not-found' }
}

export function isCurrentPage(path: string, href: string) {
  if (href === '/admin' || href === '/client') return path === href
  return path === href || path.startsWith(`${href}/`)
}

export function homeFor(roles: readonly string[]) {
  if (roles.includes('ADMIN')) return '/admin'
  if (roles.includes('CLIENT')) return '/client'
  return null
}
