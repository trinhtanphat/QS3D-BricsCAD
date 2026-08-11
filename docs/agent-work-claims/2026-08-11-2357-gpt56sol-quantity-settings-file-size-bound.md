# Work claim — Quantity Settings pre-deserialization file-size bound

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-settings-file-size-20260811-2357`
- Registered: `2026-08-11T23:57:00+07:00`
- Completed: `2026-08-12T00:02:00+07:00`
- Baseline main SHA observed: `4e15f8f1b7664aa31779a48f70af61b93665fba6`
- Priority: P1 — close the remaining pre-deserialization memory-amplification gap on machine/imported Quantity Settings JSON.

## Confirmed defect

`QuantityCalculationSettings` rejects oversized rule collections during Clone/validation, but `QuantitySettingsStore.ReadAndValidate(path)` previously invoked `DataContractJsonSerializer.ReadObject(stream)` before those Core cardinality checks. An oversized JSON file could therefore force deserialization/allocation of a very large raw object graph before validated collection limits got a chance to fail closed.

## Delivered scope

- `src/QS3D.BricsCAD.V25/Services/QuantitySettingsStore.cs`
- `scripts/preflight-quantity-settings-recovery.py`
- `scripts/preflight-quantity-settings-file-size.py`
- this claim file

## Implemented contract

- Added a 32 MiB maximum Quantity Settings JSON size. The ceiling was deliberately revised upward from the initial 8 MiB estimate so it stays comfortably above the serialized envelope expected from the already-valid 65,536 directed-rule ceiling.
- `ReadAndValidate()` opens the exact file stream, checks `stream.Length`, and only then constructs/uses `DataContractJsonSerializer.ReadObject(...)`.
- Oversized primary settings surface as ordinary `InvalidDataException`; existing `Load()` backup fallback therefore remains available, while future-schema failures remain separately marked/fail-closed.
- `CanRotatePrimaryIntoBackup()` reuses `ReadAndValidate()`, so an oversized primary is non-rotatable and cannot overwrite a last-known-good backup.
- `WriteAtomic()` now checks the serialized temp stream length after `Flush(true)` and before any `File.Replace`/`File.Move`, preventing the store from committing a settings file its own reader would reject.
- No pre-read materialization (`ReadAllBytes`, `ReadAllText`, `ReadToEnd`, `MemoryStream`) was introduced.

## Regression coverage

- Existing recovery preflight now pins `File.Open -> stream.Length guard -> serializer -> ReadObject -> future-schema check -> NormalizeAndValidate` ordering.
- The same recovery preflight pins `WriteObject -> Flush(true) -> stream.Length guard -> atomic replacement` ordering.
- New `preflight-quantity-settings-file-size.py` independently protects the 32 MiB ceiling, pre-deserialization ordering, writer symmetry, backup fallback markers and no-pre-read boundary.

## Product integration

- Claim registration: `c98dfcf5813ccc3ed53bb2a8f999080a8459170f`.
- Claim ceiling reconciliation: `eb76c91274085ab510aa817d7717518ac1b452b9`.
- PR: `#549` — `fix(quantity): bound settings JSON before deserialize`.
- Squash merge on `main`: `4733f5e7c0c507c189388446deef97cc7343905c`.

## Validation actually performed

- Re-fetched current store and recovery preflight before editing and preserved the existing primary/backup/future-schema contracts.
- PR #549 was squash-merged without force push while `main` was moving concurrently.
- Source/static review only in this remote session; the preflights were not executed from a repository checkout, so no execution PASS is claimed.
- No GitHub Actions or release workflow was dispatched. No licensed BricsCAD V25 runtime PASS is claimed.

## Coordination

Earlier Quantity Settings recovery/LKG/future-schema, cardinality, clone-cardinality and diagnostics claims are completed. No WPF/Core arithmetic, project persistence, CAD geometry, Ribbon/Start Center, updater or release files were modified.

## Completion

Reservation released. Quantity Settings files now fail on an exact-stream byte ceiling before JSON object-graph deserialization, serialized outputs are checked before atomic commit, and backup/future-schema behavior remains intact.
