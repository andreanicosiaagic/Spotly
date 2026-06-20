# Spotly — Documento di Analisi Funzionale

| | |
|---|---|
| **Progetto** | Spotly — Smart Office Booking |
| **Versione** | 1.0 |
| **Data** | 20/06/2026 |
| **Stato** | Bozza per hackathon / baseline prodotto |
| **Destinatari** | Giuria App-in-a-Day, sponsor interno, cliente potenziale |

---

## 1. Executive Summary

**Spotly** è un'applicazione che consente ai dipendenti di organizzare la propria giornata in ufficio
da un'unica interfaccia mobile-first, prenotando in anticipo **parcheggio**, **postazione di lavoro** e
**pranzo**, con visibilità in **tempo reale** sulla disponibilità delle risorse.

Il lavoro ibrido ha reso gli spazi aziendali una risorsa **variabile e condivisa**: non tutti sono in
ufficio ogni giorno, i posti auto sono scarsi, le scrivanie non sono più assegnate 1:1 e la pausa
pranzo è un punto di attrito quotidiano. Oggi parcheggio e postazione sono gestiti da
un'**applicazione interna già esistente ma macchinosa** — la prenotazione **in team** è complicata e
**non esistono piantine** né delle postazioni né dei parcheggi — mentre il **pranzo non è gestito**
affatto. Spotly li unifica in un'esperienza fluida, riducendo l'attrito per il dipendente e fornendo
all'azienda i **dati di utilizzo** necessari a ottimizzare spazi e costi.

**Obiettivo del documento:** definire il perimetro funzionale, gli attori, i requisiti, i flussi
principali e le regole di business di Spotly, distinguendo lo scope della **POC** (hackathon) da quello
del **prodotto target (MVP)**.

---

## 2. Obiettivi e contesto

### 2.1 Situazione attuale (as-is)

In azienda **esiste già un'applicazione interna** per la prenotazione di parcheggio e postazione, ma è
**macchinosa** e questo ne limita l'adozione. I limiti principali:

- **Prenotazione in team difficile:** coordinare la presenza del proprio gruppo (stesso giorno, posti
  vicini) richiede troppi passaggi manuali. È il **principale punto di dolore**.
- **Nessuna piantina:** non esistono mappe interattive né delle **postazioni** né dei **parcheggi**; si
  prenota "alla cieca", senza vedere dove si trova fisicamente la risorsa e cosa c'è intorno.
- **Pranzo non gestito:** la pausa pranzo è **completamente fuori** dal sistema attuale (modulo net-new).
- **Dati di utilizzo scarsi:** Facility/HR senza visibilità affidabile sull'occupazione reale.

### 2.2 Gap da colmare

- **Parcheggio:** posti limitati, nessuna mappa, nessuna visibilità real-time → conflitti e tensioni.
- **Postazioni:** hot-desking senza uno strumento usabile per prenotare, vedere il team e sedersi vicini.
- **Pranzo:** locali convenzionati con capienza limitata, code, nessuna programmazione, nessuna
  alternativa per chi resta fuori.
- **Azienda/Facility:** nessun dato affidabile sull'utilizzo reale → impossibile dimensionare spazi e servizi.

### 2.3 As-Is vs To-Be (Spotly)

| Aspetto | App attuale (as-is) | Spotly (to-be) |
|---|---|---|
| Prenotazione in team | Macchinosa, manuale | **Prenotazione di gruppo in un'azione** + "office day" |
| Piantine postazioni | Assenti | **Mappa interattiva** dei piani/zone |
| Piantine parcheggi | Assenti | **Mappa interattiva** dei posti auto |
| Abbinamento auto ↔ scrivania | Assente | **Suggerito automaticamente** |
| Pranzo | Non gestito | **Modulo dedicato** + lunch box di fallback |
| Disponibilità | Non in tempo reale | **Real-time** |
| Dati di utilizzo | Scarsi | **Reportistica occupancy** continua |

### 2.4 Obiettivi di business

