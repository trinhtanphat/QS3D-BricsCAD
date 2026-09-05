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

for label, position in [
    ("unique Floor identity validation", unique_floor_validation),
    ("detached candidate creation", candidate_creation),
    ("detached candidate validation", candidate_validation),
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

# ProjectFamily mutation notifications can have more than one ProjectState subscriber.
# A local check against only the supplied project's ChangeVersion therefore cannot prove
# batch revision atomicity. Do not retain a misleading single-owner admission helper here;
# global ownership/revision atomicity belongs at the catalog ownership boundary.
for forbidden in [
    "RequireRevisionHeadroom(",
    "CountPropertyMutations(",
    "long.MaxValue - project.ChangeVersion",
]:
    if forbidden in source:
        raise SystemExit(
            "Raft defaults must not claim cross-project revision atomicity with a single-project headroom check: "
            + forbidden
        )

apply_start = source.index("public static RaftFoundationVerticalPlacement ApplyFamilyPlacementToElement")
apply_end = source.index("public static string ResolveMode", apply_start)
apply_method = source[apply_start:apply_end]
first_element_mutation = apply_method.index("element.SetProperty(")
source_validation = apply_method.index("var placement = Resolve(project, family);")
target_validation = apply_method.index("if (!RaftFoundationPropertySet.IsRaftElement(element, family))")
element_candidate = apply_method.index("var candidate = Snapshot(element.Properties);")
element_candidate_validation = apply_method.index("var copied = ResolveCore(project, candidate, family.Properties, element.Id);")
placement_match_validation = apply_method.index("if (!NearlyEqual(copied.BottomElevationM, placement.BottomElevationM)")

ordered_prepublication = [
    ("source Family validation", source_validation),
    ("target element admission", target_validation),
    ("detached target candidate", element_candidate),
    ("detached target candidate validation", element_candidate_validation),
    ("detached placement parity validation", placement_match_validation),
]
for label, position in ordered_prepublication:
    if position >= first_element_mutation:
        raise SystemExit(label + " must occur before the first raft element mutation.")
for (left_label, left), (right_label, right) in zip(ordered_prepublication, ordered_prepublication[1:]):
    if left >= right:
        raise SystemExit(left_label + " must occur before " + right_label + ".")

apply_tokens = [
    "element.RemoveProperty(oppositeKey);",
    "element.RemoveProperty(ProjectFloorService.BottomLevelOffsetKey);",
    "element.RemoveProperty(ProjectFloorService.TopLevelOffsetKey);",
    "return copied;",
]
for token in apply_tokens:
    if token not in apply_method:
        raise SystemExit("Missing failure-atomic raft element publication token: " + token)
if "element.Properties.Remove(" in apply_method:
    raise SystemExit("Raft element placement must use ProjectElement.RemoveProperty so persisted removals update dirty/timestamp state.")
if "var copied = Resolve(project, element, family);" in apply_method:
    raise SystemExit("Raft element placement must validate the detached candidate before publication, not Resolve after mutation.")

print("Raft Foundation validation-publication atomicity and element handoff preflight passed.")
