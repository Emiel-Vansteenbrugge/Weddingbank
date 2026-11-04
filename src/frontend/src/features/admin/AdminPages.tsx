import { useEffect, useState } from 'react'
import { bank } from '../public/content.ts'
import type { AdminPageId } from '../../app/routes.ts'
import { SiteLink } from '../../shared/SiteLink.tsx'
import { fetchAudit, type AuditRow } from '../auth/api.ts'
import { fetchOverview, fetchParticipant, type GameOverview, type ParticipantDesk } from '../game/game-api.ts'
import { HousePasswords } from './HousePasswords.tsx'
import { CaseEditor, CaseList, DocumentEditor } from './CaseDesk.tsx'

const rooms: Record<
  Exclude<AdminPageId, 'overview' | 'participants' | 'participant' | 'audit-log' | 'settings' | 'not-found' | 'cases' | 'case' | 'case-document'>,
  { title: string; empty: string }
> = {
  tasks: {
    title: 'Tasks',
    empty: 'No tasks have been written. Individual and shared tasks can be added when the game is prepared.',
  },
  hints: {
    title: 'Hints & messages',
    empty: 'No hints or messages have been sent. A note can go to Juul, to Amber, or to both once the game is running.',
  },
  'phone-requests': {
    title: 'Phone requests',
    empty: 'No telephone requests are waiting.',
  },
  events: {
    title: 'Events',
    empty: 'No game events are recorded yet. Sign-in remains in the audit log.',
  },
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
  participant?: string
  caseId?: string
  documentId?: string
}

export function AdminPages({ page, participant, caseId, documentId }: AdminPagesProps) {
  const title =
    page === 'overview'
      ? 'Dashboard'
      : page === 'participants' || page === 'participant'
        ? 'Participants'
        : page === 'cases' || page === 'case'
          ? 'Cases'
          : page === 'case-document'
            ? 'Document'
            : page === 'audit-log'
              ? 'Audit log'
              : page === 'settings'
                ? 'Settings'
                : page === 'not-found'
                  ? 'Page not found'
                  : rooms[page].title

  useEffect(() => {
    document.title = `${title} — ${bank.name}`
  }, [title])

  if (page === 'not-found') {
    return (
      <>
        <p className="eyebrow">Game</p>
        <h1>This page is not part of the desk.</h1>
      </>
    )
  }

  if (page === 'cases') return <CaseList />
  if (page === 'case') return <CaseEditor caseId={caseId ?? 'new'} />
  if (page === 'case-document') return <DocumentEditor caseId={caseId ?? ''} documentId={documentId ?? 'new'} />
  if (page === 'settings') return <GameSettings />
  if (page === 'participants') return <ParticipantList />
  if (page === 'participant') return <ParticipantDetail login={participant ?? ''} />
  if (page === 'audit-log') return <AuditLog />
  if (page === 'overview') return <GameDashboard />

  const room = rooms[page]
  return (
    <>
      <p className="eyebrow">{room.title}</p>
      <h1>{room.title}</h1>
      <p className="section-note">{room.empty}</p>
    </>
  )
}

function GameSettings() {
  return (
    <>
      <p className="eyebrow">Settings</p>
      <h1>The game has not started.</h1>
      <p className="section-note">Start, pause and completion stay closed until the game is prepared.</p>
      <HousePasswords />
    </>
  )
}

function GameDashboard() {
  const overview = useOverview()
  if (overview.status === 'loading') return <p className="section-note">Reading the game…</p>
  if (overview.status === 'error') {
    return (
      <p className="section-note" role="alert">
        {overview.message}
      </p>
    )
  }

  const { game } = overview
  return (
    <>
      <p className="eyebrow">Dashboard</p>
      <h1>{game.status === 'NOT_STARTED' ? 'The game has not started.' : 'The game is running.'}</h1>
      <ParticipantTable game={game} />
      <div className="empty-list">
        <section>
          <h2>Shared progress</h2>
          <p>{game.shared.stage}</p>
        </section>
        <section>
          <h2>Answers waiting</h2>
          <p>{game.outstandingSubmissions === 0 ? 'None' : String(game.outstandingSubmissions)}</p>
        </section>
        <section>
          <h2>Phone requests</h2>
          <p>{game.pendingPhoneRequests === 0 ? 'None waiting' : String(game.pendingPhoneRequests)}</p>
        </section>
        <section>
          <h2>Recent hints</h2>
          <p>{game.juul.hintsSent + game.amber.hintsSent + game.shared.hintsSent === 0 ? 'None sent' : 'Recorded'}</p>
        </section>
      </div>
    </>
  )
}

