# V25 semantic capture property lifecycle qualification

Carrier: #5781

Hosted CI establishes source/static policy, deterministic Core validation, and the locked-reference V25 adapter build. It does **not** establish licensed BricsCAD runtime PASS.

## LOCAL_ONLY matrix

Run only from the exact candidate SHA with a licensed BricsCAD V25 host and preserve the resulting command transcript/artifacts.

1. **P01 control capture** — capture a normal supported source object with valid layer/XData/extension metadata. Verify semantic Layer, CAD metadata and source metrics are preserved and project persistence/reopen succeeds.
2. **P02 malformed host text rollback** — use a controlled test object/fixture whose captured host metadata contains an XML-invalid control character. Invoke semantic capture. Expected: capture fails at canonical `ProjectElement.SetProperty`, the existing `ProjectStateSnapshot` rollback restores the exact pre-command state, and a newly-created project context is forgotten. Do not sanitize the invalid value to manufacture a PASS.
3. **P03 stale CAD metadata cleanup** — capture an object, then recapture the same handle after removing one CAD metadata field and one measurable source metric. Expected: stale `CAD.*`/metric properties are removed through lifecycle-aware removal and generated-output stale/dirty semantics remain correct.
4. **P04 retry** — after P02, repair the fixture metadata and retry. Expected: one clean semantic element, no residue from the failed attempt, and successful save/reopen.
5. **P05 cold reopen** — save the P01/P03/P04 project, close BricsCAD, reopen, and verify the persisted semantic properties/quantities remain valid and no delayed XML serialization failure occurs.

Record exact repository SHA, BricsCAD version/build, drawing fingerprint, command transcript, and resulting project file hash. Until this matrix is executed locally, report `LOCAL_ONLY / NO_RESULT`; never infer licensed runtime PASS from hosted CI.
