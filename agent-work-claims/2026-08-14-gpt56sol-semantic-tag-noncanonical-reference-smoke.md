# Work claim — Semantic tag noncanonical reference smoke

- Status: `ACTIVE`
- Agent: `gpt56sol-semantic-tag-smoke-agent`
- Registered: `2026-08-14T19:35:00+07:00`
- Baseline main SHA: `c15c8360f8f9053cbdef9b64d1c6799451be90fb`
- Trigger: release #180 / run `31800673041`, deterministic Core smoke annotation from job `94767636227`

## Evidence

The exact CI annotation identifies `SemanticTagRendererSmoke.NonCanonicalReferencesFailClosed()` as failing because a padded Family reference rendered instead of throwing. `SemanticTagRenderContext.ResolveReference()` still explicitly rejects references whose raw value differs from `Trim()`. The smoke currently assigns padded Family/Floor/Zone values through `ProjectElement` public setters, which canonicalize those values before the renderer sees them.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/SemanticTagRendererSmoke.cs`
- this claim file

## Excluded scope

- `src/QS3D.Core/Documentation/SemanticTagRenderer.cs`
- `src/QS3D.Core/Documentation/SemanticTagRenderContext.cs`
- `src/QS3D.Core/Domain/ProjectElement.cs`
- all production behavior, V25 runtime, #1005 Undo semantics, workflows and release policy

## Planned fix

Keep production validation unchanged. In the smoke only, assert the public relation setters canonicalize padded input, then inject malformed legacy backing values for Family/Floor/Zone via narrow reflection before rendering. Reuse the existing reflection-fixture pattern already used in this smoke for noncanonical owner IDs.

## Completion condition

The focused smoke fixture repair is merged to current `main`, ancestry is verified, and the next exact release run advances past this failure or exposes the next deterministic smoke blocker.