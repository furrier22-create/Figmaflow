# Experimental / StateCache

**Státusz:** Karanténban (ADR-001) — nem használt, nem része a Stable Runtime-nak.

**Tartalom:**
- `StateCache.cs`
- `StateCacheBuilder.cs`

**Eredet:** A Startup Flash javításának kutatása közben készült; a fejlesztés során kiderült, hogy a végleges megoldás **editor-oldali** state cache lesz (v0.4), ezért a runtime cache építés irányát elvetettük.

**Sors:** A v0.4-ben a production útvonal a `Data/FigmaStateCacheAsset` + `Editor/StateCacheExporter` (ADR-009, P3). Ez a runtime cache kutatási kód továbbra is karanténban marad, nem használt — a v0.4 végén törölhető.
