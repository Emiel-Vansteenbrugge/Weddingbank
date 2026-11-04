import type { FormEvent } from 'react'
import { SiteLink } from '../../shared/SiteLink.tsx'
import {
  clientName,
  formatDay,
  pageCount,
  place,
  statusLabel,
  type ClientFormValues,
  type ClientList,
  type ClientRecord,
  type LinkedAccount,
  type Relationship,
} from '../clients/model.ts'

const fields = [
  ['legalName', 'Legal name'],
  ['displayName', 'Display name'],
  ['emailAddress', 'Email address'],
  ['phoneNumber', 'Telephone'],
  ['addressLine1', 'Address'],
  ['addressLine2', 'Address, continued'],
  ['postalCode', 'Postal code'],
  ['city', 'City'],
  ['country', 'Country'],
] as const

export function ClientFormFields({
  values,
  errors,
  includeStatus,
  onChange,
}: {
  values: ClientFormValues
  errors: Record<string, string>
  includeStatus: boolean
  onChange: (name: keyof ClientFormValues, value: string) => void
}) {
  return (
    <>
      {fields.map(([name, label]) => (
        <label key={name}>
          {label}
          <input
            name={name}
            type={name === 'emailAddress' ? 'email' : 'text'}
            value={values[name]}
            aria-invalid={errors[name] ? true : undefined}
            onChange={(event) => onChange(name, event.target.value)}
          />
          {errors[name] ? <span className="form-status">{errors[name]}</span> : null}
        </label>
      ))}
      {includeStatus ? (
        <label>
          Initial status
          <select
            name="status"
            value={values.status}
            aria-invalid={errors.status ? true : undefined}
            onChange={(event) => onChange('status', event.target.value)}
          >
            <option value="ACTIVE">Active</option>
            <option value="INACTIVE">Inactive</option>
            <option value="SUSPENDED">Suspended</option>
          </select>
          {errors.status ? <span className="form-status">{errors.status}</span> : null}
        </label>
      ) : null}
    </>
  )
}

