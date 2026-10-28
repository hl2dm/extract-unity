OVERALL: 6/10
TOUCH_CONTROLS: 6/10 - Joystick (bottom-left) is a solid thumb target, but the right-side cluster (換零食/互動/丟/跳/跑) packs five buttons of inconsistent size into a tight arc with small gaps — 丟 sits wedged between 互動 and 跳, inviting mis-taps during fast play. In scooter-ride mode, 互動 also crowds directly against the "雜仔店" shop banner, blurring button from world-UI.
RESPONSIVE_LAYOUT: 7/10 - Desktop correctly drops all touch chrome; tablet/phone-landscape share one touch layout that scales acceptably; phone portrait smartly shows a rotate prompt instead of cramming controls. But phone landscape 844×390 leaves almost no margin — timer sits near the top edge, mood bar flush with the bottom edge. A decorative barrel prop also clips into the 罐頭/肉肉 counter corner across nearly every resolution, suggesting a z-order/placement bug rather than a true layout issue.
TEXT_CONTRAST: 5/10 - Primary HUD text (white/orange with dark outline) reads fine against the sky. The recurring problem: floating decorative props (pastel cubes, a diamond icon) repeatedly render in front of the timer when near buildings — "3:47", "3:42", "3:39" are all partially blocked in the sign close-ups. The swipe hint "這半邊滑動看看" is low-contrast gray-on-pastel and easy to miss entirely.
SIGNS: 9/10 - Best element here: 阿姨家/豆花/冰店/早餐店 all use bold, heavily outlined dark text on cream boards with strong color separation from the pastel backdrop — legible even at a glance. Only ding: the shop-name banner can visually compete with nearby HUD text (see ride-mode 互動 button overlap above).
ART_CONSISTENCY: 8/10 - Cohesive rounded, toy-like pastel style holds up across desktop/tablet/phone and in close-up. Minor friction: saturated HUD button colors (yellow/pink/green/blue) sit slightly outside the muted palette used everywhere else, reading as a UI layer bolted onto the world rather than part of it.

TOP FIXES:
1. Fix the camera/z-order bug letting foreground building decorations (floating cubes, diamond icon) render over the countdown timer — happens in nearly every close-range shot and is the single biggest readability failure.
2. Reposition or shrink the barrel/canister prop that clips into the 罐頭×/肉肉× counters in the bottom-right corner — present across almost all resolutions, looks unintentional.
3. Space out and standardize hit-target sizes in the right-side action cluster; separate 丟 from 互動/跳 so fast thumb taps don't misfire.
4. Add safe-area padding for phone landscape (844×390) — pull timer and mood bar in from the very top/bottom edges.
5. Increase contrast/size on the swipe-hint text ("這半邊滑動看看"); it's currently near-invisible against the pastel background.
6. In scooter-ride mode, give the 互動 button clear separation from the shop signage banner so action UI and world signage don't visually merge.
lapse or hide the empty 背包 pane in the loot screen until it has items, reclaiming wasted space on phone landscape.
