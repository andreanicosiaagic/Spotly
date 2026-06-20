interface AppIconProps {
  name: string
  filled?: boolean
  className?: string
}

export function AppIcon({ name, filled = false, className = '' }: AppIconProps) {
  return <span aria-hidden="true" className={`material-symbols-rounded ${className}`}
    style={{ fontVariationSettings: `'FILL' ${filled ? 1 : 0}, 'wght' 500, 'GRAD' 0, 'opsz' 24` }}>{name}</span>
}
