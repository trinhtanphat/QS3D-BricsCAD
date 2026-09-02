# Project onboarding revision atomicity

## Boundary

`ProjectOnboardingService.Bootstrap` is a Core/project-lifecycle operation that can perform multiple persistence-state mutations in one successful starter-plan application. A rejected onboarding request must not consume part of the remaining `ProjectState.ChangeVersion` range or leave unit, Floor, Family, metadata, timestamp, or revision state partially published.

## Admission plan

After unit/material/catalog/Floor validation and before the first mutation, onboarding admits both storage capacity and the exact revision advances required by the already-built plan.

For a required drawing-unit override, onboarding first asks the canonical `ProjectMetadataDictionary` to validate the final set of public metadata keys that `DrawingUnitResolutionPolicy.SetProjectOverride` may need. With no bound-unit record this is only the override key. With a canonical bound-unit record it is the override, effective-unit, and binding-source keys. The capacity check is read-only and uses the metadata store's own `MaximumEntries` enforcement rather than duplicating the numeric limit in onboarding.

This matters because `SetProjectOverride` applies multiple public semantic metadata writes in the bound-unit path. If only one metadata slot remains, the first key can otherwise advance `ChangeVersion` before the next key fails capacity; rollback restores content but can itself advance the semantic revision/timestamp. Pre-admission makes this rejection mutation-free.

The revision plan then counts:

- drawing-unit override advances from the metadata writes that `DrawingUnitResolutionPolicy.SetProjectOverride` will actually perform;
  - without an existing canonical bound-unit record, only the override key is written when it differs;
  - with an existing bound-unit record, the override, effective-unit, and binding-source keys are each counted only when their exact stored value will change;
- one advance when a starter Floor must be created or an existing single Floor must be activated;
- for each newly created starter Family: one advance for `ProjectFamilyService.Create`, one for every default quick-schema property, and one for `Material`;
- reused Families consume no onboarding revision advance.

The service rejects when the required advances exceed `long.MaxValue - project.ChangeVersion`. It does not weaken `ProjectState.Touch()` overflow checks and it does not reserve an arbitrary safety buffer.

## Deterministic regression

`ProjectOnboardingRevisionAtomicitySmoke` proves the boundary through the normal public services:

1. one fewer remaining revision than required is rejected before any unit override, effective-unit/binding-source metadata, Floor, Family, active-Floor, timestamp, or revision mutation;
2. exactly the required remaining revision capacity is accepted and the project may finish at `long.MaxValue`;
3. the canonical bound-unit path protects all multi-key metadata revision advances; and
4. a project with 9,999 metadata entries, a canonical bound-unit record, and no override/effective/binding-source keys is rejected before `SetProjectOverride` can consume the last slot and then roll back with a changed project revision.

The smoke is module-initialized so no shared registration file is required.

## Validation

Run the focused source guard:

```text
python scripts/preflight-project-onboarding-revision-atomicity.py
```

Then run the repository Core deterministic smoke/build and the normal exact-head protected CI gates. No licensed BricsCAD runtime is applicable to this boundary.
