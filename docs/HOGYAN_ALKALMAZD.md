# Hogyan alkalmazd a v0.3.7 cleanup változásait a projektedre

> A workspace (`FigmaFlow/`) és a Unity projekted két külön másolat.
> Ez az útmutató azt írja le, hogyan vidd át a javításokat a projektedbe.

---

## 1. Melyik fájlokat másold át (egyszerű út)

A workspace-ből a Unity projekted `Assets/FigmaRuntime/` mappájába — **fájlonként**:

| Fájl (workspace) | Cél (projekt) | Mit tartalmaz |
|---|---|---|
| `Core/TransitionBuilder.cs` | `Core/` | F17 (FindByPath) + F21 (tolerancia paraméterek) |
| `Core/VisualTreeUtility.cs` | `Core/` | F17 (FindNextNamed, névtelen-átlátszó navigáció) + UTF-8 |
| `Runtime/FigmaStateMachine.cs` | `Runtime/` | F1 (Overlay kivezetés) + F2 (log) + F3 (gate) + F4 (guardok) + F21 |
| `Runtime/FigmaDocumentHost.cs` | `Runtime/` | F3 (log takarítás) |
| `Runtime/StateCapture.cs` | `Runtime/` | F3 (log gate) |
| `Runtime/FigmaFlowRuntime.cs` | `Runtime/` | F9 (felesleges using törölve) |
| `Editor/TransitionAssetBuilder.cs` | `Editor/` | **ÚJ** — az F12 átmozgatott Builder |
| `Editor/TransitionAssetEditor.cs` | `Editor/` | F25 (CustomSpring figyelmeztetés) |
| `Data/FigmaStateAsset.cs` | `Data/` | F20 (indentálás) |
| A 8 freeze-elt fájl | eredeti helyük | csak a MOTION ENGINE FREEZE fejléc került hozzá |

**Ne felejtsd el törölni a projektedben:** `Runtime/TransitionAssetBuilder.cs` (átkerült az Editor-ba).

## 2. Alternatív út: diff-ek alkalmazása

A `docs/changes/` mappában minden változtatás külön diff fájlban van:
`F1_*.diff` · `F17_*.diff` ×2 · `F20_*.diff` · `F12_*.diff` · `batch2_*.diff` ×5

Ezeket `git apply`-val vagy `patch`-tel ütheted be, ha a repód verziója megegyezik az uploads állapottal.

## 3. Ellenőrző lista — honnan tudod, hogy a javítás éles?

- [ ] `Runtime/FigmaStateMachine.cs` tartalmazza: `// F1 (v0.3.7, ADR-007)` és `Layout player-rel folytatjuk`
- [ ] `Editor/TransitionAssetBuilder.cs` létezik; `Runtime/TransitionAssetBuilder.cs` NINCS
- [ ] `Core/TransitionBuilder.cs`-ben `VisualTreeUtility.FindByPath(...)`, és nincs `Q<VisualElement>`
- [ ] `Core/VisualTreeUtility.cs`-ben van `FindNextNamed`
- [ ] A 8 freeze fájl tetején: `// MOTION ENGINE FREEZE (v0.3.7)`
- [ ] Unity: `Assets → Reimport All` (a fájlok frissítéséhez), figyeld a Console-t hibákra

## 4. Regressziós teszt (kötelező, a gate része)

- Shoes demo, Tabs demo
- `GoTo()`, `GoToState()`, 0→2, 2→0, 2→1
- Motion Studio: Duration, Delay, Ease, Custom Curve
- Különösen: az F17 miatt a path-alapú keresés szigorúbb — ha egy elem kimarad az animációból, a neve a live fán és a capture path-ben eltér

## 5. Kódolási figyelmeztetés

Több fájl (pl. `VisualTreeUtility.cs`, az Overlay fájlok) ISO-8859 (latin-1) kódolású volt. A javított `VisualTreeUtility.cs` már UTF-8. Ha a Unity furcsa karaktereket mutat kommentekben, a fájlt UTF-8-ra kell konvertálni (a v0.4 asmdef split előtt az Overlay fájlokkal is meg kell tenni).
