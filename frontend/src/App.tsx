import { BrowserRouter, Routes, Route } from 'react-router'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { Layout } from './components/Layout'
import DashboardPage from './pages/DashboardPage'
import ParkingPage from './pages/ParkingPage'
import DeskPage from './pages/DeskPage'
import LunchPage from './pages/LunchPage'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { staleTime: 30_000, retry: 1 },
  },
})

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Routes>
          <Route element={<Layout />}>
            <Route path="/" element={<DashboardPage />} />
            <Route path="/parking" element={<ParkingPage />} />
            <Route path="/desk" element={<DeskPage />} />
            <Route path="/lunch" element={<LunchPage />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </QueryClientProvider>
  )
}
