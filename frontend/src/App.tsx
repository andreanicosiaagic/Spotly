import { useCallback, useMemo, useState } from 'react'
import { createBrowserRouter, RouterProvider } from 'react-router'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { CatRain } from './components/cat-rain'
import { Layout } from './components/Layout'
import { useKonamiCode } from './hooks/use-konami-code'
import { useAuth } from './hooks/useAuth'
import DashboardPage from './pages/DashboardPage'
import ParkingPage from './pages/ParkingPage'
import DeskPage from './pages/DeskPage'
import LunchPage from './pages/LunchPage'
import LoginPage from './pages/LoginPage'
import NotFoundPage from './pages/NotFoundPage'
import RouteErrorPage from './pages/RouteErrorPage'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { staleTime: 30_000, retry: 1 },
  },
})

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthBootstrap />
    </QueryClientProvider>
  )
}

function AuthBootstrap() {
  const { user } = useAuth()
  const [catsActive, setCatsActive] = useState(false)
  const activateCats = useCallback(() => setCatsActive(true), [])
  useKonamiCode(activateCats)

  const router = useMemo(() => createBrowserRouter([
    {
      path: '/',
      element: <Layout onLogoActivate={activateCats} />,
      errorElement: <RouteErrorPage />,
      children: [
        { index: true, element: <DashboardPage /> },
        { path: 'parking', element: <ParkingPage /> },
        { path: 'desk', element: <DeskPage /> },
        { path: 'lunch', element: <LunchPage /> },
        { path: '*', element: <NotFoundPage /> },
      ],
    },
  ]), [activateCats])

  if (!user) return <LoginPage />

  return <>
    <RouterProvider router={router} />
    <CatRain isActive={catsActive} onComplete={() => setCatsActive(false)} />
  </>
}
