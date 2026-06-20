import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { applyTheme, initialTheme } from './store/themeStore'

// Apply the saved/system theme before first paint to avoid a flash.
applyTheme(initialTheme())

async function enableMocking() {
  const useMsw = import.meta.env.VITE_USE_MSW === 'true'
  if (useMsw) {
    const { worker } = await import('./mocks/browser')
    return worker.start({ onUnhandledRequest: 'bypass' })
  }
}

enableMocking().then(() => {
  createRoot(document.getElementById('root')!).render(
    <StrictMode>
      <App />
    </StrictMode>,
  )
})
