import { useEffect, useLayoutEffect, useState } from 'react'
import { heistEntryPath } from '../../app/routes.ts'
import { fetchHeist, type HeistRecord } from './heist-api.ts'

type HeistState =
  | { status: 'loading' }
  | { status: 'error' }
  | { status: 'ready'; record: HeistRecord }

export function HeistEntry({ path }: { path: string }) {
  const [state, setState] = useState<HeistState>({ status: 'loading' })

  useLayoutEffect(() => {
    if (path === heistEntryPath) return
    window.history.replaceState({}, '', heistEntryPath)
    window.dispatchEvent(new PopStateEvent('popstate'))
  }, [path])

  useEffect(() => {
    document.title = 'Wedding Bank – The Great Heist'
  }, [])

  useEffect(() => {
    let active = true
    fetchHeist()
      .then((record) => {
        if (active) setState({ status: 'ready', record })
      })
      .catch(() => {
        if (active) setState({ status: 'error' })
      })
    return () => {
      active = false
    }
  }, [])

  return (
    <main id="main" className="heist">
      <a className="skip-link" href="#main">
        Skip to content
      </a>
      <p className="heist-kicker">Wedding Bank</p>
      <h1>The Great Heist</h1>
      <hr className="heist-rule" />
      <p>One shared game. Both of you use this room. There is no player sign-in.</p>
      <HeistProgress state={state} />
      <p className="heist-safe">A physical safe holding €250 is part of this game. Its combination is not available here.</p>
    </main>
  )
}

function HeistProgress({ state }: { state: HeistState }) {
  if (state.status === 'loading') return <p>Reading the shared record…</p>
  if (state.status === 'error') {
    return (
      <p role="alert">The shared record cannot be read just now.</p>
    )
  }

  const open = state.record.status === 'OPEN'
  return (
    <p>
      Shared progress: entrance. {open ? 'The room is open.' : 'The room is closed.'} Nothing further has been prepared.
    </p>
  )
}
