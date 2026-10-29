## v0.8 Review — Poly smoothness + lighting pass

**Geometry (confirmed via wireframe pairs):**
- `sk_sofa`: Clear win. b_wire shows the armrest as a faceted/chamfered cylinder; a_wire shows proper rounded subdivision carrying through to the shaded render — the armrest and seat-cushion edges read as smooth curves instead of flat-shaded facets. This is exactly the "multi-step bevel" improvement promised, and it doesn't fight the pastel look at all.
- `pets` (dogs): Wireframe density increase is dramatic (ears/head/body are now densely tessellated spheres vs. b's coarser facets), but the **shaded result is nearly indistinguishable** from v0.7 — the low-poly dogs were already stylized/rounded enough that the silhouette gain is barely perceptible. This reads as triangle budget spent where it's least visible.
- `RideScooter`: No wireframe pair was provided for this asset, and the two shaded stills are near-pixel-identical. I can't confirm the tri budget actually landed here — worth adding a wireframe capture to verify.

**Lighting:**
- Soft shadows read correctly — bed/sofa contact shadows in `a_light_bedroom`/`a_light_living` are gently feathered vs. slightly harder edges in b, consistent with the 4-cascade change.
- Color grading has visibly shifted toward pink: `a_light_living`'s ceiling reads as a more saturated magenta-pink vs. b's muted mauve. Yellow lamp and green plant still read as distinct hues, so the storybook palette variety survives, but this is the strongest visible change in the set and is worth a gut-check against the broader palette so the world doesn't trend monochrome-pink.
- I could **not visually confirm** the specific fixes called out in the brief (bloom threshold/exposure fix on the window sun shaft, strengthened lamp/window light pools, rim light). None of the supplied stills show a grazing sun angle or a lit lamp at night, so there's nothing in this set that was clipping before or shows a pool now — the fix may be correct but is unverifiable from these screenshots. SSAO contact darkening in corners (wall/floor, shelf base) is also quite subtle in both versions — if it's meant to be a visible feature add, it could stand to read a bit stronger.
- `b_light_street` vs `a_light_street` are effectively identical — fine, since it's an outdoor midday shot where most of the new lighting features wouldn't apply, but it doesn't add evidence either way.

**Net:** Geometry quality is a real, confirmed improvement on hero furniture (sofa) with no loss of the pastel/storybook silhouette; grading shift is tasteful but trending pinker and should be watched. The lighting feature claims (shaft fix, lamp pools, rim light) aren't demonstrated by the current screenshot set — recommend capturing a dusk/lamp-lit interior and a direct window sun-angle shot next pass to actually verify those fixes landed.

**Issues to track:**
1. No wireframe evidence for `RideScooter` — can't confirm poly budget applied.
2. Triangle increase on `pets` is wireframe-only visible, not shaded-visible — reconsider where budget is allocated.
3. Lamp/window light pools, rim light, and the sun-shaft bloom/exposure fix are unverifiable from this still set — need dedicated capture angles.
4. Pink grading shift is tasteful here but worth a palette-wide check so it doesn't drift away from multi-pastel storybook variety.

SCORE: 7/10
