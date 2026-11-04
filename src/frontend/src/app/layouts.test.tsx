import { renderToStaticMarkup } from 'react-dom/server'
import { expect, test } from 'vitest'
import { AdminLayout } from '../features/admin/AdminLayout.tsx'
import { AdminPages } from '../features/admin/AdminPages.tsx'
import { ClientLayout } from '../features/client/ClientLayout.tsx'
import { ClientPages } from '../features/client/ClientPages.tsx'
import { NotFoundPage } from '../features/public/ClientAccessPage.tsx'

test('the client layout names the record and keeps the desk out', () => {
  const html = renderToStaticMarkup(
    <ClientLayout path="/client" displayName="Juul" onLogout={async () => {}}>
      <ClientPages page="dashboard" displayName="Juul" login="juul" />
    </ClientLayout>,
  )
  expect(html).toContain('Welcome, Juul.')
  expect(html).toContain('Opening the relationship')
  expect(html).toContain('href="/client/accounts"')
  expect(html).toContain('Transactions')
  expect(html).toContain('Documents')
  expect(html).toContain('Correspondence')
  expect(html).toContain('Profile')
  expect(html).toContain('Sign out')
  expect(html).not.toContain('Participants')
})

test('the administrator layout names the desk', () => {
  const html = renderToStaticMarkup(
    <AdminLayout path="/admin" displayName="Administrator" onLogout={async () => {}}>
      <AdminPages page="overview" />
    </AdminLayout>,
  )
  expect(html).toContain('Active participants')
  expect(html).toContain('Overall progress')
  expect(html).toContain('Open tasks')
  expect(html).toContain('Recent events')
  expect(html).toContain('href="/admin/participants"')
  expect(html).toContain('href="/admin/cases"')
  expect(html).toContain('href="/admin/tasks"')
  expect(html).toContain('href="/admin/communications"')
  expect(html).toContain('href="/admin/phone-requests"')
  expect(html).toContain('href="/admin/events"')
  expect(html).toContain('href="/admin/audit-log"')
  expect(html).toContain('href="/admin/settings"')

  const settings = renderToStaticMarkup(
    <AdminLayout path="/admin/settings" displayName="Administrator" onLogout={async () => {}}>
      <AdminPages page="settings" />
    </AdminLayout>,
  )
  expect(settings).toContain('House sign-ins')
  expect(settings).toContain('juul')
  expect(settings).toContain('amber')
  expect(settings).toContain('administrator')
  expect(settings).toContain('Set password')
  expect(html).toContain('Sign out')
})

test('unknown rooms stay inside the house that owns them', () => {
  const client = renderToStaticMarkup(
    <ClientLayout path="/client/vault" displayName="Juul" onLogout={async () => {}}>
      <ClientPages page="not-found" displayName="Juul" login="juul" />
    </ClientLayout>,
  )
  expect(client).toContain('not part of the record')
  expect(client).toContain('href="/client/accounts"')

  const admin = renderToStaticMarkup(
    <AdminLayout path="/admin/vault" displayName="Administrator" onLogout={async () => {}}>
      <AdminPages page="not-found" />
    </AdminLayout>,
  )
  expect(admin).toContain('not part of the desk')
  expect(admin).toContain('href="/admin/cases"')

  const publicPage = renderToStaticMarkup(<NotFoundPage />)
  expect(publicPage).toContain('This page is not part of the house.')
})
