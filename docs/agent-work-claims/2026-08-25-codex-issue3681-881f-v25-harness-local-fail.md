# #3681 exact-main V25 official runner: harness fixture defect

- Status: `SOURCE_HARNESS_DEFECT / LOCAL_FAIL / NO PRODUCT VERDICT`
- Lane-Key: `issue-3681-881f-local-evidence`
- Parent local queue: #72
- Licensed qualification: #3681
- Source-safe harness defect: #3754
- Tested exact Git SHA: `881f7b57176514e6e87c943f88165a5868c68539`
- Evidence branch baseline: `2810c81146b11b4ccad99433c3bacf2b2e96d4d1` (documentation descendant only; not the tested binary)
- BricsCAD host: V25.2.10 Windows x64, licensed native runtime
- Plugin/Core ProductVersion: `0.1.0-preview.10081`
- Plugin SHA-256: `B1B4DE624151152F814A565ACB04F6E65DBBED908D9DA3BBDD654F5A13DF27B8`
- Co-located Core SHA-256: `ECF356A045F2FF7F70579D0A6F667967D2E22A4D24016E7705331BD35D9C8055`
- Local qualification harness SHA-256: `6658ED8038D9E0FBEA26A8B7A0126EB4208E1EE0E09790559F8FBB530D5FE003`

## Exact identity and repository-safe gates

The official committed runner `scripts/run-local-v25-wall-contact-3681.ps1` ran from a clean detached worktree at the exact SHA above. Plugin and co-located Core ProductVersion checks passed, and both PDB SourceLink records bound the binaries to that exact SHA. The source-build baseline passed its repository gates, including aggregate feature preflights, Core Release build and deterministic smoke (`ALL PASS`), V25 `Release|x64` build, offline WPF smoke and local qualification harness build. The baseline deliberately skipped the general runtime probe because the #3681 runner owns the focused licensed native phase.

## Official fail-fast result

The focused native phase reached the first mandatory source-fix gate and returned:

```text
status=FAIL
phase=source_fix_gate
error_type=InvalidOperationException
error_code=touching_one_end_deduction
```

The sanitized qualification report classified this as `LOCAL_FAIL` and stopped before the penetration control and wider partial/union/top-bottom/stale/capture-refresh/two-end, second-process, and save/cold-reopen matrix. None of those unexecuted cells is promoted to PASS.

## Sanitized fixture diagnosis

A separate Git-ignored, read-only licensed probe called native `Solid3d.CreateBox(1468, 200, 800)` and applied the same displacement `(0, -100, 0)` used by the two committed #3681 harness helpers. BricsCAD V25 reported the resulting bounds as:

```text
min=(-734,-200,-400)
max=(734,0,400)
```

The harness therefore treats its requested `(x, y, z)` as a minimum corner even though the created V25 box is centered about the origin. Its intended touching topology becomes an overlap, so the failing official gate cannot establish a production wall-contact verdict. Issue #3754 owns the source-safe correction to both local-qualification helpers plus a focused placement guard.

A separate ignored production-fixture diagnostic on the same exact SHA produced the expected one-end deduction `0.15999999999999093 m2` and residual `2.5088000000000146 m2` for both touching-only and 0.05 m penetration controls. That diagnostic supports the harness/fixture divergence, but it does not replace the official runner or complete #3681.

## Cleanup and handoff

The official run restored the installed DemandLoad registration (`LoadCtrls=2`), removed its temporary registration, preserved the installed loader, left zero BricsCAD processes, and kept raw scripts, DWGs, handles, sidecars, paths and runtime logs Git-ignored. The local lane made no production or committed harness patch.

#3681 remains OPEN. Do not rerun the unchanged official harness and do not report `LOCAL_PASS`. After #3754 merges, publish one new exact descendant SHA, rebuild with exact PDB SourceLink identity, and rerun the official command from a clean worktree.
