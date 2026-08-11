# Work claim — Material usage schedule collision-free grouping identity

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:50:00+07:00`
- Completed: `2026-08-12T00:54:00+07:00`
- Baseline main SHA: `b5d91728e369b98931b2bf456302e0a237bf4039`
- Claim commit: `55a2457f0e9eb5e7f4163e50e4b84b1d768f1c6a`
- Priority: evidence-driven remote-safe reporting integrity

## Confirmed defect

`MaterialUsageScheduleBuilder` grouped rows with an unescaped U+001F delimiter across floor/material/component/category/family tokens. Accepted IDs/material text can contain U+001F internally, so distinct tuples such as floor `A<US>B` + material `C` and floor `A` + material `B<US>C` could serialize to the same dictionary key and be incorrectly merged.

## Completed scope

Material usage grouping now uses deterministic length-prefixed tokens while preserving the existing case-insensitive comparer, ordering, metrics, provenance and accepted character set.

## Product/test commits

- `21fc29ab9dec575e326000520547d362a9eab109` — `fix(reporting): make material schedule grouping collision-free`
- `782f35e14b60a8c015ac863970c18f1aa536e017` — `test(reporting): cover material schedule group key collision`
- `cbcc17b8955b07f98ecbc5d32b1d682ec9faf16f` — `test(reporting): register material schedule group key smoke`

## Validation

- Re-fetched the target blob after claim publication before the product write.
- Product diff only replaces the delimiter-only group key with a length-prefixed `GroupKey` helper and adds `System.Text`.
- Regression creates two Beam elements with identical floor/material tuple `A<US>B`/`C`, proving normal grouping and LengthM accumulation, plus one `A`/`B<US>C` element that formerly collided but now remains independent. Legitimate floor definitions are used so display identity also remains verifiable.
- Registration uses a dedicated module initializer.
- After registration, observed `main` at `de02fb0253f9caeeddf312a76ab93817ac161562`; comparison from `cbcc17b8955b07f98ecbc5d32b1d682ec9faf16f` reported `status=ahead`, `behind_by=0`, merge base equal to the registration commit. Concurrent changes touched unrelated wall-pier, V26 plugin and curtain-path surfaces.
- GitHub Actions were not dispatched.
- No .NET SDK or BricsCAD V25/V26 runtime PASS is claimed from this hosted session.

## Excluded scope

- No material catalog/unit policy, room finish lifecycle, quantity formula/business-rule, XLSX export or native BricsCAD changes.

## Completion

Distinct accepted material usage schedule tuples no longer alias through delimiter injection on current `main`; claim released as completed.