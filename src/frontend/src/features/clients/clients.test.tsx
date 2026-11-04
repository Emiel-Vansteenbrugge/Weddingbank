import { renderToStaticMarkup } from 'react-dom/server'
import { expect, test } from 'vitest'
import { isCurrentPage, matchRoute } from '../../app/routes.ts'
import { ClientDetailView, ClientFormFields, ClientListView, RelationshipSections } from '../admin/ClientViews.tsx'
import { ClientProfile } from '../client/RelationshipPanel.tsx'
import {
  clientListQuery,
  emptyClientForm,
  validateClientForm,
  type ClientList,
  type ClientRecord,
  type LinkedAccount,
  type Relationship,
} from './model.ts'

const row = {
  id: '55555555-5555-4555-8555-555555555555',
  clientReference: 'AJ-ABCD-EFGH',
  legalName: 'Maison Example',
  displayName: 'Example',
  emailAddress: 'example@example.com',
  city: 'Paris',
  country: 'France',
  status: 'ACTIVE',
  createdAt: '2026-11-04T12:00:00.000Z',
}

const list: ClientList = {
  total: 4,
  active: 2,
  inactive: 1,
  suspended: 1,
  filteredCount: 1,
  page: 1,
  pageSize: 20,
  items: [row],
}

test('client addresses stay inside the existing desk', () => {
  expect(matchRoute('/admin/participants')).toEqual({ area: 'admin', page: 'participants' })
  expect(matchRoute('/admin/participants/new')).toEqual({ area: 'admin', page: 'client-new' })
  expect(matchRoute(`/admin/participants/${row.id}`)).toEqual({
    area: 'admin',
    page: 'client-detail',
    clientId: row.id,
  })
  expect(matchRoute('/admin/participants/not-a-client')).toEqual({ area: 'admin', page: 'not-found' })
  expect(isCurrentPage('/admin/participants/new', '/admin/participants')).toBe(true)
  expect(isCurrentPage('/admin', '/admin')).toBe(true)
  expect(isCurrentPage('/admin/participants', '/admin')).toBe(false)
})

test('the client list shows the house counts, a search, and an empty or failed list', () => {
  const ready = renderToStaticMarkup(
    <ClientListView
      phase="ready"
      message={null}
      list={list}
      search="Example"
      filter="ACTIVE"
      onSearchChange={() => {}}
      onFilterChange={() => {}}
      onSubmitSearch={(event) => event.preventDefault()}
      onPage={() => {}}
      onRetry={() => {}}
    />,
  )
  expect(ready).toContain('Clients of the house')
  expect(ready).toContain('AJ-ABCD-EFGH')
  expect(ready).toContain('example@example.com')
  expect(ready).toContain('Paris, France')
  expect(ready).toContain('Active')
  expect(ready).toContain('value="Example"')
  expect(ready).toContain('New client')

  const empty = renderToStaticMarkup(
    <ClientListView
      phase="ready"
      message={null}
      list={{ ...list, total: 0, active: 0, inactive: 0, suspended: 0, filteredCount: 0, items: [] }}
      search=""
      filter=""
      onSearchChange={() => {}}
      onFilterChange={() => {}}
      onSubmitSearch={(event) => event.preventDefault()}
      onPage={() => {}}
      onRetry={() => {}}
    />,
  )
  expect(empty).toContain('No clients are recorded yet.')

  const filtered = renderToStaticMarkup(
    <ClientListView
      phase="ready"
      message={null}
      list={{ ...list, filteredCount: 0, items: [] }}
      search="missing"
      filter="SUSPENDED"
      onSearchChange={() => {}}
      onFilterChange={() => {}}
      onSubmitSearch={(event) => event.preventDefault()}
      onPage={() => {}}
      onRetry={() => {}}
    />,
  )
  expect(filtered).toContain('No clients match this search.')

  const loading = renderToStaticMarkup(
    <ClientListView
      phase="loading"
      message={null}
      list={null}
      search=""
      filter=""
      onSearchChange={() => {}}
      onFilterChange={() => {}}
      onSubmitSearch={(event) => event.preventDefault()}
      onPage={() => {}}
      onRetry={() => {}}
    />,
  )
  expect(loading).toContain('Reading the client list')

  const failed = renderToStaticMarkup(
    <ClientListView
      phase="error"
      message="The client list could not be read."
      list={null}
      search=""
      filter=""
      onSearchChange={() => {}}
      onFilterChange={() => {}}
      onSubmitSearch={(event) => event.preventDefault()}
      onPage={() => {}}
      onRetry={() => {}}
    />,
  )
  expect(failed).toContain('The client list could not be read.')
  expect(failed).toContain('Try again')
})

