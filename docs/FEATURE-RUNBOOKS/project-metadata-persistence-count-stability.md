# Project metadata persistence Count stability

Lane-Key: `issue-4430`

## Purpose

Qualify the deterministic Core boundary used when persisted project metadata is replaced from a caller-provided enumerable. The replacement is atomic: the old metadata remains published until the entire incoming generation, reserved-state validation, and cardinality contract are accepted.

## Contract

For inputs exposing deterministic `Count` through generic `ICollection<KeyValuePair<string,string>>`, `IReadOnlyCollection<KeyValuePair<string,string>>`, or non-generic `ICollection`:

- bind all supported Count surfaces before traversal;
- reject oversized, negative, or conflicting Count evidence before enumeration;
- reject item `N+1` before semantic processing when admitted Count is `N`;
- reject under-yield after traversal;
- re-bind all supported Count surfaces after an exactly-sized traversal;
- reject post-traversal drift, negative, or conflicting evidence before `ValidateReserved(next)` and before `_items` publication;
- preserve the previously published dictionary on every rejection.

Inputs exposing no supported deterministic Count remain streaming inputs and use the independent 10,000-entry ceiling.

## Deterministic validation

Run:

```powershell
python scripts/preflight-project-metadata-persistence-count-stability.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

The registered `ProjectMetadataPersistenceCountSmoke` exercises positive Count drift through each supported Count surface, atomic old-state preservation, stable counted publication, and pure streaming publication. Existing `ProjectMetadataKnownCountOverrunSmoke` continues to pin first-overrun precedence before null/duplicate-key processing.

## Runtime boundary

No licensed BricsCAD runtime is required. This package changes deterministic Core persistence admission only and does not claim `LOCAL_PASS`, private-DWG evidence, or host behavior.
