// Regenerate the .dark palette block from the existing :root identity list,
// with usage-aware lightness mapping (muted mid-tones -> light, surfaces -> deep).
import { readFileSync, writeFileSync } from 'node:fs'

const path = new URL('../src/theme-palette.generated.css', import.meta.url)
const css = readFileSync(path, 'utf8')
const root = css.split('\n\n.dark')[0]
const hexes = [...new Set([...root.matchAll(/--c-([0-9a-f]{6}):/g)].map((m) => m[1]))]

const clamp = (x) => Math.max(0, Math.min(1, x))
const hexToRgb = (hex) => [0, 2, 4].map((i) => parseInt(hex.slice(i, i + 2), 16))
function rgbToHsl([r, g, b]) {
  r /= 255; g /= 255; b /= 255
  const max = Math.max(r, g, b), min = Math.min(r, g, b)
  let h = 0, s = 0; const l = (max + min) / 2
  if (max !== min) {
    const d = max - min
    s = l > 0.5 ? d / (2 - max - min) : d / (max + min)
    if (max === r) h = (g - b) / d + (g < b ? 6 : 0)
    else if (max === g) h = (b - r) / d + 2
    else h = (r - g) / d + 4
    h /= 6
  }
  return [h, s, l]
}
function hslToHex(h, s, l) {
  let r, g, b
  if (s === 0) { r = g = b = l } else {
    const k = (p, q, t) => { if (t < 0) t += 1; if (t > 1) t -= 1; if (t < 1 / 6) return p + (q - p) * 6 * t; if (t < 1 / 2) return q; if (t < 2 / 3) return p + (q - p) * (2 / 3 - t) * 6; return p }
    const q = l < 0.5 ? l * (1 + s) : l + s - l * s
    const p = 2 * l - q
    r = k(p, q, h + 1 / 3); g = k(p, q, h); b = k(p, q, h - 1 / 3)
  }
  return '#' + [r, g, b].map((x) => Math.round(x * 255).toString(16).padStart(2, '0')).join('')
}
function darkFor(hex) {
  const [h, s, l] = rgbToHsl(hexToRgb(hex))
  if (s < 0.12) {                                   // neutrals: surfaces / borders / text
    let nl
    if (l >= 0.80) nl = 0.10 + (1 - l) * 0.6        // bright surface/border -> deep
    else if (l <= 0.34) nl = 0.86 - l * 0.2         // near-black text -> light
    else nl = 0.58 + (0.7 - Math.min(l, 0.7)) * 0.15 // muted mid text -> light muted
    return hslToHex(h, Math.min(s, 0.05), clamp(nl))
  }
  let nl                                            // saturated tints
  if (l >= 0.85) nl = 0.22
  else if (l >= 0.6) nl = 0.32
  else if (l <= 0.35) nl = 0.72
  else nl = 1 - l
  const ns = l > 0.7 ? s * 0.5 : Math.min(0.8, s)
  return hslToHex(h, ns, clamp(nl))
}

// Anchors that must NOT invert: white text stays white (literal), and these dark
// accents / brand colors keep their identity so paired white text stays readable.
const ANCHORS = {
  '2b2622': '#2a2620', // dark accent surfaces (banner, active toggles, CTAs)
  ec6a4d: '#ef7a5e',   // primary coral
  d2553a: '#d2553a',   // primary-dark
  ffffff: '#ffffff',   // map compass + ingresso label stay white on dark surfaces
}

const dark = hexes.map((h) => `  --c-${h}: ${ANCHORS[h] ?? darkFor(h)};`).join('\n')
writeFileSync(path, `${root}\n\n.dark {\n${dark}\n}\n`)
console.log(`Regenerated .dark for ${hexes.length} colors.`)
