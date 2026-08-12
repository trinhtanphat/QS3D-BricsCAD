# Work claim — Rebar notation structural read-only result

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-rebar-notation-readonly-result-20260812-0807`
- Registered: `2026-08-12T08:07:00+07:00`
- Baseline main SHA: `b3f561dc4b9de2f70e645008407740103d1e1f26`
- Priority: evidence-driven public parser result ownership during owner-requested `continue all`

## Confirmed defect

`RebarNotationParser.Parse(string)` declares `IReadOnlyList<RebarGroup>` but returns its mutable backing `List<RebarGroup>` directly. A caller can cast the parsed result to a mutable collection and structurally add, remove or clear groups after parsing, despite the public read-only collection contract.

## Reserved scope

- `src/QS3D.Core/Rebar/RebarNotationParser.cs` — return boundary only.
- `tests/QS3D.Core.SmokeTests/RebarNotationReadOnlyResultSmoke.cs` — focused CAD-independent regression.
- this claim file.

## Contract

Return a structural read-only wrapper after the existing parser has fully validated and constructed all groups. Preserve notation length/group bounds, regex grammar, whitespace behavior, diameter/spacing validation, checked quantity multiplication, group ordering and mutable `RebarGroup` row-object semantics. No deep-immutability redesign.

## Coordination

The completed notation finite-bounds lane remains authoritative for parser limits. The active LOCAL-003 gate owns only released smoke reconciliation including `RebarNotationBoundsSmoke.cs`; this lane does not edit that smoke and does not change parser grammar or limits.

## Excluded scope

No schedule arithmetic, shape planning, CAD/native behavior, Level placement, fabrication, release/update or persistence work.

## Validation plan

Parse ordinary compound count + spacing notation, preserve group order/content, require the returned `ICollection<RebarGroup>` to be read-only, and prove structural `Add` throws `NotSupportedException`. Re-fetch current source before write; never force-push. No GitHub Actions dispatch or BricsCAD runtime qualification claim.
