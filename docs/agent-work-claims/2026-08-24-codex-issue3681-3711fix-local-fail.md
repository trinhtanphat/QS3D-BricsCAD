# #3681 post-#3716 touching-contact licensed rerun

- Status: `LOCAL_FAIL / SOURCE_FIX_INCOMPLETE`
- Parent: #72
- Licensed qualification: #3681
- Reopened source defect: #3711
- Source fix under test: PR #3716
- Exact tested SHA: `a4ec7cdc84cc63cb35d1162b1e469638ed796ddf`
- Evidence branch: `agent/codex/issue3681-3711fix-v25-local-qualification`
- Host: licensed BricsCAD V25.2.10, Windows x64
- Plugin/Core ProductVersion: `0.1.0-preview.10081`
- Plugin SHA-256: `4CA94232400CBF1D43DCA3519A20B202975AF1DEDCBDDE598A72C3181C3AE8F7`
- Core SHA-256: `9744F4A38B20C0D6CB33AF8922F97FC283D00D990F5F0E73E990760AD9DD3E40`
- Date: 2026-08-24

## Exact-source gates

- clean worktree at the exact merged PR #3716 carrier and pinned platform submodule: PASS;
- StructuralWall penetration-side-strip, residual-cut, rule-matrix, runtime-probe, stale-clear and touching-probe-fallback guards: `6/6 PASS`;
- Core `Release` build: `0 warnings / 0 errors`;
- Core deterministic smoke: `ALL PASS`;
- V25 `Release|x64` build against installed licensed V25 references: `0 warnings / 0 errors`;
- exact worktree DLL `NETLOAD` in fresh licensed host processes: PASS;
- runtime host/native identity, Ribbon, Workspace palette and Right palette checks: PASS;
- DemandLoad isolation/restoration: guarded `2 -> 4 -> 2` with installed Loader identity/hash preserved;
- test-owned BricsCAD process and fixture cleanup: PASS.

## Touching-only failure reproduced twice

The mandatory control used one production-generated StructuralWall with gross vertical/formwork area `2.6688 m2` and one production-generated concrete Column touching the complete `0.2 m x 0.8 m` end face without positive-volume penetration.

Two separate fresh BricsCAD processes returned the identical production `QS3DWALLCONTACTPROBE` result:

| Field | Actual | Required |
| --- | ---: | ---: |
| available | `false` | `true` |
| target solids | `1` | `1` |
| candidate solids | `1` | `1` |
| vertical face seeds | `4` | `4` |
| positive-volume cuts | `0` | `0` |
| contact-probe cuts | `0` | `>= 1` |
| failed native cuts | `1` | `0` |
| gross area | `2.6688000000000054 m2` | `2.6688 m2` |
| residual/net area | `2.6688000000000054 m2` | `2.5088 m2` |
| contact deduction | `0 m2` | `0.1600 m2` |

PR #3716 defers a preliminary zero-volume direct-intersection failure to the positive-offset touching probe, but the licensed chain still does not resolve this fixture. A Git-ignored transient-clone stage probe then invoked the same production private stages with exact plugin/Core PDB SourceLink checks:

- the preliminary direct intersection did not throw or report failure; it returned a native result with volume 0;
- the production probe distance was `1e-6` CAD units, and `OffsetBody(1e-6)` failed before contact intersection, original-face read or subtraction could run;
- at `1e-5` CAD units (10x), native offset, intersection, original-face read and subtraction all succeeded; contact volume was approximately `1.6000000005e-6` CAD3 and eligible original-face area was `0.15999999999999093 m2`;
- 100x, 1000x and 10000x diagnostic offsets also completed the same chain and retained the same eligible original-face area.

This isolates a V25 native modeler-distance floor rather than the previously hypothesized preliminary BoolIntersect exception for this fixture. Source issue #3711 was reopened for a unit-aware/modeler-stable native probe distance that preserves the tighter original-plane identity test, partial-contact/union/top-bottom rules, the passing penetration control and fail-closed behavior for genuinely unresolved native stages. The private diagnostic probe and raw stage markers remain Git-ignored.

## Penetration regression remains correct

The same exact binary passed the independent `0.05 m` penetration control:

| Field | Result |
| --- | ---: |
| available | `true` |
| positive-volume cuts | `1` |
| contact-probe cuts | `0` |
| failed native cuts | `0` |
| gross area | `2.6688000000000054 m2` |
| residual/net area | `2.5088000000000146 m2` |
| contact deduction | `0.15999999999999093 m2` |

This preserves the #3697 penetration-side-strip correction and isolates the still-failing path to zero-volume touching contact.

## Fail-fast boundary and cleanup

Because the full-face touching cell is mandatory, the two-end BLT `0.3200 / 2.3488 m2`, partial contact, overlapping-neighbor union, top/bottom exclusion, semantic-capture refresh, stale/missing BREP, save/cold-reopen, Undo/Redo and second-DWG cells were not run or promoted. #3681 remains open and is not `LOCAL_PASS`.

Both disposable synthetic DWG/QSDB pairs remained byte-identical. DemandLoad returned to its original value, the installed loader was unchanged and zero BricsCAD processes remained after every session. A first shell wrapper assertion misread a null process exit variable after the host had already produced and cleaned up a valid failure marker; the independent second run reproduced the byte-identical product diagnostic with a clean wrapper exit. Raw paths, Handles, ProjectIds, drawings, sidecars, scripts and runtime markers remain Git-ignored. No proprietary binary, customer fixture, workflow dispatch or production source/harness change is included.
