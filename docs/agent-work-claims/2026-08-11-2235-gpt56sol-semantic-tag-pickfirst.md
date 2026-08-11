# Work claim — Semantic Tag PICKFIRST

- Status: `BLOCKED`
- Agent: `chatgpt-gpt56sol-semantic-tag-pickfirst-20260811-2235`
- Registered: `2026-08-11T22:35:00+07:00`
- Source merged: `2026-08-11T22:47:00+07:00`
- Updated: `2026-08-11T22:54:00+07:00`
- Baseline main SHA: `caccb67982d751ad0c827199a7d8a6bab6ec79cf`
- Merge PR: `#509`
- Merge SHA: `c825bb05ffc65acd0263e0df99239f02913db220`
- Priority: reduce repeated CAD picking in selection-first documentation authoring

## Remote/source status — DONE

- `QS3DTAG` and `QS3DTAGREFRESH` now declare `CommandFlags.Modal | CommandFlags.UsePickSet`.
- Exactly one implied/PICKFIRST entity is consumed directly through `EntitySnapshotReader.ReadCurrentSelection(document)`.
- Zero implied selections preserve the existing explicit `Editor.GetEntity(...)` fallback.
- Multiple implied selections fail closed before canonical project binding instead of choosing an arbitrary source.
- `QS3DTAG` still completes source selection and placement before `ExistingProjectMutationContext.Require(...)` and revalidates the canonical source owner before native build.
- `QS3DTAGREFRESH` still completes source selection before canonical bind/native rebuild.
- Generated QS3D output remains invalid as Semantic Tag source; authoritative-source-only ownership is unchanged.
- No `GetOrCreate`/project bootstrap path was introduced.
- Static contract: `scripts/preflight-semantic-tag-pickfirst.py`.
- Focused handoff detail: `docs/SEMANTIC-TAG-PICKFIRST-2026-08-11.md`.

## Only remaining blocker

Repository handoff policy requires the matching canonical local item to be updated in the same source/docs lane. `LOCAL-006 — native documentation objects` already owns Semantic Tag V25 qualification, but it predates this PICKFIRST behavior and does not yet include the exact new delta.

The available GitHub connector can replace whole files but has no bounded hunk/patch writer. `docs/LOCAL-AGENT-INBOX.md` is large and actively changing; replacing it from truncated/partial reads could delete concurrent evidence. Therefore this claim remains `BLOCKED` rather than falsely `COMPLETED`.

A patch-capable/local writer must update only `LOCAL-006`, preserving all existing evidence, to add:

- `QS3DTAG` / `QS3DTAGREFRESH`: exactly-one PICKFIRST source must skip the second source picker;
- zero implied selection must preserve explicit picker fallback;
- multiple implied selections must fail closed before canonical bind/native mutation;
- generated output must remain rejected as Tag source;
- source-picker ESC and placement ESC remain no-bind/no-residue where already required;
- active-DWG/document-switch behavior remains fail closed under the existing LOCAL-006 lifecycle matrix;
- exact tested SHA/evidence must remain sanitized.

After that bounded canonical inbox edit, mark this claim `COMPLETED`. V25 execution itself remains `PENDING_LOCAL / DO_NOT_RETRY_REMOTE` until real evidence exists.

## Coordination

The source implementation surfaces are **released**; this blocked close-out reserves only the bounded `LOCAL-006` inbox delta and this claim status. Future source work may modify Semantic Tag commands under a new claim, but must preserve or update this pending local matrix.
