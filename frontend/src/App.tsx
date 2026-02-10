import { NavLink, Route, Routes } from 'react-router-dom'
import InvoicesPage from './pages/InvoicesPage'
import InvoiceDetailPage from './pages/InvoiceDetailPage'
import ReportsPage from './pages/ReportsPage'

export default function App() {
  return (
    <>
      <div className="nav">
        <NavLink to="/" end className={({ isActive }) => (isActive ? 'active' : '')}>
          Facturas
        </NavLink>
        <NavLink to="/reports" className={({ isActive }) => (isActive ? 'active' : '')}>
          Reportes
        </NavLink>
      </div>
      <div className="container">
        <Routes>
          <Route path="/" element={<InvoicesPage />} />
          <Route path="/invoice/:invoiceNumber" element={<InvoiceDetailPage />} />
          <Route path="/reports" element={<ReportsPage />} />
        </Routes>
      </div>
    </>
  )
}
