# BLT3D Parity Foundation P1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a host-neutral, auditable BLT3D parity manifest/closure model and workflow binding registry that reuses QS3D's existing `FeatureId`, fails closed on incomplete evidence, and gives later UI/MCP/launcher carriers one canonical feature/workflow identity contract.

**Architecture:** Extend the existing `QS3D.Core.Features` model rather than creating a second feature identity system. A deterministic TSV manifest records parity inventory and evidence; `ParityManifest` validates uniqueness/applicability and computes closure; `ParityWorkflowRegistry` binds the same `FeatureId` values to stable workflow keys, invocation surfaces, and safety requirements without referencing BricsCAD types. P1 deliberately does not implement UI buttons or host mutations.

**Tech Stack:** C# / `netstandard2.0`, existing `QS3D.Core.Features.FeatureId` and `FeatureRegistry`, `QS3D.Core.SmokeTests`, deterministic UTF-8 TSV parsing, V25 `net48`, V26 `net8.0-windows` compile verification.

**Spec:** `docs/superpowers/specs/2026-09-10-blt3d-full-parity-design.md`

## Global Constraints

- `QS3D-BricsCAD` remains a Windows x64 BricsCAD-hosted plugin; P1 introduces no standalone CAD executable.
- Reuse `QS3D.Core.Features.FeatureId`; do not define another feature-ID value type.
- Core remains host-neutral and must not reference BricsCAD/Teigha/AutoCAD runtime types.
- BLT3D is reference evidence only; P1 stores no BLT3D binaries, secrets, license material, signing material, or runtime dependency.
- `100%` is claimable only when the catalog is explicitly complete and every applicable feature reaches `V25V26ParityPass`.
- `NotApplicableByHostBoundary` is valid only with a nonblank product-decision reference and reason.
- UI-only/stub/not-wired states cannot be represented as full parity.
- Workflow keys and feature IDs are stable, unique, case-normalized contracts.
- A semantic mutation workflow must declare active-document, project, atomic-mutation, and audit requirements so later UI/MCP callers cannot register an unsafe bypass.
- P1 changes only host-neutral contracts, manifest evidence, and smoke tests; BricsCAD runtime wiring belongs to later carriers.

## File Structure

- Create `src/QS3D.Core/Features/ParityEvidenceContracts.cs` — evidence/applicability value contracts and one feature record.
- Create `src/QS3D.Core/Features/ParityManifest.cs` — validated manifest snapshot and closure report.
- Create `src/QS3D.Core/Features/ParityManifestParser.cs` — deterministic TSV parser with explicit catalog-complete metadata.
- Create `src/QS3D.Core/Features/ParityWorkflowRegistry.cs` — feature-to-workflow bindings and safety invariants.
- Create `docs/BLT3D-PARITY-MANIFEST.tsv` — initial owner-approved inventory anchors, all honestly incomplete.
- Create `tests/QS3D.Core.SmokeTests/ParityManifestSmoke.cs` — contract/closure/parser smoke coverage.
- Create `tests/QS3D.Core.SmokeTests/ParityWorkflowRegistrySmoke.cs` — registry uniqueness/safety coverage.
- Modify `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs` — register the two new smoke classes.

---

### Task 1: Define parity evidence contracts on top of existing FeatureId

**Files:**
- Create: `src/QS3D.Core/Features/ParityEvidenceContracts.cs`
- Test: `tests/QS3D.Core.SmokeTests/ParityManifestSmoke.cs`

**Interfaces:**
- Consumes: `QS3D.Core.Features.FeatureId` from `FeatureInteractionContracts.cs`.
- Produces: `ParityEvidenceStage`, `ParityApplicability`, and `ParityFeatureRecord`.

- [ ] **Step 1: Write the failing contract smoke**

