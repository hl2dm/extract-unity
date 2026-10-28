# STEP 6 Review — Interact / Loot / Art / Hints

## Complaint-by-complaint

| # | Complaint | Status | Notes |
|---|---|---|---|
| 1 | Props placed backwards | **Partial** | `Art.FrontFix` (Art.cs:46) hardcodes a per-model Euler correction for exactly 4 models (`painted_wooden_cabinet`, `metal_office_desk`, `sofa_02`, `Ext/fridge`), applied correctly before bounds/collider computation in `Art.Spawn` (Art.cs:175). It fixes what was found, but it's a manual patch keyed to a fixed string, not a systemic fix — any new model with a reversed pivot (or any of the ~50 other models in `AuditModels`, Game.cs:398-401) will silently render backwards again. The audit itself is just a screenshot dump for a human to eyeball; there's no automated "front faces +Z" check. |
| 2 | Interaction only triggers at specific angles | **Partial** | Genuinely improved — 94% (367/391) per the test harness — but the 24 remaining failures are the *exact same symptom* the owner complained about, for wall/corner-adjacent safes and raised crates at the far test distance (1.5m) with ±18° yaw (Game.cs:466-472). See Serious Bug #3 below. |
| 3 | Loot icons cartoonish → realistic | **Fixed** | `ItemArt.Pic` (ItemArt.cs:9) loads photo sprites from `Resources/ItemPics/pNN`, and all 45 named items are covered either by a real 3D model (`ItemArt.models`, 23 entries) or a hand-built primitive assembly (the `switch` in `ItemArt.Build`) — the `default: fallback=true` cardboard-box path looks unreachable with the current item list. Screenshots (`step6_house_loot_revealed.jpg`, `step6_office_loot_revealed.jpg`) confirm real photographic icons in the loot grid. |
| 4 | Hint rings for explorable objects | **Fixed** | `HintRing` (ItemArt.cs:79) is wired into every `LootContainer`, `GroundLoot`, and `Station` (Interact.cs:59,81,103), fades with distance, intensifies + spawns a floating marker when focused. Confirmed visually in `step6_*_hints.jpg`. |
| 5 | Loot UI worse than web version | **Partial** | `UI.cs` was rewritten to match the web layout (container grid + side backpack panel), and `step6_web_compare.jpg` shows close parity. But the rewrite has a real regression for the top-tier bag — see Serious Bug #1. |

## Serious bugs

**1. Backpack grid overflows the panel for the largest bag (36 slots)**
`UI.cs:104` and `UI.cs:142` hardcode the backpack layout to 5 rows:
```csharp
float BWp = 5 * (BW + BG) - BG + 48, BH = Mathf.Max(H, 5 * (BW + BG) + 170);
...
bagGrid.Place(..., new Vector2(24, -96 - 5 * (BW + BG)), new Vector2(-24, -96));
```
5 cols × 5 rows = 25 cells. But the biggest bag (`b2`, Data.cs:77) has `cap=36`, which needs 8 rows. Rows 6-8 render below the panel's background image and will visually spill out over/through the "total value" footer bar and off the bottom of the panel — this hits any player who's actually progressed to the best bag, i.e. the core loop reward.
*Fix:* compute rows from `cap`, not a literal `5`: `int rows = Mathf.CeilToInt(Mathf.Max(cap, items.Count) / 5f); BH = Mathf.Max(H, rows*(BW+BG)+170);` and use the same `rows` value for `bagGrid.Place`.

**2. Stash modal silently truncates at 48 items with no scroll/indicator**
`Game.cs:322`: `for (int i = 0; i < S.stash.Count && i < 48; i++)`. Max stash capacity is 90 (Data.cs:84, `stash` upgrade level 3). Once a player banks more than 48 items, everything past #48 is invisible and unsellable — no pagination, no "+N more" label, nothing. Not introduced by this step, but it's directly adjacent to the loot-UI rework and worth fixing in the same pass since it's a dead-end for progressed players.
*Fix:* paginate or scroll the stash grid, or at minimum show a count/warning when `S.stash.Count > 48`.

**3. Residual focus failures are geometry-adjacent, not random**
`Game.cs:112-147` is a two-tier system: a SphereCast (radius 0.2, tier 1) then a 34°-cone + closest-point LOS raycast fallback (tier 2, line 136-139). For a small wall/corner-hugging container (e.g. `safe`, 0.6m cube) viewed from the far test stance (`dd=1.5`, `Game.cs:467`) with `yo=±18°`, the lateral offset at ~2.3m easily exceeds the tier-1 sphere radius, pushing it to tier 2 — which should normally still pass the 34° cutoff (18° offset minus the closest-point correction). The most likely culprit is the LOS raycast at line 138-139 false-blocking against adjacent corner furniture (these containers are placed flush against two walls next to a wardrobe/bed, `LevelGen.cs:313-348`), since `eb.Expand(0.3f)` is a fairly tight tolerance for a 0.6m object sitting in a tight corner.
This isn't a crash, and 94% is a real improvement worth crediting — but it's the literal complaint #2 behavior for a reproducible, identified subset of containers, so I'd call it unresolved rather than fixed. *Suggested next step:* widen `eb.Expand` for small/corner containers, or just log whether the actual blocking collider belongs to adjacent room dressing (wardrobe/bed) to confirm the hypothesis before changing geometry/placement.

## Minor nits
- `Interact.cs:38` — `LootContainer.Point` calls `GetComponentInChildren<Collider>()` twice in one ternary. This getter is read every frame for every interactable in the tier-2 cone scan (`Game.cs:131-142`); doubling the tree-walk cost is wasteful at scale. Cache it like `WorldBounds` already does.
- `Art.cs:46` — see complaint #1: consider turning the audit into an automated check (e.g. assert model's largest local-space marking/UV front matches +Z) rather than a human photo review, or at least document the review cadence.
- `UI.cs:89` — `Toast()` uses `DestroyImmediate` on a live gameplay object instead of `Destroy`. Works today because of the defensive null-checks in `FadeOut`, but `DestroyImmediate` during normal play is discouraged by Unity and worth swapping for consistency/safety.
- `Interact.cs:59` — corpse hint ring radius is hardcoded `0.9f` regardless of the killed enemy's actual silhouette size; fine for now but will look off if enemy models vary in size later.

## Fixes applied after review
- #1 Backpack grid now sized from BagCap (6 cols when cap>25, rows computed) — UI.cs.
- #2 Stash >48 shows a toast warning (pagination still TODO).
- #3 LOS tolerance widened 0.3->0.6; result 366/391 (no change, remaining fails are test stances at 1.5m/+-18deg past wall corners).
