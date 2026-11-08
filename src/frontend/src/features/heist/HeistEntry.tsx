import { useEffect, useLayoutEffect, useRef, useState } from 'react'
import { heistEntryPath } from '../../app/routes.ts'
import { enterHeist, fetchHeist, type HeistRecord } from './heist-api.ts'

type HeistState =
  | { status: 'loading' }
  | { status: 'error' }
  | { status: 'ready'; record: HeistRecord }

export function HeistEntry({ path }: { path: string }) {
  const [state, setState] = useState<HeistState>({ status: 'loading' })
  const [pending, setPending] = useState(false)
  const [entryError, setEntryError] = useState<string | null>(null)
  const pendingRef = useRef(false)

  useLayoutEffect(() => {
    if (path === heistEntryPath) return
    window.history.replaceState({}, '', heistEntryPath)
    window.dispatchEvent(new PopStateEvent('popstate'))
  }, [path])

  useEffect(() => {
    let active = true
    async function load() {
      try {
        const record = await fetchHeist()
        if (!active || pendingRef.current) return
        setState((current) => {
          if (current.status === 'ready' && Date.parse(record.updatedAt) < Date.parse(current.record.updatedAt)) {
            return current
          }
          return { status: 'ready', record }
        })
      } catch {
        if (active) {
          setState((current) => (current.status === 'ready' ? current : { status: 'error' }))
        }
      }
    }
    void load()
    const timer = window.setInterval(() => void load(), 2000)
    return () => {
      active = false
      window.clearInterval(timer)
    }
  }, [])

  const inside = state.status === 'ready' && state.record.stage === 'inside'

  useEffect(() => {
    document.title = inside ? 'Interior – Wedding Bank' : 'Wedding Bank – The Great Heist'
  }, [inside])

  useEffect(() => {
    if (!inside) return
    const hold = () => {
      window.history.pushState({ heist: 'inside' }, '', heistEntryPath)
    }
    hold()
    window.addEventListener('popstate', hold)
    return () => window.removeEventListener('popstate', hold)
  }, [inside])

  async function enter() {
    if (pendingRef.current) return
    pendingRef.current = true
    setPending(true)
    setEntryError(null)
    try {
      const record = await enterHeist()
      setState({ status: 'ready', record })
    } catch (caught) {
      setEntryError(caught instanceof Error ? caught.message : 'The entrance could not be left.')
    } finally {
      pendingRef.current = false
      setPending(false)
    }
  }

  if (inside) return <Interior />

  return (
    <Entrance
      notice={state.status === 'loading' ? 'reading' : state.status === 'error' ? 'error' : null}
      pending={pending}
      entryError={entryError}
      onEnter={() => void enter()}
    />
  )
}

export function Entrance({
  notice,
  pending,
  entryError,
  onEnter,
}: {
  notice: 'reading' | 'error' | null
  pending: boolean
  entryError: string | null
  onEnter: () => void
}) {
  return (
    <main id="main" className="heist">
      <a className="skip-link" href="#main">
        Skip to content
      </a>
      <p className="heist-kicker">Wedding Bank</p>
      <h1>The Great Heist</h1>
      <hr className="heist-rule" />
      <p>One shared game. Both of you use this room. There is no player sign-in.</p>
      <p className="heist-safe">A physical safe holding €250 is part of this game. Its combination is not available here.</p>
      {notice === 'reading' ? <p>Reading the shared record…</p> : null}
      {notice === 'error' ? <p role="alert">The shared record cannot be read just now.</p> : null}
      {notice === null ? (
        <>
          {entryError ? <p role="alert">{entryError}</p> : null}
          <button type="button" onClick={onEnter} disabled={pending}>
            Enter the bank
          </button>
        </>
      ) : null}
    </main>
  )
}

export function Interior() {
  return (
    <div className="interior">
      <a className="skip-link" href="#main">
        Skip to content
      </a>
      <header className="interior-bar">
        <p className="interior-mark">Wedding Bank</p>
        <nav aria-label="Interior">
          <span aria-current="page">Interior</span>
        </nav>
        <p className="interior-status">Shut until the €250 is recorded</p>
      </header>
      <div className="interior-body">
        <nav className="interior-rail" aria-label="Places">
          <span aria-current="page">Interior</span>
        </nav>
        <main id="main" className="interior-floor" />
      </div>
    </div>
  )
}

export function HeistStage({
  record,
  pending,
  entryError,
  onEnter,
}: {
  record: HeistRecord
  pending: boolean
  entryError: string | null
  onEnter: () => void
}) {
  if (record.stage === 'inside') return <Interior />
  return <Entrance notice={null} pending={pending} entryError={entryError} onEnter={onEnter} />
}
