from pathlib import Path

SOURCE = Path("src/QS3D.Core/Domain/ProjectState.cs")
text = SOURCE.read_text(encoding="utf-8")

required = [
    "public IList<QuantityRule> QuantityRules { get; }",
    "QuantityRules = new StructuralRevisionList<QuantityRule>(Touch);",
]
missing = [token for token in required if token not in text]
if missing:
    raise SystemExit(
        "ERROR: quantity-rule revision lifecycle preflight failed: persisted ProjectState.QuantityRules "
        "must use the structural revision-aware collection boundary; missing token(s): "
        + ", ".join(repr(token) for token in missing)
    )

stale = "QuantityRules = new List<QuantityRule>();"
if stale in text:
    raise SystemExit(
        "ERROR: quantity-rule revision lifecycle preflight failed: raw List<QuantityRule> bypasses "
        "ProjectState revision tracking for persisted rule mutations."
    )

print("OK: ProjectState quantity-rule structural mutations are revision-tracked.")
