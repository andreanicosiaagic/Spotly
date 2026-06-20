import { create } from 'zustand'

export type Theme = 'light' | 'dark'

const STORAGE_KEY = 'spotly.theme'

function systemTheme(): Theme {
  return typeof window !== 'undefined' && window.matchMedia?.('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
}

function readStored(): Theme | null {
  if (typeof window === 'undefined') return null
  const value = window.localStorage.getItem(STORAGE_KEY)
  return value === 'light' || value === 'dark' ? value : null
}

/** Initial theme: explicit user choice if any, otherwise the OS preference. */
export function initialTheme(): Theme {
  return readStored() ?? systemTheme()
}

/** Apply the theme to <html> (CSS `.dark` class + native color-scheme). */
export function applyTheme(theme: Theme) {
  if (typeof document === 'undefined') return
  document.documentElement.classList.toggle('dark', theme === 'dark')
  document.documentElement.style.colorScheme = theme
}

interface ThemeState {
  theme: Theme
  setTheme: (theme: Theme) => void
  toggle: () => void
}

export const useThemeStore = create<ThemeState>((set, get) => ({
  theme: initialTheme(),
  setTheme: (theme) => {
    applyTheme(theme)
    if (typeof window !== 'undefined') window.localStorage.setItem(STORAGE_KEY, theme)
    set({ theme })
  },
  toggle: () => get().setTheme(get().theme === 'dark' ? 'light' : 'dark'),
}))
