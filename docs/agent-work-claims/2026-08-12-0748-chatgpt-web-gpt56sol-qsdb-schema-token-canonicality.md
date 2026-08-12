# Work claim — QSDB schema token canonicality

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12`
- Baseline main SHA: `3531367f947a9ecc46adf4a280b976b8fa1edd9f`
- Priority: persistence schema identity canonicality

## Confirmed defect

`ProjectSchemaMigrator.ReadSchema(...)` used `int.TryParse(..., NumberStyles.Integer, ...)`, accepting alternate textual representations such as whitespace/sign aliases while ordinary integer parsing also accepted leading-zero aliases. QS3D serialization writes one canonical invariant-decimal schema token.

## Completed contract

1. Schema tokens are now parsed with `NumberStyles.None` and must exactly equal their invariant decimal round-trip representation.
2. Canonical legacy tokens `1` and `2` still migrate normally.
3. Canonical current token `3` remains accepted.
4. Noncanonical aliases such as `03`, `+3`, and ` 3 ` fail closed.
5. Schema version and migration payload behavior are otherwise unchanged.

## Commits

- Claim registration: `05202d07d9eeeac21ec470350af54526a408f4bd`
- Planning: `522d06ff8e3abcc6c81446dbda35fc4dd4ad3f9f`
- Source fix: `1af7ec780aae4b5db55c33f161ce3e32a941325b`
- Focused smoke regression source: `132432a5ce9b17d73fd4ef568d841eb5ece4c2d0`

## Validation evidence

- Exact source diff was read back and changes only `ReadSchema(...)`.
- Source and smoke commits were verified as ancestors of observed `main` `54aed82ce2fb9f34b675c3926b7917764a35ed8f` with `behind_by: 0`.
- Concurrent commits after the smoke did not touch `ProjectSchemaMigrator.cs` or this regression source.
- Smoke source covers canonical current/legacy tokens and the `03`, `+3`, ` 3 ` aliases.
- Regression source was committed but GitHub Actions were not dispatched in this remote session.
- No CI PASS, build PASS, licensed BricsCAD runtime PASS, or release publication is claimed.

## Released scope

This claim is complete; `ProjectSchemaMigrator.cs` is released for other agents.
