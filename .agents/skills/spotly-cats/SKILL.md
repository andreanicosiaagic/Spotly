---
name: spotly-cats
description: Easter egg Spotly — pioggia di gattini animati nascosta nell'app. Implementa o aggiorna il componente CatRain con trigger Konami code e click multipli sul logo.
---

# spotly-cats 🐱

Skill per l'implementazione e la manutenzione dell'**easter egg dei gattini** di Spotly.

## Regola assoluta

> ❌ **Il componente `<CatRain />` e i suoi trigger NON devono mai essere rimossi**, nemmeno durante refactoring, pulizia del codice o ottimizzazioni. È una feature intenzionale del prodotto.

## Quando usare questa skill

- Implementare l'easter egg per la prima volta
- Aggiornare il comportamento (nuovi trigger, nuove animazioni)
- Debug dell'easter egg (non funziona il Konami code, gattini non appaiono, ecc.)
- Aggiungere nuovi tipi di gattini o animazioni

## Specifica dell'easter egg

### Trigger

Due trigger indipendenti attivano l'easter egg:

1. **Konami Code:** ↑ ↑ ↓ ↓ ← → ← → B A (tastiera)
2. **Click multipli sul logo:** 5 click rapidi (entro 2 secondi) sul logo Spotly

### Effetto visivo

- Una "pioggia" di immagini di gattini cade dall'alto verso il basso sullo schermo
- I gattini sono sovrapposti al contenuto esistente (`z-index` alto, `pointer-events: none`)
- Ogni gattino è un'immagine fetched da **https://cataas.com/cat** (Cat as a Service)
- La pioggia dura 4 secondi, poi i gattini scompaiono con un fade-out
- L'easter egg può essere riattivato immediatamente dopo la fine

### Parametri

| Parametro | Valore default |
|---|---|
| Numero di gattini | 12 |
| Durata pioggia | 4000ms |
| Durata fade-out | 500ms |
| Dimensione gattino | 80×80px |
| Delay tra gattini | casuale 0–1500ms |

## Implementazione

### 1. Hook `useKonamiCode`

```typescript
// src/hooks/use-konami-code.ts
const KONAMI = ['ArrowUp','ArrowUp','ArrowDown','ArrowDown',
                 'ArrowLeft','ArrowRight','ArrowLeft','ArrowRight','b','a'];

export function useKonamiCode(onActivate: () => void) {
  // Traccia la sequenza di tasti premuti e chiama onActivate quando corrisponde
}
```

### 2. Hook `useLogoClickEasterEgg`

```typescript
// src/hooks/use-logo-click-easter-egg.ts
export function useLogoClickEasterEgg(onActivate: () => void, threshold = 5, windowMs = 2000) {
  // Conta i click sul logo; se raggiunge threshold in windowMs, chiama onActivate
}
```

### 3. Componente `CatRain`

```typescript
// src/components/cat-rain.tsx
// Quando isActive=true, renderizza N <img> con:
//   - src="https://cataas.com/cat?{random}" (param casuale per evitare cache)
//   - posizione X casuale, animation-delay casuale
//   - CSS keyframe: translateY(-10vh) → translateY(110vh)
//   - position: fixed, pointer-events: none, z-index: 9999
```

### 4. Integrazione in App.tsx

```tsx
// src/App.tsx
const [catsActive, setCatsActive] = useState(false);
const activateCats = useCallback(() => setCatsActive(true), []);
useKonamiCode(activateCats);

// Passare activateCats al componente logo tramite prop o context
// <CatRain isActive={catsActive} onComplete={() => setCatsActive(false)} />
```

### 5. Fallback offline

Se `cataas.com` non è raggiungibile (POC demo senza internet), usa emoji gatto `🐱` come fallback:

```typescript
const catUrl = navigator.onLine ? `https://cataas.com/cat?${Math.random()}` : null;
// Se null, renderizza un <span> con emoji invece di <img>
```

## Test dell'easter egg

L'easter egg è testabile con Vitest:

```typescript
// src/hooks/use-konami-code.test.ts
it('fires callback after full Konami sequence', () => {
  // Simula i 10 keydown events in sequenza
  // Verifica che il callback venga chiamato esattamente 1 volta
});
```

## Note per il demo/hackathon

- Durante la demo, il Konami code è il trigger preferito (più spettacolare)
- Assicurarsi che `cataas.com` sia accessibile dalla rete del venue
- In caso di rete limitata, il fallback emoji funziona senza connessione esterna
