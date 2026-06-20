import type { DeskSpot } from '../types'

// Piantine SVG dei due piani reali della sede, fedeli ai disegni architettonici:
//   floor 0 → Piano Terra  (uffici a Ovest, open space, servizi e sala riunioni a Est)
//   floor 1 → Piano Primo  (uffici a Ovest, open space centrale, sale riunioni a Sud)
// Le scrivanie prenotabili sono posizionate nelle stanze reali; gli altri locali
// (WC, scala/ascensore, cucina, sale, lounge) sono contesto non prenotabile.

const STATUS_STYLE = {
  available: { fill: '#D8EFE1', stroke: '#78B891', text: '#266E49' },
  occupied: { fill: '#E9E3D9', stroke: '#D9CFC0', text: '#928879' },
  pending: { fill: '#FCEDE7', stroke: '#EC6A4D', text: '#C0563C' },
  reserved: { fill: '#F8E9C9', stroke: '#D6AD60', text: '#8B651E' },
} as const

const STATUS_LABEL: Record<DeskSpot['status'], string> = {
  available: 'libera',
  occupied: 'occupata',
  pending: 'in prenotazione',
  reserved: 'riservata',
}

type Geom = { x: number; y: number; w: number; h: number }
type RoomKind = 'open' | 'office' | 'wc' | 'core' | 'meeting' | 'service'
type Room = Geom & { label: string; tint: string; kind: RoomKind; divider?: number }
type Plan = {
  name: string
  viewBox: [number, number]
  maxWidth?: number
  rooms: Room[]
  entrance?: { x: number; y: number }
  deskSize: { w: number; h: number }
  deskGeom: Record<string, { x: number; y: number }>
}

const PLAN_TERRA: Plan = {
  name: 'Piano Terra',
  viewBox: [520, 720],
  maxWidth: 440,
  rooms: [
    { x: 44, y: 44, w: 256, h: 166, label: 'Open Space Nord', tint: '#EAF4EE', kind: 'open' },
    { x: 44, y: 224, w: 256, h: 136, label: 'Uffici Ovest', tint: '#EEF2FA', kind: 'office', divider: 172 },
    { x: 44, y: 374, w: 256, h: 150, label: 'Operativo Sud', tint: '#FBEFE9', kind: 'open' },
    { x: 44, y: 540, w: 256, h: 150, label: 'Reception', tint: '#F4F1EA', kind: 'service' },
    { x: 314, y: 44, w: 162, h: 126, label: 'WC', tint: '#EEF1F4', kind: 'wc' },
    { x: 314, y: 200, w: 162, h: 180, label: 'Scala · Ascensore', tint: '#EDE7DD', kind: 'core' },
    { x: 314, y: 410, w: 162, h: 280, label: 'Sala Riunioni', tint: '#F4F1EA', kind: 'meeting' },
  ],
  entrance: { x: 240, y: 702 },
  deskSize: { w: 54, h: 40 },
  deskGeom: {
    T01: { x: 84, y: 96 }, T02: { x: 190, y: 96 }, T03: { x: 84, y: 150 }, T04: { x: 190, y: 150 },
    T05: { x: 96, y: 272 }, T06: { x: 210, y: 272 },
    T07: { x: 70, y: 430 }, T08: { x: 158, y: 430 }, T09: { x: 246, y: 430 },
  },
}

