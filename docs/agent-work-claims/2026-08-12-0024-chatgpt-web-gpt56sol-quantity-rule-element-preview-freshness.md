# Work claim — Quantity Rule element preview freshness window

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-rule-element-preview-freshness`
- Registered: `2026-08-12T00:24:00+07:00`
- Completed: `2026-08-12T00:28:00+07:00`
- Baseline main SHA: `fdc992439cf16653a7e7972c0886b8138a397bb8`
- Reservation commit: `61d4cf191cb41f6479104cc9c3404f75d3e2ec9f`
- Priority: P1 — bind element preview freshness to the project revision that existed before detached snapshot capture.

## Defect fixed

`QuantityRulePreviewService.PreviewElement(...)` created the detached project snapshot first and read `project.ChangeVersion` only afterwards. If project state changed while the detached snapshot was being captured, the returned preview could describe the earlier/mixed snapshot while being stamped with the later live revision. `ApplyElement(...)` then had no version mismatch to reject solely from that change. `PreviewProject(...)` already used the correct ordering.

`PreviewElement(...)` now captures `sourceChangeVersion` immediately after exact ownership validation and before `ProjectStateSnapshot.CreateDetachedCopy(project)`, then stamps `PreviewDetached(...)` with that immutable scalar. Any later live revision change is therefore visible to the existing apply freshness checks instead of being silently incorporated after snapshot capture.

## Published commits

- `9a0c8d4776e2685960203f26576db1dc0b1aa0bb` — `fix(quantity): bind element preview to pre-snapshot revision`.
- `5fafb37e51889b506b07af78af32c486e7a499ac` — `test(quantity): pin element preview pre-snapshot freshness`.

## Preserved contract

- Exact project-owned element validation remains first.
- Detached preview computation and element identity lookup are unchanged.
- Apply equivalence, stale-preview rejection and reviewed-apply mutation tracking are unchanged.
- `PreviewProject(...)` retains the same pre-snapshot version-capture contract.

## Validation notes

Current `main` source and dedicated static gate were re-fetched after publication and contain the intended ownership -> ChangeVersion capture -> detached snapshot -> detached element -> immutable stamp ordering. The gate also rejects the legacy post-snapshot `project.ChangeVersion` stamp. A deterministic concurrent snapshot interleaving is not exposed by current Core hooks, so no timing-sensitive threaded smoke was added and no executable Core PASS is claimed. Writes used current blob/main state without force-push. No GitHub Actions were dispatched and no BricsCAD V25 runtime PASS is claimed.

## Excluded scope

No rule formula/category/provenance changes, no apply mutation changes, no UI/native work and no release workflow changes.

## Completion condition

Satisfied for the remote-safe source/static contract: element preview freshness now starts before snapshot capture, the static regression is on current `main`, and this reservation is released.
