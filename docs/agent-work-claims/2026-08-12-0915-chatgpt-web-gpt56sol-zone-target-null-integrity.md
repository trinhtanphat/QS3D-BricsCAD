# Work claim — Zone target operations null collection integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-zone-target-null-integrity-20260812-0915`
- Registered: `2026-08-12T09:15:00+07:00`
- Completed: `2026-08-12T09:18:00+07:00`
- Baseline main SHA: `870811fb578f6afa7231fd0b9636139544cdd64f`
- Priority: P1 — Zone target operations must fail closed when the project Zone collection is structurally invalid.

## Completed scope

`ProjectZoneService` target-based operations now reject any `null` entry in `project.Zones` before resolving or using a target Zone. `ValidateUniqueZoneIds(project)` no longer skips malformed null entries, aligning target operations with the existing Zone Create structural-null contract.

## Pushed implementation

- Claim registration: `15ef67abedac0a82ea0de61090a4edc4e6e6c5fc`
- Source fix: `6257c714d1acbaec56b86b8729bad311a3c7ad34`
- Focused Core smoke: `3e61f5b39bd9974a1cccea02f6c616b1be3deee7`

## Validation evidence

- Readback from current `main` confirms `ValidateUniqueZoneIds(project)` throws `InvalidOperationException("Project zone collection contains a null zone.")` rather than continuing past a null Zone.
- `ProjectZoneGlobalNullIntegritySmoke` covers `Update`, `SetActive`, `Assign`, `Delete`, and `ReferenceCount` against a valid target plus unrelated null Zone state.
- The smoke snapshots Zone count/name, active Zone, element ZoneId, `ChangeVersion`, and `UpdatedUtc` across rejected operations and includes valid update/activate/assign/reference-count controls.
- Connector ancestry check confirmed source commit `6257c714d1acbaec56b86b8729bad311a3c7ad34` remained an ancestor of moving `main`; concurrent files were outside this reserved scope.
- Source helper and smoke source were read back from current `main` after push.

## Excluded / remaining validation

- Zone Create null/duplicate integrity and semantic element integrity remain separate completed/current lanes.
- Floor/Family services, Floor/Zone UI audit/no-op behavior, persistence/interchange and native BricsCAD adapters were not changed.
- GitHub Actions were not dispatched because `continue all fix bug update code` is not CI authorization under `CI_POLICY.md`.
- No local compile, executable smoke run, or licensed BricsCAD V25/V26 runtime PASS is claimed from this web session.

## Completion condition

`COMPLETED`: target Zone operations fail closed on null Zone collection entries, focused deterministic Core smoke coverage is present on `main`, ownership is released, and no concurrent work was overwritten.
