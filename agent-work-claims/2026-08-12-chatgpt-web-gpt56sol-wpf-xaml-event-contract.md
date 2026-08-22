# Work claim — WPF XAML event-handler contract preflight

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-wpf-xaml-event-contract-20260812-0027`
- Registered: `2026-08-12T00:27:00+07:00`
- Baseline main SHA: `0ad2b89d9f9e7ae9665912182fd040409e00ad37`
- Priority: A missing `OnRightPanelPreviewKeyDown` callback was previously able to remain declared in XAML without an implementation; add a repository-wide source guard so the same WPF contract break cannot silently recur on another UI surface.

## Reserved scope

Add a source-only preflight that scans the BricsCAD adapter WPF XAML files, extracts code-behind event-handler references, and fails when a declared handler cannot be found in the corresponding C# partial-class source set. Document the guard and its intentional boundaries.

## Expected surfaces

- `scripts/preflight-wpf-xaml-event-contract.py`
- `docs/WPF-XAML-EVENT-CONTRACT-2026-08-12.md`
- this claim file for completion close-out only

## Excluded scope

- No edits to existing `*.xaml`, `*.xaml.cs`, UI partial classes, Ribbon, palettes, quantity UI, Workspace/RightPanel product behavior, or BricsCAD commands.
- No runtime WPF/BricsCAD/DPI qualification.
- No changes to `scripts/preflight-all.py`; its existing `preflight-*.py` auto-discovery contract is preserved.
- No GitHub Actions dispatch or rerun.

## Validation plan

- Keep the parser fail-closed for malformed XAML and conservative about markup-extension/binding values so it checks only literal handler identifiers.
- Match each XAML `x:Class` to C# source files declaring the same partial class, including split partial files such as `RightPanel.Keyboard.cs`.
- Require each literal WPF event callback to appear as a method declaration in that class source set, while ignoring namespace declarations, attached/property syntax, bindings and non-event attributes.
- Source-review the new guard against current repository UI structure; syntax/synthetic checks may be run in the remote execution environment, but no claim of full repository preflight execution or BricsCAD runtime PASS will be made unless actually executed.

## Coordination

This lane is additive and deliberately avoids product source currently being modified by other agents (persistence, quantity, Direct Draw, updater, rebar, semantic schedules, openings and domain integrity). It does not take ownership of any existing UI feature lane; it only reserves the new global static contract guard and focused documentation.

## Completion

- Claim registration on `main`: `dd92bb5c8ef92c3a7bc68d841fa5b061d12c50d3`.
- Preflight implementation: `45e16ab77903d9312391c22b547580da4a376786`.
- Documentation / branch head: `8bd566100b824eec0d5e3d262106f81b3a43ba35`.
- PR: `#583` — `test(ui): guard WPF XAML event callbacks`.
- Integrated on `main`: `5b53691cd031589841baf3a24fe3c040dbfb735c`.
- PR diff was rechecked before merge and contained exactly the two reserved additive files: `scripts/preflight-wpf-xaml-event-contract.py` and `docs/WPF-XAML-EVENT-CONTRACT-2026-08-12.md`.
- Moving-`main` comparison before integration showed no overlap with those two files; concurrent work was preserved.

## Validation result

- Python syntax compilation: **PASS** in the remote execution environment.
- Synthetic valid WPF fixture with split partial classes plus `Click`, `TextChanged`, `PreviewKeyDown`, and `EventSetter` callbacks: **PASS**.
- Synthetic missing-handler fixture: **EXPECTED FAIL**, with the XAML path/event/handler and candidate C# files reported.
- Synthetic malformed-XAML fixture: **EXPECTED FAIL** with parser diagnostics.
- Full repository `preflight-all.py`: **NOT EXECUTED** because the connector session does not provide a usable repository checkout.
- Native BricsCAD V25 / WPF modeless / HiDPI runtime qualification: **NOT EXECUTED REMOTELY** and remains local-only.
- GitHub Actions: **NOT DISPATCHED / NOT RERUN**.

## Completion condition

Satisfied. The additive source contract guard and focused documentation are merged on `main`; no existing product UI or viewport source was changed by this lane.