const PLAN_PRIMO: Plan = {
  name: 'Piano Primo',
  viewBox: [820, 560],
  rooms: [
    // colonna uffici a Ovest
    { x: 44, y: 44, w: 166, h: 88, label: 'Sala', tint: '#F4F1EA', kind: 'meeting' },
    { x: 44, y: 138, w: 166, h: 92, label: 'Ufficio', tint: '#EEF2FA', kind: 'office' },
    { x: 44, y: 236, w: 166, h: 92, label: 'Ufficio', tint: '#EEF2FA', kind: 'office' },
    { x: 44, y: 334, w: 166, h: 86, label: 'Ufficio', tint: '#F4F1EA', kind: 'office' },
    { x: 44, y: 440, w: 166, h: 80, label: 'Sala Riunioni', tint: '#F4F1EA', kind: 'meeting' },
    // servizi al centro
    { x: 222, y: 44, w: 118, h: 76, label: 'WC', tint: '#EEF1F4', kind: 'wc' },
    { x: 222, y: 126, w: 118, h: 70, label: 'Cucina', tint: '#FBF1E6', kind: 'service' },
    { x: 222, y: 206, w: 118, h: 154, label: 'Scala · Ascensore', tint: '#EDE7DD', kind: 'core' },
    // fronte uffici / sale in alto
    { x: 352, y: 44, w: 138, h: 76, label: 'Ufficio', tint: '#EEF2FA', kind: 'office' },
    { x: 496, y: 44, w: 138, h: 76, label: 'Sala A', tint: '#F4F1EA', kind: 'meeting' },
    { x: 640, y: 44, w: 140, h: 76, label: 'Sala B', tint: '#F4F1EA', kind: 'meeting' },
    // open space centrale
    { x: 352, y: 140, w: 308, h: 262, label: 'Open Space Centrale', tint: '#EAF4EE', kind: 'open' },
    // lounge a Est
    { x: 672, y: 140, w: 108, h: 262, label: 'Lounge · Caffè', tint: '#F3EEF8', kind: 'service' },
    // sale riunioni a Sud
    { x: 222, y: 420, w: 176, h: 100, label: 'Sala C', tint: '#F4F1EA', kind: 'meeting' },
    { x: 408, y: 420, w: 176, h: 100, label: 'Sala D', tint: '#F4F1EA', kind: 'meeting' },
    { x: 594, y: 420, w: 186, h: 100, label: 'Sala E', tint: '#F4F1EA', kind: 'meeting' },
  ],
  deskSize: { w: 58, h: 42 },
  deskGeom: {
    P01: { x: 372, y: 206 }, P02: { x: 442, y: 206 }, P03: { x: 372, y: 290 }, P04: { x: 442, y: 290 },
    P05: { x: 520, y: 206 }, P06: { x: 590, y: 206 }, P07: { x: 520, y: 290 }, P08: { x: 590, y: 290 },
    P09: { x: 70, y: 166 }, P10: { x: 138, y: 166 }, P11: { x: 70, y: 264 }, P12: { x: 138, y: 264 },
  },
}

const PLANS: Record<number, Plan> = { 0: PLAN_TERRA, 1: PLAN_PRIMO }

interface FloorMapProps {
  floor: number
  desks: DeskSpot[]
  onSelect: (deskId: string) => void
  busy?: boolean
  monitorOnly?: boolean
}

export function FloorMap({ floor, desks, onSelect, busy = false, monitorOnly = false }: FloorMapProps) {
  const plan = PLANS[floor] ?? PLAN_TERRA
  const [vw, vh] = plan.viewBox
  return (
    <svg viewBox={`0 0 ${vw} ${vh}`} className="mx-auto block h-auto w-full" style={{ maxWidth: plan.maxWidth }}
      role="group" aria-label={`Piantina ${plan.name}`} fontFamily="'Plus Jakarta Sans', sans-serif">
      {/* perimetro edificio */}
      <rect x={8} y={8} width={vw - 16} height={vh - 16} rx={16} fill="#F3EFE7" stroke="#CBBFA9" strokeWidth={3} />

      {plan.rooms.map((room, index) => <RoomShape key={index} room={room} />)}

      {plan.entrance && <Entrance x={plan.entrance.x} y={plan.entrance.y} />}

      {desks.map(desk => {
        const pos = plan.deskGeom[desk.deskId]
        if (!pos) return null
        return <DeskStall key={desk.deskId} desk={desk} geom={{ ...pos, ...plan.deskSize }}
          onSelect={onSelect} busy={busy} dimmed={monitorOnly && !desk.hasMonitor} />
      })}
    </svg>
  )
}

