import { useEffect } from 'react'
import { bank } from '../public/content.ts'
import type { ClientPageId } from '../../app/routes.ts'
import { ClientHome, ClientProfile } from './RelationshipPanel.tsx'

const pages: Record<Exclude<ClientPageId, 'dashboard' | 'profile' | 'not-found'>, { title: string; empty: string }> = {
  accounts: { title: 'Accounts', empty: 'No accounts are open in this view.' },
  transactions: { title: 'Transactions', empty: 'No transactions are posted in this view.' },
  documents: { title: 'Documents', empty: 'No documents are held in this view.' },
  correspondence: { title: 'Correspondence', empty: 'No correspondence is held in this view.' },
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
      : page === 'profile'
        ? 'Profile'
        : page === 'not-found'
          ? 'Page not found'
          : pages[page].title

  useEffect(() => {
    document.title = `${title} — ${bank.name}`
  }, [title])

  if (page === 'not-found') {
    return (
      <>
        <p className="eyebrow">Client record</p>
        <h1>This page is not part of the record.</h1>
      </>
    )
  }

  if (page === 'profile') return <ClientProfile displayName={displayName} login={login} />

  if (page === 'dashboard') return <ClientHome displayName={displayName} />

  const content = pages[page]
  return (
    <>
      <p className="eyebrow">{content.title}</p>
      <h1>{content.title}</h1>
      <p className="section-note">{content.empty}</p>
    </>
  )
}
