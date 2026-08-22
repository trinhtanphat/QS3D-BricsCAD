# QS3D — sanitized local V25 result handoff

Use this file only as a **shareable result template** after testing an exact source SHA on an interactive Windows machine with licensed BricsCAD V25. It does not replace either canonical local instruction set:

- execution/runtime matrix: `docs/LOCAL-V25-QUALIFICATION.md`;
- unresolved physical/engineering/signing work: `docs/LOCAL-AGENT-REMAINING-GATES-2026-08-10.md`.

## Generate the safe starting summary

Run the canonical qualification first:

```powershell
.\scripts\run-local-v25-qualification.ps1 `
  -BricsCadDir "C:\Program Files\Bricsys\BricsCAD V25 en_US" `
  -Profile "QS3D-V25-TEST"
```

The raw report under `artifacts/local-v25-qualification/qualification.json` is **local-only evidence**. It can contain a local username, absolute installation/build paths and raw failure text. Do not commit or paste that raw JSON into GitHub.

Create a sanitized Markdown handoff from the raw report:

```powershell
python scripts/export-local-v25-sanitized-summary.py
```

Default output:

```text
artifacts/local-v25-qualification/qualification-summary.md
```

The exporter intentionally carries only allow-listed result data: exact SHA, validated hashes/statuses, the canonical finite qualification scopes, public-neutral branch identities (`main`, `master`, detached `HEAD`), reviewed neutral release-tag forms, and the fixed canonical step labels declared by the qualification runner. It intentionally omits usernames, machine names, absolute paths, private DWG names/content, screenshots, credentials and raw error messages. Unknown/non-neutral branch, scope or release-tag text is replaced by a fixed redaction/fallback instead of being echoed. An unknown or modified `steps[].name` is replaced with a deterministic `Step N (redacted label)` placeholder until that label is deliberately reviewed and allow-listed.

Before sharing even the sanitized file, read it once and make sure any **manual text you added later** is also sanitized.

## Manual result fields to complete

Copy the generated summary and fill only the fields actually tested on the **same exact SHA**:

```text
Exact SHA: <40-char SHA>
BricsCAD V25 edition/build: <version only; no install path>
Core/static/build gates: PASS | FAIL
V25 adapter build: PASS | FAIL
NETLOAD/runtime probe: PASS | FAIL | SKIPPED
DemandLoad: PASS | FAIL | NOT TESTED
Direct Draw: PASS | FAIL | NOT TESTED
Door/Opening booleans: PASS | FAIL | NOT TESTED
Room/HT_PHÒNG: PASS | FAIL | NOT TESTED
Curtain host + frame: PASS | FAIL | NOT TESTED
Curtain panel-by-panel: PASS | FAIL | NOT IMPLEMENTED
Physical L/T/X wall junction: PASS | FAIL | NOT IMPLEMENTED
Rebar geometry/atomicity: PASS | FAIL | NOT TESTED
Rebar governing standard/revision: <explicit value or NOT QUALIFIED>
Rebar fabrication qualification: PASS | FAIL | NOT QUALIFIED
Save/reopen + multi-DWG: PASS | FAIL | NOT TESTED
Unicode/HiDPI: PASS | FAIL | NOT TESTED
Private-DWG regression: PASS | FAIL | NOT TESTED
Clean install/upgrade/uninstall: PASS | FAIL | NOT TESTED
Authenticode + timestamp: PASS | FAIL | NOT SIGNED
Known blockers: <sanitized text only>
Source fixes committed: <commit SHAs only>
```

## Evidence rules

Safe to share when needed:

- exact Git SHA;
- BricsCAD version/build number without its local path;
- plugin/package SHA-256;
- PASS/FAIL/SKIPPED/NOT TESTED/NOT IMPLEMENTED/NOT QUALIFIED statuses;
- issue/PR/commit IDs;
- anonymized, non-customer-specific blocker descriptions.

Keep local/private:

- BricsCAD proprietary DLLs;
- customer/private DWGs and drawing content;
- usernames, computer names and absolute local paths;
- raw runtime metadata/errors that contain machine or customer details;
- screenshots showing confidential drawings;
- signing private keys, credentials or certificate secrets.

Never turn `FAIL`, `SKIPPED`, `NOT TESTED`, `NOT IMPLEMENTED` or `NOT QUALIFIED` into PASS from source review alone. A `runtimeSkipped=true` automated run cannot qualify a customer release.

GitHub Actions remain manual-only. Running or completing this local handoff does **not** authorize dispatching a workflow or publishing a release.