function RoomShape({ room }: { room: Room }) {
  return (
    <g>
      <rect x={room.x} y={room.y} width={room.w} height={room.h} rx={8} fill={room.tint} stroke="#C7BBA6" strokeWidth={2} />
      {room.divider !== undefined &&
        <line x1={room.divider} y1={room.y} x2={room.divider} y2={room.y + room.h} stroke="#C7BBA6" strokeWidth={2} />}
      <RoomDetail room={room} />
      <text x={room.x + 9} y={room.y + 17} fontSize={11} fontWeight={800} fill="#9A8F7C" letterSpacing="0.04em">
        {room.label.toUpperCase()}
      </text>
    </g>
  )
}

function RoomDetail({ room }: { room: Room }) {
  const cx = room.x + room.w / 2
  const cy = room.y + room.h / 2
  if (room.kind === 'core') {
    // gradini + vano ascensore
    const steps = Array.from({ length: 6 }, (_, i) => room.y + 34 + i * 13)
    return (
      <g stroke="#BCAF98" strokeWidth={1.5}>
        {steps.map(y => <line key={y} x1={room.x + 14} y1={y} x2={room.x + room.w / 2 - 8} y2={y} />)}
        <rect x={room.x + room.w / 2 + 6} y={room.y + 34} width={room.w / 2 - 22} height={48} fill="#E2D9C9" />
        <line x1={room.x + room.w / 2 + 6} y1={room.y + 34} x2={room.x + room.w - 16} y2={room.y + 82} />
        <line x1={room.x + room.w - 16} y1={room.y + 34} x2={room.x + room.w / 2 + 6} y2={room.y + 82} />
      </g>
    )
  }
  if (room.kind === 'wc') {
    return (
      <g fill="none" stroke="#B6C2CC" strokeWidth={1.5}>
        <circle cx={room.x + 26} cy={cy + 6} r={8} />
        <circle cx={room.x + 52} cy={cy + 6} r={8} />
        <rect x={room.x + room.w - 38} y={cy - 2} width={20} height={14} rx={3} />
      </g>
    )
  }
  if (room.kind === 'meeting') {
    return <rect x={cx - room.w * 0.28} y={cy - room.h * 0.18} width={room.w * 0.56} height={room.h * 0.36} rx={6}
      fill="none" stroke="#CDBE9E" strokeWidth={1.5} />
  }
  return null
}

function Entrance({ x, y }: { x: number; y: number }) {
  return (
    <g>
      <path d={`M${x} ${y} l12 14 l-24 0 Z`} fill="#EC6A4D" />
      <text x={x} y={y + 30} textAnchor="middle" fontSize={10} fontWeight={800} fill="#C0563C">INGRESSO</text>
    </g>
  )
}

interface DeskStallProps {
  desk: DeskSpot
  geom: Geom
  onSelect: (deskId: string) => void
  busy: boolean
  dimmed: boolean
}

function DeskStall({ desk, geom, onSelect, busy, dimmed }: DeskStallProps) {
  const style = STATUS_STYLE[desk.status]
  const clickable = desk.status === 'available' && !busy && !dimmed
  const cx = geom.x + geom.w / 2
  const marks = `${desk.hasMonitor ? '▣' : ''}${desk.isStanding ? '↕' : ''}${desk.hasWindow ? '◫' : ''}`
  const select = () => onSelect(desk.deskId)
  return (
    <g
      role="button"
      aria-label={`Postazione ${desk.deskId}, ${STATUS_LABEL[desk.status]}`}
      aria-disabled={!clickable}
      tabIndex={clickable ? 0 : -1}
      onClick={clickable ? select : undefined}
      onKeyDown={clickable ? event => {
        if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); select() }
      } : undefined}
      style={{ cursor: clickable ? 'pointer' : 'not-allowed', opacity: dimmed ? 0.22 : 1 }}
    >
      <rect x={geom.x} y={geom.y} width={geom.w} height={geom.h} rx={7} fill={style.fill} stroke={style.stroke} strokeWidth={2} />
      <text x={cx} y={geom.y + (marks ? 16 : geom.h / 2)} textAnchor="middle" dominantBaseline={marks ? 'auto' : 'central'}
        fontSize={14} fontWeight={800} fill={style.text}>{desk.deskId}</text>
      {marks && <text x={cx} y={geom.y + geom.h - 9} textAnchor="middle" fontSize={11} fill={style.text}>{marks}</text>}
    </g>
  )
}
