# Work claim — Grid naming target structural freshness

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-grid-naming-target-structural-freshness-20260812-1309`
- Registered: `2026-08-12T13:09:00+07:00`
- Baseline main SHA observed before registration: `688827f27ff832dbc380a2a7f82353eb956471e7`
- Priority: evidence-driven remote-safe Core structural freshness

## Confirmed defect

`GridNamingService.Renumber(...)` currently snapshots only `ProjectState.ChangeVersion` while enumerating caller-supplied Grid IDs, then resolves those IDs against the live public `ProjectState.Elements` list afterwards. `ProjectState.Elements` is an exposed mutable `IList<ProjectElement>`; direct structural replacement/removal/addition does not increment `ChangeVersion`.

A lazy caller can therefore replace the originally targeted Grid object with a different `ProjectElement` carrying the same ID during ID enumeration without changing `ChangeVersion`. The existing freshness check passes, `ResolveProjectElements(...)` resolves the replacement, and the renumber operation silently retargets and mutates a different semantic object than the one owned when planning began.

The earlier Grid naming input-freshness regression covers explicit `project.Touch()` during enumeration, not same-ID structural replacement without a version change. Recent Core structural-freshness fixes establish that caller enumeration must not be allowed to retarget semantic object ownership silently.

## Reserved scope

- `src/QS3D.Core/Domain/GridNamingService.cs`, limited to pinning/revalidating target semantic object identity across caller ID enumeration.
- one focused `QS3D.Core.SmokeTests` regression file.
- this claim file.

## Intended contract

- Preserve the existing 2,000-target bound, ID canonicalization/deduplication, label generation, collision checks, no-op behavior, change-version freshness check and project-touch semantics.
- A Grid ID selected for renumbering must still resolve to the same `ProjectElement` object after caller enumeration; same-ID replacement/removal must fail closed before any naming mutation or project touch.
- Do not modify Grid ARC/intersection geometry or numerical semantics.

## Coordination

Current Grid ARC/intersection claims are numerical geometry lanes and do not reserve `GridNamingService.cs`. The previous completed Grid naming input-freshness lane only covers `ChangeVersion` mutation during lazy enumeration and is not being rewritten.

## Validation boundary

Focused source/readback regression only in this connector session. No GitHub Actions dispatch, local .NET build/smoke PASS, or licensed BricsCAD V25/V26 runtime PASS will be claimed without execution.

## Completion condition

Current `main` rejects same-ID Grid target replacement/removal across caller ID enumeration before any renumber mutation, focused regression evidence is present, moving-main overlap is rechecked, and this claim is closed `COMPLETED` with exact integration evidence.