```csharp
using System;
using QS3D.Core.Features;

namespace QS3D.Core.SmokeTests
{
    internal static class ParityManifestSmoke
    {
        internal static void Run()
        {
            EvidenceRecordNormalizesRequiredText();
            HostBoundaryRequiresDecisionEvidence();
        }

        private static void EvidenceRecordNormalizesRequiredText()
        {
            var record = new ParityFeatureRecord(
                new FeatureId("BIM.Draw.Rectangle"),
                " BIM Authoring ",
                " BLT3D / MÔ HÌNH BIM / Chữ nhật ",
                " bim.draw.rectangle ",
                ParityApplicability.Applicable,
                ParityEvidenceStage.ReferenceCaptured);

            AssertEqual("bim.draw.rectangle", record.FeatureId.ToString());
            AssertEqual("BIM Authoring", record.Domain);
            AssertEqual("BLT3D / MÔ HÌNH BIM / Chữ nhật", record.ReferencePath);
            AssertEqual("bim.draw.rectangle", record.WorkflowKey);
        }

        private static void HostBoundaryRequiresDecisionEvidence()
        {
            AssertThrows<ArgumentException>(() => new ParityFeatureRecord(
                new FeatureId("host.unsupported"), "Host", "reference", "host.unsupported",
                ParityApplicability.NotApplicableByHostBoundary,
                ParityEvidenceStage.ReferenceCaptured));
        }

        private static void AssertEqual<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
        }

        private static void AssertThrows<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }
    }
}
```

- [ ] **Step 2: Run the smoke project and verify it fails to compile**

Run:

```powershell
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Expected: compile errors for the undefined parity contract types.

- [ ] **Step 3: Add the minimal evidence contracts**

```csharp
using System;

namespace QS3D.Core.Features
{
    public enum ParityEvidenceStage
    {
        ReferenceCaptured = 0,
        UiPresent = 1,
        CommandWired = 2,
        SemanticBehaviorPass = 3,
        SaveReopenPass = 4,
        V25V26ParityPass = 5
    }

    public enum ParityApplicability
    {
        Applicable = 0,
        NotApplicableByHostBoundary = 1
    }

    public sealed class ParityFeatureRecord
    {
        public ParityFeatureRecord(
            FeatureId featureId,
            string domain,
            string referencePath,
            string workflowKey,
            ParityApplicability applicability,
            ParityEvidenceStage evidenceStage,
            string decisionReference = null,
            string decisionReason = null)
        {
            FeatureId = featureId;
            Domain = Required(domain, nameof(domain));
            ReferencePath = Required(referencePath, nameof(referencePath));
            WorkflowKey = Required(workflowKey, nameof(workflowKey)).ToLowerInvariant();
            Applicability = applicability;
            EvidenceStage = evidenceStage;
            DecisionReference = Optional(decisionReference);
            DecisionReason = Optional(decisionReason);
            if (applicability == ParityApplicability.NotApplicableByHostBoundary &&
                (DecisionReference == null || DecisionReason == null))
                throw new ArgumentException("Host-boundary N/A requires decision reference and reason.");
        }

        public FeatureId FeatureId { get; }
        public string Domain { get; }
        public string ReferencePath { get; }
        public string WorkflowKey { get; }
        public ParityApplicability Applicability { get; }
        public ParityEvidenceStage EvidenceStage { get; }
        public string DecisionReference { get; }
        public string DecisionReason { get; }
        public bool IsApplicable => Applicability == ParityApplicability.Applicable;
        public bool IsFullPass => !IsApplicable || EvidenceStage == ParityEvidenceStage.V25V26ParityPass;

        private static string Required(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(name + " cannot be blank.", name);
            return value.Trim();
        }

