import { useEffect, useState, type FormEvent } from 'react'
import { bank } from '../public/content.ts'
import { visit } from '../auth/api.ts'
import {
  emptyClientForm,
  formFromRecord,
  validateClientForm,
  type ClientFormValues,
  type ClientList,
  type ClientRecord,
  type LinkedAccount,
} from '../clients/model.ts'
import {
  changeClientStatus,
  createClient,
  fetchClient,
  fetchClients,
  fetchLinkedAccounts,
  linkClientAccount,
  unlinkClientAccount,
  updateClient,
} from './client-api.ts'
import { ClientDetailView, ClientFormFields, ClientListView } from './ClientViews.tsx'

function fieldError(caught: unknown) {
  if (caught instanceof Error) {
    const fields = (caught as Error & { fields?: Record<string, string> }).fields
    return { message: caught.message, fields: fields ?? {} }
  }
  return { message: 'The request could not be completed.', fields: {} }
}

export function ClientListPage() {
  const [search, setSearch] = useState('')
  const [submitted, setSubmitted] = useState('')
  const [filter, setFilter] = useState('')
  const [page, setPage] = useState(1)
  const [reload, setReload] = useState(0)
  const [phase, setPhase] = useState<'loading' | 'error' | 'ready'>('loading')
  const [message, setMessage] = useState<string | null>(null)
  const [list, setList] = useState<ClientList | null>(null)

  useEffect(() => {
    document.title = `Clients — ${bank.name}`
  }, [])

  useEffect(() => {
    let active = true
    fetchClients({ search: submitted, status: filter, page })
      .then((body) => {
        if (!active) return
        setList(body)
        setPhase('ready')
        setMessage(null)
      })
      .catch((caught: unknown) => {
        if (!active) return
        setPhase('error')
        setMessage(fieldError(caught).message)
      })
    return () => {
      active = false
    }
  }, [submitted, filter, page, reload])

  return (
    <ClientListView
      phase={phase}
      message={message}
      list={list}
      search={search}
      filter={filter}
      onSearchChange={setSearch}
      onFilterChange={(value) => {
        setFilter(value)
        setPage(1)
        setPhase('loading')
      }}
      onSubmitSearch={(event) => {
        event.preventDefault()
        setSubmitted(search)
        setPage(1)
        setPhase('loading')
      }}
      onPage={(next) => {
        setPage(next)
        setPhase('loading')
      }}
      onRetry={() => {
        setPhase('loading')
        setReload((current) => current + 1)
      }}
    />
  )
}

export function ClientCreatePage() {
  const [values, setValues] = useState<ClientFormValues>(emptyClientForm)
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [pending, setPending] = useState(false)
  const [message, setMessage] = useState<string | null>(null)

  useEffect(() => {
    document.title = `New client — ${bank.name}`
  }, [])

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (pending) return
    const next = validateClientForm(values, 'create')
    setErrors(next)
    if (Object.keys(next).length > 0) return
    setPending(true)
    setMessage(null)
    try {
      const created = await createClient(values)
      visit(`/admin/participants/${created.id}`)
    } catch (caught) {
      const failure = fieldError(caught)
      setErrors(failure.fields)
      setMessage(failure.message)
      setPending(false)
    }
  }

  return (
    <>
      <p className="eyebrow">Clients</p>
      <h1>Open a client record</h1>
      <p className="section-note">The house assigns the client reference. It is not chosen here.</p>
      <form className="enquiry" onSubmit={onSubmit}>
        <ClientFormFields
          values={values}
          errors={errors}
          includeStatus
          onChange={(name, value) => setValues((current) => ({ ...current, [name]: value }))}
        />
        {message ? (
          <p className="form-status" role="alert">
            {message}
          </p>
        ) : null}
        <button type="submit" disabled={pending}>
          {pending ? 'Opening the record' : 'Open the record'}
        </button>
      </form>
    </>
  )
}

