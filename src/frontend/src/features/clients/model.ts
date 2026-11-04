export type ClientStatusName = 'ACTIVE' | 'INACTIVE' | 'SUSPENDED'

export type ClientFormValues = {
  legalName: string
  displayName: string
  emailAddress: string
  phoneNumber: string
  addressLine1: string
  addressLine2: string
  postalCode: string
  city: string
  country: string
  status: string
}

export type ClientList = {
  total: number
  active: number
  inactive: number
  suspended: number
  filteredCount: number
  page: number
  pageSize: number
  items: ClientRow[]
}

export type ClientRow = {
  id: string
  clientReference: string
  legalName: string
  displayName: string | null
  emailAddress: string
  city: string | null
  country: string | null
  status: string
  createdAt: string
}

export type ClientRecord = ClientRow & {
  phoneNumber: string | null
  addressLine1: string | null
  addressLine2: string | null
  postalCode: string | null
  updatedAt: string
}

export type LinkedAccount = {
  userId: string
  login: string
  displayName: string
  roles: string[]
  linkedAt: string
}

export type Relationship = {
  id: string
  clientReference: string
  legalName: string
  displayName: string | null
  status: string
  active: boolean
  emailAddress: string | null
  phoneNumber: string | null
  addressLine1: string | null
  addressLine2: string | null
  postalCode: string | null
  city: string | null
  country: string | null
}

export const emptyClientForm = (): ClientFormValues => ({
  legalName: '',
  displayName: '',
  emailAddress: '',
  phoneNumber: '',
  addressLine1: '',
  addressLine2: '',
  postalCode: '',
  city: '',
  country: '',
  status: 'ACTIVE',
})

export function formFromRecord(record: ClientRecord): ClientFormValues {
  return {
    legalName: record.legalName,
    displayName: record.displayName ?? '',
    emailAddress: record.emailAddress,
    phoneNumber: record.phoneNumber ?? '',
    addressLine1: record.addressLine1 ?? '',
    addressLine2: record.addressLine2 ?? '',
    postalCode: record.postalCode ?? '',
    city: record.city ?? '',
    country: record.country ?? '',
    status: record.status,
  }
}

export function validateClientForm(values: ClientFormValues, mode: 'create' | 'edit' = 'create') {
  const errors: Record<string, string> = {}
  if (values.legalName.trim().length === 0) errors.legalName = 'Enter a legal name.'
  const email = values.emailAddress.trim()
  if (email.length === 0 || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
    errors.emailAddress = 'Enter an email address.'
  }
  if (mode === 'create' && !['ACTIVE', 'INACTIVE', 'SUSPENDED'].includes(values.status)) {
    errors.status = 'Choose a status.'
  }
  return errors
}

export function clientPayload(values: ClientFormValues, mode: 'create' | 'edit') {
  const optional = (value: string) => {
    const trimmed = value.trim()
    return trimmed.length > 0 ? trimmed : null
  }
  const body: Record<string, string | null> = {
    legalName: values.legalName.trim(),
    displayName: optional(values.displayName),
    emailAddress: values.emailAddress.trim(),
    phoneNumber: optional(values.phoneNumber),
    addressLine1: optional(values.addressLine1),
    addressLine2: optional(values.addressLine2),
    postalCode: optional(values.postalCode),
    city: optional(values.city),
    country: optional(values.country),
  }
  if (mode === 'create') body.status = values.status
  return body
}

export function clientListQuery(input: { search: string; status: string; page: number; pageSize?: number }) {
  const params = new URLSearchParams()
  const search = input.search.trim()
  if (search) params.set('search', search)
  if (input.status) params.set('status', input.status)
  params.set('page', String(input.page))
  params.set('pageSize', String(input.pageSize ?? 20))
  return `/api/admin/clients?${params.toString()}`
}

export function statusLabel(status: string) {
  if (status === 'ACTIVE') return 'Active'
  if (status === 'INACTIVE') return 'Inactive'
  if (status === 'SUSPENDED') return 'Suspended'
  return status
}

export function clientName(legalName: string, displayName: string | null) {
  return displayName?.trim() || legalName
}

export function place(city: string | null, country: string | null) {
  const line = [city, country].filter((part) => part && part.trim().length > 0).join(', ')
  return line || '—'
}

export function formatDay(value: string) {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return date.toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })
}

export function pageCount(filteredCount: number, pageSize: number) {
  return Math.max(1, Math.ceil(filteredCount / pageSize))
}
