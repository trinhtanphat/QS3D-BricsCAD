# Work claim — Grid Annotation numeric handle identity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-grid-annotation-handle-identity`
- Registered: `2026-08-12T13:47:00+07:00`
- Completed: `2026-08-12T13:54:00+07:00`
- Baseline main SHA: `11c267129c5ee75bfbb686e63f0e2fb36d99658f`
- Priority: P0 — generated Grid annotation health must use numeric CAD Handle identity consistently.
- Task Key: `CORE-GRID-ANNOTATION-HANDLE-IDENTITY`

## Confirmed defect

`GeneratedGridAnnotationHealthService` validated each non-empty token as hexadecimal, but then keyed duplicate/count and SourceHandles checks by trimmed raw text. Provider-valid aliases such as `A` and `0A` were therefore treated as different generated entities even though BricsCAD resolves them to the same numeric CAD Handle.

## Completed contract

- Provider-valid handle tokens use `GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(...)` for duplicate/count identity.
- Generated-vs-SourceHandles checks use the same numeric identity once the generated token passes the existing validity rule.
- `A` and `0A` collapse to one generated CAD object.
- Empty/invalid-token and whitespace canonicality behavior is unchanged.
- Existing `0x` generated-token validity behavior remains unchanged.
- Distinct valid numeric handles remain distinct.

## Evidence

- Claim commit: `2857b2c5d5a81aa97d6e631f6dac919c9a01a746`
- Source branch commit: `40b544e4299a6baa8b70748ba6e529181ca392cc`
- Smoke branch commit: `58251c015a59047fc0176ef8dbf4b57100df3834`
- PR: `#923`
- Squash merge: `f27f144561f74a55a0eb752ec665944e510fddc9`
- Merged source blob: `eb14b4fa1e9fe88f0b414cf79bff1173096537a9`
- Merged smoke blob: `d07088578898a223d2de164ea8bc688138fc3b38`
- Ancestry verified against `main@650d4def3d251dd47c002a2f725d1045d3540920`; only unrelated quantity-settings and opening-boolean files changed after the merge.

No GitHub Actions were dispatched. No full local .NET build, executable smoke run, or BricsCAD V25/V26 runtime PASS is claimed.
