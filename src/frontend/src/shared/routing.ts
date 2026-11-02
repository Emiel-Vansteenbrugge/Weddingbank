import { useEffect, useState, type MouseEvent } from 'react'

export function normalizePath(pathname: string) {
  if (pathname.length > 1 && pathname.endsWith('/')) {
    return pathname.slice(0, -1)
  }
  return pathname || '/'
}

export function usePath() {
  const [path, setPath] = useState(() => normalizePath(window.location.pathname))

  useEffect(() => {
    const onChange = () => {
      setPath(normalizePath(window.location.pathname))
      window.scrollTo({ top: 0, left: 0 })
    }
    window.addEventListener('popstate', onChange)
    return () => window.removeEventListener('popstate', onChange)
  }, [])

  return path
}

export function followLink(event: MouseEvent<HTMLAnchorElement>, href: string) {
  if (
    event.defaultPrevented ||
    event.button !== 0 ||
    event.metaKey ||
    event.ctrlKey ||
    event.shiftKey ||
    event.altKey
  ) {
    return
  }
  const target = event.currentTarget.target
  if (target && target !== '_self') return
  event.preventDefault()
  if (normalizePath(window.location.pathname) !== href) {
    window.history.pushState({}, '', href)
  }
  window.dispatchEvent(new PopStateEvent('popstate'))
}
