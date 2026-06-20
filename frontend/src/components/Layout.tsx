import { Outlet } from 'react-router'
import { Navbar } from './Navbar'

export function Layout() {
  return (
    <div className="min-h-screen bg-surface-alt">
      <Navbar />
      <main className="max-w-2xl mx-auto px-4 py-6">
        <Outlet />
      </main>
    </div>
  )
}