function ParticipantList() {
  const overview = useOverview()
  if (overview.status === 'loading') return <p className="section-note">Reading the participants…</p>
  if (overview.status === 'error') {
    return (
      <p className="section-note" role="alert">
        {overview.message}
      </p>
    )
  }

  return (
    <>
      <p className="eyebrow">Participants</p>
      <h1>Juul and Amber</h1>
      <p className="section-note">These are the only two participants. They are not created or removed here.</p>
      <ParticipantTable game={overview.game} />
    </>
  )
}

function ParticipantTable({ game }: { game: GameOverview }) {
  return (
    <table className="audit-table">
      <thead>
        <tr>
          <th>Participant</th>
          <th>Stage</th>
          <th>Tasks</th>
          <th>Hints</th>
          <th>Last activity</th>
        </tr>
      </thead>
      <tbody>
        {[game.juul, game.amber].map((person) => (
          <tr key={person.login}>
            <td>
              <SiteLink href={`/admin/participants/${person.login}`} className="text-link">
                {person.displayName}
              </SiteLink>
            </td>
            <td>{person.stage}</td>
            <td>{person.availableTasks === 0 && person.completedTasks === 0 ? 'None open' : `${person.completedTasks} done`}</td>
            <td>{person.hintsSent === 0 ? 'None' : String(person.hintsSent)}</td>
            <td>{person.lastActivity ? formatWhen(person.lastActivity) : '—'}</td>
          </tr>
        ))}
      </tbody>
    </table>
  )
}

function ParticipantDetail({ login }: { login: string }) {
  const [desk, setDesk] = useState<ParticipantDesk | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let active = true
    fetchParticipant(login)
      .then((record) => {
        if (active) setDesk(record)
      })
      .catch((caught: unknown) => {
        if (active) setError(caught instanceof Error ? caught.message : 'The participant could not be read.')
      })
    return () => {
      active = false
    }
  }, [login])

  if (error) {
    return (
      <p className="section-note" role="alert">
        {error}
      </p>
    )
  }
  if (!desk) return <p className="section-note">Reading the participant…</p>

  return (
    <>
      <p className="eyebrow">Participant</p>
      <h1>{desk.displayName}</h1>
      <dl className="record">
        <dt>Login</dt>
        <dd>{desk.login}</dd>
        <dt>Stage</dt>
        <dd>{desk.stage}</dd>
        <dt>Individual tasks</dt>
        <dd>{desk.availableTasks === 0 && desk.completedTasks === 0 ? 'None open' : `${desk.completedTasks} completed`}</dd>
        <dt>Shared milestones completed</dt>
        <dd>{desk.sharedCompletedTasks === 0 ? 'None' : String(desk.sharedCompletedTasks)}</dd>
        <dt>Hints and messages</dt>
        <dd>{desk.hintsWaiting === 0 ? 'None sent' : String(desk.hintsWaiting)}</dd>
        <dt>Private account</dt>
        <dd>
          {desk.account.title}. {desk.account.note}
        </dd>
      </dl>
    </>
  )
}

type OverviewState =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; game: GameOverview }

function useOverview() {
  const [state, setState] = useState<OverviewState>({ status: 'loading' })

  useEffect(() => {
    let active = true
    fetchOverview()
      .then((game) => {
        if (active) setState({ status: 'ready', game })
      })
      .catch((caught: unknown) => {
        if (active) {
          setState({
            status: 'error',
            message: caught instanceof Error ? caught.message : 'The game could not be read.',
          })
        }
      })
    return () => {
      active = false
    }
  }, [])

  return state
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
      <p className="section-note">Security events stay here. The game timeline is kept separately.</p>
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
