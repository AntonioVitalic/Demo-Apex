import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { addCreditNote, CreditNote, getInvoice, Invoice } from '../api'

function isPositiveInt(n: number) {
  return Number.isFinite(n) && n > 0 && Math.floor(n) === n
}

export default function InvoiceDetailPage() {
  const navigate = useNavigate()
  const params = useParams()
  const invoiceNumber = useMemo(() => {
    const raw = params.invoiceNumber ?? ''
    const n = Number(raw)
    return isPositiveInt(n) ? n : null
  }, [params.invoiceNumber])

  const [data, setData] = useState<Invoice | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string>('')

  const [cnAmountText, setCnAmountText] = useState<string>('')
  const [cnSubmitting, setCnSubmitting] = useState(false)
  const [cnError, setCnError] = useState<string>('')

  async function load() {
    if (invoiceNumber === null) {
      setError('Número de factura inválido.')
      setData(null)
      return
    }

    setLoading(true)
    setError('')
    try {
      const inv = await getInvoice(invoiceNumber)
      setData(inv)
    } catch (e: any) {
      setError(e?.message ?? 'Error al cargar factura')
      setData(null)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [invoiceNumber])

  const cnAmount = useMemo(() => {
    const v = cnAmountText.trim()
    if (!v) return null
    const n = Number(v)
    return Number.isFinite(n) ? n : null
  }, [cnAmountText])

  const cnRemaining = data?.remainingBalance ?? 0

  const cnValidationMessage = useMemo(() => {
    if (!data) return ''
    if (cnAmountText.trim() === '') return ''
    if (cnAmount === null) return 'Monto inválido.'
    if (cnAmount <= 0) return 'El monto debe ser mayor a 0.'
    if (cnAmount > cnRemaining) return 'El monto no puede exceder el saldo pendiente.'
    return ''
  }, [cnAmount, cnAmountText, cnRemaining, data])

  async function submitCreditNote() {
    if (!data) return
    setCnError('')

    if (!cnAmountText.trim()) {
      setCnError('Ingresa un monto.')
      return
    }
    if (cnAmount === null || cnAmount <= 0) {
      setCnError('Monto inválido.')
      return
    }
    if (cnAmount > cnRemaining) {
      setCnError('El monto no puede exceder el saldo pendiente.')
      return
    }

    setCnSubmitting(true)
    try {
      const created: CreditNote = await addCreditNote(data.invoiceNumber, { amount: cnAmount })
      // reset and reload
      setCnAmountText('')
      await load()
      // small UX: show success via console (no toast system in this commit)
      // eslint-disable-next-line no-console
      console.log('Credit note created:', created)
    } catch (e: any) {
      setCnError(e?.message ?? 'Error al crear nota de crédito')
    } finally {
      setCnSubmitting(false)
    }
  }

  if (invoiceNumber === null) {
    return (
      <div className="card">
        <h2 style={{ marginTop: 0 }}>Detalle de Factura</h2>
        <div className="error">Número de factura inválido.</div>
        <div style={{ marginTop: 12 }}>
          <Link to="/" className="muted">
            Volver
          </Link>
        </div>
      </div>
    )
  }

  return (
    <div className="grid" style={{ gap: 16 }}>
      <div className="card">
        <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12, alignItems: 'baseline' }}>
          <div>
            <h2 style={{ marginTop: 0, marginBottom: 6 }}>Factura #{invoiceNumber}</h2>
            <div className="muted">
              <Link to="/" className="muted">
                ← Volver a facturas
              </Link>
            </div>
          </div>

          <div style={{ display: 'flex', gap: 10, flexWrap: 'wrap', justifyContent: 'flex-end' }}>
            {data && (
              <>
                <span className="badge">{data.invoiceStatus}</span>
                <span className={`badge ${data.paymentStatus}`}>{data.paymentStatus}</span>
              </>
            )}
          </div>
        </div>

        {loading && <div className="muted" style={{ marginTop: 12 }}>Cargando...</div>}
        {error && <div className="error" style={{ marginTop: 12 }}>{error}</div>}

        {data && !loading && !error && (
          <div className="grid two" style={{ marginTop: 14 }}>
            <div className="card" style={{ border: '1px solid #eef0f3' }}>
              <h3 style={{ marginTop: 0 }}>Resumen</h3>
              <div className="grid" style={{ gap: 8 }}>
                <div><b>Fecha:</b> {data.invoiceDate}</div>
                <div><b>Vence:</b> {data.paymentDueDate} <span className="muted">(days_to_due: {data.daysToDue})</span></div>
                <div><b>Total:</b> {data.totalAmount.toLocaleString('es-CL')}</div>
                <div><b>Total NC:</b> {data.creditNoteTotal.toLocaleString('es-CL')}</div>
                <div><b>Saldo pendiente:</b> {data.remainingBalance.toLocaleString('es-CL')}</div>
                <div>
                  <b>Pago:</b>{' '}
                  {data.paymentDate ? (
                    <>
                      {data.paymentMethod ?? 'N/A'} · {data.paymentDate}
                    </>
                  ) : (
                    <span className="muted">Sin pago registrado</span>
                  )}
                </div>
              </div>
            </div>

            <div className="card" style={{ border: '1px solid #eef0f3' }}>
              <h3 style={{ marginTop: 0 }}>Cliente</h3>
              <div className="grid" style={{ gap: 8 }}>
                <div><b>Nombre:</b> {data.customerName}</div>
                <div><b>RUN:</b> {data.customerRun}</div>
                <div><b>Email:</b> {data.customerEmail}</div>
                <div>
                  <b>Consistencia:</b>{' '}
                  {data.isConsistent ? (
                    <span className="badge" style={{ background: '#e9f7ef' }}>Consistente</span>
                  ) : (
                    <span className="badge" style={{ background: '#fdecec' }}>Inconsistente</span>
                  )}
                </div>
                {!data.isConsistent && (
                  <div className="muted" style={{ fontSize: '0.92rem' }}>
                    Declarado: {data.totalAmount.toLocaleString('es-CL')} · Productos: {data.productsSubtotalSum.toLocaleString('es-CL')} ·
                    Dif: {data.discrepancyAmount.toLocaleString('es-CL')}
                  </div>
                )}
              </div>
              <div style={{ marginTop: 12 }}>
                <button className="secondary" onClick={() => navigate('/reports')}>Ver reportes</button>
              </div>
            </div>
          </div>
        )}
      </div>

      {data && (
        <>
          <div className="grid two">
            <div className="card">
              <h3 style={{ marginTop: 0 }}>Productos</h3>
              <div style={{ overflowX: 'auto' }}>
                <table className="table">
                  <thead>
                    <tr>
                      <th>Producto</th>
                      <th>Precio Unit.</th>
                      <th>Cant.</th>
                      <th>Subtotal</th>
                    </tr>
                  </thead>
                  <tbody>
                    {data.details.map((d, idx) => (
                      <tr key={idx}>
                        <td>{d.productName}</td>
                        <td>{d.unitPrice.toLocaleString('es-CL')}</td>
                        <td>{d.quantity}</td>
                        <td>{d.subtotal.toLocaleString('es-CL')}</td>
                      </tr>
                    ))}
                    {data.details.length === 0 && (
                      <tr>
                        <td colSpan={4} className="muted" style={{ padding: 14 }}>
                          Sin productos.
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            </div>

            <div className="card">
              <h3 style={{ marginTop: 0 }}>Notas de Crédito</h3>

              <div className="grid" style={{ gap: 10 }}>
                <div className="muted" style={{ fontSize: '0.92rem' }}>
                  Crear NC: fecha automática (server) y validación de saldo pendiente.
                </div>

                <div className="grid two">
                  <div>
                    <label className="muted" style={{ display: 'block', marginBottom: 6 }}>
                      Monto
                    </label>
                    <input
                      value={cnAmountText}
                      onChange={(e) => setCnAmountText(e.target.value)}
                      placeholder={`Máx: ${cnRemaining.toLocaleString('es-CL')}`}
                      inputMode="numeric"
                    />
                    {cnValidationMessage && <div className="error">{cnValidationMessage}</div>}
                  </div>

                  <div style={{ display: 'flex', gap: 10, alignItems: 'end' }}>
                    <button
                      onClick={submitCreditNote}
                      disabled={cnSubmitting || !!cnValidationMessage || cnAmountText.trim() === '' || data.remainingBalance <= 0}
                      style={{ width: '100%' }}
                    >
                      {cnSubmitting ? 'Creando...' : 'Crear NC'}
                    </button>
                    <button
                      className="secondary"
                      onClick={() => {
                        setCnAmountText('')
                        setCnError('')
                      }}
                      disabled={cnSubmitting}
                      style={{ width: '100%' }}
                    >
                      Limpiar
                    </button>
                  </div>
                </div>

                {cnError && <div className="error">{cnError}</div>}
              </div>

              <div style={{ overflowX: 'auto', marginTop: 12 }}>
                <table className="table">
                  <thead>
                    <tr>
                      <th>N° NC</th>
                      <th>Fecha</th>
                      <th>Monto</th>
                    </tr>
                  </thead>
                  <tbody>
                    {data.creditNotes.map((cn) => (
                      <tr key={cn.creditNoteNumber}>
                        <td>{cn.creditNoteNumber}</td>
                        <td>{cn.creditNoteDate}</td>
                        <td>{cn.amount.toLocaleString('es-CL')}</td>
                      </tr>
                    ))}
                    {data.creditNotes.length === 0 && (
                      <tr>
                        <td colSpan={3} className="muted" style={{ padding: 14 }}>
                          Sin notas de crédito.
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  )
}
