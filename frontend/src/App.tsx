import { Route, Routes } from 'react-router-dom'
import AjustesPage from '@/pages/AjustesPage'
import EditarVehiculoPage from '@/pages/EditarVehiculoPage'
import HistorialPage from '@/pages/HistorialPage'
import HistorialVentasPage from '@/pages/HistorialVentasPage'
import LoginPage from '@/pages/LoginPage'
import NuevoVehiculoPage from '@/pages/NuevoVehiculoPage'
import OrdenDetallePage from '@/pages/OrdenDetallePage'
import StockPage from '@/pages/StockPage'
import TallerPage from '@/pages/TallerPage'
import VentasPage from '@/pages/VentasPage'

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/vehiculos/nuevo" element={<NuevoVehiculoPage />} />
      <Route path="/vehiculos/:id/editar" element={<EditarVehiculoPage />} />
      <Route path="/ordenes/:id" element={<OrdenDetallePage />} />
      <Route path="/ventas" element={<VentasPage />} />
      <Route path="/stock" element={<StockPage />} />
      <Route path="/historial" element={<HistorialPage />} />
      <Route path="/historial/ventas" element={<HistorialVentasPage />} />
      <Route path="/ajustes" element={<AjustesPage />} />
      <Route path="*" element={<TallerPage />} />
    </Routes>
  )
}
