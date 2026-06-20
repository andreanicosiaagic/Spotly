import { useThemeStore } from '../store/themeStore'
import { AppIcon } from './AppIcon'

export function ThemeToggle({ className = '' }: { className?: string }) {
  const theme = useThemeStore((state) => state.theme)
  const toggle = useThemeStore((state) => state.toggle)
  const isDark = theme === 'dark'
  return (
    <button
      type="button"
      onClick={toggle}
      aria-pressed={isDark}
      aria-label={isDark ? 'Attiva tema chiaro' : 'Attiva tema scuro'}
      title={isDark ? 'Tema chiaro' : 'Tema scuro'}
      className={`grid h-9 w-9 flex-none place-items-center rounded-[11px] border border-border bg-surface text-text-muted transition hover:text-text ${className}`}
    >
      <AppIcon name={isDark ? 'light_mode' : 'dark_mode'} filled />
    </button>
  )
}
