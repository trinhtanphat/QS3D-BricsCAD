# Door/opening schedule Family-category integrity claim
Status: ACTIVE
Agent: gpt56sol-door-opening-family-category-integrity-20260814-0905
Baseline: 59cb89014e6621fc36181bfb82059febece0a096
Scope: src/QS3D.Core/Reporting/DoorOpeningSchedule.cs; tests/QS3D.Core.SmokeTests/DoorOpeningFamilyCategorySmoke.cs; tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs.
Goal: fail closed when a Door/WallOpening schedule element resolves an existing Family whose category differs from the element category, before Family-derived width/height/sill/thickness/material/name can affect schedule quantities. Preserve valid Door/WallOpening Family inheritance and host semantics; add focused regression. Family mutation, BQ/Material Usage, persistence and host UI are out of scope.
