# Work claim — Model Health duplicate SourceHandles

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-model-health-source-handle-duplicates-20260813`
- Registered: `2026-08-13T19:24:00+07:00`
- Completed: `2026-08-13T19:30:00+07:00`
- Baseline main SHA: `ef2c860935f08d35569adbae78d4daa2988f851e`
- Priority: P1 diagnostic integrity. `ModelHealthService` normalized `ProjectElement.SourceHandles` and immediately applied case-insensitive `Distinct()`, so duplicate source identities inside one semantic element (including `ABCD` + `abcd`) disappeared before health diagnostics. This was inconsistent with existing intra-element `DUPLICATE_DEPENDENCY` diagnostics and with cross-element source-handle ownership collision diagnostics.

## Reserved scope

- `src/QS3D.Core/Diagnostics/ModelHealthService.cs`
- `tests/QS3D.Core.SmokeTests/ModelHealthSourceHandleSmoke.cs`
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs`
- this claim file for closeout

## Result

- Implementation: `1175b674cc2b5a22fc0a19018e6485d845e7364a` (`fix(health): report duplicate source handles`).
  - Nonblank SourceHandles are still trimmed exactly as before.
  - Case-insensitive duplicate normalized handles are now detected before deduplication and emit one deterministic `DUPLICATE_SOURCE_HANDLE` warning per duplicated handle/element.
  - The list is then deduplicated exactly as before for existing cross-element `DUPLICATE_HANDLE` ownership and `ORPHAN_HANDLE` live-source checks.
- Regression: `33cb147913fa8f331b441fb2060840e5a75d6605` (`test(health): guard duplicate source handles`).
  - `ABCD` + `abcd` inside one element emits exactly one intra-element duplicate warning, does not become a cross-element collision, and stays non-orphan when live.
  - A unique handle stays free of duplicate-source diagnostics and live matching remains case-insensitive.
  - Two different elements sharing one handle retain exactly the existing cross-element `DUPLICATE_HANDLE` behavior and do not trigger the intra-element code.
- Registration: `0e128fe5ad1eefd46e8aaa951073a915f114d3d0` (`test(health): register source-handle smoke`).

## Validation actually performed

- Claim was pushed alone, then `main` and recent ModelHealth/source-handle commits were refreshed/rechecked before source mutation; no overlapping current lane was found.
- Exact production commit diff was re-read from GitHub and contains only the pre-dedup duplicate detection plus warning emission; the rest of `ModelHealthService` is unchanged.
- Exact focused smoke commit was re-read from GitHub; its positive/negative controls cover intra-element duplicates, unique handles, live-handle behavior and cross-element ownership preservation.
- Exact registration diff was re-read and adds only `ModelHealthSourceHandleSmoke.Run()` to the central smoke sequence.
- At verification time remote `main` was exactly `0e128fe5ad1eefd46e8aaa951073a915f114d3d0`, so implementation, smoke and registration were all on the remote head before closeout.
- This environment has Python 3.13.5 but no `dotnet`, `csc`, `mcs`, `msbuild` or `xbuild`; managed smoke execution was unavailable. No managed-build PASS, GitHub Actions PASS or licensed BricsCAD runtime PASS is claimed.

## Excluded scope preserved

- no persistence/deserialization/source resolver changes;
- no generated-handle ownership changes;
- no semantic identity/family/floor/zone/dependency diagnostics changes;
- no UI/report/BricsCAD native work, sibling Platform migration, GitHub Actions or native qualification.

## Completion condition

Satisfied for source/static scope: Model Health no longer silently erases intra-element duplicate source identities, existing cross-element/orphan semantics are explicitly preserved in focused regression source, exact diffs and remote placement were verified, and unavailable managed/native execution is stated rather than fabricated.