| # | Obiettivo | KPI di riferimento |
|---|---|---|
| O1 | Ridurre l'attrito quotidiano del dipendente | Tempo medio per "organizzare la giornata" < 60s |
| O2 | Aumentare l'utilizzo efficiente degli spazi | Tasso di occupazione desk/parcheggio, riduzione no-show |
| O3 | Fornire dati di utilizzo a Facility/HR | Report occupancy affidabili e continui |
| O4 | Migliorare l'esperienza e il welfare | Adoption attiva, soddisfazione (CSAT/NPS) |
| O5 | Abilitare la riduzione dei costi immobiliari | Desk ratio sostenibile, mq ottimizzati |

### 2.5 Principi guida

1. **Mobile-first & zero-friction:** prenotare le tre risorse in pochi tap.
2. **Real-time:** ciò che vedo è ciò che è realmente disponibile.
3. **Abbinamento intelligente:** parcheggio e postazione proposti in modo coerente.
4. **Privacy by design:** i dati di presenza sono dati personali → minimizzazione e finalità chiare.
5. **Estendibile a prodotto:** architettura multi-sede / multi-tenant fin dalle fondamenta.

---

## 3. Attori e personas

| Attore | Descrizione | Bisogni principali |
|---|---|---|
| **Dipendente** | Utente finale che lavora in modalità ibrida | Prenotare velocemente parcheggio, postazione e pranzo; vedere disponibilità; modificare/cancellare |
| **Team Lead / Manager** | Coordina la presenza del proprio team | Vedere chi c'è, suggerire "office day", prenotare zone team |
| **Office Manager / Facility** | Gestisce spazi, risorse e policy | Configurare piani, posti, quote, policy; vedere report di occupazione |
| **HR / People** | Politiche di lavoro ibrido e welfare | Dati aggregati su presenze e utilizzo welfare pranzo |
| **Partner ristorazione** | Locali convenzionati / fornitore lunch box | Ricevere ordini/prenotazioni, gestire capienza e menù |
| **Reception / Security** | Accoglienza e controllo accessi | Allineamento prenotazioni ↔ accessi (badge/tornelli) |
| **Amministratore di sistema** | Gestione tecnica e configurazione tenant | Utenti, ruoli, integrazioni, sicurezza |

**Persona di riferimento — "Giulia, 34, Project Manager ibrida":** va in ufficio 3 giorni a settimana,
arriva in auto, vuole sedersi vicino al suo team e non perdere tempo per il pranzo. Apre Spotly la sera
prima, vede un posto auto libero, sceglie una scrivania nella zona del team con un monitor, e prenota
il pranzo al bistrot convenzionato; se è pieno, accetta il lunch box.

---

## 4. Perimetro funzionale (Scope)

### 4.1 In scope — Prodotto target (MVP)

- Modulo **Parcheggio** (M1)
- Modulo **Postazioni / Desk** (M2)
- Modulo **Pranzo** (M3)
- Funzionalità **trasversali**: autenticazione SSO, mappe interattive, notifiche, calendario,
  check-in, back-office di amministrazione, reportistica.

### 4.2 In scope — POC (hackathon)

Slice verticale "happy path" su singola sede:
- Login SSO (o mock), una mappa per modulo, prenotazione/cancellazione di 1 risorsa per modulo,
  disponibilità real-time simulata, fallback lunch box, deploy su Azure.
- Integrazioni esterne **mockate** (calendario, badge, partner, pagamenti).

### 4.3 Out of scope (prima release)

- App native pubblicate su store (si parte da PWA installabile).
- Pagamenti reali / fatturazione verso dipendente (gestiti via welfare/convenzioni).
- Gestione sale riunioni e altri asset (roadmap futura).
- Multi-lingua oltre IT/EN (predisposto, non completo).

---

## 5. Requisiti funzionali

> Notazione: **[POC]** incluso nella proof of concept · **[MVP]** previsto nel prodotto · priorità
> MoSCoW (**M**ust / **S**hould / **C**ould).

### 5.1 Modulo Parcheggio (M1)

