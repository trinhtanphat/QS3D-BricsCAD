# LOCAL V25 — Update Center failure qualification

Status: `SOURCE_READY / NO_RESULT — LOCAL_RUNTIME_REQUIRED`

This runbook qualifies the exact candidate carried by Issue #5155 / Lane-Key `issue-5155`. Remote/static CI may prove source shape and V25 compilation, but it does **not** prove licensed BricsCAD runtime behavior. Do not publish `LOCAL_PASS` unless every claimed cell was actually exercised on the exact fetched SHA and the evidence records that SHA plus the built DLL SHA-256.

## Artifact boundary

Before running any cell:

1. Fetch the canonical #5155 branch/PR candidate without modifying it.
2. Record `git rev-parse HEAD` as `SOURCE_SHA` and confirm it is the exact candidate requested by the Issue/PR handoff.
3. Build `src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj` for `Release|x64` against the repository-approved locked BricsCAD V25 references.
4. Record BricsCAD version, Windows version, plugin DLL path and SHA-256.
5. Load only the resulting exact artifact into a clean BricsCAD V25 session. A rebuild, checkout change or DLL replacement invalidates the remaining matrix until identity is recorded again.

The runtime operator may use safe local fault injection/harness controls already approved by the repository or operating environment. Do not patch production source, weaken update authenticity rules, redirect the updater to an untrusted repository, or manufacture an exception by corrupting user data that would make the test non-representative.

## Required invariants

Across all cells, user-visible Editor/WPF text must remain stable and actionable and must not expose exception messages, stack traces, local paths, registry internals, HTTP client/parser detail or host exception types. Failures must not escape into BricsCAD command processing. Existing updater safety stays intact: HTTPS/repository/tag/asset checks, signed-manifest requirement, signer/thumbprint checks, SHA-256 validation, bounded manifest parsing, generation freshness and current-release channel behavior must not be bypassed.

## Matrix

| Cell | Runtime scenario | Expected result |
|---|---|---|
| UF01 | Run `QS3DUPDATE` with normal Update Center host creation. | Window opens normally; no regression in command alias behavior. |
| UF02 | Inject a host/window creation failure for `QS3DUPDATE`. | Command reports the stable Update Center failure message; no raw host exception detail escapes; BricsCAD remains usable. |
| UF03 | Repeat UF02 through alias `QSUPDATE`. | Same redacted/fail-isolated behavior as `QS3DUPDATE`. |
| UF04 | Run `QS3DVER`/`QSVER` normally. | Product/build/ABI/location information renders as before and aliases remain functional. |
| UF05 | Inject a failure inside version reporting or Editor reporting. | Stable version failure is attempted; secondary reporting failure is swallowed and does not escape command processing. |
| UF06 | Toggle `InstallOnExit` with normal writable HKCU preference storage. | Preference persists and can be read back without changing unrelated updater state. |
| UF07 | Make the updater preference key unavailable/unwritable for the test account without changing production source. | UI receives the stable registry preference failure; no registry path/native exception detail is shown; previous/default preference remains effective. |
| UF08 | Perform a normal GitHub release check with network available. | Existing channel/prerelease selection and result publication behavior remains intact. |
| UF09 | Cause release-list network failure/timeout while Update Center is active. | Result becomes the stable check failure with actionable reconnect guidance; raw network/HTTP/parser exception detail is absent; current running QS3D is unchanged. |
| UF10 | Exercise stale lifecycle: start a check, stop/restart coordinator or otherwise advance generation before the old result publishes. | Stale result does not overwrite the current generation; redaction changes do not weaken generation freshness. |
| UF11 | Cause manifest download/JSON parsing failure after a signed-manifest release candidate is selected. | Auto-update remains blocked and UI shows the stable manifest-probe failure; no parser/network exception detail is exposed. |
| UF12 | Exercise a valid eligible manifest/package after recovery, then separately test a manifest with wrong repository/tag/asset, signer, SHA-256 or schema. | Valid candidate reaches existing eligible/update-available path; each invalid candidate remains fail-closed/manual-only as appropriate. Redaction must not weaken authenticity or bounded-parse gates. |

## Evidence record

For each executed cell record: `SOURCE_SHA`, plugin DLL SHA-256, BricsCAD V25 version, Windows version, cell ID, sanitized setup/fault-injection description, observed stable message/state, and `PASS` / `FAIL` / `NO_RESULT`. Screenshots/log excerpts must be sanitized of machine/user secrets but must retain enough state to prove the expected behavior.

A valid runtime completion statement is bounded to the exact tested artifact, for example `LOCAL_PASS: UF01-UF12 on SOURCE_SHA <sha>, DLL SHA-256 <hash>, BricsCAD V25 <version>`. If any required cell was not executed, report `NO_RESULT` or a bounded partial result instead of broad `LOCAL_PASS`.

## Remote/source acceptance boundary

The repository-safe portion is complete when production changes, the deterministic `scripts/preflight-update-failure-redaction.py` guard, this matrix and protected current-candidate CI are green and merged. Licensed V25 execution remains a separate local evidence step and is not a merge prerequisite unless the active Issue/PR explicitly changes acceptance.
