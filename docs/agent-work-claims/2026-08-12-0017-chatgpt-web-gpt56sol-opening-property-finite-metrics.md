# Work claim — OpeningPropertySet finite metric integrity

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:17:00+07:00`
- Corrected against current target blob: `2026-08-12T00:18:00+07:00`
- Completed: `2026-08-12T00:21:00+07:00`
- Baseline main SHA: `07c986cc4419eae81d11adf505b4586f7247c030`
- Claim commit: `2321d700601c344927dc6f381387818ea7078759`
- Claim correction commit: `f9192c4e95a834039be75b516e3b4931449f536d`
- Priority: evidence-driven remote-safe Core domain integrity

## Confirmed defect

Current `OpeningPropertySet` exposed four public `double` auto-properties (`WidthMm`, `HeightMm`, `ThicknessMm`, `SillOffsetMm`) that accepted `double.NaN` and infinities, allowing malformed non-finite geometric measurements to be retained at a public Core domain boundary.

## Completed scope

Every assignment to the four numeric opening properties now requires a finite `double`. Current defaults (`900`, `2200`, `110`, `0` mm) and all finite values, including zero and negative values, remain accepted. `BottomLevel` is unchanged.

## Product/test commits

- `43f75fa5cc9f8e2a77baffa8bb2800dda980ca02` — `fix(domain): reject non-finite opening metrics`
- `8dffb6b63cc23182b983df2e808540493e047913` — `test(domain): cover finite opening metrics`
- `bf00a24069dfbab38276725e568ea86fc7fd6a2d` — `test(domain): register opening metric smoke`

## Validation

- Re-fetched target blob before product write and corrected the claim before any product edit when the actual current properties differed from the initial assumed names.
- Reviewed exact source diff: only numeric property storage/setters and a shared finite guard changed; `BottomLevel` and default values were preserved.
- Smoke covers defaults, zero/negative/ordinary finite assignments, NaN/+Infinity/-Infinity rejection, and preservation of prior values after rejected assignments.
- Registration uses a dedicated module initializer to avoid shared registry contention.
- After registration, observed `main` at `5cb33887ad8937a88946bff0987d4e4aefa066cc`; comparison from `bf00a24069dfbab38276725e568ea86fc7fd6a2d` reported `status=ahead`, `behind_by=0`, merge base equal to the registration commit. Concurrent commits touched other surfaces.
- GitHub Actions were not dispatched.
- No .NET SDK or BricsCAD V25 runtime PASS is claimed from this hosted session.

## Excluded scope

- No `BottomLevel` semantics or string normalization changes.
- No physical-opening boolean/cutter/native service changes.
- No positivity/minimum-size engineering policy.

## Completion

The finite opening-metric invariant and focused regression source are on current `main`; claim released as completed.