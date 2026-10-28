# v0.4 ([pt184u])
- UI contrast: `UIContrast` scans every UI Text each 0.2 s, composites the panels behind it, and fixes base colour + `<color>` tags to >= 4.5:1 (hue kept). Autotest: checked 1032, fixed 417, fails 0.
- Street overlap: `LevelGen.ClearStreet` keeps trees/lamps/pots/bins/scooters/hydrants/mailbox clear of TownHouse/Shop/House (+0.25 m) and each other; trees/pots moved to the curb. Autotest: 46 props, 0 remaining.
- Slide: `[E] 爬上溜滑梯` -> climb, whoosh down the chute (FOV kick, roll), land; chasing pets lose track. Autotest PASS.

## v0.4.1
- Trees slide along x to party walls/gaps until no crown crosses any sign view ray (centreline eyes at sign x-3/0/+3 to sign centre+edges); fallback 0.6x smaller crown. Autotest: signBlocked 0.
- Slide ride: seated eye + steeper look so the chute is in frame.
