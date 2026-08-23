# Experimental / Overlay

**Státusz:** Karanténban — félkész kutatási eredmények. **Nem részei a Stable Runtime-nak.**

**Tartalom (12 fájl):**

| Fájl | Szerep (kéznél lévő tudás szerint) |
|---|---|
| `OverlayScene.cs` | Overlay jelenet/életciklus kutatása |
| `OverlayRenderer.cs` | Overlay renderelési kísérlet |
| `OverlayBinder.cs` | Overlay adat kötése |
| `OverlayTransitionBuilder.cs` | Overlay transition építés kutatása |
| `RenderNode.cs` / `IRenderNode.cs` | Render node absztrakció |
| `OverlayLayer.cs` | Rétegkezelés |
| `OverlayAnimationItem.cs` | Animációs elem |
| `OverlayRuntime.cs` | Runtime kísérlet |
| `OverlayTransitionPlayer.cs` | Lejátszó kísérlet |
| `OverlayRenderData.cs` / `OverlayRenderPair.cs` | Render adat modellek |

**Fontos:**
- A **Smart Overlay NEM bugfix**, hanem új render pipeline — ez a v0.4 feladata.
- A Shoes demo nem igényli, a Tabs demo nem bizonyítja — az Overlay kód csak valódi Figma Overlay esetekben (Dialog, Bottom Sheet, Popup, Dropdown, Context Menu, FAB Morph, Hero, Shared Element) kerül elő.
- v0.4-ben a karanténból a normál fejlesztési folyamaton át kerülhet Stable-be (Analysis → Builder → Motion Engine), ADR-es döntéssel.
