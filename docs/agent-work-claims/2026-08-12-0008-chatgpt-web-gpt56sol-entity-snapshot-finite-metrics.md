# Work claim — EntitySnapshot finite metric integrity

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:08:00+07:00`
- Completed: `2026-08-12T00:12:00+07:00`
- Baseline main SHA: `9e12cb7f1145659c84ed8fac4d033c8832007a68`
- Claim commit: `48591255a4dda245bed178f001e89a415bb20f8a`
- Priority: evidence-driven remote-safe Core model hardening

## Confirmed defect

`EntitySnapshot` exposed public nullable metric setters for length, area, surface area and volume that accepted `double.NaN` and infinities, allowing a public Core model instance to retain non-finite measurement state.

## Completed scope

Every non-null `EntitySnapshot` metric assignment now requires a finite `double`. Existing `null`, zero, negative and ordinary finite values remain accepted; no positivity/business semantics were added.

## Product/test commits

- `58b1253546273669417c5ae6ead97eb6d55b70d6` — `fix(model): reject non-finite entity snapshot metrics`
- `62a3de83429255c609c7ae2ad1b43e8b9a024bae` — `test(model): cover finite entity snapshot metrics`
- `e6f42b61bfb4745e308593e41126282ea92b07f9` — `test(model): register entity snapshot metric smoke`

## Validation

- Reviewed exact source diff: only the four nullable metric setters gained pre-assignment finite validation and backing fields.
- Smoke covers nullable defaults; accepted zero, negative and positive finite values; NaN/+Infinity/-Infinity rejection; and preservation of the previous value after a rejected assignment.
- Registration uses a dedicated module initializer, avoiding shared registry contention.
- An initial source write received HTTP 409 because `main` moved concurrently; no overwrite was forced. The target blob was re-fetched and the guarded write then succeeded.
- After registration, comparison from `e6f42b61bfb4745e308593e41126282ea92b07f9` to observed `main` `104c75a48edbddfb6108912cf2088e1757bf02b5` reported `status=ahead`, `behind_by=0`, merge base equal to the registration commit. Concurrent commits touched other surfaces.
- GitHub Actions were not dispatched.
- No .NET SDK or BricsCAD V25 runtime PASS is claimed from this hosted session.

## Excluded scope

- No RecognitionEngine, capture eligibility or recognition scoring changes.
- No native BricsCAD adapter changes.
- No positivity/business-rule changes.

## Completion

The finite metric model invariant and focused regression source are on `main`; claim released as completed.