| ID | Requisito | Scope | Prio |
|---|---|---|---|
| M1-01 | Visualizzare la mappa dei posti auto con stato (libero/occupato/prenotato) | POC | M |
| M1-02 | Disponibilità aggiornata in **tempo reale** | POC | M |
| M1-03 | Prenotare un posto per una data/fascia oraria | POC | M |
| M1-04 | Modificare e cancellare la prenotazione | POC | M |
| M1-05 | Gestire **posti speciali** (disabili, EV charging, ospiti, riservati) | MVP | M |
| M1-06 | **Abbinamento** automatico del posto auto alla postazione scelta | MVP | S |
| M1-07 | Check-in (QR/geofence) e **release automatico** in caso di no-show | MVP | S |
| M1-08 | Lista d'attesa / notifica quando un posto si libera | MVP | C |
| M1-09 | Regole: max 1 posto attivo per utente/giorno, finestra di prenotazione | MVP | M |

### 5.2 Modulo Postazioni / Desk (M2)

| ID | Requisito | Scope | Prio |
|---|---|---|---|
| M2-01 | Mappa interattiva di piani/zone con stato delle scrivanie | POC | M |
| M2-02 | Prenotare una postazione per data/fascia (giornata o mezza) | POC | M |
| M2-03 | Modificare/cancellare; **prenotazioni ricorrenti** (es. ogni martedì) | MVP | S |
| M2-04 | **Preferenze**: vicino al team, monitor, standing desk, finestra | MVP | S |
| M2-05 | Visualizzare **dove siede il team** (presenza colleghi, opt-in) | MVP | S |
| M2-06 | **Hot-desking** con vincoli: zone assegnate a team, quote per reparto | MVP | M |
| M2-07 | Policy lavoro ibrido (giorni max/min in ufficio, capienza per giorno) | MVP | C |
| M2-08 | Check-in QR alla scrivania; release no-show | MVP | S |
| M2-09 | Abbinamento postazione ↔ parcheggio (coerenza con M1-06) | MVP | S |
| M2-10 | **Prenotazione di gruppo/team** in un'unica azione (più persone, stessa zona/giorno) | MVP | M |
| M2-11 | **"Office day" del team**: proposta di un giorno comune e prenotazione coordinata di desk vicini | MVP | S |

### 5.3 Modulo Pranzo (M3)

| ID | Requisito | Scope | Prio |
|---|---|---|---|
| M3-01 | Elenco **locali convenzionati** con menù, fascia oraria e capienza | POC | M |
| M3-02 | Prenotare il pasto/slot presso un locale | POC | M |
| M3-03 | **Fallback lunch box**: se i locali sono al completo, ordinare un box consegnato in ufficio | POC | M |
| M3-04 | Preferenze alimentari e **allergeni/intolleranze** | MVP | M |
| M3-05 | Gestione **capienza/slot** per locale e overbooking → instradamento a lunch box | MVP | M |
| M3-06 | Integrazione **budget welfare / buoni pasto** (Edenred, Pellegrini, …) | MVP | S |
| M3-07 | Notifica conferma ordine, stato consegna lunch box | MVP | S |
| M3-08 | Storico ordini e ripeti ordine | MVP | C |

### 5.4 Funzionalità trasversali (M0)

| ID | Requisito | Scope | Prio |
|---|---|---|---|
| M0-01 | **SSO** con identità aziendale (Entra ID) e profilo utente | POC | M |
| M0-02 | Ruoli e permessi (dipendente, manager, facility, admin) — **RBAC** | MVP | M |
| M0-03 | **Dashboard "La mia giornata"**: vista unica delle 3 prenotazioni | POC | M |
| M0-04 | **Notifiche** push/email/Teams (promemoria, conferme, no-show) | MVP | S |
| M0-05 | Integrazione **calendario Outlook** (Microsoft Graph): crea/aggiorna eventi | MVP | S |
| M0-06 | **Back-office Admin**: gestione sedi, piani, risorse, policy, partner | MVP | M |
| M0-07 | **Reportistica & analytics**: occupazione, no-show, utilizzo per sede/giorno | MVP | M |
| M0-08 | Multi-sede / multi-tenant (predisposizione prodotto) | MVP | S |
| M0-09 | PWA installabile, offline-friendly per consultazione | MVP | C |

---

## 6. Flussi principali (User Journeys)

### 6.1 "Organizza la mia giornata" (flusso felice)

