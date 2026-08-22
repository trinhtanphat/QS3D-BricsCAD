# Work claim — QSDB numeric canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-qsdb-numeric-canonicality`
- Registered: `2026-08-12T08:59:00+07:00`
- Baseline main SHA: `351478288c27f025a62afdf04960b49e2ee3c129`
- Regression commit: `e42ec9823aaf66bb8257c4fb30ac561a07d11e5b`
- Completed source commit: `5395f71f6fed463558f4aed2838f23d3c37ae075`
- Readback main SHA before close-out: `ddc233073ac887efd50007aef115faa7ce4b18ef`
- Priority: P1 persistence canonicality / deterministic round-trip integrity found during owner-requested `continue all` audit.

## Confirmed defect

`QsdbProjectStore.Serialize(...)` emits persisted floor elevations and element quantities through `F(double)`, which has used invariant round-trip `ToString("R", CultureInfo.InvariantCulture)` since the repository's initial QSDB persistence implementation (`95c39e51b550b740f9df1bd77b219bcc5406998c`). The previous `Double(...)` loader accepted any finite token parseable with `NumberStyles.Float`, so semantically equivalent noncanonical tokens such as `1.0`, `1e0`, `+1`, or `-0` could be loaded and then silently rewritten on the next save.

Historical source readback confirmed the initial QSDB writer already used the same `"R"` representation, so the strict loader contract does not reject numeric tokens emitted by supported repository serializers.

## Implemented contract

1. `Double(...)` still requires invariant finite parsing.
2. After parsing, it derives the canonical persisted representation through the existing `F(result)` helper.
3. The original token must exactly match that canonical representation with `StringComparison.Ordinal`; otherwise `Load()` fails closed with `InvalidDataException`.
4. The same helper covers floor `elevationM` and element quantity `value` without changing in-memory numeric semantics or migration defaults.
5. Focused smoke coverage saves a canonical QSDB and verifies valid floor/quantity round-trip, then independently mutates `elevationM` to `1.250` and a quantity value to `2.5e0` and requires `Load()` to reject both.

## Verification

- Current-main source readback confirmed parse -> `F(result)` -> exact-original comparison in `Double(...)`.
- Current-main smoke readback confirmed canonical round-trip plus floor and quantity noncanonical-token cases.
- `5395f71f6fed463558f4aed2838f23d3c37ae075...main` compared as `ahead` with the source commit as merge base; later concurrent commits touched unrelated recognition/rule/claim files.
- The first source write attempt was rejected by GitHub with `409` during concurrent movement; source was re-fetched and the successful retry preserved concurrent work.
- The smoke source is committed but was not executed from this remote connector session. Full Core smoke execution/build and GitHub Actions were not run; no PASS is fabricated.
- This is Core persistence work and makes no licensed BricsCAD runtime claim.

## Excluded

- No schema-version bump or migration rewrite.
- No timestamp, change-version, dirty-flag, category, map/list or relation canonicality changes.
- No BricsCAD adapter/UI changes.
- No installer/signing/release changes.
