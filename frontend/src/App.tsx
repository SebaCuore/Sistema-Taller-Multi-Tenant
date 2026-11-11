import { Route, Routes } from 'react-router-dom'
import InicioPage from '@/pages/InicioPage'
import LoginPage from '@/pages/LoginPage'

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="*" element={<InicioPage />} />
    </Routes>
  )
}
