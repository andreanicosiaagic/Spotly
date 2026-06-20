// One-shot transform: turn hardcoded hex colors into CSS variables so a single
// `.dark` override can re-theme the whole app. Light values stay identity → light
// mode is pixel-identical. Emits the :root (light) and .dark (auto) palette blocks.
import { readFileSync, writeFileSync, readdirSync, statSync } from 'node:fs'
import { join } from 'node:path'

const SRC = new URL('../src', import.meta.url).pathname.replace(/^\/([A-Za-z]:)/, '$1')

function walk(dir) {
  return readdirSync(dir).flatMap((name) => {
    const full = join(dir, name)
    return statSync(full).isDirectory() ? walk(full) : [full]
  })
}

const files = walk(SRC).filter((f) => /\.(tsx?|ts)$/.test(f) && !f.endsWith('.d.ts'))
const hexRe = /#([0-9a-fA-F]{6}|[0-9a-fA-F]{3})\b/g
const used = new Map() // normalized hex -> original-ish

function normalize(h) {
  let hex = h.slice(1).toLowerCase()
  if (hex.length === 3) hex = hex.split('').map((c) => c + c).join('')
  return hex
}

for (const file of files) {
  const original = readFileSync(file, 'utf8')
  const replaced = original.replace(hexRe, (m) => {
    const hex = normalize(m)
    used.set(hex, true)
    return `var(--c-${hex})`
  })
  if (replaced !== original) writeFileSync(file, replaced)
}

// --- dark palette generation -------------------------------------------------
function hexToRgb(hex) {
  return [0, 2, 4].map((i) => parseInt(hex.slice(i, i + 2), 16))
}
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
    const hue2rgb = (p, q, t) => {
      if (t < 0) t += 1; if (t > 1) t -= 1
      if (t < 1 / 6) return p + (q - p) * 6 * t
      if (t < 1 / 2) return q
      if (t < 2 / 3) return p + (q - p) * (2 / 3 - t) * 6
      return p
    }
    const q = l < 0.5 ? l * (1 + s) : l + s - l * s
    const p = 2 * l - q
    r = hue2rgb(p, q, h + 1 / 3); g = hue2rgb(p, q, h); b = hue2rgb(p, q, h - 1 / 3)
  }
  return '#' + [r, g, b].map((x) => Math.round(x * 255).toString(16).padStart(2, '0')).join('')
}

function darkFor(hex) {
  const [h, s, l] = rgbToHsl(hexToRgb(hex))
  // Near-neutral (low saturation): surfaces / text / borders.
  if (s < 0.12) {
    let nl
    if (l > 0.82) nl = 0.10 + (1 - l) * 0.5          // bright surface -> deep surface
    else if (l > 0.6) nl = 0.18                       // light border -> dark border
    else if (l < 0.3) nl = 0.90 - l * 0.3             // dark text -> light text
    else nl = 1 - l
    return hslToHex(h, Math.min(s, 0.06), Math.max(0, Math.min(1, nl)))
  }
  // Saturated tints: invert lightness, keep hue, soften saturation.
  let nl
  if (l > 0.85) nl = 0.20            // pale pastel fill -> muted dark fill
  else if (l > 0.6) nl = 0.30
  else if (l < 0.35) nl = 0.74       // strong colored text -> bright on dark
  else nl = 1 - l
  const ns = l > 0.7 ? s * 0.55 : Math.min(0.85, s * 1.05)
  return hslToHex(h, ns, nl)
}

const hexes = [...used.keys()].sort()
const light = hexes.map((h) => `  --c-${h}: #${h};`).join('\n')
const dark = hexes.map((h) => `  --c-${h}: ${darkFor(h)};`).join('\n')

writeFileSync(new URL('../src/theme-palette.generated.css', import.meta.url), `:root {\n${light}\n}\n\n.dark {\n${dark}\n}\n`)
console.log(`Variabilized ${hexes.length} colors across ${files.length} files.`)