        private static string Optional(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
```

- [ ] **Step 4: Temporarily invoke `ParityManifestSmoke.Run()` from `SmokeTestRegistration.RunAll()` and run the smoke project**

Expected: the two new contract checks pass. The permanent registration remains in Task 5; keeping the call now is acceptable and will be finalized there.

- [ ] **Step 5: Commit the contract slice**

```bash
git add src/QS3D.Core/Features/ParityEvidenceContracts.cs tests/QS3D.Core.SmokeTests/ParityManifestSmoke.cs tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs
git commit -m "feat(parity): add evidence contracts"
```

---

### Task 2: Implement fail-closed manifest validation and closure reporting

**Files:**
- Create: `src/QS3D.Core/Features/ParityManifest.cs`
- Modify: `tests/QS3D.Core.SmokeTests/ParityManifestSmoke.cs`

**Interfaces:**
- Consumes: `IEnumerable<ParityFeatureRecord>` and `bool catalogComplete`.
- Produces: `ParityManifest.Records`, `ParityManifest.GetRequired(FeatureId)`, and `ParityManifest.GetClosureReport()` returning `ParityClosureReport`.

- [ ] **Step 1: Add failing smoke cases**

Add calls from `Run()` and methods equivalent to:

```csharp
private static void DuplicateFeatureIdsFailClosed()
{
    var a = Record("bim.draw.rectangle", ParityEvidenceStage.ReferenceCaptured);
    var b = Record("BIM.DRAW.RECTANGLE", ParityEvidenceStage.UiPresent);
    AssertThrows<InvalidOperationException>(() => new ParityManifest(new[] { a, b }, false));
}

private static void IncompleteCatalogCannotClaimFullParity()
{
    var manifest = new ParityManifest(new[] { Record("bim.draw.rectangle", ParityEvidenceStage.V25V26ParityPass) }, false);
    if (manifest.GetClosureReport().CanClaimFullParity) throw new InvalidOperationException("Incomplete catalog claimed full parity.");
}

private static void UiOnlyCannotClaimFullParity()
{
    var manifest = new ParityManifest(new[] { Record("bim.draw.rectangle", ParityEvidenceStage.UiPresent) }, true);
    if (manifest.GetClosureReport().CanClaimFullParity) throw new InvalidOperationException("UI-only item claimed full parity.");
}

private static void CompleteCatalogAtFinalStageCanClose()
{
    var manifest = new ParityManifest(new[] { Record("bim.draw.rectangle", ParityEvidenceStage.V25V26ParityPass) }, true);
    if (!manifest.GetClosureReport().CanClaimFullParity) throw new InvalidOperationException("Fully qualified catalog did not close.");
}

private static ParityFeatureRecord Record(string id, ParityEvidenceStage stage) =>
    new ParityFeatureRecord(new FeatureId(id), "BIM", "BLT3D reference", id, ParityApplicability.Applicable, stage);
```

- [ ] **Step 2: Run and verify failure**

Run the smoke project. Expected: compile failure for `ParityManifest`/`ParityClosureReport`.

- [ ] **Step 3: Implement validated manifest and closure report**

Required public shape:

```csharp
public sealed class ParityManifest
{
    public ParityManifest(IEnumerable<ParityFeatureRecord> records, bool catalogComplete);
    public IReadOnlyList<ParityFeatureRecord> Records { get; }
    public bool CatalogComplete { get; }
    public ParityFeatureRecord GetRequired(FeatureId id);
    public ParityClosureReport GetClosureReport();
}

public sealed class ParityClosureReport
{
    public ParityClosureReport(bool catalogComplete, int applicableCount, int fullPassCount, int hostBoundaryCount);
    public bool CatalogComplete { get; }
    public int ApplicableCount { get; }
    public int FullPassCount { get; }
    public int HostBoundaryCount { get; }
    public bool CanClaimFullParity { get; }
}
```

Implementation rules:

```csharp
if (materialized.Length == 0)
    throw new InvalidOperationException("Parity manifest cannot be empty.");
if (materialized.GroupBy(x => x.FeatureId).Any(g => g.Count() > 1))
    throw new InvalidOperationException("Parity manifest contains duplicate FeatureId values.");
if (materialized.GroupBy(x => x.WorkflowKey, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1))
    throw new InvalidOperationException("Parity manifest contains duplicate workflow keys.");
```

`CanClaimFullParity` is exactly:

```csharp
CatalogComplete && ApplicableCount > 0 && FullPassCount == ApplicableCount
```

Host-boundary N/A records are reported separately and excluded from `ApplicableCount` only because their constructor already requires explicit decision evidence.

- [ ] **Step 4: Run smoke project and verify PASS**

Expected: all existing smoke tests plus the new manifest cases pass.

- [ ] **Step 5: Commit**

```bash
git add src/QS3D.Core/Features/ParityManifest.cs tests/QS3D.Core.SmokeTests/ParityManifestSmoke.cs
git commit -m "feat(parity): add fail-closed closure model"
```

---

### Task 3: Add deterministic TSV manifest parsing

**Files:**
- Create: `src/QS3D.Core/Features/ParityManifestParser.cs`
- Modify: `tests/QS3D.Core.SmokeTests/ParityManifestSmoke.cs`

**Interfaces:**
- Consumes: UTF-8 text lines in the documented TSV format.
- Produces: `ParityManifest ParityManifestParser.Parse(IEnumerable<string> lines)`.

- [ ] **Step 1: Add failing parser tests**

```csharp
private static void ParserRequiresCatalogMetadata()
{
    AssertThrows<FormatException>(() => ParityManifestParser.Parse(new[] {
        "FeatureId\tDomain\tReferencePath\tWorkflowKey\tApplicability\tEvidenceStage\tDecisionReference\tDecisionReason",
        "bim.draw.rectangle\tBIM\tBLT3D / MÔ HÌNH BIM / Chữ nhật\tbim.draw.rectangle\tApplicable\tReferenceCaptured\t\t"
    }));
}

private static void ParserReadsDeterministicManifest()
{
    var manifest = ParityManifestParser.Parse(new[] {
        "# catalog-complete=false",
        "FeatureId\tDomain\tReferencePath\tWorkflowKey\tApplicability\tEvidenceStage\tDecisionReference\tDecisionReason",
        "bim.draw.rectangle\tBIM\tBLT3D / MÔ HÌNH BIM / Chữ nhật\tbim.draw.rectangle\tApplicable\tReferenceCaptured\t\t"
    });
    if (manifest.CatalogComplete) throw new InvalidOperationException("Parser lost catalog-complete=false.");
    AssertEqual(1, manifest.Records.Count);
}
```

- [ ] **Step 2: Run and verify compile failure**

Expected: undefined `ParityManifestParser`.

- [ ] **Step 3: Implement parser with exact format rules**

Parser rules:

```text
line 1: # catalog-complete=true|false
line 2: FeatureId<TAB>Domain<TAB>ReferencePath<TAB>WorkflowKey<TAB>Applicability<TAB>EvidenceStage<TAB>DecisionReference<TAB>DecisionReason
line 3+: exactly eight TSV fields
blank lines: ignored
other comment lines beginning with #: ignored after metadata
```

Use `Enum.TryParse(value, ignoreCase: false, out ...)`; misspelled enum names fail with `FormatException`. Reject tabs/newlines inside values by format rather than implementing escaping in P1. Instantiate `FeatureId` for canonical ID validation and let `ParityManifest` perform duplicate checks.

- [ ] **Step 4: Run smoke project and verify PASS**

Expected: parser tests and prior tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/QS3D.Core/Features/ParityManifestParser.cs tests/QS3D.Core.SmokeTests/ParityManifestSmoke.cs
git commit -m "feat(parity): parse deterministic manifest"
```

---

### Task 4: Add the host-neutral workflow binding registry

**Files:**
- Create: `src/QS3D.Core/Features/ParityWorkflowRegistry.cs`
- Create: `tests/QS3D.Core.SmokeTests/ParityWorkflowRegistrySmoke.cs`

**Interfaces:**
- Consumes: existing `FeatureId` plus a sequence of `ParityWorkflowBinding`.
- Produces: `ParityWorkflowRegistry.GetRequired(FeatureId)` and `TryGet` for later Ribbon/palette/MCP/launcher adapters.

- [ ] **Step 1: Write failing workflow registry smoke**

```csharp
using System;
using QS3D.Core.Features;

namespace QS3D.Core.SmokeTests
{
    internal static class ParityWorkflowRegistrySmoke
    {
        internal static void Run()
        {
            DuplicateFeatureFailsClosed();
            DuplicateWorkflowKeyFailsClosed();
            SemanticMutationRequiresSafetyContract();
            ValidSemanticMutationCanBeResolved();
        }

        private static void DuplicateFeatureFailsClosed()
        {
            var feature = new FeatureId("bim.draw.rectangle");
            AssertThrows<InvalidOperationException>(() => new ParityWorkflowRegistry(new[] {
                ReadOnly(feature, "bim.draw.rectangle"),
                ReadOnly(feature, "bim.draw.other")
            }));
        }

        private static void DuplicateWorkflowKeyFailsClosed()
        {
            AssertThrows<InvalidOperationException>(() => new ParityWorkflowRegistry(new[] {
                ReadOnly(new FeatureId("view.a"), "view.same"),
                ReadOnly(new FeatureId("view.b"), "VIEW.SAME")
            }));
        }

        private static void SemanticMutationRequiresSafetyContract()
        {
            AssertThrows<ArgumentException>(() => new ParityWorkflowBinding(
                new FeatureId("bim.draw.rectangle"), "bim.draw.rectangle",
                ParityWorkflowKind.SemanticMutation,
                ParityWorkflowSurface.Ui | ParityWorkflowSurface.Mcp,
                ParityWorkflowRequirement.ActiveDocument | ParityWorkflowRequirement.Project));
        }

        private static void ValidSemanticMutationCanBeResolved()
        {
            var id = new FeatureId("bim.draw.rectangle");
            var binding = new ParityWorkflowBinding(id, "bim.draw.rectangle",
                ParityWorkflowKind.SemanticMutation,
                ParityWorkflowSurface.Ui | ParityWorkflowSurface.Mcp,
                ParityWorkflowRequirement.ActiveDocument | ParityWorkflowRequirement.Project |
                ParityWorkflowRequirement.AtomicMutation | ParityWorkflowRequirement.Audit);
            var registry = new ParityWorkflowRegistry(new[] { binding });
            if (registry.GetRequired(id) != binding) throw new InvalidOperationException("Registry returned wrong binding.");
        }

        private static ParityWorkflowBinding ReadOnly(FeatureId id, string key) =>
            new ParityWorkflowBinding(id, key, ParityWorkflowKind.ReadOnly, ParityWorkflowSurface.Ui, ParityWorkflowRequirement.None);

        private static void AssertThrows<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }
    }
}
```

- [ ] **Step 2: Run and verify compile failure**

Expected: undefined workflow types.

- [ ] **Step 3: Implement the workflow binding contracts and registry**

Required contract shape:

```csharp
[Flags]
public enum ParityWorkflowSurface { None=0, Ui=1, Mcp=2, Launcher=4 }

public enum ParityWorkflowKind { ReadOnly=0, SemanticMutation=1, Infrastructure=2 }

[Flags]
public enum ParityWorkflowRequirement
{
    None=0, ActiveDocument=1, Project=2, Zone=4, Floor=8, Family=16,
    Selection=32, AtomicMutation=64, Audit=128
}

public sealed class ParityWorkflowBinding
{
    public ParityWorkflowBinding(FeatureId featureId, string workflowKey,
        ParityWorkflowKind kind, ParityWorkflowSurface surfaces,
        ParityWorkflowRequirement requirements);
    public FeatureId FeatureId { get; }
    public string WorkflowKey { get; }
    public ParityWorkflowKind Kind { get; }
    public ParityWorkflowSurface Surfaces { get; }
    public ParityWorkflowRequirement Requirements { get; }
}

public sealed class ParityWorkflowRegistry
{
    public ParityWorkflowRegistry(IEnumerable<ParityWorkflowBinding> bindings);
    public IReadOnlyList<ParityWorkflowBinding> Bindings { get; }
    public bool TryGet(FeatureId id, out ParityWorkflowBinding binding);
    public ParityWorkflowBinding GetRequired(FeatureId id);
}
```

For `SemanticMutation`, validate all four flags:

```csharp
var required = ParityWorkflowRequirement.ActiveDocument |
               ParityWorkflowRequirement.Project |
               ParityWorkflowRequirement.AtomicMutation |
               ParityWorkflowRequirement.Audit;
if ((requirements & required) != required)
    throw new ArgumentException("Semantic mutation workflows require ActiveDocument, Project, AtomicMutation and Audit.", nameof(requirements));
```

Also reject `ParityWorkflowSurface.None`, blank workflow keys, duplicate feature IDs and duplicate workflow keys case-insensitively.

- [ ] **Step 4: Run smoke project and verify PASS**

Expected: registry tests pass without host libraries.

- [ ] **Step 5: Commit**

```bash
git add src/QS3D.Core/Features/ParityWorkflowRegistry.cs tests/QS3D.Core.SmokeTests/ParityWorkflowRegistrySmoke.cs
git commit -m "feat(parity): add workflow binding registry"
```

---

### Task 5: Seed the auditable manifest and register permanent smoke coverage

**Files:**
- Create: `docs/BLT3D-PARITY-MANIFEST.tsv`
- Modify: `tests/QS3D.Core.SmokeTests/ParityManifestSmoke.cs`
- Modify: `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs`

**Interfaces:**
- Consumes: parser/manifest contracts from Tasks 1-3.
- Produces: a checked-in manifest whose initial state truthfully blocks a 100% parity claim.

- [ ] **Step 1: Create the initial TSV inventory anchors**

Use exactly this header and initial records:

```text
# catalog-complete=false
FeatureId	Domain	ReferencePath	WorkflowKey	Applicability	EvidenceStage	DecisionReference	DecisionReason
shell.start	Shell	BLT3D / KHỞI ĐẦU	shell.start	Applicable	ReferenceCaptured		
project.setup	Project	BLT3D / THIẾT LẬP DỰ ÁN	project.setup	Applicable	ReferenceCaptured		
bim.authoring	BIM	BLT3D / MÔ HÌNH BIM	bim.authoring	Applicable	ReferenceCaptured		
recognition	Recognition	BLT3D / NHẬN DẠNG	recognition	Applicable	ReferenceCaptured		
draw	Draw	BLT3D / VẼ	draw	Applicable	ReferenceCaptured		
tool.editing	Tool	BLT3D / TOOL	tool.editing	Applicable	ReferenceCaptured		
modeling	Modeling	BLT3D / MODELING	modeling	Applicable	ReferenceCaptured		
rebar	Rebar	BLT3D / CỐT THÉP	rebar	Applicable	ReferenceCaptured		
view	View	BLT3D / XEM	view	Applicable	ReferenceCaptured		
quantity	Quantity	BLT3D / ĐỊNH LƯỢNG	quantity	Applicable	ReferenceCaptured		
revision	Revision	BLT3D / BẢN SỬA ĐỔI	revision	Applicable	ReferenceCaptured		
drawing-manager	DrawingManager	BLT3D / Drawing Manager	drawing-manager	Applicable	ReferenceCaptured		
ifc	Interoperability	BLT3D / IFC	ifc	Applicable	ReferenceCaptured		
ai.luna	AI	BLT3D / LUNA AI	ai.luna	Applicable	ReferenceCaptured		
mcp.direct-cad	MCP	BLT3D MCP reference + QS3D direct MCP-to-CAD requirement	mcp.direct-cad	Applicable	ReferenceCaptured		
settings	Settings	BLT3D / Settings	settings	Applicable	ReferenceCaptured		
launcher	Infrastructure	BLT3D Launcher observed workflow	launcher	Applicable	ReferenceCaptured		
license.activation	Infrastructure	BLT3D activation UX reference; QS3D-owned implementation	license.activation	Applicable	ReferenceCaptured		
updater	Infrastructure	BLT3D updater UX reference; QS3D-owned implementation	updater	Applicable	ReferenceCaptured		
installer	Infrastructure	BLT3D install/repair workflow reference	installer	Applicable	ReferenceCaptured		
```

The file intentionally says `catalog-complete=false`; P2+ carriers expand the anchors into individual feature rows before any closure claim.

- [ ] **Step 2: Add a smoke that reads the checked-in file and proves closure is blocked**

```csharp
private static void RepositoryManifestParsesAndBlocksPrematureClosure()
{
    var path = Path.Combine("docs", "BLT3D-PARITY-MANIFEST.tsv");
    if (!File.Exists(path)) throw new InvalidOperationException("Missing parity manifest: " + path);
    var manifest = ParityManifestParser.Parse(File.ReadAllLines(path));
    if (manifest.Records.Count < 20) throw new InvalidOperationException("Parity manifest lost approved domain anchors.");
    if (manifest.GetClosureReport().CanClaimFullParity) throw new InvalidOperationException("Seed manifest must not claim full parity.");
}
```

Add `using System.IO;` and invoke this method from `ParityManifestSmoke.Run()`.

- [ ] **Step 3: Permanently register both smoke classes**

Append these calls in `SmokeTestRegistration.RunAll()` near the existing feature/workspace contract tests:

```csharp
ParityManifestSmoke.Run();
ParityWorkflowRegistrySmoke.Run();
```

Do not leave duplicate temporary registration from Task 1.

- [ ] **Step 4: Run the full Core smoke suite**

```powershell
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Expected: exit 0 and final `ALL PASS`.

- [ ] **Step 5: Commit**

```bash
git add docs/BLT3D-PARITY-MANIFEST.tsv tests/QS3D.Core.SmokeTests/ParityManifestSmoke.cs tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs
git commit -m "test(parity): seed manifest closure guard"
```

---

### Task 6: Verify host-neutrality and V25/V26 compile compatibility

**Files:**
- No new files unless a compile error exposes a P1-owned contract defect.

**Interfaces:**
- Consumes: completed P1 branch.
- Produces: exact-head build/smoke evidence suitable for the P1 PR.

- [ ] **Step 1: Build Core directly**

```powershell
dotnet build src/QS3D.Core/QS3D.Core.csproj -c Release
```

Expected: exit 0.

- [ ] **Step 2: Run the full Core smoke suite again on the exact head**

```powershell
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Expected: exit 0, `ALL PASS`.

- [ ] **Step 3: Build V25 host project**

```powershell
dotnet build src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj -c Release
```

Expected: exit 0 when the licensed/reference dependency environment required by the repository is available. If the repository classifies this environment as LOCAL_ONLY, record it as LOCAL_ONLY rather than fabricating PASS.

- [ ] **Step 4: Build V26 host project**

```powershell
dotnet build src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj -c Release
```

Expected: exit 0 when the V26 dependency environment is available; otherwise use the repository's documented LOCAL_ONLY handoff classification.

- [ ] **Step 5: Run repository preflight for the P1 carrier before opening/merging its PR**

```powershell
python scripts/preflight-agent-reservation-v2.py
python scripts/preflight-agent-lane-collision.py
```

Expected: both exit 0 for the dedicated P1 Reservation-v2 carrier. If either fails because another carrier owns one of the planned files, stop and reconcile ownership; do not bypass or weaken the guard.

- [ ] **Step 6: Final commit only if verification required a P1-owned correction**

```bash
git status --short
git add <only-the-P1-owned-corrected-paths>
git commit -m "fix(parity): close P1 verification gap"
```

If `git status --short` is empty, do not create an empty commit.

---

## P1 Completion Gate

P1 is complete only when all of the following are true:

1. There is exactly one canonical `FeatureId` type and parity reuses it.
2. Duplicate FeatureId/workflow keys fail closed.
3. `catalog-complete=false` blocks the 100% claim even if every current row is at final evidence stage.
4. An applicable feature below `V25V26ParityPass` blocks the 100% claim.
5. Host-boundary N/A requires explicit product-decision evidence.
6. Semantic mutation workflow registration requires ActiveDocument + Project + AtomicMutation + Audit.
7. `docs/BLT3D-PARITY-MANIFEST.tsv` parses deterministically and starts incomplete.
8. Core smoke is green on exact head.
9. V25/V26 compile evidence is recorded truthfully as PASS or documented LOCAL_ONLY according to actual environment.
10. The implementation travels through its own Reservation-v2 Issue/branch/PR; no production P1 code is committed to the design carrier.