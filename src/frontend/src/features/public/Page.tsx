import { useEffect, type ReactNode } from 'react'
import { bank } from './content.ts'

type PageProps = {
  title: string
  children: ReactNode
}

export function Page({ title, children }: PageProps) {
  useEffect(() => {
    document.title = title === 'Home' ? `${bank.name} — ${bank.descriptor}` : `${title} — ${bank.name}`
  }, [title])

  return <main id="main">{children}</main>
}
