import { useEffect, useRef } from 'react'

const KONAMI = [
  'ArrowUp',
  'ArrowUp',
  'ArrowDown',
  'ArrowDown',
  'ArrowLeft',
  'ArrowRight',
  'ArrowLeft',
  'ArrowRight',
  'b',
  'a',
]

function normalizeKey(key: string) {
  return key.length === 1 ? key.toLowerCase() : key
}

export function useKonamiCode(onActivate: () => void) {
  const indexRef = useRef(0)

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      const key = normalizeKey(event.key)
      const expected = KONAMI[indexRef.current]

      if (key === expected) {
        indexRef.current += 1
        if (indexRef.current === KONAMI.length) {
          indexRef.current = 0
          onActivate()
        }
        return
      }

      indexRef.current = key === KONAMI[0] ? 1 : 0
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [onActivate])
}
