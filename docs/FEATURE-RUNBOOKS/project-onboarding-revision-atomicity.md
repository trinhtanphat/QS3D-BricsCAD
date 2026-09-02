# Project onboarding revision atomicity

## Boundary

`ProjectOnboardingService.Bootstrap` is a Core/project-lifecycle operation that can perform multiple persistence-state mutations in one successful starter-plan application. A rejected onboarding request must not consume part of the remaining `ProjectState.ChangeVersion` range or leave unit, Floor, Family, or metadata state partially published.

## Revision plan

After unit/material/catalog/Floor validation and before the first mutation, onboarding computes the exact revision advances required by the already-built plan:

- drawing-unit override advances are counted from the metadata writes that `DrawingUnitResolutionPolicy.SetProjectOverride` will actually perform;
  - without an existing canonical bound-unit record, only the override key is written when it differs;
  - with an existing bound-unit record, the override, effective-unit, and binding-source keys are each counted only when their exact stored value will change;
- one advance when a starter Floor must be created or an existing single Floor must be activated;
- for each newly created starter Family: one advance for `ProjectFamilyService.Create`, one for every default quick-schema property, and one for `Material`;
- reused Families consume no onboarding revision advance.

The service rejects when the required advances exceed `long.MaxValue - project.ChangeVersion`. It does not weaken `ProjectState.Touch()` overflow checks and it does not reserve an arbitrary safety buffer.

## Deterministic regression

`ProjectOnboardingRevisionAtomicitySmoke` measures the canonical onboarding revision footprint using the normal public services, then proves both sides of the boundary for both ordinary fresh metadata and the canonical bound-unit metadata path:

1. one fewer remaining revision than required is rejected before any unit override, effective-unit/binding-source metadata, Floor, Family, active-Floor, timestamp, or revision mutation;
2. exactly the required remaining capacity is accepted and the project may finish at `long.MaxValue`.

The bound-unit cases specifically protect the multi-key metadata path in `SetProjectOverride`, where up to three semantic metadata writes can advance the project revision. The smoke is module-initialized so no shared registration file is required.

## Validation

Run the focused source guard:

```text
python scripts/preflight-project-onboarding-revision-atomicity.py
```

Then run the repository Core deterministic smoke/build and the normal exact-head protected CI gates. No licensed BricsCAD runtime is applicable to this boundary.
