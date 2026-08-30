# V25 Update Center preview — licensed local qualification

Issue: #4675  
Lane-Key: `issue-4675`  
Status: `REMOTE_COMPLETE / READY_FOR_LOCAL_QUALIFICATION`

## Candidate

Use a published V25 preview whose source is a descendant of merge commit `757bf2a1af4567938984131c6a74f48a0e15aa7d` (PR #4734) and includes the bounded release-pagination fix from PR #4754. `v0.1.0-preview.10258` is the first known published candidate satisfying both source requirements.

Hosted CI, source guards, compile checks, package hashing, and release publication are not a `LOCAL_PASS`. The checks below must be executed in a licensed BricsCAD V25 host using the exact candidate package.

## Required evidence

Record before testing:

- release tag and source SHA;
- installed `QS3D.BricsCAD.V25.dll` path and SHA-256;
- installed `QS3D.Core.dll` path and SHA-256;
- BricsCAD ProductVersion and exact `bricscad.exe` path;
- disposable DWG fixture name;
- Update Center current/latest version text;
- sanitized updater log path under `%LOCALAPPDATA%\QS3D\UpdateLogs`.

Do not publish private DWG content, credentials, tokens, or unrelated machine state.

## Qualification matrix

1. **Discovery / pagination** — open Update Center, run discovery, and confirm the latest eligible V25 preview is selected without the historical 200-release bound failure or a raw GitHub exception.
2. **Coherent eligibility UI** — with a release containing `QS3D-BricsCAD-V25.zip` plus `.sha256`, confirm the state card and primary action agree that the verified preview is eligible for user-initiated one-click update; current → latest versions must be visible.
3. **Update-on-close preference** — from a clean/default preference state, verify `Update khi đóng` is OFF; toggle and reopen to verify persistence, then restore OFF before the explicit one-click test unless that path is being tested.
4. **Download progress** — start `Tải & cài đặt`; confirm byte progress changes during transfer and stage text/percent continues through verification and staging; package SHA-256 must be accepted before installation is scheduled.
5. **Graceful host handoff** — save a disposable DWG first; confirm updater requests normal BricsCAD shutdown rather than killing the process and the detached worker waits for the exact parent process to exit before replacing payload.
6. **Apply / installed-hash validation** — confirm `QS3D.BricsCAD.V25.dll` and `QS3D.Core.dll` are replaced only after staged hashes validate; installed hashes must equal expected staged hashes and rollback backups must remain verifiably usable until successful completion.
7. **Automatic restart** — confirm the worker restarts the exact captured `bricscad.exe` path, BricsCAD opens normally, and QS3D loads from the expected V25 install directory. Normal BricsCAD session restoration is intended; the updater must not force-open an individual DWG.
8. **Post-update identity** — reopen Update Center after restart, confirm current version equals the installed preview with no stale same-tag update state, and record installed DLL SHA-256 values again.
9. **403 / rate-limit UX** — only with an authorized non-destructive harness or naturally occurring rate limit, exercise the 403 path; bounded recent-snapshot reuse may occur only under its freshness rules, otherwise actionable Vietnamese retry/rate-limit text must replace raw `(403) Forbidden` output.
10. **Failure / rollback probe** — only in an authorized disposable setup, induce an apply failure without damaging the production install; verify fail-closed behavior, validated backup restoration when replacement had started, best-effort BricsCAD recovery restart, and retained diagnostic log.

## Acceptance

Mark #4675 `LOCAL_PASS` only when the same exact candidate passes all applicable normal-path cells and authorized failure/rate-limit probes with:

- verified download and installed hashes;
- meaningful progress/status UI;
- coherent preview eligibility text/action;
- default-OFF update-on-close behavior;
- graceful host close and exact-host restart;
- no cross-version or cross-path install;
- fail-closed rollback behavior;
- no raw 403 diagnostic;
- sanitized evidence attached to the canonical issue.

If any cell fails, keep #4675 open, attach sanitized evidence, and reserve the smallest source fix under the existing issue/lane protocol before changing updater code.
