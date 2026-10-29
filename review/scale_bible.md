# Extract Lite – Scale Bible (v0.7)

Reference unit **K = 1.30 m** = the player kid (CharacterController height). Everything is authored in metres and audited in K by `ScaleAudit.cs` (`[scale]` log lines; part of the full autotest loop, `-scaleonly` for a quick pass). The world is a *toy storybook*: shapes are chunky and rounded, but sizes follow real child-vs-adult proportions with at most ~+15 % "toy puff" so the kid still feels small in a grown-up world.

## Player / camera
| item | value | K | why |
|---|---|---|---|
| kid capsule | 1.30 m (r 0.28) | 1.00 | reference |
| eye height | 1.15 m | 0.88 | eyes ≈ 88 % of standing height (was 1.25 = adult-ish 0.96 K) |
| FOV (vertical) | 70° (+5 sprint) | – | 75° stretched rooms; 64–74 allowed |
| walk / run | 3.5 / 5.6 m/s | 2.7 / 4.3 K/s | game-brisk but scaled to kid (was 4.1/6.4 = 3.2 K/s) |
| ride scooter cam | 1.6 m | 1.23 | seated on toy scooter (was 2.0) |

## Architecture
| item | target | K |
|---|---|---|
| interior ceiling | 3.0 m (2.7–3.1) | 2.3 |
| interior door opening | 2.3–2.4 m (2.0–2.45) | ~1.8 |
| shop / house façade door | 2.2–2.35 m (≤2.45 incl. arch frame) | ~1.8 |
| window sill / head | 1.0 / 2.4 m | 0.77 / 1.85 |
| townhouse storey | ground 3.2 m, upper 2.5 m | 2.5 / 1.9 |
| road | 8 m (2 lanes) | 6.2 |
| sidewalk | 2 m main strip; up to 5 m at plazas (audit logs first found) | 1.5–3.8 |

## Furniture (height to top, min–max allowed)
sofa 0.75–1.00 (seat ≈0.45) · dining/coffee table 0.42–0.80 · stool 0.38–0.55 · bed incl. headboard 0.45–1.30 · counter 0.85–1.40 incl. tap (worktop 0.97) · bathtub 0.45–0.72 · toilet 0.55–0.90 · TV on stand 0.9–1.6 · floor lamp 1.3–1.8 · fridge / drink cooler 1.6–2.0 · wardrobe 1.7–2.25 · drawer chest 0.6–1.1 · crate 0.3–0.9 · store shelf 1.6–2.6 · display case 1.4–2.1 · safe 0.4–0.9 · till 0.2–0.7 (lid opens to 0.66 after looting).

## Street & vehicles
street lamp 3.2–4.6 · tree 3–7 · bench 0.7–1.1 (incl. back) · bin 0.7–1.1 · mailbox 1.0–1.5 · parked scooter 1.0–1.4 (mirror top) · ride scooter 1.2–1.6 · slide 1.6–2.6 · bus 2.8–3.8 · townhouse 6–16 · 3D sign letters 0.3–1.4 · ground loot 0.05–0.7.

## Pets (toy exaggeration, consistent)
cat 0.35–0.75 (incl. ears/tail ≈ 0.55 K) · dog 0.45–0.95 (≈0.7 K). Pets are ~1.5× real size on purpose so they read in first person; both use the same factor.

## Rules
* New props must be added to `ScaleAudit.Rules`; the autotest fails on any OUT.
* Never size things relative to the camera; size to K, then the camera follows the bible.
