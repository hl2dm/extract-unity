## Review: v0.8 vs v0.7

**Poly smoothness — clear win, confirmed in wireframes.** `a_poly_pets_wire` / `a_poly_sk_sofa_wire` show a dramatic increase in triangle density over the b_ versions — the cat/dog heads, ears, and the sofa's cushion squares and shelf trinkets all go from low-facet/chamfered to properly rounded multi-step bevels. In the shaded shots this reads as intended: softer, more storybook-plush silhouettes on the pets and noticeably rounder cushion edges on the sofa, without looking "sculpted" or deviating from the chibi-pastel shape language.

**Lighting — mostly a win, with two specific missteps:**
- Bedroom: nice improvement — soft contact shadow under the bed, gentle warm/cool wall gradient, cooler window glass tone, better corner AO. Pastel pinks read well and intact.
- Street (long view + pets street): subtle AO under canopies/eaves, smoother medallion shapes, atmospheric fog on the distant hill unchanged — good, understated upgrade, no palette drift.
- Living room: the character's fur/hat picks up a nice rim light — good. But the lamp's warm light pool on the left wall, which in v0.7 had real contrast (golden hotspot fading to cooler pink above it), reads much flatter and more uniformly beige in v0.8. That's a regression against the stated "lamp/window light pools" goal — the pool itself has lost definition rather than gained it.
- Sofa shot: understood that the diagonal beam itself is a pre-existing v0.7 window sun shaft, not new. However its intensity/bloom appears pushed up in v0.8 — it now overexposes the back wall and the top of the cream shelf to near-white, flattening the pastel tone of the cabinet in that hotspot. Worth dialing back bloom/shaft intensity (or adding falloff) so it doesn't clip pastel hues on hit surfaces.

**Pastel storybook look:** holds up well overall — bedroom, street, and pets scenes keep saturated, consistent pastel colors. The grading retune does appear to have restored the pinks in most shots (bedroom walls match well between versions). The living room wall flattening and the sofa shelf blowout are the two spots where it slips.

**Issues to fix before ship:**
1. Living-room lamp light pool lost contrast/warmth vs v0.7 — re-strengthen the falloff so the lamp actually reads as a light pool.
2. Sofa-shot window beam bloom is overexposing the shelf/back wall, washing out pastel color there — reduce intensity or add highlight clamping.

SCORE: 7/10
