# Quantity Locate Validation Failure Clear Plan

Date: 2026-08-11
Owner: ChatGPT Web / GPT-5.6 Sol
Claim: `docs/agent-work-claims/2026-08-11-2318-chatgpt-web-gpt56sol-quantity-locate-validation-failure-clear.md`
Status: `IMPLEMENTED_SOURCE_SIDE`

## Goal

Ensure a quantity locate attempt cannot leave a previous CAD implied selection highlighted when stale-row or stale-project validation fails before the normal explicit `CadHandleService.Select(...)` call.

## Implemented behavior

The completed zero-live/zero-candidate lane already handled failures after candidate resolution. This follow-up closes the earlier validation window without rewriting canonical Quantity code-behind/XAML.

- `QuantitySummaryWindow.LocateSelectionFailureGuard.cs` registers WPF class handlers for the exact existing Summary locate triggers: the unique `Định vị` button, qualifying `QuantityGrid` selection changes, and the existing double-click path.
- `QuantityInsightPanel.LocateSelectionFailureGuard.cs` registers handlers for the exact existing Insight locate triggers: the unique `Định vị` button, qualifying `QuantityTree.SelectedItemChanged`, and the existing double-click path.
- Both partials use explicit static constructors so registration is deterministic before instance initialization rather than relying on CLR `beforefieldinit` timing.
- Before the existing instance locate handler validates the row/project, the guard best-effort clears through `CadHandleService.Select(document, Array.Empty<string>())` only if the bound document is still `MdiActiveDocument`.
- Wrong-DWG stale surfaces do not clear the newly active document.
- Successful locate flows immediately revalidate and select the intended target again; stale validation failures leave no unrelated previous implied selection visible.
- Canonical locate code, zero-candidate handling, reporting/persistence semantics and positive-count-only `QS3DZOOMSELECTED` ordering remain unchanged.

## Regression gate

`scripts/preflight-quantity-locate-validation-failure-clear.py` locks:

- SDK-style WPF/default compile inclusion for the new partial files;
- explicit static constructors and rejection of delayed static-field registration;
- exact class-handler trigger types and current XAML event wiring;
- exact Summary/Insight ownership filtering;
- active-document affinity before `Select(empty)`;
- no project mutation/bootstrap/touch/zoom logic inside the guards;
- unchanged canonical validated-target selection and zoom gating.

The final gate source is 153 lines and AST-parses successfully.

## Integration evidence

- Claim registered before implementation: `0affeed6f5bc1133ca6a7598936d20b85ad18573`.
- Initial plan: `07f56571314e63d606b3c1348fd611ee01426abd`.
- Implementation refinement recorded before integration: `17113d91de92150103be3f01e52e8d12b400ae0d`.
- Summary guard commits: `771687906b5e3e1273e34d9458f3bd734de99f8e`, deterministic-registration refinement `b46218feab812673f0b383fcb38bd18a1a7a3255`.
- Insight guard commits: `9a23645dd36d8615a175aa03e459e93aa792a277`, deterministic-registration refinement `451c1875969c742f2dd0c95079e914731be30bcc`.
- Focused gate commits: `539909f45db2dd755b518b803b6819ba5fcf5410`, final deterministic-registration gate `521f2b7b7f80dffaea526ee5c7537f97bc725245`.
- PR #532 was `mergeable=true`, `rebaseable=true`, `mergeable_state=clean`; it merged server-side with expected head SHA and no force update.
- Merge commit: `17a798fabb5bcce06b27d0fd4d011af79481f94c`.
- Merge-SHA blobs: Summary guard `74290de93b7dbe169effc19189a040506857bc49`; Insight guard `922c8ac786249f443faa9ae87df5fb0420d8355c`; gate `8ecd932935ab99e92cf96f4a8ca72c2f25392d7c`.

## Qualification

- Prior Quantity merge to implementation checkpoint was compared across 171 commits with no edits to canonical Summary/Insight locate files.
- Branch base to pre-merge `main` was compared across another 105 commits with no overlap on canonical locate/XAML/project/guard/gate paths.
- Branch diff was exactly three new files: two partial guards plus one focused preflight.
- Merge-SHA source re-fetch confirms explicit static constructors, exact trigger/owner filters, active-document check before `Select(empty)`, and best-effort failure isolation.
- Focused gate AST parse: `PASS` (153 lines); source-order contracts were independently inspected against merge-SHA source.
- Eight commits landed after the merge during qualification; none modified the two guards, focused gate, or canonical Quantity locate surfaces.
- GitHub registered no combined status checks and no workflow runs for the merge SHA; this is recorded as absence, not CI PASS.

Licensed BricsCAD V25 interaction remains `PENDING_LOCAL / DO_NOT_RETRY_REMOTE` under the existing local qualification queue. No remote native-runtime PASS is claimed.
