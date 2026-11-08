import { useEffect, useState } from 'react'
import { fetchHeist, resetHeist, setHeistOpen, type HeistRecord } from './heist-api.ts'

type DeskState =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; record: HeistRecord }

export function HeistDesk() {
  const [state, setState] = useState<DeskState>({ status: 'loading' })
  const [pending, setPending] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let active = true
    fetchHeist()
      .then((record) => {
        if (active) setState({ status: 'ready', record })
      })
      .catch((caught: unknown) => {
        if (active) {
          setState({
            status: 'error',
            message: caught instanceof Error ? caught.message : 'The shared record could not be read.',
          })
        }
      })
    return () => {
      active = false
    }
  }, [])

  async function reset() {
    if (pending) return
    setPending(true)
    setError(null)
    try {
      const record = await resetHeist()
      setState({ status: 'ready', record })
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'The room could not be reset.')
    } finally {
      setPending(false)
    }
  }

  async function change(open: boolean) {
    if (pending) return
    setPending(true)
    setError(null)
    try {
      const record = await setHeistOpen(open)
      setState({ status: 'ready', record })
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'The room could not be changed.')
    } finally {
      setPending(false)
    }
  }

  if (state.status === 'loading') return <p className="section-note">Reading the game…</p>
  if (state.status === 'error') {
    return (
      <p className="section-note" role="alert">
        {state.message}
      </p>
    )
  }

  const { record } = state
  const open = record.status === 'OPEN'
  return (
    <>
      <p className="eyebrow">Dashboard</p>
      <h1>{open ? 'The room is open.' : 'The room is closed.'}</h1>
      <p className="section-note">One shared record. There are no player accounts.</p>
      {error ? (
        <p className="form-status" role="alert">
          {error}
        </p>
      ) : null}
      <div className="empty-list">
        <section>
          <h2>Shared progress</h2>
          <p>{record.stage === 'inside' ? 'Inside the bank' : 'Entrance'}</p>
        </section>
        <section>
          <h2>Physical safe</h2>
          <p>€{record.safe.amountEuros}. Combination not revealed.</p>
        </section>
      </div>
      <div className="heist-actions">
        <button type="button" onClick={() => void change(true)} disabled={pending || open}>
          Open the room
        </button>
        <button className="quiet-button" type="button" onClick={() => void change(false)} disabled={pending || !open}>
          Close the room
        </button>
        <button className="quiet-button" type="button" onClick={() => void reset()} disabled={pending || record.stage !== 'inside'}>
          Reset to the entrance
        </button>
      </div>
    </>
  )
}
