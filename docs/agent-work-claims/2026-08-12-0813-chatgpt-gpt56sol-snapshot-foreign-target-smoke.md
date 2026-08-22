# Work claim — ProjectStateSnapshot foreign-target identity smoke

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-snapshot-foreign-target-smoke`
- Registered: `2026-08-12T08:13:00+07:00`
- Last Updated: `2026-08-12T08:17:00+07:00`
- Baseline main SHA: `68517455c46f688a74f4a1d6632c9b93e8d4bb3a`
- Priority: follow-up regression coverage explicitly left uncommitted after prior optimistic-lock 409s
- Task Key: `CORE-PROJECT-STATE-SNAPSHOT-FOREIGN-TARGET-SMOKE`

## Scope completed

Added only the previously missing regression assertion proving that a snapshot captured from one canonical `ProjectState` does not inject captured `ProjectElement` object references when restored into a different `ProjectState` instance with the same `ProjectId`.

The production hardening remains the existing `1994fcf9ea0ae7fbdf679e442c8d9775bd12d413`; this follow-up did not modify `ProjectStateSnapshot` source.

## Committed evidence

- Claim registration: `8cdc02dde9227117fd53554c2280729b98f6894c` — `chore(agent): claim snapshot foreign-target smoke`
- Focused smoke completion: `5fdd71d48a75d120a68bdea77865f3406f03847c` — `test(core): cover snapshot foreign-target identity isolation`
- Existing smoke registration remains `95c7c1a9f26c17744198cac83f8efb8466e71d0f`, so the added assertion is reached through `ProjectStateSnapshotElementIdentitySmoke.Run()`.
- Moving-main readback at `64818fd1b078b9b55161be5261ccca8773794fe0` confirmed `5fdd71d...` is an ancestor of `main` and the added foreign-target assertion remains present after concurrent writes.

The test verifies the restored element is neither the captured project's canonical object nor the foreign project's pre-restore object, captured values are cloned into the foreign target, and subsequent mutation of the restored foreign object does not mutate the captured canonical element.

## Validation boundary

- GitHub ancestry and source readback were verified.
- No executable Core smoke run, GitHub Actions, build, release, or BricsCAD runtime qualification is claimed.

## Completion condition

Satisfied: the previously uncommitted 409-blocked regression is now committed on `main`, read back, registered through the existing smoke suite, and this follow-up claim is closed.