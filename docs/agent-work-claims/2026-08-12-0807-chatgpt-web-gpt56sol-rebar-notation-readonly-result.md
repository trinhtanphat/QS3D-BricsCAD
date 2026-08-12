# Work claim — Rebar notation structural read-only result

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-rebar-notation-readonly-result-20260812-0807`
- Registered: `2026-08-12T08:07:00+07:00`
- Completed: `2026-08-12T08:08:00+07:00`
- Baseline main SHA: `b3f561dc4b9de2f70e645008407740103d1e1f26`
- Claim commit: `7cfb0a55e0721162d7316aeb386238f0b89142c4`
- Source commit: `0157f3bb6b7332b298672b3cd4a43cc2e58208cb`
- Regression commit: `ade119ba7165d90f09e18f46a542443a1b22d965`
- Pre-close verification SHA: `85daf8844c99fbb6d265e28acb80b8ebe2dc00d3`
- Priority: evidence-driven public parser result ownership during owner-requested `continue all`

## Confirmed defect

`RebarNotationParser.Parse(string)` declared `IReadOnlyList<RebarGroup>` but returned its mutable backing `List<RebarGroup>` directly. A caller could cast the parsed result to a mutable collection and structurally add, remove or clear groups after parsing despite the public read-only collection contract.

## Completed change

The parser now returns `result.AsReadOnly()` after all existing validation and group construction completes. Notation length/group bounds, regex grammar, whitespace behavior, diameter/spacing validation, checked quantity multiplication, group ordering and mutable `RebarGroup` object semantics are unchanged.

## Regression coverage

`RebarNotationReadOnlyResultSmoke` parses `2x3D16+D12@200`, preserves count/spacing values and ordering, uses nullable-safe `GetValueOrDefault()` after `HasValue`, asserts the returned `ICollection<RebarGroup>` is read-only, and verifies structural `Add` throws `NotSupportedException`.

## Coordination respected

The completed notation finite-bounds lane remains authoritative for parser limits. The active LOCAL-003 exact-gate work owns `RebarNotationBoundsSmoke.cs` reconciliation; this lane did not edit that smoke or change parser grammar/limits.

## Scope respected

No schedule arithmetic, shape planning, CAD/native behavior, Level placement, fabrication, release/update or persistence work was changed. No deep-immutability redesign was attempted.

## Validation evidence

Source and focused regression were re-fetched from current `main@85daf8844c99fbb6d265e28acb80b8ebe2dc00d3` after a concurrent commit and both remained present. This web session performed source/static read-back only: no GitHub Actions dispatch, local `dotnet`/Core smoke execution, private-DWG execution or BricsCAD V25/V26 runtime qualification is claimed.
