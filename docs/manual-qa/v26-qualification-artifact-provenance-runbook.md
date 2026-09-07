# V26 qualification artifact provenance

Issue: #6006  
Lane-Key: `issue-6006`

## Boundary

This source-safe package makes the manual BricsCAD V26 workflow emit a deterministic manifest beside its plugin/Core payload. The manifest binds the artifact to the exact workflow source commit, payload SHA-256/length identities, and the already-admitted held V26 host-reference generation.

It does **not** create a V26 release channel and it does **not** establish licensed runtime PASS. `RuntimeEvidence=present` means only that the runtime artifact directory contained files when packaging occurred; those files remain subject to their own licensed acceptance contract. `RuntimeEvidence=absent` is a valid source/build qualification artifact.

## Required manifest contract

The manifest is strict UTF-8 JSON with `Version=1`, canonical lowercase 40-hex `SourceCommit`, `EvidenceClass=V26_SOURCE_BUILD_QUALIFICATION`, and `RuntimeEvidence` exactly `present` or `absent`. It contains exactly one payload record for `QS3D.BricsCAD.V26.dll` and `QS3D.Core.dll`, plus exactly one host-reference identity for each of `bricscad.exe`, `BrxMgd.dll`, `TD_Mgd.dll`, and `TD_MgdBrep.dll`.

Payload records contain byte length and lowercase SHA-256. Host-reference records intentionally omit machine-local absolute paths while retaining length/SHA-256 identity copied from the held-state admission file. Before packaging, the workflow re-verifies that held host state against the live V26 installation.

## Source validation

Run:

```powershell
python scripts/preflight-v26-qualification-artifact-provenance.py
python scripts/preflight-all.py
```

On a V26-qualified host, the manual workflow additionally creates the manifest after build/runtime handling, validates the emitted manifest against the exact plugin/Core files and `${{ github.sha }}`, and uploads it with the existing qualification payload.

## Fail-closed controls

Reject noncanonical or whitespace-normalized source SHA, missing/reparse required payload, malformed/oversized host-state JSON, duplicate/missing payload or host-reference identities, noncanonical SHA-256, nonpositive host lengths, payload length/hash drift, source mismatch, and unknown runtime classification.

No hosted/static result produced by this package may be reported as `LOCAL_PASS`.
