# Remote late-hardening close-out — 2026-08-11

Updated: 2026-08-11 (UTC+7)

This note records the evidence-driven remote hardening that landed **after** the main `docs/REMOTE-IMPLEMENTATION-COMPLETION-2026-08-11.md` snapshot. It is not a new backlog, not a second LOCAL_ONLY queue, and not a claim that the product is released. Current source and `docs/LOCAL-AGENT-INBOX.md` remain authoritative.

## Why this note exists

The repository is being modified by multiple concurrent agents. Several late defects were found only because current `main` introduced or exposed a concrete deterministic contract. Remote agents should not repeatedly rediscover these same lanes after they have landed.

The default after these batches remains the completion document's rule: **do not broaden remote implementation merely to increase feature count**. Continue remotely only when current source demonstrates another specific deterministic defect. Native BricsCAD V25 qualification remains LOCAL_ONLY.

## Late hardening now in `main` ancestry

### Native Table placement lifecycle

- `eab82c4a5cc7461af6078139e19faeafa04840ec` — BQ/BBS native Table placement now probes existing project state read-only before `GetPoint`, returns on placement cancel before canonical binding, then rebinds and verifies the same ProjectId before build.
- `661d1e945b01a683bc3ae628027327fae14cd947` — extends that lifecycle to all six native Table Build families: BQ, BBS, Semantic Element, Material Usage, Room Finish and Door/Opening.
- `2fe3230229bd53054cbbd48821945f2ac26824be` — supporting exact-V25 qualification matrix for the existing `LOCAL-006` item. It is not a second queue and does not establish LOCAL_PASS.

**Do not retry remotely:** placement-cancel, stale/replaced project identity, save/reopen ownership, foreign-object protection and multi-DWG proof require licensed V25 and remain under LOCAL-006.

### Recognition / B4D lifecycle

The source now acquires/guards usable CAD input before unit resolution/project bootstrap, lets B4D inspect generated handles from read-only project state, and requires canonical same-ProjectId rebind before existing-project mutation. `docs/LOCAL-RECOGNITION-PROJECT-LIFECYCLE-2026-08-11.md` is supporting detail for existing `LOCAL-011` only.

**Do not retry remotely:** empty/generated-only runtime behavior, cache/sidecar replacement during scan, exact ProjectId continuity and multi-DWG evidence remain LOCAL_ONLY under LOCAL-011.

### Interchange field-merge cleanup authority

- `612d04c81aa7a8fa91e397b8fa1c7c53ccafe3c0` — reviewed field-merge cleanup authorization now rejects ambiguous generated-handle ownership and requires a non-empty target drawing fingerprint before destructive native cleanup can be authorized.
- `0bd0b3a9e9d928e44c8d9d6c6f475526616c062c` — Core provenance no longer claims that native CAD cleanup already happened. Canonical reporting is `NativeCleanupHandlesRequired` / `Interchange.LastImport.NativeCleanupHandlesRequired`; deprecated aliases preserve source compatibility, and new imports remove stale legacy `...TargetGeneratedHandlesCleaned` metadata.
- `746fd721397e01bd70e7f829709ccfe9afa67ce0` — portable `ProjectElement` interchange properties now exclude drawing-local handle-bearing metadata at both export and typed-read boundaries; legacy snapshots can remain validator-compatible without allowing those handles to be rebound by canonical import/merge paths. Family properties keep their separate semantic boundary.

The field-merge Core boundary remains intentionally separate from generic BricsCAD orchestration. Core may clear semantic ownership metadata and mark generated output dirty after reviewed authorization; it does **not** erase/rebuild native BricsCAD objects. Native cleanup/rebuild, transaction/compensation, Undo, save/reopen and multi-DWG qualification remain LOCAL_ONLY/COORDINATED under the existing Interchange scope.

### Modeless document/project affinity audit

A focused audit of Family Manager, Floor/Level, Material Catalog, Rebar Mesh Setup, Recognition, Revision, Quantity Summary, Project Tools and Schedule Hub found that mutating/dispatch callbacks already enforce active-DWG/project affinity at the caller boundary. Rebar Mesh Setup additionally revalidates canonical `ProjectState` reference identity after reload. Global launcher windows intentionally dispatch to the current `MdiActiveDocument` and do not carry document affinity.

**Do not add a blanket `MdiActiveDocument` requirement to `DocumentBoundWindowLifetime` merely because the lifetime helper itself does not enforce it.** Current dangerous callers already own the active-document rule. Change the shared lifetime contract only when a concrete current caller demonstrates a failure that cannot be safely handled at its action boundary.

### Unknown reference-shaped extension properties

`ProjectInterchangeRemapPlanner` already treats unregistered `*Id/*Ids/*Ref/*Refs/*RefId/*RefIds` properties as opaque reference warnings and fails closed for Import-As-New when IDs may be remapped. The generic snapshot/field-merge boundary currently preserves forward-compatible extension properties unless they are registered semantic references or drawing-local handle metadata.

**Do not globally reject every unknown reference-shaped property without a reviewed product/schema decision.** A broader rule would change the current extension/interoperability contract. Fix only a demonstrated dangling/remap bug or an explicitly approved reference registry expansion.

## Validation truth

No GitHub Actions or release workflow was dispatched by this late-hardening pass. The remote environment could not obtain a working Git clone because GitHub DNS resolution failed, so this pass does not claim execution of the aggregate preflights, Core build/smoke, adapter build, NETLOAD/DemandLoad, native UI, private-DWG or exact-V25 runtime matrices.

Source review, focused regression/preflight source and GitHub mergeability are not substitutes for LOCAL_PASS. Exact-current-SHA runtime evidence must be produced by a compatible local agent and written back through the existing `docs/LOCAL-AGENT-INBOX.md` item rather than creating another live queue.

## Remote continuation rule

Before another remote `continue all` pass:

1. fetch current `main` and search open PRs;
2. preserve concurrent winners and close/supersede stale duplicate branches/PRs rather than merging them wholesale;
3. read `docs/REMOTE-IMPLEMENTATION-COMPLETION-2026-08-11.md`, this note and `docs/LOCAL-AGENT-INBOX.md`;
4. do not repeat the Table placement, Recognition/B4D, field-merge cleanup-provenance, element-handle portability or modeless-affinity audits above unless current source materially changes those contracts;
5. proceed remotely only for a concrete deterministic source defect;
6. otherwise stop remote churn and hand execution to the local V25 qualification queue.
