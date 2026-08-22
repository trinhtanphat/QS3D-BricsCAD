# Work claim — Interchange v1 unknown-member refusal

- Status: `COMPLETED`
- Agent: `/root/fix_curtain_method_gates`
- Registered: `2026-08-14T16:03:05+07:00`
- Baseline main SHA: `968761f9cf97850cb3e43f3b5e009e04b7765f07`
- Issue: `#84`
- Priority: remote-safe semantic interchange integrity

## Verified gap

`ProjectInterchangeJsonValidator.Validate` uses `DataContractJsonSerializer`, which ignores JSON members absent from the private v1 contracts. A canonical v1 snapshot with an added root or nested object member therefore returns `IsValid = true`, and `ProjectInterchangeValidatedSnapshotReader.Read` succeeds while silently dropping the unrepresented member. Duplicate known members and trailing tokens already fail parsing; the bounded gap is unknown object-member loss at the exact-version validation boundary.

No open PR or active exact claim owns unknown interchange JSON members. The completed Unicode validator lane covers malformed UTF-16/UTF-8 only and is independent.

## Reserved scope

- `src/QS3D.Core/Export/ProjectInterchangeJsonValidator.cs`: inspect the allowed member sets of the seven private v1 object contracts and emit deterministic `JSON_UNKNOWN_MEMBER` errors at their object paths before semantic validation.
- `tests/QS3D.Core.SmokeTests/ProjectInterchangeJsonUnknownMemberSmoke.cs`: self-register canonical acceptance, root/nested unknown-member refusal, typed-reader refusal, and declared property/quantity dictionary extension preservation.
- this claim document for closeout only.

## Preserved contracts and exclusions

- Declared Family/element `properties` and element `quantities` dictionary keys remain supported data and are not schema members subject to this guard.
- Preserve exact v1 format/version, strict Unicode, size/object-graph limits, existing validation codes/order, typed-reader delegation, and all import policy/ownership behavior.
- No reader/importer/schema migration, native/runtime/UI/LOCAL automation, private data, release/signing, or GitHub Actions changes.
- Validate focused interchange gates, Core `Release` build, and full Core smoke; report any independent blocker without expanding.

Completion means the bounded validator/smoke fix is merged through normal PR, this claim is closed, and exact merged-main SHAs are returned to `/root`.

## Outcome

- Merged validator/smoke fix: PR `#1266`, main SHA `ac8abeea0d1c8e7408dfbe11376a31d87a974636`.
- The validator now inspects the already-bounded UTF-8 JSON through the platform JSON shape reader and applies exact allowed-member sets at root, units, project, Zone, Floor, Family and Element object paths before semantic validation. Unknown members produce `JSON_UNKNOWN_MEMBER`; the typed reader remains unchanged and rejects through its existing validator delegation.
- The registered smoke and a direct Core invocation proved all seven object paths reject unknown members while arbitrary declared Family/element `properties` and element `quantities` keys remain valid and survive typed reading.
- Core and smoke-project `Release` builds passed with 0 warnings and 0 errors. Focused interchange JSON, validation, canonical-validator, validated-reader and element-property-portability preflights passed.
- Full Core smoke advanced through this initializer and stopped at the independent `QsdbRelationIdentityCanonicalSmoke` stale relation-normalization fixture; no scope expansion was made.
- No reader/importer/schema migration, native/runtime/UI/LOCAL automation, private data, release/signing, or GitHub Actions surface changed.
