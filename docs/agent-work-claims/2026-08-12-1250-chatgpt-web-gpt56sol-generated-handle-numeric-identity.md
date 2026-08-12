# Work claim — Generated handle numeric CAD identity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-generated-handle-numeric-identity`
- Registered: `2026-08-12T12:50:00+07:00`
- Baseline main SHA: `201afc8fba8149b96daac03581b54d1ad139ff69`
- Priority: P0 — generated/source ownership must use the same numeric CAD Handle identity as BricsCAD runtime resolution.
- Task Key: `CORE-GENERATED-HANDLE-NUMERIC-IDENTITY`

## Confirmed defect

BricsCAD `CadHandleService.NormalizeHexHandle(...)` accepts optional `0x`, parses positive hexadecimal identity, and canonicalizes it to uppercase hex without leading zeros before `ResolveOne(...)` creates a numeric `Handle`. Thus `A`, `0A`, and `0xA` resolve to the same CAD object identity.

Core ownership currently keys `GeneratedHandleOwnershipPolicy`, `GeneratedHandleOwnershipIndex`, `SafeGeneratedHandleOwnershipHealthService`, and `GeneratedRebarOwnershipHealthService` by trimmed/case-insensitive raw text. Two semantic/generated owner slots can therefore claim aliases of the same CAD object without being recognized as a conflict; owner lookup can also miss an alias of an existing owner.

## Non-overlap check

Recent history contains ownership slot-isolation, malformed-token, separator-collision, null-safety and structural-identity work, but no numeric-hex alias identity fix. The latest exact collision search returned no `generated handle ownership numeric alias` commit, and there were no open PRs at `main@201afc8fba8149b96daac03581b54d1ad139ff69`.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipPolicy.cs`
- `src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipIndex.cs`
- `src/QS3D.Core/Diagnostics/SafeGeneratedHandleOwnershipHealthService.cs`
- `src/QS3D.Core/Diagnostics/GeneratedRebarOwnershipHealthService.cs`
- one focused Core smoke regression for numeric CAD-handle identity
- this claim file

Do not modify BricsCAD `CadHandleService`, generated-handle persistence spelling, dedicated malformed/canonical handle diagnostics, native CAD generation/replacement, command wrappers, or runtime licensing/build code.

## Intended contract

- Ownership identity canonicalizes only text that maps to a positive signed-`long` hexadecimal CAD handle, accepting optional `0x`, to uppercase hex without leading zeros.
- `A`, `0A`, and `0xA` are the same ownership identity everywhere in the reserved Core paths.
- Invalid, zero, or otherwise non-resolvable handle text is preserved as trimmed text rather than discarded/collapsed, retaining existing malformed-token ownership evidence.
- Case-insensitive and outer-whitespace compatibility remains intact.
- Same semantic owner + same logical owner slot aliases remain allowed; different owners/logical slots fail closed as before.

## Completion condition

Numeric handle aliases resolve/conflict consistently across shared policy/index, global safe ownership health, and rebar ownership health; malformed identity behavior is preserved; focused smoke coverage pins alias lookup/conflict/source-owner/malformed/distinct controls; source + smoke are read back from merged `main`, ancestry is verified, and this claim is closed with exact commit SHAs.
