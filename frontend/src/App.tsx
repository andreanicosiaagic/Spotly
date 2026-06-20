import { useCallback, useEffect, useMemo, useState } from 'react'
import { createBrowserRouter, RouterProvider } from 'react-router'
import { QueryClient, QueryClientProvider, useQuery } from '@tanstack/react-query'
import { CatRain } from './components/cat-rain'
import { Layout } from './components/Layout'
import { useKonamiCode } from './hooks/use-konami-code'
import { fetchCurrentUser } from './lib/api'
import { useAuth } from './hooks/useAuth'
import DashboardPage from './pages/DashboardPage'
import ParkingPage from './pages/ParkingPage'
import DeskPage from './pages/DeskPage'
import LunchPage from './pages/LunchPage'
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
  const { profile, initialized, setInitialized, setUser } = useAuth()
  const [catsActive, setCatsActive] = useState(false)
  const activateCats = useCallback(() => setCatsActive(true), [])
  const meQuery = useQuery({
    queryKey: ['me', profile.id],
    queryFn: fetchCurrentUser,
    retry: 0,
  })
  useKonamiCode(activateCats)

  useEffect(() => {
    if (!meQuery.data) return
    setUser(meQuery.data)
    setInitialized(true)
  }, [meQuery.data, setInitialized, setUser])

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

  if (meQuery.isLoading && !initialized) {
    return <div className="min-h-screen bg-[var(--c-fbf6ef)] px-6 py-20 text-center text-sm text-text-muted">Caricamento profilo demo…</div>
  }

  if (meQuery.isError) {
    return <div className="min-h-screen bg-[var(--c-fbf6ef)] px-6 py-20 text-center">
      <div className="mx-auto max-w-md rounded-[24px] border border-[var(--c-f3c9bc)] bg-surface p-6 text-left">
        <h1 className="mt-0 mb-2 text-xl font-bold text-text">Profilo demo non disponibile</h1>
        <p className="mb-0 text-sm text-text-muted">{meQuery.error.message}</p>
      </div>
    </div>
  }

  return <>
    <RouterProvider router={router} />
    <CatRain isActive={catsActive} onComplete={() => setCatsActive(false)} />
  </>
}
