# Work claim — Generated ownership XData identity hardening

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-generated-ownership-xdata-identity`
- Registered: `2026-08-11`
- Completed: `2026-08-11`
- Baseline main SHA: `8af68ec758825341113d0d70ae3f01e994ff6c2b`
- Priority: make generated native ownership XData identities bounded ASCII tokens while preserving legacy raw-marker compatibility

## Confirmed defect

`ProjectState.ProjectId` and `ProjectElement.Id` accept arbitrary non-empty trimmed strings; they are not constrained to GUID/ASCII identifiers. Generated ownership writers placed raw `ProjectId` and raw semantic element IDs into `DxfCode.ExtendedDataAsciiString` values, creating a host-sensitive representation boundary for otherwise valid Core identifiers.

The repository already contained the compatibility precedent in `ProjectOwnedNativeTableArtifactService`: `p1:` + SHA-256(UTF-8(trimmed ProjectId)) with token-or-legacy matching. This lane applied that model to generated native ownership without changing Core ID semantics or touching the Table/Reporting implementation.

## Implemented contract

- Added `GeneratedOwnershipIdentityToken` with project token prefix `p1:` and element token prefix `e1:`.
- Tokens are lowercase SHA-256 hex over UTF-8 bytes of trimmed identity text, producing bounded ASCII values.
- Rebar, Curtain Frame, Curtain Panel and general Generated Geometry XData writers now store project/element tokens rather than raw identity strings.
- All four readers use token matching first and retain case-insensitive raw legacy fallback, preserving existing DWG ownership markers.
- Existing RegApp names, ownership version `1`, owner-slot/category checks, destructive-operation fail-closed behavior and semantic metadata remain unchanged.
- `GeneratedGeometryService.CommitReplacement` intentionally continues storing raw project/element IDs in `ProjectElement.Properties`; this is semantic application metadata, not the XData host boundary being hardened.
- Core `ProjectId` / `ProjectElement.Id` formats were not restricted or migrated.
- `ProjectOwnedNativeTableArtifactService.cs` was not modified.

## Merged commits

- `37d8b60e9984e58596b8728483378542dae350ab` — `chore(agent): claim generated ownership XData identity`
- `a205ab2b0728007986399c6ee6727f980ce156f6` — `feat(cad): add generated ownership identity tokens`
- `aa31a32e172e7adf1719ee2b93f1a467535bce21` — `fix(rebar): tokenize generated ownership XData identities`
- `f2f78a4b0cc4739ec4add9b9f185996b090e344c` — `fix(curtain): tokenize frame ownership XData identities`
- `8fa88f1c9128d6f37f1c02b7787cffc63533ba1d` — `fix(curtain): tokenize panel ownership XData identities`
- `0a289c7aba7c440a79994bdd060b32d504ac7efe` — `fix(cad): tokenize generated geometry XData identities`
- `8d93c23c1088b37e73d4ee8110d7de1b2aad4e92` — `test(cad): lock generated ownership XData identity tokens`

## Focused regression gate

Added `scripts/preflight-generated-ownership-xdata-identity.py` requiring:

- `p1:` / `e1:` versioned prefixes;
- SHA-256 + UTF-8 digest construction;
- token-or-legacy matching in the shared helper;
- token writer + matcher calls in all four generated ownership paths;
- absence of the previous raw identity XData writer patterns;
- preservation of existing RegApp/version/read boundaries.

## Validation performed

- Refetched the live `main` target sources immediately before writes.
- Refetched the committed helper from live `main` and confirmed digest/token + legacy fallback behavior.
- Refetched the committed Rebar, Curtain Frame, Curtain Panel and Generated Geometry ownership code from live `main` and confirmed token writers/readers were not overwritten by concurrent agents.
- Refetched the committed focused gate from live `main`.
- Confirmed the BricsCAD project is SDK-style `Microsoft.NET.Sdk.WindowsDesktop` targeting `net48`, so the new `.cs` helper is automatically included without a `.csproj` edit.

## Validation not claimed

- Did not run the Python gate locally in this remote session because there is no usable repository checkout/runtime path in the current container.
- Did not run a BricsCAD V25 build or licensed Windows UI runtime.
- Did not dispatch or claim GitHub Actions.

## Excluded scope preserved

- No `ProjectOwnedNativeTableArtifactService.cs` change.
- No Core ID schema/validation change.
- No Floor/Level/vertical-placement change (`LOCAL-003`).
- No schedule/revision viewer, Quantity, Ribbon/Start Center, Create Similar, Room or Documentation #77 change in this claim.
- No local inbox, release, CI or Actions mutation.

## Completion

Generated native ownership XData now uses bounded versioned project/element identity tokens while remaining backward-compatible with legacy raw ownership markers. Source-level verification and regression coverage are merged; native V25/runtime execution remains explicitly unclaimed.
