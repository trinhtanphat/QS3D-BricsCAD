from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Documentation/SemanticScheduleCatalog.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/SemanticScheduleCatalogSaveKnownCountSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "var knownCount = ResolveSaveKnownCount(definitions);",
    "RequireStableSaveKnownCount(definitions, knownCount, \"before MoveNext\");",
    "var moved = enumerator.MoveNext();",
    "RequireStableSaveKnownCount(definitions, knownCount, \"after MoveNext\");",
    "if (knownCount.HasValue && list.Count >= knownCount.Value)",
    "var current = enumerator.Current;",
    "RequireStableSaveKnownCount(definitions, knownCount, \"after Current\");",
    "list.Add(current);",
    "if (knownCount.HasValue && list.Count != knownCount.Value)",
    "RequireStableSaveKnownCount(definitions, knownCount, \"after traversal\");",
    "definitions as ICollection<SemanticScheduleDefinition>",
    "definitions as IReadOnlyCollection<SemanticScheduleDefinition>",
    "definitions as System.Collections.ICollection",
    "Semantic schedule catalog source exposes conflicting known Count values.",
    "Semantic schedule catalog source reports an invalid negative known Count.",
]
required_smoke = [
    "RejectKnownCountOverrunBeforeUnexpectedCurrent();",
    "RejectTransientMoveNextCountDrift();",
    "RejectTransientCurrentCountDrift();",
    "RejectKnownCountUnderYield();",
    "StableCountedSaveStillPersists();",
    "PureStreamingSaveStillPersists();",
    "Equal(0, source.CurrentReads",
    "Unchanged(project, beforeVersion",
    "[ModuleInitializer]",
]

missing = [token for token in required_source if token not in source]
missing += [token for token in required_smoke if token not in smoke]
if missing:
    raise SystemExit("Semantic schedule Save known-Count integrity preflight failed; missing: " + ", ".join(missing))

save = source.index("public static void Save(ProjectState project, IEnumerable<SemanticScheduleDefinition> definitions)")
admit = source.index("var knownCount = ResolveSaveKnownCount(definitions);", save)
before_move = source.index('RequireStableSaveKnownCount(definitions, knownCount, "before MoveNext");', admit)
move = source.index("var moved = enumerator.MoveNext();", before_move)
after_move = source.index('RequireStableSaveKnownCount(definitions, knownCount, "after MoveNext");', move)
overrun = source.index("if (knownCount.HasValue && list.Count >= knownCount.Value)", after_move)
current = source.index("var current = enumerator.Current;", overrun)
after_current = source.index('RequireStableSaveKnownCount(definitions, knownCount, "after Current");', current)
retain = source.index("list.Add(current);", after_current)
under_yield = source.index("if (knownCount.HasValue && list.Count != knownCount.Value)", retain)
final_rebound = source.index('RequireStableSaveKnownCount(definitions, knownCount, "after traversal");', under_yield)
validate = source.index("ValidateCatalog(list);", final_rebound)

if not (admit < before_move < move < after_move < overrun < current < after_current < retain < under_yield < final_rebound < validate):
    raise SystemExit(
        "Semantic schedule Save ordering must remain admission Count -> pre-MoveNext rebound -> MoveNext -> post-MoveNext rebound -> overrun -> Current -> post-Current rebound -> retain -> under-yield -> final rebound -> validation."
    )

print("PASS semantic schedule Save known-Count integrity source guard")