test('the client form asks for a legal name and an email address before it is sent', () => {
  const errors = validateClientForm(emptyClientForm(), 'create')
  expect(errors.legalName).toBeTruthy()
  expect(errors.emailAddress).toBeTruthy()
  const html = renderToStaticMarkup(
    <form>
      <ClientFormFields values={emptyClientForm()} errors={errors} includeStatus onChange={() => {}} />
    </form>,
  )
  expect(html).toContain('Enter a legal name.')
  expect(html).toContain('Enter an email address.')
  expect(html).toContain('Initial status')
  expect(clientListQuery({ search: 'AJ-ABCD', status: 'ACTIVE', page: 2 })).toBe(
    '/api/admin/clients?search=AJ-ABCD&status=ACTIVE&page=2&pageSize=20',
  )
})

test('the client record asks before a suspension or a removed association', () => {
  const client: ClientRecord = {
    ...row,
    phoneNumber: null,
    addressLine1: '1 Rue de la Paix',
    addressLine2: null,
    postalCode: '75002',
    updatedAt: row.createdAt,
  }
  const account: LinkedAccount = {
    userId: '11111111-1111-4111-8111-111111111111',
    login: 'juul',
    displayName: 'Juul',
    roles: ['CLIENT'],
    linkedAt: row.createdAt,
  }
  const html = renderToStaticMarkup(
    <ClientDetailView
      client={client}
      accounts={[account]}
      editing={false}
      values={emptyClientForm()}
      errors={{}}
      pending={false}
      statusChoice="SUSPENDED"
      confirmSuspend
      linkLogin=""
      linkError={null}
      confirmUserId={account.userId}
      message={null}
      onEdit={() => {}}
      onCancelEdit={() => {}}
      onChange={() => {}}
      onSave={(event) => event.preventDefault()}
      onStatusChoice={() => {}}
      onAskSuspend={(event) => event.preventDefault()}
      onCancelSuspend={() => {}}
      onConfirmSuspend={() => {}}
      onLinkLogin={() => {}}
      onLink={(event) => event.preventDefault()}
      onAskUnlink={() => {}}
      onCancelUnlink={() => {}}
      onConfirmUnlink={() => {}}
    />,
  )
  expect(html).toContain('AJ-ABCD-EFGH')
  expect(html).toContain('Maison Example')
  expect(html).toContain('Suspending this relationship closes it to the client.')
  expect(html).toContain('Remove the association with juul?')
  expect(html).not.toContain('PasswordHash')
})

test('a client dashboard can show no relationship, one relationship, or several', () => {
  const none = renderToStaticMarkup(<RelationshipSections relationships={[]} />)
  expect(none).toContain('No relationship is open in your name.')

  const one: Relationship = {
    id: row.id,
    clientReference: row.clientReference,
    legalName: row.legalName,
    displayName: row.displayName,
    status: 'ACTIVE',
    active: true,
    emailAddress: row.emailAddress,
    phoneNumber: null,
    addressLine1: null,
    addressLine2: null,
    postalCode: null,
    city: 'Paris',
    country: 'France',
    }
  const single = renderToStaticMarkup(<RelationshipSections relationships={[one]} />)
  expect(single).toContain('AJ-ABCD-EFGH')
  expect(single).toContain('example@example.com')

  const second: Relationship = { ...one, id: '66666666-6666-4666-8666-666666666666', clientReference: 'AJ-WXYZ-2345', legalName: 'Maison Amber', displayName: 'Amber', status: 'SUSPENDED', active: false, emailAddress: null }
  const many = renderToStaticMarkup(<RelationshipSections relationships={[one, second]} />)
  expect(many).toContain('AJ-ABCD-EFGH')
  expect(many).toContain('AJ-WXYZ-2345')
  expect(many).toContain('This relationship is not active.')
})

test('the profile keeps the sign-in account apart from the relationship', () => {
  const html = renderToStaticMarkup(<ClientProfile displayName="Juul" login="juul" />)
  expect(html).toContain('Account')
  expect(html).toContain('Relationships')
  expect(html).toContain('juul')
  expect(html).toContain('Opening the relationship')
  expect(html).toContain('not changed from this page')
})
