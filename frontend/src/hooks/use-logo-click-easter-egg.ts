import { useCallback, useRef } from 'react'

export function useLogoClickEasterEgg(
  onActivate: () => void,
  threshold = 5,
  windowMs = 2_000,
) {
  const clickTimestampsRef = useRef<number[]>([])

  return useCallback(() => {
    const now = Date.now()
    const recentClicks = [...clickTimestampsRef.current, now]
      .filter((value) => now - value <= windowMs)

    clickTimestampsRef.current = recentClicks

    if (recentClicks.length >= threshold) {
      clickTimestampsRef.current = []
      onActivate()
    }
  }, [onActivate, threshold, windowMs])
}
