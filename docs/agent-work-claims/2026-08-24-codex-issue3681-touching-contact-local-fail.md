# #3681 post-#3702 touching-contact licensed result

- Status: `LOCAL_FAIL / SOURCE_FIX_REQUIRED`
- Parent: #72
- Licensed qualification: #3681
- New source defect: #3711
- Penetration side-strip source fix: #3697 / PR #3702
- Exact tested SHA: `43257555256057896e395a2303d32dbddd3f3567`
- Required fix merge contained: `c3485e4adece4d3cf267c11a59a305e56aa0ebfa`
- Host: licensed BricsCAD V25.2.10, Windows x64
- Plugin/Core ProductVersion: `0.1.0-preview.10081`
- Plugin SHA-256: `0BC747E4DBBC43252AD54D0945292530B3123E2285168C12FE8539E3D7CA5CF9`
- Core SHA-256: `C84B81DC477EB6CDFC2DB850BBEA1CD1AE26BE8D5A8970CCD7AA0A876A3001C0`

## Gates

- exact clean source descendant containing PR #3702: PASS;
- exact plugin/Core ProductVersion and portable-PDB SourceLink binding: PASS;
- StructuralWall penetration-side-strip, residual-cut, rule-matrix, runtime-probe and stale-clear preflights: PASS;
- Core deterministic smoke: `ALL PASS`;
- V25 `Release|x64` build against the installed licensed V25 references: PASS, zero warnings and zero errors;
- exact-candidate licensed `NETLOAD`: PASS;
- OnStartup DemandLoad isolation/restoration: PASS, guarded `2 -> 4 -> 2` with installed Loader path/hash unchanged;
- test-owned BricsCAD process cleanup: PASS after each session.

## Sanitized licensed result

The touching-only control used one production-generated StructuralWall with gross vertical/formwork area `2.6688 m2` and one production-generated concrete Column touching the complete `0.2 m x 0.8 m` end face without positive-volume penetration.

Production `QS3DWALLCONTACTPROBE` returned:

| Field | Result |
|---|---:|
| available | `false` |
| target solids | `1` |
| candidate solids | `1` |
| vertical face seeds | `4` |
| positive-volume cuts | `0` |
| contact-probe cuts | `0` |
| failed native cuts | `1` |
| gross area | `2.6688000000000054 m2` |
| residual/net area | `2.6688000000000054 m2` |
| contact deduction | `0 m2` |

The required full end-face result is available `true`, deduction `0.1600 m2` and residual/net `2.5088 m2` with no native failure.

The same exact binary passed the independent `0.05 m` penetration control:

| Field | Result |
|---|---:|
| available | `true` |
| positive-volume cuts | `1` |
| failed native cuts | `0` |
| gross area | `2.6688000000000054 m2` |
| residual/net area | `2.5088000000000146 m2` |
| contact deduction | `0.15999999999999093 m2` |

This proves PR #3702 corrected the penetration-side-strip over-deduction while leaving an independent failure in the zero-volume touching/contact-probe path. The full-face touching cell is mandatory, so partial contact, overlapping-neighbor union, two-end BLT, stale/capture refresh, save/cold-reopen, Undo/Redo and second-DWG cells were stopped and not promoted.

## Handoff

Issue #3711 owns the new source correction. The local lane did not patch production source. A source agent must make the valid touching-only offset/intersection/face-read/subtract path succeed while preserving the exact `0.1600 / 2.5088` penetration control, ExternalBoundedSurface unwrapping, union behavior and fail-closed handling for genuinely ambiguous or missing BREP. A new pushed exact SHA is required before #3681 reruns.

Both synthetic disposable DWG/QSDB pairs remained byte-identical. Raw Handles, ProjectIds, paths, drawings, sidecars, scripts and runtime markers remain local/Git-ignored. No proprietary binary or customer fixture is committed.
