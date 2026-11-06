import { useEffect } from 'react'
import type { AppRoute } from '../../app/routes.ts'
import { homeFor } from '../../app/routes.ts'
import { visit } from './api.ts'
import { LoginPage } from './LoginPage.tsx'
import { useSession } from './useSession.ts'
import { AdminLayout } from '../admin/AdminLayout.tsx'
import { AdminPages } from '../admin/AdminPages.tsx'
import { SiteLink } from '../../shared/SiteLink.tsx'

type PrivateRoute = Exclude<AppRoute, { area: 'heist' }>

export function PrivateApp({ path, route }: { path: string; route: PrivateRoute }) {
  const session = useSession()
  const needsLogin = session.state.status === 'anonymous' && route.area !== 'login'

  useEffect(() => {
    if (needsLogin) visit('/login')
  }, [needsLogin])

  if (route.area === 'login') return <LoginPage />

  if (session.state.status === 'loading') {
    return (
      <main id="main" className="panel-status">
        <p>Opening the desk…</p>
      </main>
    )
  }

  if (session.state.status === 'error') {
    return (
      <main id="main" className="panel-status">
        <h1>The desk cannot be opened just now.</h1>
        <p>The session could not be confirmed.</p>
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

  if (route.area === 'admin' && !user.roles.includes('ADMIN')) {
    return <Forbidden home={home} onLogout={logout} />
  }

  return (
    <AdminLayout path={path} displayName={user.displayName} onLogout={logout}>
      <AdminPages
        page={route.page}
        participant={route.participant}
        caseId={route.caseId}
        documentId={route.documentId}
        questId={route.questId}
        taskId={route.taskId}
      />
    </AdminLayout>
  )
}

function Forbidden({ home, onLogout }: { home: string | null; onLogout: () => Promise<void> }) {
  useEffect(() => {
    document.title = 'Administrator — Wedding Bank'
  }, [])

  return (
    <main id="main" className="gate">
      <p className="eyebrow">Administrator</p>
      <h1>This desk is not open to you.</h1>
      {home ? <SiteLink href={home}>Continue</SiteLink> : null}
      <button className="quiet-button" type="button" onClick={() => void onLogout()}>
        Sign out
      </button>
    </main>
  )
}