export function ClientDetailPage({ clientId }: { clientId: string }) {
  const [phase, setPhase] = useState<'loading' | 'error' | 'ready'>('loading')
  const [message, setMessage] = useState<string | null>(null)
  const [client, setClient] = useState<ClientRecord | null>(null)
  const [accounts, setAccounts] = useState<LinkedAccount[]>([])
  const [editing, setEditing] = useState(false)
  const [values, setValues] = useState<ClientFormValues>(emptyClientForm)
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [pending, setPending] = useState(false)
  const [statusChoice, setStatusChoice] = useState('ACTIVE')
  const [confirmSuspend, setConfirmSuspend] = useState(false)
  const [linkLogin, setLinkLogin] = useState('')
  const [linkError, setLinkError] = useState<string | null>(null)
  const [confirmUserId, setConfirmUserId] = useState<string | null>(null)

  useEffect(() => {
    let active = true
    Promise.all([fetchClient(clientId), fetchLinkedAccounts(clientId)])
      .then(([record, linked]) => {
        if (!active) return
        setClient(record)
        setAccounts(linked)
        setValues(formFromRecord(record))
        setStatusChoice(record.status)
        setPhase('ready')
      })
      .catch((caught: unknown) => {
        if (!active) return
        setPhase('error')
        setMessage(fieldError(caught).message)
      })
    return () => {
      active = false
    }
  }, [clientId])

  useEffect(() => {
    document.title = `${client?.clientReference ?? 'Client'} — ${bank.name}`
  }, [client])

  async function refresh(record?: ClientRecord) {
    const linked = await fetchLinkedAccounts(clientId)
    setAccounts(linked)
    if (record) {
      setClient(record)
      setValues(formFromRecord(record))
      setStatusChoice(record.status)
    }
  }

  async function onSave(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (pending || !client) return
    const next = validateClientForm(values, 'edit')
    setErrors(next)
    if (Object.keys(next).length > 0) return
    setPending(true)
    setMessage(null)
    try {
      const saved = await updateClient(client.id, values)
      setClient(saved)
      setValues(formFromRecord(saved))
      setEditing(false)
    } catch (caught) {
      const failure = fieldError(caught)
      setErrors(failure.fields)
      setMessage(failure.message)
    } finally {
      setPending(false)
    }
  }

  async function applyStatus(status: string) {
    if (!client || pending) return
    setPending(true)
    setMessage(null)
    try {
      const saved = await changeClientStatus(client.id, status)
      setClient(saved)
      setStatusChoice(saved.status)
      setConfirmSuspend(false)
    } catch (caught) {
      setMessage(fieldError(caught).message)
    } finally {
      setPending(false)
    }
  }

  function onAskSuspend(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (statusChoice === 'SUSPENDED' && client?.status !== 'SUSPENDED') {
      setConfirmSuspend(true)
      return
    }
    void applyStatus(statusChoice)
  }

  async function onLink(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!client || pending) return
    if (linkLogin.trim().length === 0) {
      setLinkError('Enter the login of an existing account.')
      return
    }
    setPending(true)
    setLinkError(null)
    setMessage(null)
    try {
      await linkClientAccount(client.id, linkLogin.trim())
      setLinkLogin('')
      await refresh()
    } catch (caught) {
      const failure = fieldError(caught)
      setLinkError(failure.fields.login ?? failure.message)
    } finally {
      setPending(false)
    }
  }

  async function onConfirmUnlink() {
    if (!client || !confirmUserId || pending) return
    setPending(true)
    setMessage(null)
    try {
      await unlinkClientAccount(client.id, confirmUserId)
      setConfirmUserId(null)
      await refresh()
    } catch (caught) {
      setMessage(fieldError(caught).message)
    } finally {
      setPending(false)
    }
  }

  if (phase === 'loading') return <p className="section-note">Opening the client record…</p>
  if (phase === 'error' || !client) {
    return (
      <p className="section-note" role="alert">
        {message ?? 'This client record could not be opened.'}
      </p>
    )
  }

  return (
    <ClientDetailView
      client={client}
      accounts={accounts}
      editing={editing}
      values={values}
      errors={errors}
      pending={pending}
      statusChoice={statusChoice}
      confirmSuspend={confirmSuspend}
      linkLogin={linkLogin}
      linkError={linkError}
      confirmUserId={confirmUserId}
      message={message}
      onEdit={() => {
        setValues(formFromRecord(client))
        setErrors({})
        setEditing(true)
      }}
      onCancelEdit={() => setEditing(false)}
      onChange={(name, value) => setValues((current) => ({ ...current, [name]: value }))}
      onSave={onSave}
      onStatusChoice={(value) => {
        setStatusChoice(value)
        setConfirmSuspend(false)
      }}
      onAskSuspend={onAskSuspend}
      onCancelSuspend={() => setConfirmSuspend(false)}
      onConfirmSuspend={() => void applyStatus('SUSPENDED')}
      onLinkLogin={setLinkLogin}
      onLink={onLink}
      onAskUnlink={setConfirmUserId}
      onCancelUnlink={() => setConfirmUserId(null)}
      onConfirmUnlink={() => void onConfirmUnlink()}
    />
  )
}
