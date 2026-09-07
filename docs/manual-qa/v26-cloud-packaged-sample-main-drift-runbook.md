# V26 cloud packaged-sample protected-main drift fence

Issue: #6016  
Lane: C05 CI / Release Gates  
Lane-Key: `issue-6016`  
Ownership-Key: `release.v26-cloud.packaged-sample-main-drift-v1`

## Product boundary

The V26 package script copies tracked synthetic project fixtures from `samples/generated/` into the held cloud candidate. Therefore a protected-main change under that root after the candidate SHA is release-relevant to the bytes/fixtures represented by the qualified package and must invalidate the workflow-level final publication admission.

The final V26 cloud classifier now includes `samples/generated/` alongside source, tests, scripts, external/build and workflow inputs. Existing ancestry, exact protected-main fetch, fail-closed `git diff`, and second protected-main identity confirmation remain unchanged.

## Deterministic regression

Run:

```text
python scripts/preflight-v26-cloud-packaged-sample-main-drift.py
```

The guard proves all of the following from repository source:

- `package-v26.ps1` binds and consumes `samples/generated/`;
- `$finalReleaseRelevantPaths` contains exactly one active packaged-sample entry;
- the final `git diff` actually uses that bounded classifier;
- protected-main stability confirmation remains present after classification;
- removal, commenting, or duplication of the sample entry fails closed.

The script is auto-discovered by `scripts/preflight-all.py` and therefore participates in normal shared `preflight` validation.

## Evidence boundary

This package is REMOTE_SAFE source completion. Shared CI can prove source shape, deterministic mutation coverage, workflow syntax/policy admission, Core smoke and applicable compile validation for the exact candidate SHA.

It does **not** dispatch `release-v26-cloud.yml`, publish or sign a release, establish licensed BricsCAD runtime behavior, or create `LOCAL_PASS`. Those claims require their separately authorized execution environments and exact-SHA evidence.
