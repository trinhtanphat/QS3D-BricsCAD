# V25 cloud packaged-sample protected-main drift fence

## Scope

This runbook qualifies the repository/source contract for issue #6013. It does **not** authorize or claim a GitHub release publication, signing operation, licensed BricsCAD runtime run, or `LOCAL_PASS`.

## Defect boundary

`scripts/package-v25.ps1` copies tracked fixtures from `samples/generated/` into the V25 package. A cloud release therefore depends on those bytes just as it depends on production source, package scripts, build inputs, and the release workflow itself.

Before this fix, both `preMutationReleaseRelevantPaths` and `finalReleaseRelevantPaths` in `.github/workflows/release-v25-cloud.yml` omitted `samples/generated/`. A protected-main change to a packaged sample after `SOURCE_SHA` could consequently pass both stale-source drift classifications even though the published package candidate was built from an older sample generation.

## Required source contract

Both persistent release boundaries must include exactly one `samples/generated/` entry in their release-relevant path arrays:

1. the pre-mutation fence before draft prerelease creation; and
2. the final fence immediately before publication.

The existing fail-closed boundaries remain intact: exact source SHA, current-main API/fetch identity, source ancestry, stable-main confirmation, held asset hash/size checks, draft asset round-trip verification, final asset identity, and published-release identity.

## Deterministic qualification

Run from the repository root on the exact candidate SHA:

```text
python scripts/preflight-v25-cloud-packaged-sample-main-drift.py
python scripts/preflight-all.py
```

The focused guard binds the drift classifier to the package script's actual `samples/generated` source root. It also mutation-probes removal of the sample entry from each freshness boundary independently, so either omission must fail.

Shared CI is authoritative for hosted source validation. The protected PR must obtain fresh exact-head `preflight` and `core` success before merge.

## Runtime/publication boundary

Do not dispatch `release-v25-cloud.yml` merely to validate this source change. No hosted/static result proves licensed BricsCAD NETLOAD/native behavior. No release or `LOCAL_PASS` claim is valid unless the separately authorized execution actually occurred and is bound to its exact SHA/artifacts.
