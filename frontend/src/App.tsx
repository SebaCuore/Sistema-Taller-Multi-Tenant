import { Route, Routes } from 'react-router-dom'
import LoginPage from '@/pages/LoginPage'
import NuevoVehiculoPage from '@/pages/NuevoVehiculoPage'
import OrdenDetallePage from '@/pages/OrdenDetallePage'
import TallerPage from '@/pages/TallerPage'

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/vehiculos/nuevo" element={<NuevoVehiculoPage />} />
      <Route path="/ordenes/:id" element={<OrdenDetallePage />} />
      <Route path="*" element={<TallerPage />} />
    </Routes>
  )
}