```
Login SSO
  └─> Dashboard "La mia giornata" (data selezionata)
        ├─ 1. Parcheggio: mappa → seleziona posto libero → conferma
        ├─ 2. Postazione: mappa zona team → seleziona desk (filtro: monitor) → conferma
        │        └─ Spotly propone l'abbinamento col posto auto più vicino all'ingresso
        └─ 3. Pranzo: scegli locale convenzionato → slot 13:00
                 └─ se COMPLETO → proposta Lunch Box (menù + allergeni) → conferma consegna in ufficio
  └─> Riepilogo + (opz.) evento in calendario Outlook + promemoria
```

### 6.2 Disponibilità in tempo reale

Ogni client è sottoscritto agli aggiornamenti della sede/giorno. Quando un utente prenota o cancella,
gli altri vedono la mappa aggiornarsi **senza refresh** (push via SignalR). Una prenotazione "tiene"
la risorsa per N minuti durante la conferma per evitare doppie assegnazioni (prenotazione ottimistica
con lock temporaneo).

### 6.3 No-show e release automatico

```
Prenotazione attiva → finestra di check-in (es. entro le 10:30)
  ├─ Check-in effettuato (QR/geofence) → risorsa confermata
  └─ Nessun check-in → release automatico → risorsa torna disponibile → (opz.) assegnata a lista d'attesa
```

### 6.4 Overflow pranzo → Lunch Box

```
Richiesta pasto al locale X, slot 13:00
  ├─ Capienza disponibile → prenotazione confermata
  └─ Capienza esaurita → Spotly propone:
        ├─ altro locale/slot disponibile, oppure
        └─ Lunch Box (catalogo box, allergeni) → ordine → consegna in ufficio
```

### 6.5 Prenotazione di team (gruppo)

```
Team Lead apre "Prenota per il team" → seleziona i membri (o il gruppo)
  └─> sceglie il giorno (Spotly suggerisce l'"office day" con più disponibilità)
        └─> Spotly propone un blocco di postazioni vicine nella zona del team
              ├─ conferma in un'unica azione per tutti i membri
              └─ (opz.) propone i parcheggi e invia l'invito in calendario ai membri
  └─> Ciascun membro riceve notifica e può aggiustare la propria prenotazione
```

> Risolve il principale limite dell'app attuale: la prenotazione in team passa da molti passaggi
> manuali a **un'unica azione coordinata**.

---

## 7. Regole di business

| ID | Regola |
|---|---|
| R-01 | Un utente può avere **al massimo una** prenotazione attiva per tipo-risorsa per giorno. |
| R-02 | Finestra di prenotazione configurabile (es. fino a 14 giorni avanti, entro le 23:59 del giorno prima per il lunch box). |
| R-03 | Una risorsa non può essere prenotata da due utenti per la stessa fascia (lock atomico). |
| R-04 | No-show oltre la finestra di check-in → release automatico + eventuale penalità soft (es. priorità ridotta). |
| R-05 | L'abbinamento parcheggio↔postazione è **suggerito** ma non obbligatorio; l'utente può disaccoppiare. |
| R-06 | Il lunch box è attivabile **solo** quando i locali sono al completo o fuori orario, salvo override admin. |
| R-07 | Le quote per reparto/zona e le policy hybrid sono definite dal Facility e prevalgono sulle scelte individuali. |
| R-08 | Posti speciali (disabili/EV/ospiti) seguono regole di idoneità dedicate. |
| R-09 | Cancellazione gratuita entro una soglia; oltre soglia conta come no-show. |

---

## 8. Integrazioni

| Sistema | Scopo | Direzione | Note |
|---|---|---|---|
| **Microsoft Entra ID** | Autenticazione SSO, profilo, gruppi | In | OIDC/OAuth2 |
| **Microsoft Graph** | Calendario Outlook, presenza Teams, notifiche Teams | In/Out | Eventi e promemoria |
| **Sistema badge / tornelli** | Allineare prenotazione ↔ accesso fisico, check-in | In/Out | Per release no-show |
| **Partner ristorazione** | Capienza, menù, ordini, lunch box | In/Out | API o portale partner |
| **Welfare / buoni pasto** | Budget pranzo, scalare il buono | Out | Edenred, Pellegrini, … |
| **HRIS** (opz.) | Anagrafica dipendenti, reparti, sedi | In | Sincronizzazione utenti |

Per la **POC** tutte le integrazioni esterne sono mockate dietro interfacce, così da poterle sostituire
con i connettori reali senza modificare il dominio.

