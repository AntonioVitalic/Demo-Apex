import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { InvoiceListItem, InvoiceStatus, PaymentStatus, searchInvoices } from '../api'

const invoiceStatusOptions: Array<{ label: string; value: InvoiceStatus | '' }> = [
  { label: 'Todos', value: '' },
  { label: 'Issued', value: 'Issued' },
  { label: 'Partial', value: 'Partial' },
  { label: 'Cancelled', value: 'Cancelled' }
]

const paymentStatusOptions: Array<{ label: string; value: PaymentStatus | '' }> = [
  { label: 'Todos', value: '' },
  { label: 'Pending', value: 'Pending' },
  { label: 'Overdue', value: 'Overdue' },
  { label: 'Paid', value: 'Paid' }
]

export default function InvoicesPage() {
  const navigate = useNavigate()

  const [invoiceNumberText, setInvoiceNumberText] = useState<string>('')
  const [invoiceStatus, setInvoiceStatus] = useState<InvoiceStatus | ''>('')
  const [paymentStatus, setPaymentStatus] = useState<PaymentStatus | ''>('')

  const [items, setItems] = useState<InvoiceListItem[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string>('')

  const parsedInvoiceNumber = useMemo(() => {
    const v = invoiceNumberText.trim()
    if (!v) return undefined
    const n = Number(v)
    return Number.isFinite(n) && n > 0 ? n : undefined
  }, [invoiceNumberText])

  async function load() {
    setLoading(true)
    setError('')
    try {
      const data = await searchInvoices({
        invoiceNumber: parsedInvoiceNumber,
        invoiceStatus,
        paymentStatus
      })
      setItems(data)
    } catch (e: any) {
      setError(e?.message ?? 'Error fetching invoices')
      setItems([])
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    // initial load
    load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  return (
    <div className="grid">
      <div className="card">
        <h2 style={{ marginTop: 0 }}>Facturas</h2>
        <p className="muted" style={{ marginTop: 6 }}>
          Búsqueda por número y filtros por estado (se consultan desde las vistas del backend).
        </p>

        <div className="grid two" style={{ marginTop: 14 }}>
          <div>
            <label className="muted" style={{ display: 'block', marginBottom: 6 }}>
              Número de factura
            </label>
            <input
              value={invoiceNumberText}
              onChange={(e) => setInvoiceNumberText(e.target.value)}
              placeholder="Ej: 1491"
              inputMode="numeric"
            />
          </div>

          <div>
            <label className="muted" style={{ display: 'block', marginBottom: 6 }}>
              Estado de factura
            </label>
            <select value={invoiceStatus} onChange={(e) => setInvoiceStatus(e.target.value as any)}>
              {invoiceStatusOptions.map((o) => (
                <option key={o.label} value={o.value}>
                  {o.label}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="muted" style={{ display: 'block', marginBottom: 6 }}>
              Estado de pago
            </label>
            <select value={paymentStatus} onChange={(e) => setPaymentStatus(e.target.value as any)}>
              {paymentStatusOptions.map((o) => (
                <option key={o.label} value={o.value}>
                  {o.label}
                </option>
              ))}
            </select>
          </div>

          <div style={{ display: 'flex', gap: 10, alignItems: 'end' }}>
            <button onClick={load} disabled={loading} style={{ width: '100%' }}>
              {loading ? 'Cargando...' : 'Buscar'}
            </button>
            <button
              className="secondary"
              onClick={() => {
                setInvoiceNumberText('')
                setInvoiceStatus('')
                setPaymentStatus('')
                setTimeout(load, 0)
              }}
              disabled={loading}
              style={{ width: '100%' }}
            >
              Limpiar
            </button>
          </div>
        </div>

        {error && <div className="error">{error}</div>}
      </div>

      <div className="card">
        <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12, alignItems: 'baseline' }}>
          <h3 style={{ margin: 0 }}>Resultados</h3>
          <span className="muted">{items.length} factura(s)</span>
        </div>

        <div style={{ overflowX: 'auto', marginTop: 10 }}>
          <table className="table">
            <thead>
              <tr>
                <th>Número</th>
                <th>Cliente</th>
                <th>Fecha</th>
                <th>Vence</th>
                <th>Total</th>
                <th>NC</th>
                <th>Saldo</th>
                <th>Estado Factura</th>
                <th>Estado Pago</th>
              </tr>
            </thead>
            <tbody>
              {items.map((it) => (
                <tr
                  key={it.invoiceNumber}
                  className="rowLink"
                  onClick={() => navigate(`/invoice/${it.invoiceNumber}`)}
                  title="Ver detalle"
                >
                  <td>{it.invoiceNumber}</td>
                  <td>
                    <div style={{ fontWeight: 700 }}>{it.customerName}</div>
                    <div className="muted" style={{ fontSize: '0.9rem' }}>
                      {it.customerRun} · {it.customerEmail}
                    </div>
                  </td>
                  <td>{it.invoiceDate}</td>
                  <td>{it.paymentDueDate}</td>
                  <td>{it.totalAmount.toLocaleString('es-CL')}</td>
                  <td>{it.creditNoteTotal.toLocaleString('es-CL')}</td>
                  <td>{it.remainingBalance.toLocaleString('es-CL')}</td>
                  <td>
                    <span className="badge">{it.invoiceStatus}</span>
                  </td>
                  <td>
                    <span className={`badge ${it.paymentStatus}`}>{it.paymentStatus}</span>
                  </td>
                </tr>
              ))}

              {!loading && items.length === 0 && (
                <tr>
                  <td colSpan={9} className="muted" style={{ padding: 14 }}>
                    Sin resultados.
                  </td>
                </tr>
              )}

              {loading && (
                <tr>
                  <td colSpan={9} className="muted" style={{ padding: 14 }}>
                    Cargando...
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
