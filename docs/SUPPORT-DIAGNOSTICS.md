# QS3D support diagnostics

Updated: 2026-08-10 (UTC+7)

## Command

`QS3DSUPPORTBUNDLE`

This command exports a small UTF-8 text report intended for customer support and runtime triage.

## Included by design

- QS3D plugin product/assembly version;
- QS3D Core product/assembly version;
- loaded BrxMgd / TD_Mgd assembly versions;
- process/OS x64 state and interactive-session state;
- QS3D project schema version;
- counts for Zone, Floor, Family and semantic Element objects;
- dirty semantic element count;
- whether a drawing fingerprint is recorded, as a boolean only;
- semantic element counts grouped by `ElementCategory`.

## Excluded by design

The default bundle must not contain:

- DWG file name or path;
- source CAD handles;
- generated CAD handles;
- semantic element IDs;
- Project ID/name;
- Family names/IDs;
- project metadata values;
- user name;
- machine name;
- private drawing geometry/content;
- license/signing secrets.

The output path chosen by the user is displayed in the BricsCAD command line after export, but that path is not written into the bundle itself.

## Support workflow

1. Run `QS3DRUNTIMECHECK` first.
2. Run `QS3DHEALTHALL` / the relevant domain health command when the issue concerns model state.
3. Run `QS3DSUPPORTBUNDLE` and save the report outside private project folders when practical.
4. Inspect the report before sharing it.
5. If runtime reproduction requires a private DWG, keep that file local and report only sanitized findings/commit IDs back to GitHub.

`QS3DSUPPORTBUNDLE` is a diagnostic aid, not a release qualification command. Exact-SHA V25 qualification still follows `docs/LOCAL-V25-QUALIFICATION.md`.
