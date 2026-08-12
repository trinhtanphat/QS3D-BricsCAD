# Work claim — Generated handle numeric CAD identity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-generated-handle-numeric-identity`
- Registered: `2026-08-12T12:50:00+07:00`
- Completed: `2026-08-12T13:02:00+07:00`
- Baseline main SHA: `201afc8fba8149b96daac03581b54d1ad139ff69`
- Priority: P0 — generated/source ownership must use the same numeric CAD Handle identity as BricsCAD runtime resolution.
- Task Key: `CORE-GENERATED-HANDLE-NUMERIC-IDENTITY`

## Confirmed defect

BricsCAD `CadHandleService.NormalizeHexHandle(...)` accepts optional `0x`, parses positive hexadecimal identity, and canonicalizes it to uppercase hex without leading zeros before `ResolveOne(...)` creates a numeric `Handle`. Thus `A`, `0A`, and `0xA` resolve to the same CAD object identity.

Core ownership previously keyed `GeneratedHandleOwnershipPolicy`, `GeneratedHandleOwnershipIndex`, `SafeGeneratedHandleOwnershipHealthService`, and `GeneratedRebarOwnershipHealthService` by trimmed/case-insensitive raw text. Two semantic/generated owner slots could therefore claim aliases of the same CAD object without being recognized as a conflict; owner lookup could also miss an alias of an existing owner.

## Completed implementation

- Claim commit: `9aecdf65fd92485d025afca8f4e1b758112853e3`.
- Shared policy source commit: `4ff2d5161a07097af798e0d4cc9517bd6a930390`.
- Ownership-index source commit: `742fe1cee13fdd37e48b81b9675a60d5bc47b01a`.
- Safe-ownership source commit: `9c9f7eee66de5d85f996fada2d8d8f2196f94aa5`.
- Rebar-ownership source commit: `3ce4b055a60d130ce52e0c5b38647dfed4c08c38`.
- Smoke commit: `766c1699cf001060bd9d78458674d01cf870d14c`.
- PR #906 squash merge: `01dfad87e8faf3b50de4db79864def994afe1a78`.
- Merged policy blob: `d861f87742b998dec0b1fc16c4eab3a6288e8ddb`.
- Merged index blob: `834ea3b3796483e349a072e27d8f4b9ea5c06211`.
- Merged Safe-ownership blob: `ab3e68fec2c7687bb2e3d840bc324c28ba09415c`.
- Merged Rebar-ownership blob: `d301c809b03f59a0df35ec3e0b8736356df1c526`.
- Merged smoke blob: `77a6719f3411fe9f9a0f226fb97f74808450c55a`.
- `main` readback immediately after merge was `01dfad87e8faf3b50de4db79864def994afe1a78`, so the authoritative merge is the verified current ancestor/root of that snapshot.

## Final contract

- Ownership identity canonicalizes only text that maps to a positive signed-`long` hexadecimal CAD handle, accepting optional `0x`, to uppercase hex without leading zeros.
- `A`, `0A`, and `0xA` are the same ownership identity in shared Policy/Index, global Safe ownership health, and Rebar ownership health.
- Invalid, zero, or otherwise non-resolvable handle text remains trimmed textual identity rather than being discarded/collapsed, retaining malformed-token conflict evidence.
- Case-insensitive and outer-whitespace compatibility remains intact.
- Same semantic owner + same logical owner slot aliases remain allowed; different owners/logical slots fail closed as before.
- SourceHandles and generated owner slots now use the same numeric CAD-handle identity in global ownership health.

No GitHub Actions were dispatched. No full local .NET build PASS, executable smoke PASS, or BricsCAD V25/V26 runtime PASS is claimed for this lane.
