# 極速撤離 Unity Port — Design Doc (Vertical Slice)

## Architecture

**Bootstrap.cs** — sole scene's entry point. Loads save, inits `AssetCatalog`, `EventBus`, `PostFXRig`, then hands off to `HubBuilder` or `RaidManager` based on save state.

**AssetCatalog.cs** — keyed `Resources.Load<T>` cache (enum → GameObject/AnimationClip/AudioClip) for Quaternius/Kenney FBX and clips. Loads once, never re-hits disk.

**Procedural generation**
- `ProceduralLevelBuilder` (abstract) → `HubBuilder`, `HouseDistrictBuilder` (民房社區).
- `HouseDistrictBuilder`: `System.Random(seed)` drives room graph → wall/door mesh instancing (Kenney Furniture Kit), container placement, loot table rolls, one soldier spawn, locked-room+key assignment. Seed is persisted so a raid can be resumed/replayed deterministically.

**Loot**
- `LootTable` (rarity weights) + `LootContainer` (per-cell state machine: hidden → spinning → revealed).
- `ContainerSpinnerUI`: cell-by-cell reveal, ring tint by rarity (grey/green/blue/purple/gold), spin duration scales with rarity, `全部拿走` instant-resolve, `💸付費加速` fake paywall gag (always "declines").

**Player**
- `PlayerController`: `CharacterController` move/look, `E` interact raycast.
- `WeaponSystem`: hitscan raycast, recoil curve, muzzle flash, tracer, ammo, ADS lerp; viewmodel lives on a camera-stacked rig (`ViewmodelRig`) — separate camera, culling-mask isolated, no arms.
- `HolsterController`: swaps viewmodel visibility for extraction cinematic.

**Enemy**
- `EnemyAI`: idle → alert → shoot → dead FSM, driven by `Animation` (legacy) clips sliced from the 17-clip FBX. Vertical slice uses raycast line-of-sight + straight-line approach, **not** NavMesh (see Risks).

**Flow/meta**
- `RaidManager`: timer, extraction trigger, helicopter cinematic (lerp-driven, no external tween dep), death→insurance→secure-slot resolution.
- `SaveSystem`: JSON ⇄ `PlayerPrefs` (stash, currency, secure slot).
- `AudioManager`: `AudioClip.Create` synthesis (gunshots/heli via sine+noise) + Kenney CC0 SFX playback through 2 mixer groups.
- `UIManager`: legacy `uGUI` (Canvas/Text), HUD, hit markers, spinner UI, vending/map-table UI.
- `EventBus`: static C# events (`OnLootRevealed`, `OnEnemyDeath`, `OnExtract`) — keeps builders/UI decoupled.

## Art Direction

| | Hub | 民房社區 |
|---|---|---|
| Palette | neutral industrial grey-blue, warm practical fills | weathered beige/brick, overcast grey sky, amber sodium streetlights, sickly green interior fluorescents |
| Lighting | flat bright ambient, single soft directional | low-intensity directional (overcast), warm flickering point lights indoors, stronger SSAO in clutter |
| Post (Volume) | ACES, light vignette, bloom near-off | ACES, moderate vignette, bloom gated to emissives only (muzzle flash, signage, loot ring glow) |
| Rarity ring colors | — | grey/green/blue/purple/gold, gag paywall button = neon pink |

**Feel/juice list**: procedural camera bob (sine, speed-scaled) · mouse-delta weapon sway · recoil kick+spring-back on viewmodel & camera pitch · muzzle flash (point light pulse + billboard sprite) · tracer (short-lived stretched quad/`LineRenderer`) · hit marker (crosshair flash + tick SFX) · screen shake (Perlin on camera rig for shots/heli) · loot fanfare (cell flicker → ring pulse → item pop with scale-bounce, pitch-shifted sound per rarity tier, confetti burst on gold).

