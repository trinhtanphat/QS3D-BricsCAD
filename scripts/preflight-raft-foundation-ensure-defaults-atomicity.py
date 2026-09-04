from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Domain/RaftFoundationLevelPlacement.cs").read_text(encoding="utf-8")

start = source.index("public static bool EnsureDefaults")
end = source.index("public static RaftFoundationVerticalPlacement Resolve(ProjectState project, ProjectFamily family)", start)
method = source[start:end]

first_live_mutation = method.index("family.Properties[")
unique_floor_validation = method.index("ValidateUniqueFloorIds(project);")
candidate_creation = method.index("var candidate = Snapshot(family.Properties);")
candidate_validation = method.index("ResolveCore(project, candidate, null, family.Name);")
revision_admission = method.index("RequireRevisionHeadroom(project, family, before, candidate);")

for label, position in [
    ("unique Floor identity validation", unique_floor_validation),
    ("detached candidate creation", candidate_creation),
    ("detached candidate validation", candidate_validation),
    ("revision headroom admission", revision_admission),
]:
    if position >= first_live_mutation:
        raise SystemExit(label + " must occur before the first live raft Family property mutation.")

candidate_tokens = [
    "candidate[RaftFoundationPropertySet.ElevationModeKey] = mode;",
    "candidate[activeKey] = floor.Id;",
    "candidate.Remove(oppositeKey);",
    "candidate.Remove(ProjectFloorService.BottomLevelOffsetKey);",
    "candidate.Remove(ProjectFloorService.TopLevelOffsetKey);",
    "candidate[RaftFoundationPropertySet.BottomOffsetKey] =",
]
for token in candidate_tokens:
    if token not in method:
        raise SystemExit("Missing detached raft defaults candidate token: " + token)

if "Resolve(project, family);" in method:
    raise SystemExit("EnsureDefaults must not defer final validation until after live Family publication.")

helper_tokens = [
    "private static void RequireRevisionHeadroom(",
    "if (!project.Families.Any(x => ReferenceEquals(x, family))) return;",
    "var requiredMutations = CountPropertyMutations(before, candidate);",
    "requiredMutations > long.MaxValue - project.ChangeVersion",
    "private static long CountPropertyMutations(",
    "StringComparison.Ordinal",
]
for token in helper_tokens:
    if token not in source:
        raise SystemExit("Missing raft defaults revision atomicity contract token: " + token)

apply_start = source.index("public static RaftFoundationVerticalPlacement ApplyFamilyPlacementToElement")
apply_end = source.index("public static string ResolveMode", apply_start)
apply_method = source[apply_start:apply_end]
first_element_mutation = apply_method.index("element.SetProperty(")
target_validation = apply_method.index("if (!RaftFoundationPropertySet.IsRaftElement(element, family))")
source_validation = apply_method.index("var placement = Resolve(project, family);")
if source_validation >= target_validation:
    raise SystemExit("Raft source Family validation must remain before target element admission.")
if target_validation >= first_element_mutation:
    raise SystemExit("Raft target element admission must occur before the first element property mutation.")
if "throw new InvalidOperationException(\"Cấu kiện không phải Móng Bè.\");" not in apply_method:
    raise SystemExit("Missing fail-closed invalid raft target rejection before element handoff publication.")

print("Raft Foundation defaults and element handoff failure atomicity preflight passed.")
