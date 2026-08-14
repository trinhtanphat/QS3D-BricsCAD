# Door/opening schedule Family-category integrity claim
Status: COMPLETED
Agent: gpt56sol-door-opening-family-category-integrity-20260814-0905
Baseline: 59cb89014e6621fc36181bfb82059febece0a096
Scope: src/QS3D.Core/Reporting/DoorOpeningSchedule.cs; tests/QS3D.Core.SmokeTests/DoorOpeningFamilyCategorySmoke.cs; tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs.
Defect: Door/opening schedule resolved an existing mismatched Family and could inherit WidthM/HeightM/SillHeightM/BottomOffsetM/ThicknessM/Material/FamilyName, including computing OpeningAreaM2 from the wrong Family dimensions when no stored area existed.
Implemented: claim 7fbab8d8acc27d8b73e3e39eae1e472cc3157888; source guard ba020c93cb7d1841037669257b1c03e5ee72a15a; focused regression 28e19a066f89b67b12ff9bdbdfef3bbceb5568e3; smoke registration e0710f04d8fb2bb1cd68621c0b08e50aa8133ce1.
Validation: remote source diff is only the Family-category guard before Number/Text fallback; registry diff is only DoorOpeningFamilyCategorySmoke.Run(). e0710f04d8fb2bb1cd68621c0b08e50aa8133ce1 remains an ancestor of live main (ahead 5, behind 0 at verification) and intervening commits did not touch this scope. GitHub exposes no combined status checks for the checkpoint; no Actions were dispatched. No dotnet/csc/mcs or licensed BricsCAD runtime is available here, so executable managed/native PASS is not claimed.
Excluded: Family mutation, BQ/Material Usage, persistence/schema, MAP/IFC and host UI.
