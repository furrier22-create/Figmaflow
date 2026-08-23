# Experimental — Karanténszabályok

> Ez a mappa a félkész, kutatási jellegű fejlesztéseknek ad helyet.
> **A Stable Runtime (Core / Runtime / Tracks / Data) soha nem hivatkozhat erre a mappára.**
> A benne lévő kód nem része a kiadott Runtime-nak, és nem számít bizonyítottnak.

---

## Mi kerül ide

- Félkész fejlesztések (kutatási eredmények, prototípusok, spike-ok)
- Olyan kód, ami új render pipeline-t / új architektúrát vizsgál (pl. Overlay)
- Olyan kód, amit a fejlesztés során "megelőztek" az események (pl. StateCache, ha a végleges megoldás editor-oldali)

## Mi NEM kerül ide

- Működő, bizonyított Runtime kód
- Motion Engine fájlok (ezek Freeze alatt állnak)
- Bugfixek — a bugfix a Stable-ben történik, az Experimental nem menedék a javítások elől

## Visszakerülés a Stable-be

Csak akkor történik meg, ha:
1. Van dokumentált követelmény (pl. v0.4: Figma Overlay Support)
2. A kód átmegy a normál fejlesztési folyamaton (Analysis → Builder → Motion Engine)
3. A kód hivatkozhat a Stable rétegekre — de a Stable nem hivatkozhat rá
4. A döntés ADR-be kerül (ld. `docs/ADRs.md`)

## Tartalom

| Mappa | Tartalom | Státusz |
|---|---|---|
| `Overlay/` | Overlay kutatási kód (15 fájl): OverlayScene, OverlayRenderer, OverlayBinder, OverlayTransitionBuilder, RenderNode, IRenderNode, OverlayLayer, OverlayAnimationItem, OverlayRuntime, OverlayTransitionPlayer, OverlayRenderData, OverlayRenderPair, OverlayDocumentHost, OverlayInstance, OverlayRenderItem | Félkész kutatási eredmények — a Smart Overlay (v0.4) alapja, de NEM Stable |
| `StateCache/` | StateCache.cs, StateCacheBuilder.cs | Nem használt; a Startup Flash editor-oldali megoldásának alapja (ADR-001) — a v0.4-ben aktív fejlesztésre kerül |
| ~~`Spike/`~~ | ~~OverlaySpike.cs, VisualSnapshotSpike.cs~~ | **Törölve** (2026-08-01, ADR-002) |

---

## Szabályok listája

- [ ] Semmi Experimental nem lehet referenciában a Stable Runtime felől
- [ ] Minden ide kerülő kód kap ADR bejegyzést (miért került ide, mi a sorsa)
- [ ] Az Experimental kód nem kaphat "bizonyított" státuszt semmilyen dokumentumban
