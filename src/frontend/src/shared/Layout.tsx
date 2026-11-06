import type { ReactNode } from 'react'
import { Footer } from './Footer.tsx'
import { Header } from './Header.tsx'

type LayoutProps = {
  path: string
  children: ReactNode
}

export function Layout({ path, children }: LayoutProps) {
  return (
    <>
      <Header path={path} />
      {children}
      <Footer />
    </>
  )
}
