import { useEffect, useState } from 'react'
import { bank } from '../public/content.ts'
import { fetchOwnProfile, fetchOwnRelationships } from '../admin/client-api.ts'
import { RelationshipSections } from '../admin/ClientViews.tsx'
import type { Relationship } from '../clients/model.ts'

type Load = 'loading' | 'error' | 'ready'

export function ClientHome({ displayName }: { displayName: string }) {
  const [phase, setPhase] = useState<Load>('loading')
  const [message, setMessage] = useState<string | null>(null)
  const [relationships, setRelationships] = useState<Relationship[]>([])

  useEffect(() => {
    document.title = `Dashboard — ${bank.name}`
    let active = true
    fetchOwnRelationships()
      .then((records) => {
        if (!active) return
        setRelationships(records)
        setPhase('ready')
      })
      .catch((caught: unknown) => {
        if (!active) return
        setPhase('error')
        setMessage(caught instanceof Error ? caught.message : 'The relationship could not be read.')
      })
    return () => {
      active = false
    }
  }, [])

  return (
    <>
      <p className="eyebrow">Dashboard</p>
      <h1>Welcome, {displayName}.</h1>
      {phase === 'loading' ? <p className="section-note">Opening the relationship…</p> : null}
      {phase === 'error' ? (
        <p className="section-note" role="alert">
          {message}
        </p>
      ) : null}
      {phase === 'ready' ? <RelationshipSections relationships={relationships} /> : null}
    </>
  )
}

export function ClientProfile({ displayName, login }: { displayName: string; login: string }) {
  const [phase, setPhase] = useState<Load>('loading')
  const [message, setMessage] = useState<string | null>(null)
  const [account, setAccount] = useState({ displayName, login })
  const [relationships, setRelationships] = useState<Relationship[]>([])

  useEffect(() => {
    document.title = `Profile — ${bank.name}`
    let active = true
    Promise.all([fetchOwnProfile(), fetchOwnRelationships()])
      .then(([profile, records]) => {
        if (!active) return
        setAccount(profile)
        setRelationships(records)
        setPhase('ready')
      })
      .catch((caught: unknown) => {
        if (!active) return
        setPhase('error')
        setMessage(caught instanceof Error ? caught.message : 'The profile could not be read.')
      })
    return () => {
      active = false
    }
  }, [])

  return (
    <>
      <p className="eyebrow">Profile</p>
      <h1>{account.displayName}</h1>
      <h2>Account</h2>
      <dl className="record">
        <dt>Name</dt>
        <dd>{account.displayName}</dd>
        <dt>Login</dt>
        <dd>{account.login}</dd>
      </dl>
      <h2>Relationships</h2>
      <p className="section-note">Relationship details are held by the house and are not changed from this page.</p>
      {phase === 'loading' ? <p className="section-note">Opening the relationship…</p> : null}
      {phase === 'error' ? (
        <p className="section-note" role="alert">
          {message}
        </p>
      ) : null}
      {phase === 'ready' ? <RelationshipSections relationships={relationships} /> : null}
    </>
  )
}
