import { Route, Routes } from 'react-router-dom'
import InicioPage from '@/pages/InicioPage'
import LoginPage from '@/pages/LoginPage'
import NuevoVehiculoPage from '@/pages/NuevoVehiculoPage'

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/vehiculos/nuevo" element={<NuevoVehiculoPage />} />
      <Route path="*" element={<InicioPage />} />
    </Routes>
  )
}
