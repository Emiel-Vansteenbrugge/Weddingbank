import { mutationHeaders, readFailure } from '../auth/api.ts'
import {
  clientListQuery,
  clientPayload,
  type ClientFormValues,
  type ClientList,
  type ClientRecord,
  type LinkedAccount,
  type Relationship,
} from '../clients/model.ts'

async function expectOk<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const failure = await readFailure(response)
    const error = new Error(failure.title ?? 'The request could not be completed.') as Error & {
      fields?: Record<string, string>
    }
    error.fields = failure.errors
    throw error
  }
  if (response.status === 204) return undefined as T
  return (await response.json()) as T
}

export async function fetchClients(input: { search: string; status: string; page: number }) {
  const response = await fetch(clientListQuery(input), { headers: { Accept: 'application/json' } })
  return expectOk<ClientList>(response)
}

export async function fetchClient(id: string) {
  const response = await fetch(`/api/admin/clients/${id}`, { headers: { Accept: 'application/json' } })
  return expectOk<ClientRecord>(response)
}

export async function fetchLinkedAccounts(id: string) {
  const response = await fetch(`/api/admin/clients/${id}/users`, { headers: { Accept: 'application/json' } })
  return expectOk<LinkedAccount[]>(response)
}

export async function createClient(values: ClientFormValues) {
  const response = await fetch('/api/admin/clients', {
    method: 'POST',
    headers: await mutationHeaders(),
    body: JSON.stringify(clientPayload(values, 'create')),
  })
  return expectOk<ClientRecord>(response)
}

export async function updateClient(id: string, values: ClientFormValues) {
  const response = await fetch(`/api/admin/clients/${id}`, {
    method: 'PATCH',
    headers: await mutationHeaders(),
    body: JSON.stringify(clientPayload(values, 'edit')),
  })
  return expectOk<ClientRecord>(response)
}

export async function changeClientStatus(id: string, status: string) {
  const response = await fetch(`/api/admin/clients/${id}/status`, {
    method: 'POST',
    headers: await mutationHeaders(),
    body: JSON.stringify({ status }),
  })
  return expectOk<ClientRecord>(response)
}

export async function linkClientAccount(id: string, login: string) {
  const response = await fetch(`/api/admin/clients/${id}/users`, {
    method: 'POST',
    headers: await mutationHeaders(),
    body: JSON.stringify({ login }),
  })
  return expectOk<LinkedAccount>(response)
}

export async function unlinkClientAccount(id: string, userId: string) {
  const response = await fetch(`/api/admin/clients/${id}/users/${userId}`, {
    method: 'DELETE',
    headers: await mutationHeaders(),
  })
  return expectOk<void>(response)
}

export async function fetchOwnProfile() {
  const response = await fetch('/api/client/profile', { headers: { Accept: 'application/json' } })
  return expectOk<{ login: string; displayName: string }>(response)
}

export async function fetchOwnRelationships() {
  const response = await fetch('/api/client/relationships', { headers: { Accept: 'application/json' } })
  return expectOk<Relationship[]>(response)
}
