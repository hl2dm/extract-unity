# Casual MVP report (2026-10-28)
Win + WebGL builds succeed. The autotest gives "[casual] full loop ok", errors 0, failed checks 0, and saves 35 casual_*.png files.
Fixed after the Claude review: pets no longer act while input is locked, the mom-fail loot count no longer counts kept items, the quest theme index is bounds-checked, and pet-name modulo can no longer go negative.
Fixed after looking at the screenshots: the pet FBX rendered about 57 m tall (bone scale 100 inside the FBX). It is now fitted from renderer bounds after 2 frames, rotated 180°, and tinted.
Weak spots: pet facing is unverified, extraction payouts are low (8–17 元 on some runs), the bus cinematic is basic, and the source/licence of the pet FBX needs checking.
