# #3681 exact-carrier live-BREP rerun result

- Status: `LOCAL_FAIL / SOURCE_FIX_REQUIRED`
- Parent: #72
- Licensed qualification: #3681
- New source defect: #3697
- Prior source fix: #3687 / PR #3692
- Exact tested SHA: `cb10e04954973aedf77a9cfeebbd28a5ccbcbbdb`
- Host: licensed BricsCAD V25.2.10, Windows x64
- Plugin ProductVersion: `0.1.0-preview.10081`
- Plugin SHA-256: `5A1D289A773FE81F08A8481541C2704B437E1754B0D4A1B8C94A23AD1C4DF466`
- Core ProductVersion: `0.1.0-preview.10081`
- Core SHA-256: `1CA4D48BFABDA127E491EF0A639A595059993C3F4C5CCA7A669AE31E0C0A8C19`

## Gates

- exact detached source carrier and pinned platform submodule: PASS;
- StructuralWall residual-cut, rule-matrix, runtime-probe and stale-clear guards: PASS;
- Core deterministic smoke: `ALL PASS`;
- V25 `Release|x64` build against installed V25 references: PASS, zero warnings and zero errors;
- exact candidate `NETLOAD`: PASS;
- OnStartup DemandLoad isolation/restoration: PASS, guarded `2 -> 4 -> 2` with installed Loader and hash unchanged;
- test-owned BricsCAD process cleanup: PASS after every session.

## Sanitized licensed result

The required one-end control used a synthetic StructuralWall with gross vertical/formwork area `2.6688 m2` and one concrete neighbor penetrating its end by `0.05 m`. The same sanitized fixture previously proved a native overlap volume of `0.008 m3`.

Production `QS3DWALLCONTACTPROBE` returned the following identical aggregate result in two separate fresh BricsCAD processes:

| Field | Result |
|---|---:|
| available | `true` |
| target solids | `1` |
| candidate solids | `1` |
| vertical face seeds | `4` |
| positive-volume cuts | `1` |
| contact-probe cuts | `0` |
| failed native cuts | `0` |
| gross area | `2.6688 m2` |
| residual/net area | `2.4288 m2` |
| contact deduction | `0.2400 m2` |

The #3687 handoff requires contact deduction `0.1600 m2` and residual/net formwork `2.5088 m2`. Actual deduction is high by `0.0800 m2`, exactly two penetration-side strips of `0.05 m x 0.8 m`. Sanitized classification: `PENETRATION_SIDE_STRIP_OVERDEDUCTION`.

A touching-only production-generated neighbor was also not qualified on the exact same binary: the production probe returned `available=false`, one failed native cut and zero deduction.

The first required full-contact control therefore remains a blocking source failure. The two-end BLT control, partial/union matrix, stale/missing lifecycle, capture refresh, save/cold-reopen, Undo/Redo and second-DWG cells were not continued or relabeled after this blocker.

## Handoff

Issue #3697 owns the source correction. The local lane did not patch production source. A source agent must preserve V25 planar-wrapper support and fail-closed native behavior while measuring only the intended original contacted face rather than penetration-created side strips, then publish a new exact SHA for #3681.

Raw Handles, paths, disposable DWGs, sidecars, scripts and probe files remain private/Git-ignored. No customer or proprietary fixture is committed.