**Critique**: Quaternius kit is toon-shaded; ACES + bloom pushes toward filmic/realistic grading, which will fight the flat toon look. Lower bloom threshold to near-off by default, push contrast instead of exposure, and consider swapping toon-unlit shaders for URP Lit w/ low smoothness so lighting reads consistently across themes — an art pass to schedule before ship, not a slice blocker.

## Risks (headless/URP/WebGL)

1. **Shader variant stripping** — biggest risk. Materials/keywords created only at runtime (`new Material(Shader.Find(...))`, enabling `_NORMALMAP`, shadow/SSAO keywords) aren't visible to Unity's static stripper; they can render pink/black in a built Player while working fine in-Editor.
2. **URP Renderer Features at runtime** — `ScriptableRendererFeature`s (SSAO) are normally serialized on the `UniversalRendererData` asset at edit time. Adding one purely at runtime is unsupported/fragile; same for enabling "Post Processing" + opaque/transparent post passes on the Renderer asset.
3. **Legacy `Animation` component** — works, but FBX must import with Rig = Legacy, a dead end for later blending/retargeting and a potential clash if any Quaternius anims assume Humanoid.
4. **Dynamic CJK font** — Noto Sans TC subset must ship as an embedded `.ttf` Font asset; an OS/system dynamic font (`Font.CreateDynamicFontFromOSFont`) will not resolve in WebGL or on a clean Windows box without the font installed.
5. **Input** — the new Input System package needs an Input Actions asset + generated wrapper, awkward to author headless; legacy `Input.GetAxis/GetKey` is simpler but needs the legacy backend active in Player Settings, and WebGL mouse-look needs pointer lock (works via `Cursor.lockState`, but only after a user gesture — the first click must be "consumed" by the lock prompt, not treated as a shot).
6. **WebGL build from batchmode on Windows** — needs the WebGL module + IL2CPP/Emscripten toolchain pre-installed; IL2CPP builds are slow and batchmode errors are easy to miss without GUI feedback.
7. **PlayerPrefs save** — works (IndexedDB-backed in WebGL) but cleared by private/incognito mode or site-data clears; fine for a vertical slice, not for real persistence later.
8. **Runtime NavMesh per seed** — baking a `NavMeshSurface` for every procedurally generated district adds load-time cost and another runtime-vs-editor-asset gap; unnecessary with 1 enemy.

## Recommendations

- **De-risk #1/#2 together**: keep level/loot/enemy generation runtime-procedural as planned, but use an **editor script run via `-batchmode -executeMethod`** (still non-GUI) to pre-bake the handful of assets the build pipeline must see statically: the URP Pipeline Asset + Renderer Data with SSAO feature and post-processing enabled, plus one placeholder `.mat` per shader+keyword combination you'll instantiate at runtime (forces inclusion). Add those shaders to Graphics Settings' "Always Included Shaders" too.
- **Legacy Animation**: acceptable for the single soldier's 4 clips in this slice; don't invest further — migrate to Animator + Humanoid/Generic avatar only if later enemy variety needs blending.
- **Font**: import the Noto Sans TC subset as a real `.ttf` asset in `Resources`, build the `Font` from that asset explicitly; never rely on OS font resolution.
- **Input**: stick with the legacy Input Manager for this slice — simplest path to WebGL parity; explicitly request pointer lock on first click with a visible "click to play" prompt rather than firing a shot.
- **WebGL toolchain**: verify the WebGL module + IL2CPP/Emscripten are installed and do one empty-scene WebGL build *before* the full slice to catch batchmode/toolchain failures early and cheaply.
- **NavMesh**: defer; straight-line/LOS movement suffices for 1 enemy. Revisit only when adding multiple enemies or complex interiors.
- **Save**: fine as-is for the milestone; flag PlayerPrefs fragility for the backlog, don't fix now.

## Decisions taken (executor)
- Accept: editor-baked URP asset + renderer (SSAO feature) + template materials in Resources; legacy Input; legacy uGUI Text w/ embedded Noto Sans TC subset; legacy Animation for soldier; no NavMesh.
- Lit look: URP/Lit low smoothness, ACES, low bloom (threshold 1.1), emissive rings/muzzle only.
