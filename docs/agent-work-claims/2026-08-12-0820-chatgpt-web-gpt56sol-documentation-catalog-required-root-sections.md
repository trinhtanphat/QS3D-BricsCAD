# Work claim — Documentation catalog required root sections

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T08:20:00+07:00`
- Completed: `2026-08-12T08:23:00+07:00`
- Baseline main SHA observed: `c79e8f1588aa0b28475ba7e8b604616cee530880`
- Priority: P1 — fail-closed documentation persistence completeness

## Confirmed defect

The v1 documentation catalog serializer always emits exactly one `<views>` section and exactly one `<sheets>` section, but `ValidateSchema(...)` only enforced *at most one* of each. If either root section was removed from stored metadata, `Load(...)` treated the missing container as an empty collection and could silently accept a lossy/corrupted catalog instead of failing closed.

## Completed contract

1. A format-v1 catalog now requires exactly one `<views>` root child and exactly one `<sheets>` root child.
2. Existing duplicate-section rejection remains through the same exact-one check.
3. Empty but present `<views />` / `<sheets />` remain valid.
4. Optional nested containers (`categories`, `include`, `exclude`, `placements`) keep their existing at-most-one semantics.
5. No format-version, enum-token, save-bound, planner/editor/native behavior change was made.

## Commits

- Claim registration: `354b6668b885fb69ca7f9d1b513fae1693b3625b`
- Planning: `62f830b098837cd0666bedcc5829c0720e80d460`
- Source fix: `4addd1eec0a066bf028287d6522cae0b8a684476`
- Focused smoke regression source: `afd05b956abb1a1db142562f9f83b586e4922f95`

## Validation evidence

- Post-plan source was re-fetched before writing; the two root checks still used `EnsureAtMostOneChild(...)` and the store blob was `5ae5bdb793517850ed563da6fb7c205a2a96aed8`.
- Exact source diff was read back: two root calls changed plus one exact-one helper (`+8/-2`); nested-container calls were untouched.
- Source and smoke commits were verified as ancestors of observed `main` `77e2b8cc94340a8d2b51d95b3afc01060b28526b` with `behind_by: 0`.
- Concurrent commits after the source fix did not modify `SemanticDocumentationCatalogStore.cs`.
- Smoke source uses a real `Save(...)` payload, verifies canonical load, then removes `views` and `sheets` independently and expects fail-closed load.
- GitHub Actions were not dispatched; executable smoke/build PASS and licensed BricsCAD runtime PASS are not claimed.

## Released scope

This lane is complete; the documentation catalog root-cardinality scope is released for other agents. Nested containers and excluded planner/editor/native/licensing/regeneration/XLSX/BOM/interchange/health scopes were not modified.
