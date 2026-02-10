const API_BASE = (import.meta.env.VITE_API_BASE_URL as string | undefined) ?? 'http://localhost:5000/api'

type ApiError = { error: string }

async function http<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    headers: {
      'Content-Type': 'application/json',
      ...(init?.headers ?? {})
    },
    ...init
  })

  const text = await res.text()
  const data = text ? JSON.parse(text) : null

  if (!res.ok) {
    const errMsg =
      (data && typeof data === 'object' && 'error' in data && (data as ApiError).error) ||
      `Request failed (${res.status})`
    throw new Error(errMsg)
  }

  return data as T
}

/** -------- Types -------- */
export type InvoiceStatus = 'Issued' | 'Partial' | 'Cancelled'
export type PaymentStatus = 'Pending' | 'Overdue' | 'Paid'

export type InvoiceListItem = {
  invoiceNumber: number
  invoiceDate: string
  totalAmount: number
  paymentDueDate: string
  invoiceStatus: InvoiceStatus
  paymentStatus: PaymentStatus
  customerName: string
  customerRun: string
  customerEmail: string
  creditNoteTotal: number
  remainingBalance: number
}

export type InvoiceDetail = {
  productName: string
  unitPrice: number
  quantity: number
  subtotal: number
}

export type CreditNote = {
  creditNoteNumber: number
  creditNoteDate: string
  amount: number
}

export type Invoice = {
  invoiceNumber: number
  invoiceDate: string
  totalAmount: number
  paymentDueDate: string
  daysToDue: number
  invoiceStatus: InvoiceStatus
  paymentStatus: PaymentStatus
  paymentMethod: string | null
  paymentDate: string | null
  customerName: string
  customerRun: string
  customerEmail: string
  isConsistent: boolean
  productsSubtotalSum: number
  discrepancyAmount: number
  creditNoteTotal: number
  remainingBalance: number
  details: InvoiceDetail[]
  creditNotes: CreditNote[]
}

export type AddCreditNoteRequest = { amount: number }

export type OverdueNoActionRow = {
  invoiceNumber: number
  invoiceDate: string
  totalAmount: number
  paymentDueDate: string
  daysOverdue: number
  customerName: string
  customerRun: string
  customerEmail: string
}

export type PaymentStatusSummaryRow = {
  paymentStatus: PaymentStatus
  count: number
  percentage: number
}

export type PaymentStatusSummary = {
  totalInvoices: number
  rows: PaymentStatusSummaryRow[]
}

export type InconsistentInvoiceRow = {
  invoiceNumber: number
  invoiceDate: string
  declaredTotalAmount: number
  computedProductsTotal: number
  discrepancyAmount: number
  customerName: string
  customerRun: string
  customerEmail: string
}

/** -------- API Functions -------- */

export async function searchInvoices(params: {
  invoiceNumber?: number
  invoiceStatus?: InvoiceStatus | ''
  paymentStatus?: PaymentStatus | ''
}): Promise<InvoiceListItem[]> {
  const qp = new URLSearchParams()
  if (params.invoiceNumber !== undefined && params.invoiceNumber !== null) qp.set('invoiceNumber', String(params.invoiceNumber))
  if (params.invoiceStatus) qp.set('invoiceStatus', params.invoiceStatus)
  if (params.paymentStatus) qp.set('paymentStatus', params.paymentStatus)

  const qs = qp.toString()
  return http<InvoiceListItem[]>(`/invoices${qs ? `?${qs}` : ''}`)
}

export async function getInvoice(invoiceNumber: number): Promise<Invoice> {
  return http<Invoice>(`/invoices/${invoiceNumber}`)
}

export async function addCreditNote(invoiceNumber: number, body: AddCreditNoteRequest): Promise<CreditNote> {
  return http<CreditNote>(`/invoices/${invoiceNumber}/credit-notes`, {
    method: 'POST',
    body: JSON.stringify(body)
  })
}

export async function reportOverdueNoAction(): Promise<OverdueNoActionRow[]> {
  return http<OverdueNoActionRow[]>(`/reports/overdue-30-no-action`)
}

export async function reportPaymentStatusSummary(): Promise<PaymentStatusSummary> {
  return http<PaymentStatusSummary>(`/reports/payment-status-summary`)
}

export async function reportInconsistent(): Promise<InconsistentInvoiceRow[]> {
  return http<InconsistentInvoiceRow[]>(`/reports/inconsistent`)
}