---

## 9. Requisiti non funzionali (NFR)

| Categoria | Requisito |
|---|---|
| **Performance** | Caricamento mappa < 1.5s; aggiornamenti real-time < 1s; API p95 < 300ms. |
| **Scalabilità** | Multi-sede e multi-tenant; scaling orizzontale stateless; SignalR gestito. |
| **Disponibilità** | Target 99.9% per il prodotto; health/readiness probe; nessun single point of failure. |
| **Sicurezza** | SSO, RBAC, least privilege, segreti in Key Vault, TLS terminato upstream, audit log. |
| **Privacy / GDPR** | I dati di presenza sono **dati personali**: minimizzazione, finalità esplicite, retention limitata, DPIA, opt-in per la visibilità della presenza ai colleghi. |
| **Accessibilità** | WCAG 2.1 AA: contrasto, navigazione da tastiera, screen reader sulle mappe. |
| **Osservabilità** | Logging tecnico (mai PII di dominio), metriche, tracing, alerting. |
| **Localizzazione** | IT/EN; fusi orari e festività per sede. |
| **Manutenibilità** | Codice modulare per dominio, test automatici, IaC riproducibile. |

---

## 10. Modello di dominio (concettuale)

```
Tenant ──< Sede ──< Edificio ──< Piano ──< Zona ──< Risorsa
                                                      ├─ PostoAuto (tipo: standard/disabili/EV/ospiti)
                                                      └─ Postazione (attributi: monitor, standing, finestra)

Sede ──< LocaleConvenzionato ──< Slot (capienza) / Menù ──< Piatto (allergeni)
Sede ──< CatalogoLunchBox ──< Box (allergeni)

Utente ──< Prenotazione (tipo: PARCHEGGIO | POSTAZIONE | PRANSO)
   Prenotazione { risorsa, data, fascia, stato, checkIn, abbinamentoId? }
   OrdinePranzo { localeId | boxId, slot, preferenze, allergeni, statoConsegna }

Policy { quotePerReparto, finestraPrenotazione, regoleHybrid, finestraCheckIn }
```

> Nota: il modello è **concettuale** e verrà raffinato in fase di design tecnico. Gli identificatori e
> gli schemi DB non sono ancora vincolati.

---

## 11. Metriche di successo (KPI)

| Area | Metrica | Target indicativo |
|---|---|---|
| Adoption | % dipendenti attivi/mese | > 60% nei primi 3 mesi |
| Efficienza | Tempo medio per organizzare la giornata | < 60 secondi |
| Spazi | Tasso di occupazione desk/parcheggio | visibilità continua, +utilizzo |
| Affidabilità | Tasso di no-show dopo introduzione check-in | −30% |
| Pranzo | % richieste soddisfatte (locale + lunch box) | > 95% |
| Soddisfazione | CSAT / NPS | CSAT > 4/5 |

---

## 12. Rischi e assunzioni

| Tipo | Voce | Mitigazione |
|---|---|---|
| Assunzione | L'azienda usa Microsoft 365 / Entra ID | Astratto dietro provider di identità |
| Assunzione | Esistono planimetrie digitali dei piani | Editor mappe nel back-office come fallback |
| Rischio | Integrazione badge/tornelli eterogenea | Connettore dedicato + modalità check-in QR alternativa |
| Rischio | Accettazione del check-in (no-show) | UX leggera, promemoria, penalità "soft" |
| Rischio | Privacy presenza colleghi | Opt-in esplicito, granularità configurabile |
| Rischio | Capienza/ordini partner non in tempo reale | Buffer di sicurezza + fallback lunch box |

---

## 13. Glossario

- **Hot-desking:** scrivanie non assegnate, prenotabili di volta in volta.
- **Desk ratio:** rapporto scrivanie/dipendenti (es. 0,7 = 7 scrivanie ogni 10 persone).
- **No-show:** prenotazione non onorata senza cancellazione.
- **Lunch box:** pasto confezionato consegnato in ufficio quando i locali sono al completo.
- **Tenant:** istanza logica di un'azienda cliente (rilevante per la versione prodotto).
- **PWA:** Progressive Web App, installabile e mobile-first.
