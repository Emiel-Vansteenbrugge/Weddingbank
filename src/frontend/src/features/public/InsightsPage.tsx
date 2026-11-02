import { articles, disclaimer, type Article } from './content.ts'
import { Page } from './Page.tsx'
import { SiteLink } from '../../shared/SiteLink.tsx'

export function InsightsPage() {
  return (
    <Page title="Insights">
      <header className="page-head">
        <p className="eyebrow">Insights</p>
        <h1>Commentary from the partnership.</h1>
        <p className="lede">{disclaimer}</p>
      </header>
      <ul className="publication-list">
        {articles.map((article) => (
          <li key={article.slug}>
            <p>
              {article.category}
              <span>{article.date}</span>
            </p>
            <h2>
              <SiteLink href={`/insights/${article.slug}`}>{article.title}</SiteLink>
            </h2>
            <p>{article.standfirst}</p>
          </li>
        ))}
      </ul>
    </Page>
  )
}

export function ArticlePage({ article }: { article: Article }) {
  return (
    <Page title={article.title}>
      <article className="article">
        <header className="page-head">
          <p className="eyebrow">
            {article.category}
            <span>{article.date}</span>
          </p>
          <h1>{article.title}</h1>
          <p className="lede">{article.standfirst}</p>
        </header>
        <div className="article-body">
          {article.paragraphs.map((paragraph) => (
            <p key={paragraph}>{paragraph}</p>
          ))}
          <p className="disclaimer">{disclaimer}</p>
          <SiteLink href="/insights" className="text-link">
            All insights
          </SiteLink>
        </div>
      </article>
    </Page>
  )
}
