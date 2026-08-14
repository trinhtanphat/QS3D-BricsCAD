# Work claim — BBS CSV resource bound

- Status: `ACTIVE`
- Agent: `gpt56sol-bbs-csv-resource-bound-20260814-1244`
- Registered: `2026-08-14T12:44:00+07:00`
- Workstream: `REB / export-resource integrity`
- Priority: `P1`
- Baseline: `e6230682ee8ef0dea0abe44ffd35a5a0cfec9087`

## Confirmed defect

`RebarCsvExporter.ToCsv(IEnumerable<RebarScheduleRow>)` accepts arbitrary/lazy public input and appends every row to one `StringBuilder` with no row ceiling. This leaves the BBS CSV public boundary open to unbounded enumeration/memory growth. The same Core export subsystem already fail-closes an arbitrary/lazy rebar-procurement CSV at a finite public row bound, and the BBS XLSX exporter likewise has an explicit worksheet row ceiling; BBS CSV is the remaining unbounded projection.

## Reserved scope

- `src/QS3D.Core/Export/RebarCsvExporter.cs`
- `tests/QS3D.Core.SmokeTests/BbsRegressionSmoke.cs`
- `docs/agent-work-claims/2026-08-14-1244-gpt56sol-bbs-csv-resource-bound.md`

## Intended change

Add a finite BBS CSV row ceiling at the public renderer boundary, rejecting the next row before serialising it and preserving existing schema, ordering, numeric validation/formatting, CSV formula hardening and atomic file replacement. Prefer an existing BBS/export limit if source contract supplies one; otherwise use the already-established 10,000-row Core rebar CSV resource ceiling rather than inventing a second arbitrary policy.

## Regression plan

Extend the focused BBS smoke to prove the configured maximum is accepted, the next row throws `ArgumentOutOfRangeException`, and lazy enumeration is stopped at a bounded number of `MoveNext` calls. Keep the fixture domain-valid and do not mutate an existing destination on preflight failure.

## Excluded scope

- no rebar quantity/fabrication math or schedule semantics;
- no procurement CSV/report/optimizer changes;
- no BBS XLSX behavior changes;
- no V25/V26/native builders or Level-placement claim files;
- no MAP/IFC/persistence changes;
- no GitHub Actions or licensed BricsCAD qualification.

## Validation boundary

This environment has GitHub connector read/write but no local `gh`, no DNS checkout, and therefore no executable .NET/native runner. Source/regression commits may be published only with exact remote readback; executable PASS will not be claimed unless independently evidenced on the resulting SHA.

## Completion condition

Claim-only reservation is visible on remote `main`; source + focused regression are reconciled against current `main`; remote readback confirms both changes; then this claim is closed `COMPLETED` with explicit validation limitations.
