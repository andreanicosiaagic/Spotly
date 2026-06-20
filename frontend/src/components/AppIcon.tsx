import type { ReactNode } from 'react'

interface AppIconProps {
  name: string
  filled?: boolean
  className?: string
}

const iconMap: Record<string, ReactNode> = {
  home: <path d="M4 10.5 12 4l8 6.5V20a1 1 0 0 1-1 1h-4.5v-6h-5v6H5a1 1 0 0 1-1-1z" fill="currentColor" />,
  directions_car: <>
    <path d="M6 7h12l2 5v6h-2v2h-2v-2H8v2H6v-2H4v-6z" fill="currentColor" />
    <circle cx="8" cy="15.5" r="1.5" fill="#FBF6EF" />
    <circle cx="16" cy="15.5" r="1.5" fill="#FBF6EF" />
  </>,
  chair: <path d="M8 4a3 3 0 1 1 6 0v5h1.5A2.5 2.5 0 0 1 18 11.5V16h-2v4h-2v-4H10v4H8v-4H6v4H4v-8.5A2.5 2.5 0 0 1 6.5 9H8z" fill="currentColor" />,
  restaurant: <path d="M7 3h2v8a2 2 0 0 1-2 2v8H5v-8a2 2 0 0 1-2-2V3h2v4h2zm8 0v6a3 3 0 0 1-2 2.816V21h-2V3z" fill="currentColor" />,
  chevron_right: <path d="m9 6 6 6-6 6" fill="none" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="2.2" />,
  error: <>
    <circle cx="12" cy="12" r="9" fill="currentColor" />
    <path d="M12 7v6" fill="none" stroke="#FBF6EF" strokeLinecap="round" strokeWidth="2.2" />
    <circle cx="12" cy="16.5" r="1.2" fill="#FBF6EF" />
  </>,
  groups: <>
    <circle cx="8" cy="10" r="3" fill="currentColor" />
    <circle cx="16" cy="10" r="3" fill="currentColor" opacity=".75" />
    <path d="M3.5 19a4.5 4.5 0 0 1 9 0" fill="none" stroke="currentColor" strokeWidth="2.1" strokeLinecap="round" />
    <path d="M11.5 19a4.5 4.5 0 0 1 9 0" fill="none" stroke="currentColor" strokeWidth="2.1" strokeLinecap="round" opacity=".75" />
  </>,
  tips_and_updates: <>
    <path d="M12 3a6 6 0 0 1 3.8 10.6A4 4 0 0 0 14.5 17h-5a4 4 0 0 0-1.3-3.4A6 6 0 0 1 12 3Z" fill="currentColor" />
    <path d="M9.5 19h5" stroke="currentColor" strokeLinecap="round" strokeWidth="2.1" />
    <path d="M10 21h4" stroke="currentColor" strokeLinecap="round" strokeWidth="2.1" />
  </>,
  info: <>
    <circle cx="12" cy="12" r="9" fill="currentColor" />
    <path d="M12 11v5" stroke="#FBF6EF" strokeWidth="2.1" strokeLinecap="round" />
    <circle cx="12" cy="8" r="1.2" fill="#FBF6EF" />
  </>,
  desktop_windows: <>
    <rect x="3" y="5" width="18" height="12" rx="2" fill="currentColor" />
    <path d="M9 20h6" stroke="currentColor" strokeWidth="2.1" strokeLinecap="round" />
  </>,
  sync: <path d="M20 7v5h-5m-6 5H4v-5m1.6-4.4A7 7 0 0 1 18 8m.4 8a7 7 0 0 1-12.4-.6" fill="none" stroke="currentColor" strokeWidth="2.1" strokeLinecap="round" strokeLinejoin="round" />,
  check_circle: <>
    <circle cx="12" cy="12" r="9" fill="currentColor" />
    <path d="m8.2 12.2 2.4 2.4 5.2-5.2" fill="none" stroke="#FBF6EF" strokeWidth="2.1" strokeLinecap="round" strokeLinejoin="round" />
  </>,
  redeem: <>
    <path d="M4 8.5h16v11.5H4z" fill="currentColor" />
    <path d="M4 8.5h16v-2a2.5 2.5 0 0 0-4.8-.9A2.5 2.5 0 0 0 10 6a2.5 2.5 0 0 0-5.2-.4A2.5 2.5 0 0 0 4 6.5z" fill="currentColor" opacity=".75" />
    <path d="M12 8.5v11.5" stroke="#FBF6EF" strokeWidth="1.8" />
  </>,
  lunch_dining: <>
    <path d="M7 3h2v8a2 2 0 0 1-2 2v8H5v-8a2 2 0 0 1-2-2V3h2v4h2zm6 0h2a3 3 0 0 1 0 6v12h-2z" fill="currentColor" />
  </>,
}

export function AppIcon({ name, className = '' }: AppIconProps) {
  return <svg aria-hidden="true" viewBox="0 0 24 24" className={`inline-block ${className}`} fill="none">
    {iconMap[name] ?? <circle cx="12" cy="12" r="9" fill="currentColor" />}
  </svg>
}
