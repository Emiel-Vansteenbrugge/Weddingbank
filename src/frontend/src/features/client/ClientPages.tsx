import { useEffect, useState } from 'react'
import { bank } from '../public/content.ts'
import type { ClientPageId } from '../../app/routes.ts'
import { SiteLink } from '../../shared/SiteLink.tsx'
import { fetchDesk, type ParticipantDesk } from '../game/game-api.ts'

const rooms: Record<
  Exclude<ClientPageId, 'dashboard' | 'accounts' | 'profile' | 'not-found'>,
  { title: string; empty: string }
> = {
  cases: { title: 'Cases', empty: 'No dossier is open in your name.' },
  tasks: { title: 'Tasks', empty: 'Nothing is waiting for you.' },
  documents: { title: 'Documents', empty: 'No papers are held for you.' },
  correspondence: { title: 'Correspondence', empty: 'The house has not written.' },
  archive: { title: 'Archive', empty: 'The archive has not been opened.' },
}

type ClientPagesProps = {
  page: ClientPageId
  displayName: string
  login: string
}

export function ClientPages({ page, displayName, login }: ClientPagesProps) {
  const title =
    page === 'dashboard'
      ? 'Dashboard'
      : page === 'accounts'
        ? 'Accounts'
        : page === 'profile'
          ? 'Profile'
          : page === 'not-found'
            ? 'Page not found'
            : rooms[page].title

  useEffect(() => {
    document.title = `${title} — ${bank.name}`
  }, [title])

  if (page === 'not-found') {
    return (
      <>
        <p className="eyebrow">Private account</p>
        <h1>This page is not part of the record.</h1>
      </>
    )
  }

  if (page === 'profile') {
    return (
      <>
        <p className="eyebrow">Sign-in</p>
        <h1>{displayName}</h1>
        <dl className="record">
          <dt>Name</dt>
          <dd>{displayName}</dd>
          <dt>Login</dt>
          <dd>{login}</dd>
        </dl>
        <p className="section-note">This is the account used to enter. It is not the private account held with the house.</p>
      </>
    )
  }

  if (page === 'dashboard' || page === 'accounts') {
    return <LiveDesk page={page} />
  }

  const room = rooms[page]
  return (
    <>
      <p className="eyebrow">{room.title}</p>
      <h1>{room.title}</h1>
      <p className="section-note">{room.empty}</p>
    </>
  )
}

function LiveDesk({ page }: { page: 'dashboard' | 'accounts' }) {
  const [desk, setDesk] = useState<ParticipantDesk | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let active = true
    fetchDesk()
      .then((record) => {
        if (active) setDesk(record)
      })
      .catch((caught: unknown) => {
        if (active) setError(caught instanceof Error ? caught.message : 'Your record could not be read.')
      })
    return () => {
      active = false
    }
  }, [])

  if (error) {
    return (
      <p className="section-note" role="alert">
        {error}
      </p>
    )
  }

  if (!desk) return <p className="section-note">Opening your record…</p>

  if (page === 'accounts') {
    return (
      <>
        <p className="eyebrow">Accounts</p>
        <h1>{desk.account.title}</h1>
        <dl className="record">
          <dt>Held for</dt>
          <dd>{desk.account.holder}</dd>
          <dt>With the house</dt>
          <dd>{desk.account.note}</dd>
        </dl>
      </>
    )
  }

  return (
    <>
      <p className="eyebrow">Dashboard</p>
      <h1>Welcome, {desk.displayName}.</h1>
      <p className="section-note">
        {desk.stage === 'Not started'
          ? 'The matter has not started. Nothing new is waiting.'
          : `Your matter is ${desk.stage.toLowerCase()}.`}
      </p>
      <div className="empty-list">
        <section>
          <h2>Your progress</h2>
          <p>{desk.stage}</p>
        </section>
        <section>
          <h2>Shared progress</h2>
          <p>{desk.sharedCompletedTasks === 0 ? 'Not started' : `${desk.sharedCompletedTasks} completed`}</p>
        </section>
        <section>
          <h2>Tasks</h2>
          <p>{desk.availableTasks === 0 ? 'Nothing is waiting for you.' : `${desk.availableTasks} open`}</p>
        </section>
        <section>
          <h2>Correspondence</h2>
          <p>{desk.hintsWaiting === 0 ? 'The house has not written.' : `${desk.hintsWaiting} waiting`}</p>
        </section>
      </div>
      <p className="section-note">
        <SiteLink href="/client/profile" className="text-link">
          Sign-in details
        </SiteLink>
      </p>
    </>
  )
}
