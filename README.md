# TI ZILL DIGITAL PWA

TI ZILL DIGITAL is a React + Vite Progressive Web App for credit sales, key delivery, expenditure, customers, catalog, reports, Dolibarr synchronization, and 58mm receipt printing. It has two runtime shells: a compact mobile UI with bottom tabs and BLE printing, and a desktop UI with two-row navigation and wider working panels.

## Setup

```bash
npm install
npm run dev
npm run build
npm run preview
```

The app is installable as a PWA in Chrome/Edge desktop and Android. Service workers and Web Bluetooth require HTTPS in production; localhost is treated as secure for development.

## Dolibarr setup

Default base URL: `https://internal.tizilldigital.xyz`. Generate a Dolibarr `DOLAPIKEY`, enter it in Settings, and use **Test Connection**. The token is kept only in memory and `sessionStorage`, never in `localStorage`.

Required Dolibarr API permissions:

- Third parties read/write
- Products read
- Invoices read/write/validate
- Payments write
- Bank accounts write

Use **Sync Products from Dolibarr** in Catalog or Settings workflows to map products into the local catalog.

## Tender mappings

| POS label | Dolibarr code | Bank account ID |
| --- | --- | --- |
| Cash | LIQ | 14 |
| Card | CB | 16 |
| BLINK BY EMTEL | BLINK | 17 |
| POP/MIPS | MIPS | 18 |
| MCB JUICE QR | JM | 19 |
| MCB JUICE | JU | 20 |
| MY.T MONEY | MT | 21 |
| MY.T MONEY BANK TRANSFER | MTBT | 22 |

Mappings are editable in Settings. Legacy labels are normalized: Juice to MCB JUICE, Juice Online to MCB JUICE QR, Juice POS to MCB JUICE, and Bank Transfer to MY.T MONEY BANK TRANSFER.

## Sync workflow

Credit sale sync performs customer lookup, customer creation, invoice creation, invoice line creation, invoice validation, payment creation, then marks the record synced. The retry queue uses exponential backoff from 30 seconds to a 1 hour cap, with 10 attempts max. Sync Logs show queue state, redacted errors, and JSON export.

Dolibarr API responses are not cached by the service worker; API calls are network-only.

## 58mm Bluetooth printing

On mobile Chrome/Android, open Settings → Printer, pair a generic ESC/POS BLE 58mm printer, then use Test Print or record print buttons. The printer path writes raw ESC/POS in 512-byte BLE chunks. iOS Safari does not support Web Bluetooth, so the app falls back to the 80mm HTML print flow.

## Dual UI

Layout mode is automatic by viewport (`lg` / 1024px) and can be forced in Settings: Auto, Force Mobile, or Force Desktop. Both shells share the same stores, data layer, Dolibarr client, printing code, and business rules.

## Backup and restore

Reports can export sales CSV and application JSON. Settings → Data Management exports all `tizill_*` local data and can clear all local data after confirmation.

## Security

- Dolibarr token is stored in `sessionStorage` only.
- Logs redact sensitive header names.
- Dolibarr API calls are never cached by the service worker.
- Internal UUIDs and Dolibarr IDs are not displayed in list/profile UIs.
