# #3751 licensed V25/V26 SaveAs lifecycle qualification

- Date: 2026-08-25 (UTC+7)
- Parent: #72
- Source-preparation issue: #3751
- Result: `LOCAL_PASS / BOUNDED_SAVEAS_LIFECYCLE`
- Exact tested source: `119124ee0112be206e3008354503e7563c996e1a`
- Publication baseline: `main@7fec6f36a7c1181d7113f0e7220ea3dafca66e29`
- Runner: `scripts/test-bricscad-saveas-lifecycle.ps1` (unchanged)
- Fixture: repository-generated `samples/generated/QS3D-Sample.dwg`

## Source and build identity

The exact tested checkout initialized the repository-pinned `QS3D-Platform`
submodule at `a5778f4abcf3b5c308c5d6854040dbc0c3082390`. The focused SaveAs
source/runner preflight passed. Both matching adapters then built in
`Release|x64` with .NET SDK `8.0.424` / MSBuild `17.11.48`, zero warnings and
zero errors.

| Host | BricsCAD | Adapter ProductVersion | Adapter SHA-256 |
| --- | --- | --- | --- |
| V25 | `25.2.10` | `0.1.0-preview.10081` | `79DF04E40C6C8E7E62F5EFF1EA672B828DB9B44455B5789BFF08EE07EDF82174` |
| V26 | `26.2.07` | `0.1.0-preview.10081` | `F2A067AEA5D5A46AE2C13DC2D668B57F736F9514FDEC15AEA8BF9C3BE62B30BA` |

The runner's exact-source check accepted both adapter/PDB pairs against the
tested Git SHA before either host was launched.

## Licensed runtime result

The unmodified guarded runner passed independently in licensed interactive
BricsCAD V25 and V26. Each host reported all required lifecycle assertions:

- `nativeSaveAsPathTransition=true`
- `canonicalProjectIdentityPreserved=true`
- `targetSidecarPersisted=true`
- `originalSidecarUnchanged=true`
- `pendingStateCleared=true`
- `coldCacheReloadMatched=true`

The repository fixture SHA-256 remained
`CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`
before and after both runs. The V25 installed DemandLoad registration was
isolated from the exact NETLOAD candidate for the run and restored from
`LoadCtrls=2` after completion. Its installed loader remained at the same path
and SHA-256
`0D89D8D828BCE5CFC966EC2EF54358DC50E4FED560D5A908F94643AFA1D74E30`.
Both runner-owned hosts were removed and the final matching BricsCAD process
count was zero.

Raw scripts, disposable DWGs, sidecars, markers and machine paths remain under
gitignored `artifacts/`; none are committed. No production source, runner,
probe, tolerance, package, workflow or installed plugin was modified.

## Scope boundary

This closes only the bounded synthetic-fixture native SaveAs path/sidecar and
in-process cold-cache reload cell prepared by #3751. It does not claim a
fresh-process close/cold-reopen matrix, private/customer DWG coverage,
installer/signing/update qualification, full LOCAL-001 completion, full V26
release qualification, or completion of parent #72/#1462.

The tested SHA is an ancestor of the publication baseline. The only intervening
`main` change at publication was the #3681 sanitized evidence claim; no SaveAs,
adapter, Core, runner or fixture source changed.
