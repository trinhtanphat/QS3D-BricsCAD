# V26 package held-input file identity

## Scope

This runbook covers the repository-safe V26 package-construction boundary in `scripts/package-v26.ps1`. It does not qualify licensed BricsCAD runtime behavior, signing, publication, update rollout, or customer/private DWGs.

Canonical carrier: issue #5689 / Lane-Key `issue-5689`.

## Defect

The prior held-input contract kept a read stream open across package consumers and rechecked the pathname, length, and last-write timestamp. That closes pathname reopen/copy races, but metadata equality is not a stable file-generation identity. A same-path replacement can preserve length and timestamp during the pre-open window and cause the held stream to refer to a different generation while metadata-only admission still succeeds.

## Required invariant

Every held package input on Windows must be bound to the currently admitted pathname by native file identity in addition to the existing path/reparse/length/timestamp checks.

The implementation uses `GetFileInformationByHandle` on the long-lived held `FileStream.SafeFileHandle`, recording the volume serial plus file-index high/low identity. Before the held input is returned to a consumer, the current admitted pathname is independently opened with read-only sharing, its handle identity is read, and exact identity equality is required. `Assert-HeldPathBinding` repeats the same proof at later semantic/copy boundaries.

Failure to obtain native identity, a non-Windows execution environment, pathname safety refusal, or identity mismatch fails closed. There is no metadata-only fallback.

## Preserved behavior

The change does not alter package contents or release qualification semantics. It preserves:

- ordinary/non-reparse repository input and output checks;
- held-stream copying of build artifacts and synthetic samples;
- bounded strict-UTF8 source/project reads;
- held transformer/source-script generations;
- staged V26 plugin/Core identity and Authenticode/version validation;
- command discovery, package metadata, README, SHA256SUMS and ZIP creation;
- separation from V25 packaging and from V26 signing/publication/runtime qualification.

## Deterministic regression

Run:

```text
python scripts/preflight-v26-package-held-input-file-identity.py
```

The auto-discovered guard requires native handle identity capture, current-path identity comparison before authority is granted, repeated identity proof at later path-binding checks, and fail-closed ordering. Mutation probes reject removal of the identity primitives or either admission/revalidation call.

Existing V26 package held-generation guards must remain green as compatibility evidence that the stronger identity check did not reintroduce pathname reopen/copy shortcuts.

## Acceptance

Repository acceptance requires the focused guard plus the normal exact-head Shared CI. The final candidate must have fresh protected `preflight` and `core` success after any latest-main reconciliation, then merge through the protected PR path with expected-head identity and verify the resulting protected main.

No hosted/static result from this carrier may be reported as licensed BricsCAD `LOCAL_PASS` or as an actual signed/published V26 release.
