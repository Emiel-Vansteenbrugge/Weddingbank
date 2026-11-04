import { useEffect, useLayoutEffect } from 'react'
import type { AppRoute } from '../../app/routes.ts'
import { homeFor } from '../../app/routes.ts'
import { visit } from './api.ts'
import { LoginPage } from './LoginPage.tsx'
import { useSession } from './useSession.ts'
import { ClientLayout } from '../client/ClientLayout.tsx'
import { ClientPages } from '../client/ClientPages.tsx'
import { AdminLayout } from '../admin/AdminLayout.tsx'
import { AdminPages } from '../admin/AdminPages.tsx'
import { SiteLink } from '../../shared/SiteLink.tsx'

type PrivateRoute = Exclude<AppRoute, { area: 'public' }>

export function PrivateApp({ path, route }: { path: string; route: PrivateRoute }) {
  const session = useSession()
  const needsLogin = session.state.status === 'anonymous' && route.area !== 'login'

  useLayoutEffect(() => {
    if (path !== '/client-access') return
    window.history.replaceState({}, '', '/login')
    window.dispatchEvent(new PopStateEvent('popstate'))
  }, [path])

  useEffect(() => {
    if (needsLogin) visit('/login')
  }, [needsLogin])

  if (route.area === 'login') return <LoginPage />

  if (session.state.status === 'loading') {
    return (
      <main id="main" className="panel-status">
        <p>Opening the record…</p>
      </main>
    )
  }

  if (session.state.status === 'error') {
    return (
      <main id="main" className="panel-status">
        <h1>The record cannot be opened just now.</h1>
        <p>The house could not confirm this session.</p>
        <button className="quiet-button" type="button" onClick={session.retry}>
          Try again
        </button>
      </main>
    )
  }

  if (session.state.status !== 'ready') {
    return (
      <main id="main" className="panel-status">
        <p>Sign in to continue.</p>
      </main>
    )
  }

  const user = session.state.user
  const home = homeFor(user.roles)

  async function logout() {
    await session.signOut()
    visit('/login')
  }

  if (route.area === 'client' && !user.roles.includes('CLIENT')) {
    return <Forbidden home={home} onLogout={logout} />
  }

  if (route.area === 'admin' && !user.roles.includes('ADMIN')) {
    return <Forbidden home={home} onLogout={logout} />
  }

  if (route.area === 'client') {
    return (
      <ClientLayout path={path} displayName={user.displayName} onLogout={logout}>
        <ClientPages
          page={route.page}
          displayName={user.displayName}
          login={user.login}
          caseId={route.caseId}
          documentId={route.documentId}
        />
      </ClientLayout>
    )
  }

  return (
    <AdminLayout path={path} displayName={user.displayName} onLogout={logout}>
      <AdminPages page={route.page} participant={route.participant} caseId={route.caseId} documentId={route.documentId} />
    </AdminLayout>
  )
}

function Forbidden({ home, onLogout }: { home: string | null; onLogout: () => Promise<void> }) {
  useEffect(() => {
    document.title = 'Access — Ambre & Jules'
  }, [])

  return (
    <main id="main" className="gate">
      <p className="eyebrow">Access</p>
      <h1>This record is not open to you.</h1>
      <p className="lede">The other side of the house keeps its own accounts.</p>
      {home ? (
        <SiteLink href={home} className="text-link">
          Continue
        </SiteLink>
      ) : null}
      <button className="quiet-button" type="button" onClick={() => void onLogout()}>
        Sign out
      </button>
    </main>
  )
}