export function ClientListView({
  phase,
  message,
  list,
  search,
  filter,
  onSearchChange,
  onFilterChange,
  onSubmitSearch,
  onPage,
  onRetry,
}: {
  phase: 'loading' | 'error' | 'ready'
  message: string | null
  list: ClientList | null
  search: string
  filter: string
  onSearchChange: (value: string) => void
  onFilterChange: (value: string) => void
  onSubmitSearch: (event: FormEvent<HTMLFormElement>) => void
  onPage: (page: number) => void
  onRetry: () => void
}) {
  const pages = list ? pageCount(list.filteredCount, list.pageSize) : 1
  return (
    <>
      <div className="ledger-head">
        <div>
          <p className="eyebrow">Clients</p>
          <h1>Clients of the house</h1>
          <p className="section-note">
            A client is a banking relationship. It is kept apart from the account used to sign in.
          </p>
        </div>
        <SiteLink href="/admin/participants/new" className="text-link">
          New client
        </SiteLink>
      </div>
      {list ? (
        <dl className="ledger-counts">
          <div>
            <dt>Total</dt>
            <dd>{list.total}</dd>
          </div>
          <div>
            <dt>Active</dt>
            <dd>{list.active}</dd>
          </div>
          <div>
            <dt>Inactive</dt>
            <dd>{list.inactive}</dd>
          </div>
          <div>
            <dt>Suspended</dt>
            <dd>{list.suspended}</dd>
          </div>
        </dl>
      ) : null}
      <form className="enquiry ledger-toolbar" onSubmit={onSubmitSearch}>
        <label>
          Search
          <input
            name="search"
            value={search}
            placeholder="Reference, name, or email"
            onChange={(event) => onSearchChange(event.target.value)}
          />
        </label>
        <label>
          Status
          <select name="status" value={filter} onChange={(event) => onFilterChange(event.target.value)}>
            <option value="">All statuses</option>
            <option value="ACTIVE">Active</option>
            <option value="INACTIVE">Inactive</option>
            <option value="SUSPENDED">Suspended</option>
          </select>
        </label>
        <button type="submit">Search</button>
      </form>
      {phase === 'loading' ? <p className="section-note">Reading the client list…</p> : null}
      {phase === 'error' ? (
        <p className="section-note" role="alert">
          {message ?? 'The client list could not be read.'}{' '}
          <button className="quiet-button" type="button" onClick={onRetry}>
            Try again
          </button>
        </p>
      ) : null}
      {phase === 'ready' && list && list.items.length === 0 ? (
        <p className="section-note">
          {list.total === 0 ? 'No clients are recorded yet.' : 'No clients match this search.'}
        </p>
      ) : null}
      {phase === 'ready' && list && list.items.length > 0 ? (
        <>
          <div className="table-wrap">
            <table className="audit-table">
              <thead>
                <tr>
                  <th>Reference</th>
                  <th>Name</th>
                  <th>Email</th>
                  <th>Place</th>
                  <th>Status</th>
                  <th>Opened</th>
                </tr>
              </thead>
              <tbody>
                {list.items.map((row) => (
                  <tr key={row.id}>
                    <td>
                      <SiteLink href={`/admin/participants/${row.id}`}>{row.clientReference}</SiteLink>
                    </td>
                    <td>{clientName(row.legalName, row.displayName)}</td>
                    <td>{row.emailAddress}</td>
                    <td>{place(row.city, row.country)}</td>
                    <td>
                      <span className="status-mark" data-status={row.status}>
                        {statusLabel(row.status)}
                      </span>
                    </td>
                    <td>{formatDay(row.createdAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="pager">
            <button className="quiet-button" type="button" disabled={list.page <= 1} onClick={() => onPage(list.page - 1)}>
              Previous
            </button>
            <p>
              Page {list.page} of {pages}
            </p>
            <button
              className="quiet-button"
              type="button"
              disabled={list.page >= pages}
              onClick={() => onPage(list.page + 1)}
            >
              Next
            </button>
          </div>
        </>
      ) : null}
    </>
  )
}

export function ClientDetailView({
  client,
  accounts,
  editing,
  values,
  errors,
  pending,
  statusChoice,
  confirmSuspend,
  linkLogin,
  linkError,
  confirmUserId,
  message,
  onEdit,
  onCancelEdit,
  onChange,
  onSave,
  onStatusChoice,
  onAskSuspend,
  onCancelSuspend,
  onConfirmSuspend,
  onLinkLogin,
  onLink,
  onAskUnlink,
  onCancelUnlink,
  onConfirmUnlink,
}: {
  client: ClientRecord
  accounts: LinkedAccount[]
  editing: boolean
  values: ClientFormValues
  errors: Record<string, string>
  pending: boolean
  statusChoice: string
  confirmSuspend: boolean
  linkLogin: string
  linkError: string | null
  confirmUserId: string | null
  message: string | null
  onEdit: () => void
  onCancelEdit: () => void
  onChange: (name: keyof ClientFormValues, value: string) => void
  onSave: (event: FormEvent<HTMLFormElement>) => void
  onStatusChoice: (value: string) => void
  onAskSuspend: (event: FormEvent<HTMLFormElement>) => void
  onCancelSuspend: () => void
  onConfirmSuspend: () => void
  onLinkLogin: (value: string) => void
  onLink: (event: FormEvent<HTMLFormElement>) => void
  onAskUnlink: (userId: string) => void
  onCancelUnlink: () => void
  onConfirmUnlink: () => void
}) {
  const confirming = accounts.find((account) => account.userId === confirmUserId) ?? null
  return (
    <>
      <p className="eyebrow">{client.clientReference}</p>
      <h1>{clientName(client.legalName, client.displayName)}</h1>
      <p className="status-mark" data-status={client.status}>
        {statusLabel(client.status)}
      </p>
      {message ? (
        <p className="form-status" role="alert">
          {message}
        </p>
      ) : null}
      {editing ? (
        <form className="enquiry" onSubmit={onSave}>
          <ClientFormFields values={values} errors={errors} includeStatus={false} onChange={onChange} />
          <button type="submit" disabled={pending}>
            {pending ? 'Saving' : 'Save the record'}
          </button>
          <button className="quiet-button" type="button" onClick={onCancelEdit}>
            Cancel
          </button>
        </form>
      ) : (
        <>
          <dl className="record">
            <dt>Legal name</dt>
            <dd>{client.legalName}</dd>
            <dt>Display name</dt>
            <dd>{client.displayName ?? '—'}</dd>
            <dt>Email</dt>
            <dd>{client.emailAddress}</dd>
            <dt>Telephone</dt>
            <dd>{client.phoneNumber ?? '—'}</dd>
            <dt>Address</dt>
            <dd>
              {[client.addressLine1, client.addressLine2, client.postalCode, place(client.city, client.country)]
                .filter((line) => line && line !== '—')
                .join(', ') || '—'}
            </dd>
            <dt>Opened</dt>
            <dd>{formatDay(client.createdAt)}</dd>
            <dt>Last revised</dt>
            <dd>{formatDay(client.updatedAt)}</dd>
          </dl>
          <button className="quiet-button" type="button" onClick={onEdit}>
            Edit details
          </button>
        </>
      )}
      <form className="enquiry" onSubmit={onAskSuspend}>
        <h2>Status</h2>
        <label>
          Change status
          <select name="status" value={statusChoice} onChange={(event) => onStatusChoice(event.target.value)}>
            <option value="ACTIVE">Active</option>
            <option value="INACTIVE">Inactive</option>
            <option value="SUSPENDED">Suspended</option>
          </select>
        </label>
        {confirmSuspend ? (
          <div className="confirm-line">
            <p>Suspending this relationship closes it to the client. The record remains.</p>
            <button type="button" disabled={pending} onClick={onConfirmSuspend}>
              Suspend the relationship
            </button>
            <button className="quiet-button" type="button" onClick={onCancelSuspend}>
              Keep the current status
            </button>
          </div>
        ) : (
          <button type="submit" disabled={pending}>
            {pending ? 'Saving' : 'Change status'}
          </button>
        )}
      </form>
      <section className="linked-list">
        <h2>Associated accounts</h2>
        <p className="section-note">Link an account that already exists. This does not create a login.</p>
        {accounts.length === 0 ? <p className="section-note">No accounts are linked to this relationship.</p> : null}
        {accounts.length > 0 ? (
          <div className="table-wrap">
            <table className="audit-table">
              <thead>
                <tr>
                  <th>Login</th>
                  <th>Name</th>
                  <th>Role</th>
                  <th>Linked</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {accounts.map((account) => (
                  <tr key={account.userId}>
                    <td>{account.login}</td>
                    <td>{account.displayName}</td>
                    <td>{account.roles.join(', ') || '—'}</td>
                    <td>{formatDay(account.linkedAt)}</td>
                    <td>
                      <button className="quiet-button" type="button" onClick={() => onAskUnlink(account.userId)}>
                        Remove
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : null}
        {confirming ? (
          <div className="confirm-line">
            <p>
              Remove the association with {confirming.login}? The account and the client record both remain.
            </p>
            <button type="button" disabled={pending} onClick={onConfirmUnlink}>
              Remove the association
            </button>
            <button className="quiet-button" type="button" onClick={onCancelUnlink}>
              Keep the association
            </button>
          </div>
        ) : null}
        <form className="enquiry" onSubmit={onLink}>
          <label>
            Existing login
            <input name="login" value={linkLogin} autoComplete="off" onChange={(event) => onLinkLogin(event.target.value)} />
          </label>
          {linkError ? <p className="form-status">{linkError}</p> : null}
          <button type="submit" disabled={pending}>
            {pending ? 'Linking' : 'Link account'}
          </button>
        </form>
      </section>
    </>
  )
}

export function RelationshipSections({ relationships }: { relationships: Relationship[] }) {
  if (relationships.length === 0) {
    return <p className="section-note">No relationship is open in your name.</p>
  }

  return (
    <div className="empty-list">
      {relationships.map((relationship) => (
        <section key={relationship.id}>
          <p className="eyebrow">{relationship.clientReference}</p>
          <h2>{clientName(relationship.legalName, relationship.displayName)}</h2>
          <p className="status-mark" data-status={relationship.status}>
            {statusLabel(relationship.status)}
          </p>
          {relationship.active ? (
            <p>
              {[relationship.emailAddress, relationship.phoneNumber, relationship.addressLine1, relationship.city, relationship.country]
                .filter((line) => line && line.trim().length > 0)
                .join(' · ')}
            </p>
          ) : (
            <p>This relationship is not active. The house will write when it is open again.</p>
          )}
        </section>
      ))}
    </div>
  )
}
