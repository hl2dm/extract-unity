```markdown
# Roadmap — This Round + Beyond

## 1. Polish fixes (this round)

**Foliage sparse.** `LevelGen.Vegetation()` caps at 2100 placed clumps and skips the road buffer (|z|<6.4), so density per m² is thin even though trees are leaf-cut to save tris. Fix: raise the cap (~4000+), shrink per-anchor jitter radius so clumps read as clusters, add a cheap second layer — 2-quad alpha-cutout "grass card" crosses (no mesh budget cost, GPU-instanced via `enableInstancing`) interleaved with the existing Poly Haven clumps to thicken visual coverage without adding tri budget.

**Toy-like helicopter.** `Heli.Build` is flat-color primitives (spheres/boxes), no greebling. No CC0 heli model exists in the project or Poly Haven's catalog (props/nature only), so don't add a new asset pipeline this round — stability first. Instead: bake simple panel-line/AO detail into a small tiling metal texture, add a rotor-blur disc texture instead of 4 static blades, tint glass canopy darker with a faint reflection, add exhaust soot + two-tone paint decal. Mesh stays procedural; only materials change.

**Covered car = gray block.** `Art.PHMat` falls back to flat gray when `_d`/`_n` textures fail to load. The resource path for `PH/covered_car` is `Resources.Load<Texture2D>("PH/covered_car_d")` etc. — check whether those files were actually downloaded/imported (likely missing from the Poly Haven pull or wrong import preset). Fix: re-fetch `covered_car` textures at 1-2k, confirm sRGB on diffuse / linear on normal+mask, verify filenames match the `_d/_n/_m` convention.

**Red blob above soldier head.** `Art.ApplyPH(g, "PH/soldier")` has no entry in `phSets`, so `PHKey` returns the same `"PH/soldier"` id for *every* material slot on the model (body, gear, patches, etc. all sample one diffuse atlas). If the source rig has a separate accessory submesh (e.g. rank patch/headset) that should sample a different region of the atlas, it now samples wherever the body UV lands — if that spot is a red print on the uniform texture, you get a flat red swatch floating near the head bone. Fix: add a `phSets["PH/soldier"]` entry mapping each real material-slot name to its correct texture key (log slot names once in editor, then map), same pattern already used for `vintage_cabinet_01`/`chainlink_fence`.

**Red splat near a door.** `LevelGen.Wall()` draws the locked-door indicator as two bare flat emissive rectangles (`0.5×0.22`, intensity 2.5) directly on the leaf — no shape, no bezel, reads as a smear. Fix: replace with a small padlock prop (cylinder shackle + box body) or a rounded-rect decal using the existing `Art.Rounded` sprite as a quad texture, shrink size, lower intensity to ~1.5.

**Repetitive house layout.** Only 3-4 fixed lot positions (`LevelGen.cs:33`) and a small `types` room array reused every raid. Fix: parametrize lot count/positions per seed (shuffle from a larger candidate pool of 6-8 lot slots, pick 3-5), and expand room `types` with 2-3 more variants (garage, home office) so `Furnish()` has more branches to roll.

**Fridge not realistic.** No CC0 fridge on Poly Haven; current model is a Kenney/Quaternius-style low-poly asset that clashes with PBR furniture. Cheapest fix without new asset sourcing: give it a proper PBR material (steel normal/roughness map reused from existing `metal` texture set) instead of flat color, add a visible door handle + small vent grille prop — raises fidelity without a new download.

**AK viewmodel slightly large.** `Player.Equip` sets AK viewmodel length `L=0.64` positioned at `RealPos=(-0.04,0.01,0.26)` with `vmCam.fieldOfView=55`. Perceived size comes from proximity (0.26m from camera) more than absolute scale. Fix: push `RealPos.z` out slightly (e.g. 0.26→0.30) and/or trim `L` to ~0.6; cheaper than refitting FOV since FOV changes also affect the crosshair/ADS feel.

**Stiff soldier pose.** No CC0 rifle-hold anims; currently using `LeftHandIK` against the `Pistol_Aim` clip, which doesn't match rifle grip geometry. Fix: don't chase new animation packs this round (stability) — instead hand-tune the IK target offset/rotation per gun in `LeftHandIK` so the off-hand lands convincingly on the AK's handguard for the *idle/walk* poses specifically (the ones players see most), accepting imperfect aim-pose for now.

## 2. Themes this round: Mart + Office

Both reuse the existing cell-grid generator (`LevelGen.BuildHouse`'s wall/door/room machinery) rather than inventing new geometry systems — lowest-risk way to add content. Generalize `BuildHouse` into a shared grid builder parametrized by theme (open vs. walled cells, furniture-per-roomtype callback). **Base** and **Port** are deferred (warehouse needs new crate-stacking rules, port needs container-maze nav — bigger engineering lift).

### Mart (便利商店/大賣場, tier 2, ×1.15)
Single open-plan building: sales floor (unwalled aisle cells — set interior wall `op` to `"none"` so cells merge into one room), 1 locked stockroom (crates+safe), 1 locked walk-in cooler (small, drinks).
- Containers: `shelf` (new CKind, pool mid, reuse drawer-style model), `cooler` (reuse fridge model, pool mid), `register` (pool low, small), `box`/`safe` in stockroom — nMin/nMax matching house's 2-7 range.
- Items (r0→r4, matching house's $4→$9000 scale): 🍙過期御飯糰 $5 · 🎟️刮刮樂(銘謝惠顧) $65 · 📶儲值卡(餘額不明) $380 · 🎰拉霸機零件 $1700 · 🏆神秘店長的金招牌 $9500.
- Enemies: 2 (clerk guard + security). LZ: same parking-lot pattern as house, ~40-50m from entrance.

### Office (辦公大樓, tier 3, ×1.35)
Single-story suite reusing the full room+door+lock logic unchanged: open bullpen (cubicles, printer, water cooler), 1 meeting room, 1 locked manager office (safe+filing cabinet), 1 locked server room. Rooftop LZ deferred — keep street-level LZ to avoid new elevator/stair nav work.
- Containers: `drawer`→filing cabinet skin, new `rack` CKind (server room, pool high, procedural box like `safe`'s fallback).
- Items: 📎迴紋針一盒 $6 · ☕美式咖啡(冰半糖) $85 · 🖨️故障印表機零件 $460 · 💼機密企劃書 $2000 · 🗝️總座的隨身碟 $11000.
- Enemies: 2 (保全 + 加班同事). LZ: street-level, same pattern.

## 3. Enemy increase plan

Currently exactly **one** hardcoded patrol enemy per raid (`LevelGen.cs` house build). Plan:
1. Add `enemyMin/enemyMax` to `ThemeDef` (house 1-2, mart 2-3, office 2-3).
2. Loop-spawn in `LevelGen` using existing `Room`/`spots` list to distribute patrol routes without clustering.
3. No new AI class needed — `Enemy`'s fields (`hp,dmg,range,speed,acc,patrol`) already support variants by just tweaking values post-`Create()`: **guard** (default), **stationary** (`patrol.Count=0`, existing alert/idle logic already handles this), **rusher** (speed ×1.5, acc ×0.8, lower hp). Roll archetype 60/25/15%.
4. Verify `Game.AliveEnemies`/`CanBoard` scale correctly with N>1 (already generic, no change needed).

## 4. Later rounds (5-8)

- **Round 5 — Base (軍事倉庫, tier 4):** open warehouse + crate shelving, reuses `box`/`safe` CKinds, 2-3 enemies incl. one armored "elite" (hp×2).
- **Round 6 — Port (港口碼頭, tier 5):** container-maze outdoor/night map — needs new stacking/nav rules, water/crane hazards, 3+ enemies.
- **Round 7 — Loot & quest depth:** multi-step quest chains, trophy-shelf set bonuses, rotating quest board, dynamic sell prices.
- **Round 8 — Replayability:** multiple lot/layout pools per theme, POI variants, day/night & weather, larger maps, optional "hardcore" raid modifier.
```
