import { useEffect, useLayoutEffect, useState, type MouseEvent } from 'react'

export function normalizePath(pathname: string) {
  if (pathname.length > 1 && pathname.endsWith('/')) {
    return pathname.slice(0, -1)
  }
  return pathname || '/'
}

export function usePath() {
  const [path, setPath] = useState(() => normalizePath(window.location.pathname))
  const [hash, setHash] = useState(() => window.location.hash)

  useEffect(() => {
    const onChange = () => {
      setPath(normalizePath(window.location.pathname))
      setHash(window.location.hash)
    }
    window.addEventListener('popstate', onChange)
    return () => window.removeEventListener('popstate', onChange)
  }, [])

  useLayoutEffect(() => {
    const id = hash.startsWith('#') ? decodeURIComponent(hash.slice(1)) : ''
    const target = id ? document.getElementById(id) : null
    if (target) {
      target.scrollIntoView({ block: 'start' })
      return
    }
    window.scrollTo({ top: 0, left: 0 })
  }, [path, hash])

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
  let url: URL
  try {
    url = new URL(href, window.location.href)
  } catch {
    return
  }
  if (url.origin !== window.location.origin) return

  event.preventDefault()
  const nextPath = normalizePath(url.pathname)
  const next = `${nextPath}${url.search}${url.hash}`
  const current = `${normalizePath(window.location.pathname)}${window.location.search}${window.location.hash}`
  if (current !== next) {
    window.history.pushState({}, '', next)
  }
  window.dispatchEvent(new PopStateEvent('popstate'))
}
