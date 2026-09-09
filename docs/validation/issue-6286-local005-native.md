# LOCAL-005 native multi-region qualification (#6286)

Status: **PENDING_NATIVE** until a committed runner is executed on an interactive licensed BricsCAD V25 host against the exact allocated product source.

## Bounded row in this carrier

This carrier qualifies only the first LOCAL-005 row:

- one synthetic Slab semantic owner;
- two disjoint straight rectangular outer loops;
- one straight rectangular hole inside the first outer loop;
- public production command `QS3DSLABREBAR3DMULTI`;
- supplemental public health command `QS3DMULTIREBARHEALTH`;
- generated aggregate/manifests/topology fingerprint;
- native `QS3D_REBAR` and `QS3D_REBAR_REGION` ownership;
- geometric exclusion of the hole interior;
- `QS3DSAVE` + native `QSAVE` + cold-process reopen;
- exact disposable drawing/sidecar/profile cleanup.

The probe is test-only and must not invoke `SlabFoundationMultiRegionMeshSolidBuilder` directly. It may seed source loops/project state and inspect product state, while mutation remains command-level.

## Harness

- `tests/QS3D.LocalQualification.MultiRegion/Run-Local005NativeMultiRegion.ps1`
- `tests/QS3D.LocalQualification.MultiRegion/Local005NativeMultiRegionProbeCommands.cs`
- `tests/QS3D.LocalQualification.MultiRegion/QS3D.LocalQualification.MultiRegion.csproj`
- `tests/QS3D.LocalQualification.MultiRegion/test-runner-contract.ps1`

The runner fails closed unless the repository is clean and committed, the supplied product payload reports the exact allocated `gitCommit`, BricsCAD V25 is licensed/interactive, no BricsCAD or tunnel process is already active, and a nonce profile sandbox can be restored. Public receipts contain no machine paths.

## Required evidence before PASS

A `LOCAL_PASS_BOUNDED` receipt must show all four verified phases (`setup`, `run`, `saved`, `reopen`), real production command execution, geometry + ownership success, save/reopen continuity, zero remaining BricsCAD processes, and private allocation cleanup.

## Deliberately not qualified by this first row

These stay **PENDING_LOCAL** and must not inherit PASS from the straight-row result:

- Slab bulged source loops;
- add/remove region regeneration;
- corrupt/missing ownership fail-closed replacement;
- native bar-cap fail-closed behavior;
- Foundation straight multi-region + hole command;
- Foundation bulge/regeneration/corruption/cap rows.

## Evidence log

No licensed-host receipt has been admitted yet. After the committed harness is executed, record only sanitized receipt identity/status and exact product/harness SHAs here; do not commit private diagnostics, absolute paths, profile names, or machine-specific state.
