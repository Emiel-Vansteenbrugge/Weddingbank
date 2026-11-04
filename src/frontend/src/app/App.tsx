import { articles } from '../features/public/content.ts'
import { AboutPage } from '../features/public/AboutPage.tsx'
import { ClientAccessPage, NotFoundPage } from '../features/public/ClientAccessPage.tsx'
import { ContactPage } from '../features/public/ContactPage.tsx'
import { ExpertisePage } from '../features/public/ExpertisePage.tsx'
import { HomePage } from '../features/public/HomePage.tsx'
import { ArticlePage, InsightsPage } from '../features/public/InsightsPage.tsx'
import { ServicesPage } from '../features/public/ServicesPage.tsx'
import { PrivateApp } from '../features/auth/PrivateApp.tsx'
import { SessionProvider } from '../features/auth/Session.tsx'
import { Layout } from '../shared/Layout.tsx'
import { usePath } from '../shared/routing.ts'
import { matchRoute, type AppRoute } from './routes.ts'

export default function App() {
  const path = usePath()
  const route = matchRoute(path)

  if (route.area !== 'public') {
    return (
      <SessionProvider>
        <PrivateApp path={path} route={route} />
      </SessionProvider>
    )
  }

  return <Layout path={path}>{publicPage(route)}</Layout>
}

function publicPage(route: Extract<AppRoute, { area: 'public' }>) {
  if (route.page === 'home') return <HomePage />
  if (route.page === 'about') return <AboutPage />
  if (route.page === 'services') return <ServicesPage />
  if (route.page === 'expertise') return <ExpertisePage />
  if (route.page === 'insights') return <InsightsPage />
  if (route.page === 'contact') return <ContactPage />
  if (route.page === 'client-access') return <ClientAccessPage />
  if (route.page === 'article') {
    const article = articles.find((item) => item.slug === route.slug)
    if (article) return <ArticlePage article={article} />
  }
  return <NotFoundPage />
}
