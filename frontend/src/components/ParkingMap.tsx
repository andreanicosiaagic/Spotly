import type { ParkingSpot } from '../types'

// Piantina SVG del parcheggio esterno, fedele al disegno reale della sede:
// ingresso principale + bussola a Nord-Ovest, secchioni/cassette postali sul
// fronte, strip "Area Partner" riservata lungo l'edificio, fila principale di
// posti e fascia speciale (EV / disabili / ospiti) verso il corpo di fabbrica.

const STATUS_STYLE = {
  available: { fill: '#D8EFE1', stroke: '#78B891', text: '#266E49' },
  occupied: { fill: '#E9E3D9', stroke: '#D9CFC0', text: '#928879' },
  pending: { fill: '#FCEDE7', stroke: '#EC6A4D', text: '#C0563C' },
  reserved: { fill: '#F8E9C9', stroke: '#D6AD60', text: '#8B651E' },
} as const

const STATUS_LABEL: Record<ParkingSpot['status'], string> = {
  available: 'libero',
  occupied: 'occupato',
  pending: 'in prenotazione',
  reserved: 'riservato',
}

type Geom = { x: number; y: number; w: number; h: number }

// Coordinate nel viewBox 0 0 740 440 (origine in alto a sinistra).
const SPOT_GEOM: Record<string, Geom> = {
  // strip partner riservata (verticale, lato edificio)
  P01: { x: 92, y: 156, w: 92, h: 64 },
  P02: { x: 92, y: 238, w: 92, h: 64 },
  // fila principale lungo il fronte
  P03: { x: 198, y: 110, w: 78, h: 66 },
  P04: { x: 282, y: 110, w: 78, h: 66 },
  P05: { x: 366, y: 110, w: 78, h: 66 },
  P06: { x: 450, y: 110, w: 78, h: 66 },
  P07: { x: 534, y: 110, w: 78, h: 66 },
  // seconda fascia
  P08: { x: 198, y: 200, w: 78, h: 66 },
  P09: { x: 282, y: 200, w: 78, h: 66 },
  P10: { x: 366, y: 200, w: 78, h: 66 },
  // posti speciali vicino all'ingresso edificio
  P11: { x: 198, y: 292, w: 78, h: 66 },
  P12: { x: 282, y: 292, w: 78, h: 66 },
}

interface ParkingMapProps {
  spots: ParkingSpot[]
  onSelect: (spotId: string) => void
  busy?: boolean
}

export function ParkingMap({ spots, onSelect, busy = false }: ParkingMapProps) {
  return (
    <svg viewBox="0 0 740 440" className="block h-auto w-full" role="group" aria-label="Mappa del parcheggio esterno"
      fontFamily="'Plus Jakarta Sans', sans-serif">
      {/* asfalto */}
      <rect x={8} y={8} width={724} height={424} rx={22} fill="#ECEFE9" stroke="#DBE0D4" strokeWidth={2} />

      {/* marciapiede / percorso pedonale verso l'ingresso */}
      <rect x={8} y={8} width={62} height={300} rx={20} fill="#E6E0D3" />
      <line x1={39} y1={70} x2={39} y2={300} stroke="#CFC7B6" strokeWidth={2} strokeDasharray="2 9" />

      {/* bussola Nord */}
      <g transform="translate(38 44)">
        <circle r={17} fill="#fff" stroke="#C9BDAB" strokeWidth={1.5} />
        <path d="M0 -11 L4 2 L0 -1 L-4 2 Z" fill="#EC6A4D" />
        <text y={13} textAnchor="middle" fontSize={9} fontWeight={800} fill="#A8987E">N</text>
      </g>

      {/* ingresso principale */}
      <g>
        <rect x={86} y={10} width={150} height={26} rx={9} fill="#2B2622" />
        <text x={161} y={27} textAnchor="middle" fontSize={12} fontWeight={700} fill="#fff">Ingresso principale</text>
      </g>

      {/* secchioni & cassette postali */}
      <g>
        <rect x={300} y={20} width={132} height={28} rx={7} fill="#E0DACD" stroke="#CFC4B0" strokeWidth={1.5} />
        <rect x={309} y={27} width={13} height={14} rx={2} fill="#B9AE99" />
        <rect x={326} y={27} width={13} height={14} rx={2} fill="#B9AE99" />
        <text x={366} y={38} textAnchor="middle" fontSize={10.5} fontWeight={700} fill="#8A7F6E">Secchioni · Cassette postali</text>
      </g>

      {/* strip "Area Partner" riservata (lato edificio) */}
      <g>
        <rect x={82} y={128} width={112} height={188} rx={12} fill="#FBEDE6" stroke="#E0A78F" strokeWidth={1.5} strokeDasharray="7 5" />
        <text x={74} y={222} textAnchor="middle" fontSize={11} fontWeight={800} fill="#C77E62" letterSpacing="0.12em"
          transform="rotate(-90 74 222)">AREA PARTNER</text>
      </g>

      {/* fascia/striscia della fila principale (manto + linee) */}
      <rect x={190} y={100} width={430} height={88} rx={10} fill="#E5E9E0" />

      {/* corpo di fabbrica (edificio) */}
      <g>
        <rect x={626} y={100} width={92} height={232} rx={10} fill="#EAE1D2" stroke="#DACDB8" strokeWidth={1.5} />
        <text x={672} y={216} textAnchor="middle" fontSize={12} fontWeight={800} fill="#A8987E" letterSpacing="0.14em"
          transform="rotate(-90 672 216)">EDIFICIO</text>
      </g>

      {/* aree verdi */}
      <g fill="#E0EBD7" stroke="#C7D8B9" strokeWidth={1.5}>
        <path d="M96 372 q60 -26 132 -8 q70 18 150 6 q70 -12 150 4 q40 8 80 2 l0 46 q-256 16 -512 0 Z" />
      </g>
      <text x={350} y={406} textAnchor="middle" fontSize={11} fontWeight={800} fill="#8FA77E" letterSpacing="0.16em">VERDE</text>

      {/* posti auto */}
      {spots.map(spot => {
        const geom = SPOT_GEOM[spot.spotId]
        if (!geom) return null
        return <ParkingStall key={spot.spotId} spot={spot} geom={geom} onSelect={onSelect} busy={busy} />
      })}
    </svg>
  )
}

interface StallProps {
  spot: ParkingSpot
  geom: Geom
  onSelect: (spotId: string) => void
  busy: boolean
}

function ParkingStall({ spot, geom, onSelect, busy }: StallProps) {
  const style = STATUS_STYLE[spot.status]
  const clickable = spot.status === 'available' && !busy
  const cx = geom.x + geom.w / 2
  const select = () => onSelect(spot.spotId)
  return (
    <g
      role="button"
      aria-label={`Posto ${spot.spotNumber}, ${STATUS_LABEL[spot.status]}`}
      aria-disabled={!clickable}
      tabIndex={clickable ? 0 : -1}
      onClick={clickable ? select : undefined}
      onKeyDown={clickable ? event => {
        if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); select() }
      } : undefined}
      style={{ cursor: clickable ? 'pointer' : 'not-allowed' }}
    >
      <rect x={geom.x} y={geom.y} width={geom.w} height={geom.h} rx={9} fill={style.fill} stroke={style.stroke} strokeWidth={2} />
      <text x={cx} y={geom.y + geom.h / 2} textAnchor="middle" dominantBaseline="central"
        fontSize={20} fontWeight={800} fill={style.text}>{spot.spotNumber}</text>
    </g>
  )
}
