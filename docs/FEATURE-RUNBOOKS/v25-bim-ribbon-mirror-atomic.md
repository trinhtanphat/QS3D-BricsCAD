# V25/V26 BIM Ribbon mirror fail-closed qualification

## Scope

Issue #5818 hardens only `BltBimRibbonMirrorAugmenter`. It does not replace the Ribbon materializer, command handlers, palette authorities, or native BricsCAD Ribbon ownership.

Hosted guards/builds are `REMOTE_SAFE` source evidence only. Licensed BricsCAD Ribbon behavior remains `LOCAL_ONLY`.

## Deterministic source contract

On the exact candidate SHA:

1. `scripts/preflight-v25-bim-ribbon-mirror-atomic.py` must pass.
2. All three requested DRAW/TOOLS/IFC mirrors are constructed off the live BIM panel collection before QS3D-owned BIM panels are replaced.
3. Unsupported QS3D Ribbon item types fail the requested mirror instead of being silently omitted.
4. If publication fails after replacement begins, the initializer removes QS3D-owned partial BIM mirrors and returns false with `_initialized` false.
5. Command parameters, IDs, image rasterization, source lookup and native/third-party panel boundaries remain unchanged.

## LOCAL_ONLY matrix

Freeze exact candidate SHA, adapter/Core hashes and licensed host version before execution. Use a disposable profile and restore UI/profile state afterwards.

Run independently on licensed BricsCAD V25 and V26 where available:

- initialize VẼ then MÔ HÌNH BIM and confirm exactly one complete DRAW/TOOLS/IFC mirror set;
- reset/reconstruct the host Ribbon and initialize again, proving no duplicate QS3D-owned BIM panels;
- with an approved diagnostic/failure-injection hook, introduce one unsupported QS3D-owned source item and prove initialization fails without publishing a reduced BIM mirror;
- inject a one-shot failure on the second/third live BIM panel Add and prove no partial QS3D-owned BIM mirror remains after the failed attempt;
- remove the injected failure and prove a later retry converges to the full mirror set;
- verify native/third-party BIM panels remain untouched throughout;
- verify IFC icons/commands and DRAW/TOOLS command parameters remain unchanged;
- record exact-SHA PASS/FAIL/NO_RESULT and cleanup evidence.

If deterministic host failure injection is unavailable, record those rows `NO_RESULT`; source inspection or hosted CI must not be promoted to runtime PASS.
