## Review: v0.7 → v0.8

**Poly/mesh quality:** The wireframe panels in `v08_compare.jpg` confirm the extra triangle budget is real and well-placed — dog bodies, couch arms, and the shelf unit all pick up visible edge loops/bevels that round off what were hard facets in v0.7. Shaded silhouettes on the sofa arm and shelf crown read noticeably smoother. In the pets/street shots, though, the gain is only visible in the wireframe overlay — at normal camera distance the shaded result is nearly identical to v0.7, which is worth flagging given it's a 4.3x triangle cost.

**Lighting:** The living room shot is the best demonstration of the brief — warm window-light pooling on the floor reads clearly against a cooler fill, and it stays pastel. Bedroom shows a believable soft shadow under the bed and gentle color grading without crushing the palette.

**Issues:**
1. **Specular/bloom artifact in `a_poly_sk_sofa.png`** — a hard white diagonal glare streaks across the wall and floor (top-left), blowing out to pure white. It reads as a reflection/specular bug, not intentional bloom, and it breaks the flat cel-shaded look more than any other single change in this set.
2. **Color grading drift in the same shot** — the wall/floor shift from pastel pink to a warmer tan/amber, which dulls the pastel identity precisely where it should be protected.
3. **Pickup-ring color changed from white to cyan** (`light_bedroom`) — likely a side effect of the cool-fill/rim light pass bleeding into a UI/VFX element rather than an intentional art decision; worth a deliberate call either way.
4. **SSAO isn't legible** in any of these frames — no added contact darkening under the couch, shelf base, or bed that I can distinguish from the v0.7 baked shadowing.
5. **Fog and 4-cascade shadows aren't demonstrated** by the given shots (street scene has no midground depth to show fog falloff, and shadow edges look about the same softness as v0.7) — can't confirm these are working from this set, would want a longer sightline screenshot.
6. **Cost/benefit on poly bump** — in the pets and street pairs, the shaded difference is marginal enough that the 4.3x triangle cost should be justified mostly by the still/interior shots (sofa, shelf), not uniformly across all assets.

**Positives:** outlines and flat base shading are preserved, the storybook silhouette language is intact, warm/cool balance in the living room is genuinely nicer, and the mesh smoothing is real and visible where geometry has actual curvature (couch, shelf, dog heads).

SCORE: 7/10
