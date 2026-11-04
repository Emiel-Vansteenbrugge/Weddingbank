import { useEffect, useState } from 'react'
import { bank } from '../public/content.ts'
import type { AdminPageId } from '../../app/routes.ts'
import { fetchAudit, type AuditRow } from '../auth/api.ts'
import { ClientCreatePage, ClientDetailPage, ClientListPage } from './ClientScreens.tsx'
import { HousePasswords } from './HousePasswords.tsx'

const pages: Record<
  Exclude<AdminPageId, 'overview' | 'audit-log' | 'settings' | 'not-found' | 'participants' | 'client-new' | 'client-detail'>,
  { title: string; empty: string }
> = {
  cases: { title: 'Cases', empty: 'No cases are open.' },
  tasks: { title: 'Tasks', empty: 'No tasks are open.' },
  communications: { title: 'Communications', empty: 'No communications are recorded yet.' },
  'phone-requests': { title: 'Phone requests', empty: 'No telephone requests are waiting.' },
  events: { title: 'Events', empty: 'No events are recorded yet.' },
}

const eventLabels: Record<string, string> = {
  'login.succeeded': 'Signed in',
  'login.failed': 'Sign-in refused',
  logout: 'Signed out',
  'admin.audit.viewed': 'Audit log opened',
  'client.created': 'Client opened',
  'client.updated': 'Client revised',
  'client.status_changed': 'Client status changed',
  'client.user_linked': 'Account linked',
  'client.user_unlinked': 'Account removed',
  'user.password_changed': 'Password changed',
}

type AdminPagesProps = {
  page: AdminPageId
  clientId?: string
}

export function AdminPages({ page, clientId }: AdminPagesProps) {
  const title =
    page === 'overview'
      ? 'Overview'
      : page === 'audit-log'
        ? 'Audit log'
        : page === 'settings'
          ? 'Settings'
          : page === 'participants'
            ? 'Clients'
            : page === 'client-new'
              ? 'New client'
              : page === 'client-detail'
                ? 'Client'
                : page === 'not-found'
                  ? 'Page not found'
                  : pages[page].title

  useEffect(() => {
    document.title = `${title} — ${bank.name}`
  }, [title])

  if (page === 'not-found') {
    return (
      <>
        <p className="eyebrow">Operations</p>
        <h1>This page is not part of the desk.</h1>
      </>
    )
  }

  if (page === 'settings') return <HousePasswords />

  if (page === 'participants') return <ClientListPage />
  if (page === 'client-new') return <ClientCreatePage />
  if (page === 'client-detail') return <ClientDetailPage clientId={clientId ?? ''} />

  if (page === 'audit-log') return <AuditLog />

  if (page === 'overview') {
    return (
      <>
        <p className="eyebrow">Overview</p>
        <h1>The desk is quiet.</h1>
        <div className="empty-list">
          <section>
            <h2>Active participants</h2>
            <p>Nothing is recorded yet.</p>
          </section>
          <section>
            <h2>Overall progress</h2>
            <p>Nothing is recorded yet.</p>
          </section>
          <section>
            <h2>Open tasks</h2>
            <p>Nothing is recorded yet.</p>
          </section>
          <section>
            <h2>Recent events</h2>
            <p>Nothing is recorded yet.</p>
          </section>
        </div>
      </>
    )
  }

  const content = pages[page]
  return (
    <>
      <p className="eyebrow">{content.title}</p>
      <h1>{content.title}</h1>
      <p className="section-note">{content.empty}</p>
    </>
  )
}

function AuditLog() {
  const [status, setStatus] = useState<'loading' | 'error' | 'ready'>('loading')
  const [message, setMessage] = useState<string | null>(null)
  const [rows, setRows] = useState<AuditRow[]>([])

  useEffect(() => {
    let active = true
    fetchAudit()
      .then((events) => {
        if (!active) return
        setRows(events)
        setStatus('ready')
      })
      .catch((caught: unknown) => {
        if (!active) return
        setMessage(caught instanceof Error ? caught.message : 'The audit log could not be read.')
        setStatus('error')
      })
    return () => {
      active = false
    }
  }, [])

  return (
    <>
      <p className="eyebrow">Audit log</p>
      <h1>Audit log</h1>
      {status === 'loading' ? <p className="section-note">Reading the log…</p> : null}
      {status === 'error' ? (
        <p className="section-note" role="alert">
          {message}
        </p>
      ) : null}
      {status === 'ready' && rows.length === 0 ? <p className="section-note">No events are recorded yet.</p> : null}
      {status === 'ready' && rows.length > 0 ? (
        <table className="audit-table">
          <thead>
            <tr>
              <th>When</th>
              <th>Event</th>
              <th>Actor</th>
              <th>Target</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr key={row.id}>
                <td>{formatWhen(row.occurredAt)}</td>
                <td>{eventLabels[row.eventType] ?? row.eventType}</td>
                <td>{row.actorLogin ?? '—'}</td>
                <td>{row.targetId ?? '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      ) : null}
    </>
  )
}

function formatWhen(value: string) {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return date.toLocaleString('en-GB', { dateStyle: 'medium', timeStyle: 'short' })
}
