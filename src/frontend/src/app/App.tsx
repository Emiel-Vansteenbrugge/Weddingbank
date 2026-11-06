import { PrivateApp } from '../features/auth/PrivateApp.tsx'
import { SessionProvider } from '../features/auth/Session.tsx'
import { HeistEntry } from '../features/heist/HeistEntry.tsx'
import { usePath } from '../shared/routing.ts'
import { matchRoute } from './routes.ts'

export default function App() {
  const path = usePath()
  const route = matchRoute(path)

  if (route.area === 'heist') return <HeistEntry path={path} />

  return (
    <SessionProvider>
      <PrivateApp path={path} route={route} />
    </SessionProvider>
  )
}
