from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Persistence" / "ProjectPersistenceCheckpoint.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "ProjectPersistenceCheckpointElementDriftSmoke.cs"


def fail(message: str) -> None:
    print(f"ERROR: {message}", file=sys.stderr)
    raise SystemExit(1)


source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

restore_start = source.find("public void Restore(ProjectState project)")
restore_end = source.find("private static void RequireStableKnownCount", restore_start)
if restore_start < 0 or restore_end < 0:
    fail("ProjectPersistenceCheckpoint.Restore source boundary was not found.")
restore = source[restore_start:restore_end]

semantic_check = "if (!pair.Value.SemanticMatches(targets[pair.Key]))"
restore_mutation = "pair.Value.Restore(targets[pair.Key]);"
semantic_check_index = restore.find(semantic_check)
restore_mutation_index = restore.find(restore_mutation)
if semantic_check_index < 0:
    fail("checkpoint restore does not fence captured element semantic generation")
if restore_mutation_index < 0:
    fail("checkpoint element persistence restore mutation boundary was not found")
if semantic_check_index >= restore_mutation_index:
    fail("element semantic generation fence must complete before the first persistence restore mutation")
if "captured element semantic state changed" not in restore:
    fail("checkpoint restore semantic-drift rejection is not explicit")

state_start = source.find("private sealed class ElementSemanticState")
if state_start < 0:
    fail("bounded element semantic checkpoint state was not found")
state = source[state_start:]
required_state_tokens = (
    "byte[] _signature",
    "SHA256.Create()",
    "new BinaryWriter(crypto, Encoding.UTF8, leaveOpen: true)",
    "writer.Write((int)element.Category)",
    "writer.Write(element.FamilyId",
    "writer.Write(element.FloorId",
    "writer.Write(element.ZoneId",
    "writer.Write(element.DrawingFingerprint",
    'WriteSequence(writer, element.SourceHandles, "source handles")',
    'WriteSequence(writer, element.DependsOn, "dependencies")',
    'WriteMap(writer, element.Properties, "properties"',
    'WriteMap(writer, element.Quantities, "quantities"',
    "snapshot.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.Key, right.Key))",
    "writer.Write(CanonicalizeOrdinalIgnoreCaseIdentity(value))",
    "writer.Write(CanonicalizeOrdinalIgnoreCaseIdentity(pair.Key))",
    "(value ?? string.Empty).ToUpperInvariant()",
    "RequireSupportedNestedCount",
)
for token in required_state_tokens:
    if token not in state:
        fail("element semantic checkpoint signature is incomplete: missing " + token)

signature_start = state.find("private static byte[] ComputeSignature(ProjectElement element)")
signature_end = state.find("private static void WriteSequence", signature_start)
if signature_start < 0 or signature_end < 0:
    fail("element semantic signature source boundary was not found")
signature = state[signature_start:signature_end]
if "element.Dirty" in signature or "element.UpdatedUtc" in signature:
    fail("element persistence-only Dirty/UpdatedUtc state must not participate in the semantic restore fence")

persistence_start = source.find("private sealed class ElementPersistenceState")
persistence_end = state_start
if persistence_start < 0 or persistence_end <= persistence_start:
    fail("element persistence checkpoint state boundary was not found")
persistence_state = source[persistence_start:persistence_end]
if "_semanticState = ElementSemanticState.Capture(owner);" not in persistence_state:
    fail("element checkpoint does not capture semantic generation alongside persistence state")
if "element.Dirty == Dirty" not in persistence_state or "element.UpdatedUtc == UpdatedUtc" not in persistence_state:
    fail("existing element persistence-state matching contract was lost")
if "SemanticMatches(element)" not in persistence_state:
    fail("checkpoint Matches no longer includes captured semantic generation")

required_smoke_tokens = (
    'SetProperty("WidthM", "1.25")',
    'SetQuantity("AreaM2", 12.5)',
    'SourceHandles.Add("AB12")',
    "element.Category = ElementCategory.GlassWall",
    "element.MarkDirty(ElementDirtyFlags.Quantity)",
    'element.SetProperty("alpha", "1")',
    'element.SourceHandles.Add("ab12")',
    "checkpoint.Restore(project);",
    "captured element semantic state changed",
)
for token in required_smoke_tokens:
    if token not in smoke:
        fail("checkpoint element semantic drift smoke is incomplete: missing " + token)

print("Project persistence checkpoint element semantic drift preflight passed.")
