I read all 5 changed files (`LevelGen.cs`, `Data.cs`, `Heli.cs`, `Art.cs`, `Game.cs`) in full and the six contact sheets. I specifically chased the highest-risk new code paths — the mart/office open-floor room generator, the locked-room/key placement, the new enemy-archetype roll and its waypoint-pool indexing, and the Heli FBX swap — tracing each for div/mod-by-zero, unreachable keys, and softlocks.

**Serious bugs: none found.**

Checked and ruled out explicitly:
- `LevelGen.cs:106` `pool[(i*2+j) % pool.Count]` looked like a mod-by-zero risk when `openSpots` is empty, but it's guarded by `pool.Count<2` on the branch above, and mart/office always populate exactly 10/12 open cells (fixed `cols*rows` constants), so `pool.Count` can never be 0 or 1 in practice.
- Locked-room key reachability: for every style (house/mart/office) exactly one room is flagged `locked`, its door is always carved into the shared open-floor wall (`LevelGen.cs:155-156`), and every container inside that room is flagged `special=true` so the key generator (`LevelGen.cs:65-66`) can never place the only key behind its own door.
- `CanBoard`/`BoardWindow` (`Game.cs:45`) is a hard safety valve — even if an enemy spawned somewhere unkillable, the raid is never unwinnable since extraction opens unconditionally in the last 60s.
- Quest theme check `level.theme == Data.Themes[q.theme]` (`Game.cs:201`) is reference equality on a shared static array element — safe since `Deploy` passes the same `Data.Themes[idx]` reference through unmodified.
- Heli FBX swap: confirmed via `strings heli.fbx` that `rotor-main`/`rotor-tail` node names actually exist, matching `Heli.cs:52`'s lookup, so rotor spin won't silently break.
- No perf cliffs: new mart/office furnishing adds on the order of tens of extra `Art.Spawn` calls per raid (bounded by fixed `cols×rows`), not an unbounded or per-frame cost.

Minor polish notes:

1. **`LevelGen.cs:322-325`** — the new `register` CKind (`Data.cs:42`, `model=null`) falls through the same procedural-fallback branch as `safe`, so every mart checkout counter renders as a tiny wall-safe with a combination dial + green LED instead of a cash register. Fix: branch the fallback mesh on `kind=="register"` (flat box + small screen/keypad decal) instead of reusing the safe shape.
2. **`Heli.cs:54`** — `var lmin = root.transform.InverseTransformPoint(bb.min);` is computed and never read (the actual vertical placement on line 55 uses `bb.min.y` directly). Dead line, safe to delete.
3. Mart's roof sign/color band and office's HVAC units (`LevelGen.cs:425-436`) read as very small/easy to miss from normal street-approach angles in the g1 contact sheet — worth enlarging or brightening if the goal is a recognizable storefront silhouette.
4. `Heli.cs:52-53` silently leaves `h.rotor`/`h.tail` null if a future model reimport renames `rotor-main`/`rotor-tail` — a `Debug.LogWarning` when either isn't found would catch that regression immediately instead of just producing a static rotor.
5. Mart's bonus satellite lot (`LevelGen.cs:56`, pastel pitched-roof "house") sits right next to the flat-roofed big-box store — a deliberate reuse for low risk, but thematically it reads oddly (suburban house next to a strip mall) in the aerial shots.
