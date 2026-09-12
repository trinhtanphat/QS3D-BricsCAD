from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CORE = ROOT / "src/QS3D.Core/Commercial/ContractAdministrationWorkflow.cs"
COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/ContractAdministrationCommands.cs"
UI = ROOT / "src/QS3D.BricsCAD.V25/UI/ContractAdministrationWindow.xaml.cs"
XAML = ROOT / "src/QS3D.BricsCAD.V25/UI/ContractAdministrationWindow.xaml"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ContractAdministrationWorkflowSmoke.cs"
ENTRYPOINT = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestEntryPoint.cs"


def fail(message: str) -> None:
    print("FAIL: " + message)
    sys.exit(1)


def text(path: Path) -> str:
    if not path.is_file():
        fail("missing contract-administration feature file: " + str(path.relative_to(ROOT)))
    return path.read_text(encoding="utf-8")


core = text(CORE)
commands = text(COMMANDS)
ui = text(UI)
xaml = text(XAML)
smoke = text(SMOKE)
entrypoint = text(ENTRYPOINT)

for token in [
    "public sealed class ContractAdministrationWorkflow",
    "public sealed class ContractEventRecord",
    "public sealed class ContractNoticeRecord",
    "public sealed class EotSubmissionRecord",
    "public sealed class ContractClaimRecord",
    "public enum ContractDeadlineStatus",
    "CommercialRevisionRef",
    "CommercialAuditLog",
    "RequireKnownRevision",
    "DeadlineStatus(DateTime asOfUtc)",
    "EotAdministrationStatus.Decided",
    "ContractClaimStatus.Decided",
]:
    if token not in core:
        fail("contract-administration Core lost required authority/lifecycle token: " + token)

for token in [
    "CommercialSettlementWorkspace",
    "CommercialQsWindow",
    "ProjectMetadataDictionary",
    "VariationAssessment",
    "InterimPaymentCertificate",
    "FinalAccount",
    "decimal ClaimAmount",
    "decimal AssessedAmount",
]:
    if token in core:
        fail("contract-administration Core crossed a reserved settlement/persistence authority boundary: " + token)

for token in [
    '[CommandMethod("QS3DCONTRACTADMIN"',
    '[CommandMethod("QS3DCLAIMS"',
    "ContractAdministrationWindow",
    "Application.ShowModelessWindow",
    "database.UnmanagedObject",
]:
    if token not in commands:
        fail("contract-administration command surface lost required modeless binding token: " + token)

for token in [
    "new ContractAdministrationWorkflow()",
    "EnsureActive()",
    "AppendEvent",
    "AppendNotice",
    "AppendEot",
    "AppendClaim",
    "AuditLog.Events",
]:
    if token not in ui:
        fail("contract-administration UI lost thin Core-delegation token: " + token)

for token in [
    'x:Class="QS3D.BricsCAD.V25.UI.ContractAdministrationWindow"',
    'x:Name="EventIdBox"',
    'x:Name="NoticeIdBox"',
    'x:Name="EotIdBox"',
    'x:Name="ClaimIdBox"',
    'Content="Register event"',
    'Content="Issue notice"',
    'Content="Advance EOT"',
    'Content="Advance claim"',
]:
    if token not in xaml:
        fail("contract-administration XAML lost workflow surface token: " + token)

for token in [
    "EventNoticeEotClaimLifecycleIsRevisionLinkedAndAudited",
    "DeadlineStatusIsDeterministic",
    "InvalidTransitionsAndUnknownLinksFailClosed",
    "StaleRevisionChainFailsClosed",
    "Equal(12, workflow.AuditLog.Events.Count",
]:
    if token not in smoke:
        fail("contract-administration smoke lost deterministic oracle: " + token)

if "ContractAdministrationWorkflowSmoke.Run();" not in entrypoint:
    fail("contract-administration smoke is not registered through SmokeTestEntryPoint.")

if "[ModuleInitializer]" in smoke or "RegisterAndRun()" in smoke:
    fail("contract-administration smoke must use the explicit deterministic smoke entrypoint.")

print("PASS: contract administration preserves revision provenance, deterministic deadlines, EOT/claim lifecycle, audit authority, and thin V25 boundaries.")
