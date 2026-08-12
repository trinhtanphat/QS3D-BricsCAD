# Work claim — QSDB changeVersion token canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-qsdb-changeversion-canonicality`
- Registered: `2026-08-12T09:40:00+07:00`
- Baseline main SHA: `f1f8e8e2e647db4d67ea9da7703e3cbc289ec98f`
- Regression commit: `fe2c1c768e2f61a3ff47c16c21e800c59d0b3f5b`
- Completed source commit: `8144ac7e23930351a12d116ec4f878dd639487ce`
- Readback main SHA before close-out: `fc365b54f7d5db899b48ffd5dbd2b195253cd911`
- Priority: P1 deterministic persistence / fail-closed token integrity found during owner-requested `continue all` audit.

## Confirmed defect

The original change-version persistence commit `ba50b0c6655eeea0e0fb69b5a3cf5dade9770796` writes `project.ChangeVersion.ToString(CultureInfo.InvariantCulture)`. Current migration synthesizes missing legacy changeVersion as canonical `0`. The previous loader rejected blank, signed/negative/malformed/out-of-range values, but `long.TryParse(..., NumberStyles.None, ...)` still accepted semantically equivalent zero-padded tokens such as `01`, allowing silent rewrite to `1` on the next save.

## Implemented contract

1. Existing `long.TryParse` / non-negative / range validation remains unchanged.
2. After parsing, `ChangeVersion(...)` derives `result.ToString(CultureInfo.InvariantCulture)`.
3. The original persisted token must exactly match that canonical integer with `StringComparison.Ordinal`; otherwise `Load()` fails closed with `InvalidDataException`.
4. Legacy migration/default behavior and valid current-schema round-trip remain unchanged.
5. Focused smoke coverage uses the real QSDB writer, verifies canonical round-trip, then prefixes the serialized changeVersion with `0` and requires `Load()` to reject the equivalent token.

## Verification

- Current-main source readback confirmed parse/range validation followed by exact canonical integer comparison.
- Current-main smoke readback confirmed canonical round-trip plus zero-padded-token rejection.
- `8144ac7e23930351a12d116ec4f878dd639487ce...main` compared as `ahead` with the source commit as merge base; later concurrent changes touched unrelated claims only in that comparison window.
- Smoke source was committed but not executed from this remote connector session. Full Core smoke execution/build and GitHub Actions were not run; no PASS is fabricated.
- This is Core persistence work and makes no licensed BricsCAD runtime claim.

## Excluded

- No ProjectSchemaMigrator or missing-attribute behavior changes.
- No timestamp/numeric/dirty/category/name/rule-text or adapter/UI changes beyond preserving already merged contracts.
- No installer/release work.
