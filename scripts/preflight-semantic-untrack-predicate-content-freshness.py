#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Services/SemanticUntrackService.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/SemanticUntrackPredicateContentFreshnessSmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/semantic-untrack-predicate-content-freshness.md"

for path in (SOURCE, SMOKE, RUNBOOK):
    if not path.is_file():
        raise SystemExit("Semantic-untrack predicate content freshness preflight missing file: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

for token in (
    "var predicateContent = SnapshotElementContent(project);",
    "RequireElementOwnershipUnchanged(project, predicateOwnership);\n                RequireElementContentUnchanged(project, predicateContent);",
    "private static IReadOnlyDictionary<string, ElementContentSnapshot> SnapshotElementContent(ProjectState project)",
    "private sealed class ElementContentSnapshot",
    "_sourceHandles = element.SourceHandles.ToArray();",
    "_dependsOn = element.DependsOn.ToArray();",
    "_properties = new Dictionary<string, string>(element.Properties, StringComparer.OrdinalIgnoreCase);",
    "_quantities = new Dictionary<string, double>(element.Quantities, StringComparer.OrdinalIgnoreCase);",
    "Project element content changed while evaluating semantic untrack predicate.",
):
    if token not in source:
        raise SystemExit("Semantic-untrack predicate content freshness source missing contract: " + token)

for token in (
    "DependencyMutationCannotBypassDependentGuard();",
    "SourceHandleMutationFailsBeforeRemoval();",
    "PropertyMutationFailsBeforeNoOp();",
    "QuantityMutationFailsBeforeRemoval();",
    "ScalarRelationMutationFailsBeforeRemoval();",
    "DirtyMutationFailsBeforeRemoval();",
    "StablePredicateStillUsesNormalDependencyGuard();",
    "StablePredicateStillUntracksIndependentTarget();",
    "dependent.DependsOn.Clear();",
    "ThrowsContentFreshness",
):
    if token not in smoke:
        raise SystemExit("Semantic-untrack predicate content freshness smoke missing contract: " + token)

for phrase in (
    "Lane-Key: `issue-5041`",
    "predicate content freshness",
    "dependency planning",
    "caller side effects are not rolled back",
    "stable dependency guard",
    "No licensed BricsCAD runtime",
):
    if phrase not in runbook:
        raise SystemExit("Semantic-untrack predicate content freshness runbook missing boundary: " + phrase)

print("PASS semantic untrack predicate content freshness contract")
