import { useEffect, useState } from 'react'
import {
  InconsistentInvoiceRow,
  OverdueNoActionRow,
  PaymentStatusSummary,
  reportInconsistent,
  reportOverdueNoAction,
  reportPaymentStatusSummary
} from '../api'
import { Link } from 'react-router-dom'

export default function ReportsPage() {
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string>('')

  const [summary, setSummary] = useState<PaymentStatusSummary | null>(null)
  const [overdue, setOverdue] = useState<OverdueNoActionRow[]>([])
  const [inconsistent, setInconsistent] = useState<InconsistentInvoiceRow[]>([])

  async function load() {
    setLoading(true)
    setError('')
    try {
      const [s, o, i] = await Promise.all([
        reportPaymentStatusSummary(),
        reportOverdueNoAction(),
        reportInconsistent()
      ])

      setSummary(s)
      setOverdue(o)
      setInconsistent(i)
    } catch (e: any) {
      setError(e?.message ?? 'Error cargando reportes')
      setSummary(null)
      setOverdue([])
      setInconsistent([])
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    load()
  }, [])

  return (
    <div className="grid" style={{ gap: 16 }}>
      <div className="card">
        <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12, alignItems: 'baseline' }}>
          <div>
            <h2 style={{ marginTop: 0, marginBottom: 6 }}>Reportes</h2>
            <p className="muted" style={{ marginTop: 0 }}>
              Reportes basados en vistas SQLite del backend.
            </p>
          </div>

          <div style={{ display: 'flex', gap: 10 }}>
            <button className="secondary" onClick={load} disabled={loading}>
              {loading ? 'Actualizando...' : 'Actualizar'}
            </button>
            <Link to="/" className="muted" style={{ alignSelf: 'center' }}>
              Volver
            </Link>
          </div>
        </div>

        {error && <div className="error">{error}</div>}
      </div>

      {/* Summary */}
      <div className="card">
        <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12, alignItems: 'baseline' }}>
          <h3 style={{ margin: 0 }}>Resumen por estado de pago</h3>
          <span className="muted">
            Total: {summary?.totalInvoices ?? 0}
          </span>
        </div>

        <div style={{ overflowX: 'auto', marginTop: 10 }}>
          <table className="table">
            <thead>
              <tr>
                <th>Estado</th>
                <th>Cantidad</th>
                <th>Porcentaje</th>
              </tr>
            </thead>
            <tbody>
              {summary?.rows?.map((r) => (
                <tr key={r.paymentStatus}>
                  <td>
                    <span className={`badge ${r.paymentStatus}`}>{r.paymentStatus}</span>
                  </td>
                  <td>{r.count}</td>
                  <td>{r.percentage.toFixed(2)}%</td>
                </tr>
              ))}

              {!loading && (!summary || summary.rows.length === 0) && (
                <tr>
                  <td colSpan={3} className="muted" style={{ padding: 14 }}>
                    Sin datos.
                  </td>
                </tr>
              )}

              {loading && (
                <tr>
                  <td colSpan={3} className="muted" style={{ padding: 14 }}>
                    Cargando...
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Overdue no action */}
      <div className="card">
        <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12, alignItems: 'baseline' }}>
          <h3 style={{ margin: 0 }}>Vencidas &gt; 30 días sin pago ni NC</h3>
          <span className="muted">{overdue.length} factura(s)</span>
        </div>

        <div style={{ overflowX: 'auto', marginTop: 10 }}>
          <table className="table">
            <thead>
              <tr>
                <th>N°</th>
                <th>Cliente</th>
                <th>Fecha</th>
                <th>Vence</th>
                <th>Días vencida</th>
                <th>Total</th>
              </tr>
            </thead>
            <tbody>
              {overdue.map((r) => (
                <tr key={r.invoiceNumber}>
                  <td>
                    <Link to={`/invoice/${r.invoiceNumber}`}>{r.invoiceNumber}</Link>
                  </td>
                  <td>
                    <div style={{ fontWeight: 700 }}>{r.customerName}</div>
                    <div className="muted" style={{ fontSize: '0.9rem' }}>
                      {r.customerRun} · {r.customerEmail}
                    </div>
                  </td>
                  <td>{r.invoiceDate}</td>
                  <td>{r.paymentDueDate}</td>
                  <td>{r.daysOverdue}</td>
                  <td>{r.totalAmount.toLocaleString('es-CL')}</td>
                </tr>
              ))}

              {!loading && overdue.length === 0 && (
                <tr>
                  <td colSpan={6} className="muted" style={{ padding: 14 }}>
                    Sin resultados.
                  </td>
                </tr>
              )}

              {loading && (
                <tr>
                  <td colSpan={6} className="muted" style={{ padding: 14 }}>
                    Cargando...
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Inconsistent */}
      <div className="card">
        <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12, alignItems: 'baseline' }}>
          <h3 style={{ margin: 0 }}>Facturas inconsistentes</h3>
          <span className="muted">{inconsistent.length} factura(s)</span>
        </div>

        <p className="muted" style={{ marginTop: 10 }}>
          Son facturas donde <b>total_amount</b> no coincide con la suma de subtotales de productos.
        </p>

        <div style={{ overflowX: 'auto', marginTop: 10 }}>
          <table className="table">
            <thead>
              <tr>
                <th>N°</th>
                <th>Cliente</th>
                <th>Fecha</th>
                <th>Total declarado</th>
                <th>Total productos</th>
                <th>Diferencia</th>
              </tr>
            </thead>
            <tbody>
              {inconsistent.map((r) => (
                <tr key={r.invoiceNumber}>
                  <td>{r.invoiceNumber}</td>
                  <td>
                    <div style={{ fontWeight: 700 }}>{r.customerName}</div>
                    <div className="muted" style={{ fontSize: '0.9rem' }}>
                      {r.customerRun} · {r.customerEmail}
                    </div>
                  </td>
                  <td>{r.invoiceDate}</td>
                  <td>{r.declaredTotalAmount.toLocaleString('es-CL')}</td>
                  <td>{r.computedProductsTotal.toLocaleString('es-CL')}</td>
                  <td>{r.discrepancyAmount.toLocaleString('es-CL')}</td>
                </tr>
              ))}

              {!loading && inconsistent.length === 0 && (
                <tr>
                  <td colSpan={6} className="muted" style={{ padding: 14 }}>
                    Sin resultados.
                  </td>
                </tr>
              )}

              {loading && (
                <tr>
                  <td colSpan={6} className="muted" style={{ padding: 14 }}>
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
