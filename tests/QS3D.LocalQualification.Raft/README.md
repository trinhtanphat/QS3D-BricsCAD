# LOCAL-021 native continuation probe

This is a **licensed BricsCAD V25 AutoLISP test**, not production plugin code,
an unattended host launcher, a fixture generator, or a full LOCAL-021 qualifier.
The exact probe bytes were executed against the installed official
`v0.2.0-preview.1` package at source
`d8adfc0e2661e1f1210b81d7d98ebbc62e722333`.

Probe SHA-256:
`6CEEAF2278B6621AE60A5C8D3D04E0A4565B4007E430639D36D6F07BEF053DE3`.

## Preconditions

Use the canonical #4041 / LOCAL-021 host allocation and
`docs/LOCAL-V25-QUALIFICATION.md`. Never use customer drawings. Freeze the
product, probe, runner, input and host identities before launch; preserve the
installed registration, original profile and tunnel preferences. Require no
pre-existing BricsCAD process and a fresh result directory.

The `rebuild` phase expects an already authored, disposable World-UCS,
millimetre drawing with one native 4,000 x 6,000 x 900 mm solid and two source
polylines. Its matching QSDB must already contain the accepted 800 mm semantic
thickness edit. The probe does not manufacture that edit or create the fixture.

Set these process environment variables **before** launching the test host:

- `QS3D_LOCAL021_RESULT`: new phase-specific JSON output path.
- `QS3D_LOCAL021_RUN_ID`: unique allocation identifier.

After loading the exact product and its matching saved project, load this file
through the native CAD `load` function. In the licensed CAD command line:

```lisp
(qs021-write-result "rebuild")
```

This asserts the initial 21.6 m3 geometry/bounds, selects its actual native
solid, calls production `QS3DBUILD3D`, verifies 19.2 m3 and 0-800 mm bounds,
checks that the old output is no longer live, verifies two source polylines,
then calls production `QS3DSAVE` and native `QSAVE`. A failure anywhere stops
that sequence; result generation catches it and writes FAIL. The result is
never based only on the final volume while ignoring an earlier assertion.

After verified graceful exit, launch a **different process** on that exact
saved DWG/QSDB, with a new phase output path and properly loaded saved project:

```lisp
(qs021-write-result "cold")
```

This checks one native solid, two source polylines, 19.2 m3 and matching
0-800 mm bounds. It does not rebuild or save the drawing to obtain that result.

## External runner checks are mandatory

The caller must independently validate the actual loaded assembly/host,
matching phase/run ID, native host exit, saved QSDB cardinality and thickness/
height/elevation values, unchanged cold-open DWG bytes, and cleanup. A missing
or malformed marker, timeout, wrong process/product, mismatched input or cleanup
failure is not PASS. Dispose the exited process handle and verify zero remaining
hosts before restoring the nonce profile or starting another phase.

Do not overwrite a consumed allocation or reuse its marker. Do not use this
probe's narrow PASS to certify Add/schema, top-level placement, Quantity Insight,
formwork arithmetic, highlight, export, whole-project read-only behavior,
installer lifecycle, MCP, V26, or the complete LOCAL-021 matrix.

See `docs/validation/issue-4041-local021-native.md` for observed results and
remaining failures. Machine-specific PowerShell wrappers and raw fixtures remain
local; they are not falsely represented as having been published with this probe.
