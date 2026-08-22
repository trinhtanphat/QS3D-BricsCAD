# Defect triage postmortem — 2026-08-15

## Why this note exists

This postmortem records a concrete failure mode from the `ProjectDataStore / MergeNonConflicting` investigation so future agents do not repeat it.

The mistake was not a bad code patch. The mistake happened earlier: a stale conversation checkpoint was treated as if it were the current failing producer, so the investigation stayed anchored to a defect name that was not present in the current source, history, expected sibling repositories, or current Actions evidence.

## What went wrong

The handoff said:

```text
ProjectDataStore MergeNonConflicting should merge incoming linked relations
```

That sentence was initially treated as an already-established regression. A source lane was created before proving where the string came from.

The actual current release/Core failure was later shown to be:

```text
AutoRoomDanglingPreviousFamilySmoke.EmptyPreviousFamilyStillBootstraps
```

The Auto Room failure had the evidence the ProjectDataStore statement lacked:

- exact repository and baseline SHA;
- exact .NET SDK/toolchain;
- real full Core smoke execution;
- exact failing fixture/assertion;
- real source/test files in the current tree;
- a deterministic source call-chain explaining the failure.

## Root decision error

The primary error was **anchoring on a stale checkpoint instead of re-ranking the task from the newest real failure producer**.

A previous chat/handoff may be useful coordination context, but it must never outrank:

1. a fresh failing local command on the exact current candidate;
2. a fresh failing CI job/log on the exact current candidate;
3. an existing current-tree test/source surface with deterministic evidence.

The investigation should have asked first:

```text
What is the newest real failure currently blocking this candidate?
Does the quoted defect appear in that producer?
Does its named test/type/method exist in the exact current tree?
```

Instead, it spent time trying to locate a named surface whose provenance had not been proven.

## Contributing factors

### 1. Conversation anchoring

The prior checkpoint sounded precise: it contained a type, method and assertion-like sentence. Precision of wording was mistaken for evidence.

Rule: **quoted specificity is not provenance**.

### 2. Issue creation happened too early

Issue #1774 was created as a source-fix reservation before the named defect surface was proven. Once a source-bug issue existed, it increased confirmation pressure to find an implementation matching the wording.

Rule: provenance classification must happen before a source-fix lane is created or adopted.

### 3. Incomplete GitHub code search created noise

GitHub code search returned incomplete/empty results. That correctly prevented a premature conclusion, but it also encouraged repeated adjacent-source hunting.

Rule: when both the alleged producer and named source surface are absent, switch to provenance triage instead of opening semantically nearby files.

### 4. Search order was inverted

History, sibling repositories and related importer/persistence code were investigated before the newest real blocking failure had been established.

Correct order:

```text
latest real failure producer
  -> exact current tree
  -> smallest focused reproduction
  -> call-chain/root cause
  -> history/refs only if current evidence is insufficient
  -> sibling repo only when product-boundary evidence requires it
  -> docs/spec/handoff provenance
  -> web only when external provenance is plausible
```

### 5. A plausible nearby abstraction looked attractive

Project Interchange and persistence code contained merge/relation concepts, so it was tempting to map the missing `ProjectDataStore.MergeNonConflicting` wording onto those existing classes.

Rule: vocabulary similarity is not ownership evidence. Trace from a real assertion to a real API before patching.

## The new freshness rule

### Latest failing producer outranks stale checkpoint

Whenever an agent continues a previous debugging session, it must perform a **failure-freshness check** before resuming source work:

1. refresh the exact current baseline;
2. identify the newest relevant failing local command/CI run for that baseline or integration candidate;
3. record the first deterministic failing test/step;
4. compare it with the defect named by the previous handoff;
5. if they differ, classify both independently;
6. do not keep the old defect as the active blocker unless it still has its own current reproducer/source evidence.

This does not mean every newest failure belongs to the current agent. Scope/non-interference still applies. It means the agent must not falsely call a stale or missing defect the current blocker.

## Positive example: Auto Room #1773

`AutoRoomDanglingPreviousFamilySmoke.EmptyPreviousFamilyStillBootstraps` is a valid `REPRODUCED` source defect.

The smoke requires `ProjectState.ChangeVersion` to advance exactly once during empty-previous-Family bootstrap while assigning the target Family, applying defaults and persisting the default snapshot.

Current `AutoRoomLifecycle.SyncFamilyDefaults` performs an explicit `project.Touch()` and then mutates public `project.Metadata` entries. Public `ProjectMetadataDictionary` mutations call `TouchProject()`, which calls `project.Touch()` again. Therefore a bootstrap that persists at least one metadata snapshot advances revision more than once.

This is the correct debugging shape:

```text
real failing assertion
  -> real test fixture
  -> real current method
  -> real metadata mutation contract
  -> deterministic revision mismatch
```

The source repair remains owned by the agent/session reserved on Issue #1773; this postmortem does not take over that implementation lane.

## Negative example: ProjectDataStore #1774

The `ProjectDataStore / MergeNonConflicting` wording had no verified current producer and no named surface across the checked product repositories/refs/history at the time of investigation.

Correct classification:

```text
SPEC_ONLY / MISSING_SURFACE
```

Correct behavior:

- record the missing evidence;
- do not invent the class/test;
- do not patch `QsdbProjectStore` or Project Interchange merely because they sound related;
- preserve the requirement as provenance/spec work until a real artifact appears.

## Mandatory priority hierarchy

For current defect truth, use this order:

```text
exact current local reproduction
  > exact current CI producer/log
  > exact current source + deterministic proof
  > earlier exact run for the same surface
  > issue/PR factual evidence
  > conversation handoff/status summary
  > Markdown/spec/research note
  > inferred/model-generated wording
```

External owner requirements remain authoritative as requirements, but they are not automatically evidence that a regression/test already exists.

## Stop-loss rule

Do not spend an open-ended investigation budget searching for a named class/test that may not exist.

If the initial pass shows both:

- no literal failure in the alleged producer; and
- no named source/test surface in the exact tree,

immediately classify the lane for provenance investigation. Only expand to history/sibling repositories when there is a concrete migration/product-boundary reason.

## Expected agent behavior after this incident

For future `continue all`, `fix CI`, or handoff-based debugging requests:

```text
refresh exact baseline
  -> inspect newest relevant real failure producer
  -> classify provenance
  -> collision/reservation check
  -> only then create/adopt source lane
  -> smallest reproduction
  -> source trace
  -> patch + regression
  -> branch/PR CI
```

This sequence is faster, safer and reduces both speculative patches and wasted multi-agent work.
