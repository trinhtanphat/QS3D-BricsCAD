# Work claim — release #30 targeted opening-cut preflight reconciliation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-release30-targeted-opening-cut-preflight`
- Registered: `2026-08-12T09:53:00+07:00`
- Completed: `2026-08-12T09:55:00+07:00`
- Baseline main SHA: `2fd8a0f6a0f38ee4123bd18ad8902b15cb34d392`
- Claim commit: `ea3b2e1ae20f3821830115b5c30b57f772256675`
- Implementation commit: `8d8c7712ac23200884baeaf7fd920b4212aa6bac`
- Priority: QS3D Cloud V25 Preview Build & Release #30 reported two targeted-opening-cut failures after Direct Draw Auto Host was intentionally narrowed to exact single-opening linking and selected-cut target resolution moved behind a helper.

## Completed scope

Reconciled only `scripts/preflight-targeted-opening-cut.py` with the current exact Auto Host and helper-based selected-opening contracts. Opening boolean/Direct Draw production source remained unchanged.

## Implemented gate contract

- Direct Draw Door/WallOpening must call `AutoHostLinkCommands.LinkSingleOpening(document, project, createdElementId)` and the gate explicitly rejects broad `new AutoHostLinkCommands().AutoLinkHosts()` re-entry.
- Physical boolean cutting remains explicit; existing Direct Draw cut-service/command-dispatch prohibitions are retained.
- Selected cut must resolve ids read-only through `ResolveOpeningIds(previewProject, handles)`, re-resolve after canonical binding, compare exact target sets and execute the targeted opening-id overload.
- The actual `ResolveOpeningIds` helper is isolated and must retain `Where(IsOpening)`, semantic selection matching, case-insensitive deduplication and deterministic ordering.
- Existing OpeningBooleanService normalization, pre-transaction target validation, UI/docs wiring and command uniqueness checks remain intact.

## Validation performed

- Verified claim commit `ea3b2e1ae20f3821830115b5c30b57f772256675` remained an ancestor of moving `main`; the only intervening commit at that check closed an unrelated Recognition claim.
- Re-fetched the exact gate before implementation.
- Read current DirectDrawOpeningCommands and OpeningBooleanCommands before changing the gate.
- Implementation commit `8d8c7712ac23200884baeaf7fd920b4212aa6bac` is on `main`.
- A closeout write raced moving `main` once; current claim content was re-fetched and no force/overwrite was used.
- No production source was changed.
- No GitHub Actions/build/release dispatch was performed and no BricsCAD V25/V26 runtime PASS is claimed.

## Completion condition

Completed. The targeted opening-cut gate now follows exact single-opening Auto Host and helper-based deduplicated selected targets without weakening explicit-cut/ownership safety, and this reservation is released.
