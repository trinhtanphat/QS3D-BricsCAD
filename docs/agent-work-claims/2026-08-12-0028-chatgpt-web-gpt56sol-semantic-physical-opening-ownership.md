# Work claim — Semantic property edit physical-opening ownership guard

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:28:00+07:00`
- Completed: `2026-08-12T00:32:00+07:00`
- Baseline main SHA: `997eab1c953a5f943074bda103928999cb2379c0`
- Claim commit: `41d8cb6c90a63d70f0b2ff0d3b2f1d5c53df50a5`
- Priority: evidence-driven remote-safe ownership integrity

## Confirmed defect

`SemanticPropertyEditPolicy` blocked generic edits for keys beginning with `PhysicalOpeningCut` but not the namespaced form `QS3D.PhysicalOpeningCut...`, even though the interchange portability boundary treats both forms as drawing-local/native ownership state.

## Completed scope

`QS3D.PhysicalOpeningCut...` is now blocked by the same generic semantic property edit ownership guard. Existing ordinary semantic, CAD-derived, identity/reference, handle, generated and legacy physical-opening rules remain unchanged.

## Product/test commits

- `fbf8cc3495639de1694f7130e5fd983b30ea750f` — `fix(properties): protect namespaced opening ownership state`
- `40fde43bfe793f8d19e95261d4fb2b385ae4abe1` — `test(properties): cover opening ownership edit guard`
- `fe34c95c26bac556e649721618faceab400599c8` — `test(properties): register opening ownership edit smoke`

## Validation

- The product diff adds only the missing case-insensitive `QS3D.PhysicalOpeningCut` prefix to the existing native/generated block.
- Public-API smoke keeps `FinishCode` editable while proving legacy physical-opening, namespaced physical-opening state/fingerprint/targets, lower-case namespaced input, generated handles/state and identity references remain blocked as appropriate.
- Registration uses a dedicated module initializer to avoid shared smoke registry contention.
- The initial claim-file write received HTTP 409 because `main` moved; no forced overwrite occurred. The claim was then registered on refreshed `main` before product edits.
- After registration, observed `main` at `dc6e4d01dea6c1c48dc1a3287ef40fe8fddd741c`; comparison from `fe34c95c26bac556e649721618faceab400599c8` reported `status=ahead`, `behind_by=0`, merge base equal to the registration commit. Concurrent commits touched unrelated surfaces.
- GitHub Actions were not dispatched.
- No .NET SDK or BricsCAD V25 runtime PASS is claimed from this hosted session.

## Excluded scope

- No physical-opening boolean, target-state codec, host/cutter/native service or command changes.
- No interchange policy changes.
- No ordinary user-editable semantic property behavior changes.

## Completion

Namespaced physical-opening ownership state is protected from generic semantic edits on current `main`; claim released as completed.