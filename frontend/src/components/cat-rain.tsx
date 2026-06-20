import { useEffect, useMemo, useState } from 'react'

interface CatRainProps {
  isActive: boolean
  onComplete: () => void
  count?: number
  durationMs?: number
  fadeOutMs?: number
}

interface CatDrop {
  id: string
  left: number
  delayMs: number
  rotateDeg: number
  imageUrl: string | null
}

const LOCAL_CAT_SVG = `data:image/svg+xml;utf8,${encodeURIComponent(`
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 96 96">
  <path fill="#F6C75F" d="M18 36 24 14l16 12h16l16-12 6 22c4 6 6 13 6 20 0 20-16 36-36 36S12 76 12 56c0-7 2-14 6-20Z"/>
  <path fill="#E39A48" d="m28 25 10 8H28zm40 0-10 8h10z"/>
  <circle cx="35" cy="52" r="5" fill="#2B2622"/>
  <circle cx="61" cy="52" r="5" fill="#2B2622"/>
  <path d="M48 58c2 0 4 2 4 4s-2 5-4 5-4-3-4-5 2-4 4-4Z" fill="#EC6A4D"/>
  <path d="M36 70c4 4 20 4 24 0" fill="none" stroke="#2B2622" stroke-linecap="round" stroke-width="4"/>
  <path d="M22 56h10m32 0h10M20 64h12m32 0h12" fill="none" stroke="#2B2622" stroke-linecap="round" stroke-width="3"/>
</svg>
`)}` as const

function buildCatUrl() {
  if (import.meta.env.VITE_CAT_RAIN_REMOTE === 'true' && navigator.onLine) {
    return `https://cataas.com/cat?cacheBust=${crypto.randomUUID()}`
  }

  return LOCAL_CAT_SVG
}

export function CatRain({
  isActive,
  onComplete,
  count = 12,
  durationMs = 4_000,
  fadeOutMs = 500,
}: CatRainProps) {
  const [visible, setVisible] = useState(false)
  const drops = useMemo<CatDrop[]>(() => {
    if (!isActive) return []

    return Array.from({ length: count }, (_, index) => ({
      id: `${index}-${crypto.randomUUID()}`,
      left: Math.round((index + Math.random()) / count * 100),
      delayMs: Math.round(Math.random() * 1_500),
      rotateDeg: Math.round((Math.random() - 0.5) * 36),
      imageUrl: buildCatUrl(),
    }))
  }, [count, isActive])

  useEffect(() => {
    if (!isActive) {
      setVisible(false)
      return
    }

    setVisible(true)
    const timer = window.setTimeout(() => {
      setVisible(false)
      onComplete()
    }, durationMs + fadeOutMs)

    return () => window.clearTimeout(timer)
  }, [durationMs, fadeOutMs, isActive, onComplete])

  if (!isActive || drops.length === 0) return null

  return <div
    aria-hidden="true"
    className={`cat-rain-layer ${visible ? 'cat-rain-layer-active' : ''}`}
    style={{ ['--cat-rain-duration' as string]: `${durationMs}ms`, ['--cat-rain-fade' as string]: `${fadeOutMs}ms` }}>
    {drops.map((drop) => <div
      key={drop.id}
      className="cat-rain-drop"
      style={{ left: `${drop.left}%`, animationDelay: `${drop.delayMs}ms`, transform: `rotate(${drop.rotateDeg}deg)` }}>
      {drop.imageUrl
        ? <img src={drop.imageUrl} alt="" className="cat-rain-asset" />
        : <span className="cat-rain-emoji" role="presentation">🐱</span>}
    </div>)}
  </div>
}
