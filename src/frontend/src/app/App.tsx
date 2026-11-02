import { articles } from '../features/public/content.ts'
import { AboutPage } from '../features/public/AboutPage.tsx'
import { ClientAccessPage, NotFoundPage } from '../features/public/ClientAccessPage.tsx'
import { ContactPage } from '../features/public/ContactPage.tsx'
import { ExpertisePage } from '../features/public/ExpertisePage.tsx'
import { HomePage } from '../features/public/HomePage.tsx'
import { ArticlePage, InsightsPage } from '../features/public/InsightsPage.tsx'
import { ServicesPage } from '../features/public/ServicesPage.tsx'
import { Layout } from '../shared/Layout.tsx'
import { usePath } from '../shared/routing.ts'

export default function App() {
  const path = usePath()
  const articleSlug = path.startsWith('/insights/') ? path.slice('/insights/'.length) : ''
  const article = articles.find((item) => item.slug === articleSlug)

  let page = <NotFoundPage />
  if (path === '/') page = <HomePage />
  else if (path === '/about') page = <AboutPage />
  else if (path === '/services') page = <ServicesPage />
  else if (path === '/expertise') page = <ExpertisePage />
  else if (path === '/insights') page = <InsightsPage />
  else if (article) page = <ArticlePage article={article} />
  else if (path === '/contact') page = <ContactPage />
  else if (path === '/client-access') page = <ClientAccessPage />

  return <Layout path={path}>{page}</Layout>
}
