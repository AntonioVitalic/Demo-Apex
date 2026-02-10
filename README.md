# Demo-Apex -  Ejercicio Técnico Fullstack - Facturas

Repositorio que implementa el ejercicio técnico (backend + frontend) para gestión de facturas, notas de crédito, búsqueda y reportes.

El patrón de diseño utilizado es **Repositorio + Servicio** (Repository + Service). Referencia conceptual:
https://medium.com/@ankitpal181/service-repository-pattern-802540254019

---

## Stack (objetivo)
- **Backend:** ASP.NET Core 8 (API REST) + SQLite (EF Core)
- **Frontend:** React + Vite (TypeScript)

---

## Puertos y URLs (localhost)

### Backend (API)
- **Base API:** `http://localhost:5000/api`
- **Swagger UI:** `http://localhost:5000/swagger`

Notas:
- El puerto HTTP puede variar según configuración/entorno; al iniciar la API, la consola muestra la URL efectiva.
- En desarrollo, CORS queda habilitado para el origen del frontend Vite (`http://localhost:5173`) mediante `Cors:AllowedOrigins` en `appsettings.json`.

### Frontend (Vite)
- **Aplicación web:** `http://localhost:5173`
- **Config de API:** `VITE_API_BASE_URL` (por defecto `http://localhost:5000/api`)

---

## Requisitos cubiertos (según enunciado)

### Integración desde JSON (runtime)
- Carga de facturas desde archivo JSON basado en **`bd_exam.json`** al iniciar la API (Hosted Service).
- Validación de unicidad de `invoice_number` durante la importación.
- Verificación de coherencia: `total_amount` debe coincidir con la suma de `invoice_detail.subtotal`.
  - Si no coincide: `IsConsistent=false`, se mantiene en base de datos para reporte, pero queda fuera del sistema activo.
- Cálculo automático del **estado de factura** (derivado de notas de crédito):
  - `Issued`: sin notas de crédito.
  - `Cancelled`: suma de montos NC igual a `total_amount`.
  - `Partial`: suma de montos NC menor a `total_amount`.
- Cálculo automático del **estado de pago**:
  - `Paid`: existe `payment_date`.
  - `Overdue`: fecha actual > `payment_due_date` y sin pago.
  - `Pending`: dentro de plazo y sin pago.

> Los estados provenientes del JSON no se usan como fuente de verdad para lógica; el cálculo se realiza con datos persistidos (vistas/queries).

### Búsqueda (con vistas)
- Búsqueda por:
  - número de factura
  - estado de factura
  - estado de pago
- Implementación basada en **vistas SQLite** (consulta a `vw_invoice_search`).

### Gestión de Notas de Crédito (NC)
- Creación de NC asociada a una factura **consistente**.
- Fecha de creación asignada automáticamente por el backend.
- Validación: el monto de la NC no puede superar el saldo pendiente (`total_amount - suma(NC)`).

### Reportes
1. Facturas consistentes con **más de 30 días vencidas**, sin pago y sin NC (vista `vw_report_overdue_30_no_action`).
2. Resumen total y porcentaje por estado de pago (`Paid`, `Pending`, `Overdue`) derivado de `vw_invoice_search`.
3. Facturas inconsistentes (`total_amount` declarado no coincide con suma de productos) desde `vw_inconsistent_invoices`.

---

## Estructura relevante

- `backend/src/InvoiceManager.Api` — API .NET + EF Core + SQLite
- `backend/bd_exam.json` — dataset de importación runtime
- `backend/scripts/views.sql` — script SQL de vistas (referencia/documentación)
- `frontend/` — React + Vite + TypeScript

---

## Cómo ejecutar

## Backend
```bash
cd backend/src/InvoiceManager.Api
dotnet restore
dotnet run
