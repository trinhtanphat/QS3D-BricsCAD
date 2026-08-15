# Work claim — ProjectZoneService XML current-main recovery

- Status: `RELEASED` — implementation complete; pending authorized review/integration
- Agent: `chatgpt-gpt56sol-zone-service-xml-main-recovery-20260815`
- Registered: `2026-08-15T11:35+07:00`
- Exact main baseline: `0bf036c49fa7efdc04745f8a2af57e390d2b8cd7`
- Latest reconciled main: `a45759d7d9b7d90d63ad32f9e8bc4997e5c14d9a`
- Issue: `#1469`
- Replacement PR: `#1594`
- Historical reviewed PR: `#1470`
- Branch: `agent/chatgpt-gpt56sol/zone-service-xml-main-recovery-20260815`
- Priority: Core P1 persistence / failure atomicity

## Recovered defect

`ProjectZoneService.Required(...)` accepted XML-illegal UTF-16 after required/trim/length/control validation. With the current public ZoneDefinition XML-safe boundary, `Update(...)` could call `project.Touch()` before the later Zone setter rejected such text.

The recovery restores only the reviewed `System.Xml` import and `XmlConvert.VerifyXmlChars(...)` guard in `Required(...)` while preserving current assignment/freshness/reference behavior.

## Evidence

- claim-only: `71fc7e9be9438a6f0efdb090d497cdd91007f25a`
- reviewed implementation overlay: `03407354e6ed17dffcf837591ac992ff2e370dc7`
- first non-force reconciliation: `e1f90677cccc5f93c440e017e6d9cd1615ecafee`
- latest non-force refresh: `fe6379b6b5572bf5d2ad52dcb84997fab3a24ec2`
- PR: `#1594` — ready for review; latest readback `mergeable=true`
- task diff: exactly four files; production source delta `+9/-0`
- reviewed source/smoke/registration blobs: `5da8ea778b4ff5444543f53c55a8e41032bc2978` / `d88f53324217773a1a7a60355cd21f549d6dcd32` / `f3e5444f51e582451bee5a48ed189165ec1f7b1c`
- exact GitHub source/diff readback: PASS
- managed build/smoke: **NOT_RUN** — no `dotnet` available; no PASS claimed
- BricsCAD runtime: not applicable to this Core-only lane
- GitHub Actions: not manually dispatched/rerun

## Exclusions / handoff

No ProjectState, Floor/Family service, ProjectElement, serializer/schema, adapter/native, workflow/release or product-boundary changes. No direct main merge by this normal-agent session.

Reservation ownership is released. Keep #1469 open until an authorized coordinator integrates #1594 and exact-main readback confirms the guard landed.
