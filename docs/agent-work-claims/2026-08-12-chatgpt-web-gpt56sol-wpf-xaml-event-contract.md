# Work claim — WPF XAML event-handler contract preflight

- Status: `ACTIVE`
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

## Completion condition

The additive preflight and focused documentation are pushed/merged onto current `main`, the final diff is checked for only the reserved files, and this claim is marked `COMPLETED` with exact implementation/integration SHAs plus truthful validation evidence.
