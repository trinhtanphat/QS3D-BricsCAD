# BLT3D Parity Foundation P1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a host-neutral, auditable parity manifest/closure model and workflow-binding registry that reuses QS3D's existing `FeatureId`, blocks premature 100% claims, and gives later UI/MCP/launcher carriers one canonical feature/workflow contract.

**Architecture:** Extend `QS3D.Core.Features`; do not create a second feature identity system. A deterministic TSV manifest records inventory/evidence, `ParityManifest` validates it and computes closure, and `ParityWorkflowRegistry` binds `FeatureId` values to stable workflow keys, surfaces, and safety requirements without BricsCAD references. P1 adds no UI and performs no CAD mutation.

**Tech Stack:** C# `netstandard2.0`, existing `QS3D.Core.Features.FeatureId`, `QS3D.Core.SmokeTests`, UTF-8 TSV, V25 `net48`, V26 `net8.0-windows` compile verification.

**Spec:** `docs/superpowers/specs/2026-09-10-blt3d-full-parity-design.md`

## Global Constraints

- `QS3D-BricsCAD` remains a Windows x64 BricsCAD-hosted plugin.
- Reuse `QS3D.Core.Features.FeatureId` from `FeatureInteractionContracts.cs`.
- Core must remain host-neutral: no BricsCAD/Teigha/AutoCAD runtime types in P1 contracts.
- BLT3D is reference evidence only; no BLT3D binary, key, credential, signing material, license algorithm, or runtime dependency is added.
- Full parity is claimable only when `CatalogComplete == true` and every applicable feature is `V25V26ParityPass`.
- `NotApplicableByHostBoundary` requires a nonblank product-decision reference and reason.
- Semantic mutation workflow bindings must require active document, project, atomic mutation, and audit.
- P1 production code must travel through its own Reservation-v2 implementation carrier; this document is only the plan.

## File Structure

- Create `src/QS3D.Core/Features/ParityEvidenceContracts.cs` — evidence/applicability records.
- Create `src/QS3D.Core/Features/ParityManifest.cs` — validated snapshot and closure report.
- Create `src/QS3D.Core/Features/ParityManifestParser.cs` — deterministic TSV parser.
- Create `src/QS3D.Core/Features/ParityWorkflowRegistry.cs` — workflow binding/safety contracts.
- Create `docs/BLT3D-PARITY-MANIFEST.tsv` — initial incomplete inventory anchors.
- Create `tests/QS3D.Core.SmokeTests/ParityManifestSmoke.cs` — evidence/parser/closure tests.
- Create `tests/QS3D.Core.SmokeTests/ParityWorkflowRegistrySmoke.cs` — registry/safety tests.
- Modify `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs` — register both smoke classes.

---

### Task 1: Add parity evidence contracts

**Files:**
- Create: `src/QS3D.Core/Features/ParityEvidenceContracts.cs`
- Create: `tests/QS3D.Core.SmokeTests/ParityManifestSmoke.cs`
- Modify: `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs`

**Interfaces:**
- Consumes: `FeatureId`.
- Produces: `ParityEvidenceStage`, `ParityApplicability`, `ParityFeatureRecord`.

- [ ] **Step 1: Write the failing smoke**

```csharp
using System;
using QS3D.Core.Features;

namespace QS3D.Core.SmokeTests
{
    internal static class ParityManifestSmoke
    {
        internal static void Run()
        {
            var record = new ParityFeatureRecord(
                new FeatureId("BIM.Draw.Rectangle"), " BIM ",
                " BLT3D / MÔ HÌNH BIM / Chữ nhật ", " BIM.Draw.Rectangle ",
                ParityApplicability.Applicable, ParityEvidenceStage.ReferenceCaptured);
            Equal("bim.draw.rectangle", record.FeatureId.ToString());
            Equal("BIM", record.Domain);
            Equal("bim.draw.rectangle", record.WorkflowKey);

            Throws<ArgumentException>(() => new ParityFeatureRecord(
                new FeatureId("host.unsupported"), "Host", "reference", "host.unsupported",
                ParityApplicability.NotApplicableByHostBoundary, ParityEvidenceStage.ReferenceCaptured));
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); } catch (T) { return; }
            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }
    }
}
```

Add `ParityManifestSmoke.Run();` near the existing feature/workspace smoke registrations.

- [ ] **Step 2: Run to prove RED**

```powershell
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Expected: compile failure because parity contract types do not exist.

- [ ] **Step 3: Implement minimal contracts**

```csharp
using System;

namespace QS3D.Core.Features
{
    public enum ParityEvidenceStage
    {
        ReferenceCaptured = 0, UiPresent = 1, CommandWired = 2,
        SemanticBehaviorPass = 3, SaveReopenPass = 4, V25V26ParityPass = 5
    }

    public enum ParityApplicability { Applicable = 0, NotApplicableByHostBoundary = 1 }

    public sealed class ParityFeatureRecord
    {
        public ParityFeatureRecord(FeatureId featureId, string domain, string referencePath,
            string workflowKey, ParityApplicability applicability, ParityEvidenceStage evidenceStage,
            string decisionReference = null, string decisionReason = null)
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

        private static string Required(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(name + " cannot be blank.", name);
            return value.Trim();
        }
        private static string Optional(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
```

- [ ] **Step 4: Run to prove GREEN**

Same smoke command. Expected: new smoke and existing suite pass.

- [ ] **Step 5: Commit**

```bash
git add src/QS3D.Core/Features/ParityEvidenceContracts.cs tests/QS3D.Core.SmokeTests/ParityManifestSmoke.cs tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs
git commit -m "feat(parity): add evidence contracts"
```

---

### Task 2: Add fail-closed manifest and closure report

**Files:**
- Create: `src/QS3D.Core/Features/ParityManifest.cs`
- Modify: `tests/QS3D.Core.SmokeTests/ParityManifestSmoke.cs`

**Interfaces:**
- Produces: `ParityManifest(IEnumerable<ParityFeatureRecord>, bool)`, `GetRequired(FeatureId)`, `GetClosureReport()`.

- [ ] **Step 1: Add failing closure tests**

```csharp
private static ParityFeatureRecord Record(string id, ParityEvidenceStage stage) =>
    new ParityFeatureRecord(new FeatureId(id), "BIM", "BLT3D reference", id,
        ParityApplicability.Applicable, stage);

private static void ClosureRules()
{
    Throws<InvalidOperationException>(() => new ParityManifest(new[] {
        Record("bim.draw.rectangle", ParityEvidenceStage.ReferenceCaptured),
        Record("BIM.DRAW.RECTANGLE", ParityEvidenceStage.UiPresent)
    }, false));

    var incompleteCatalog = new ParityManifest(new[] {
        Record("bim.draw.rectangle", ParityEvidenceStage.V25V26ParityPass)
    }, false);
    if (incompleteCatalog.GetClosureReport().CanClaimFullParity)
        throw new InvalidOperationException("Incomplete catalog claimed full parity.");

    var uiOnly = new ParityManifest(new[] {
        Record("bim.draw.rectangle", ParityEvidenceStage.UiPresent)
    }, true);
    if (uiOnly.GetClosureReport().CanClaimFullParity)
        throw new InvalidOperationException("UI-only feature claimed full parity.");

    var complete = new ParityManifest(new[] {
        Record("bim.draw.rectangle", ParityEvidenceStage.V25V26ParityPass)
    }, true);
    if (!complete.GetClosureReport().CanClaimFullParity)
        throw new InvalidOperationException("Qualified catalog did not close.");
}
```

Invoke `ClosureRules()` from `Run()`.

- [ ] **Step 2: Run to prove RED**

Expected: compile failure for `ParityManifest`.

- [ ] **Step 3: Implement manifest validation and report**

Required API:

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
    public bool CatalogComplete { get; }
    public int ApplicableCount { get; }
    public int FullPassCount { get; }
    public int HostBoundaryCount { get; }
    public bool CanClaimFullParity => CatalogComplete && ApplicableCount > 0 && FullPassCount == ApplicableCount;
}
```

Constructor rules:

```csharp
if (materialized.Length == 0)
    throw new InvalidOperationException("Parity manifest cannot be empty.");
if (materialized.GroupBy(x => x.FeatureId).Any(g => g.Count() > 1))
    throw new InvalidOperationException("Parity manifest contains duplicate FeatureId values.");
if (materialized.GroupBy(x => x.WorkflowKey, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1))
    throw new InvalidOperationException("Parity manifest contains duplicate workflow keys.");
```

`FullPassCount` counts only applicable rows at `V25V26ParityPass`; host-boundary rows are counted separately.

- [ ] **Step 4: Run to prove GREEN**

Expected: all Core smoke tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/QS3D.Core/Features/ParityManifest.cs tests/QS3D.Core.SmokeTests/ParityManifestSmoke.cs
git commit -m "feat(parity): add closure model"
```

---

### Task 3: Add deterministic TSV parser

**Files:**
- Create: `src/QS3D.Core/Features/ParityManifestParser.cs`
- Modify: `tests/QS3D.Core.SmokeTests/ParityManifestSmoke.cs`

**Interfaces:**
- Produces: `static ParityManifest Parse(IEnumerable<string> lines)`.

- [ ] **Step 1: Add failing parser tests**

```csharp
private static void ParserRules()
{
    Throws<FormatException>(() => ParityManifestParser.Parse(new[] {
        "FeatureId\tDomain\tReferencePath\tWorkflowKey\tApplicability\tEvidenceStage\tDecisionReference\tDecisionReason",
        "bim.draw.rectangle\tBIM\treference\tbim.draw.rectangle\tApplicable\tReferenceCaptured\t\t"
    }));

    var parsed = ParityManifestParser.Parse(new[] {
        "# catalog-complete=false",
        "FeatureId\tDomain\tReferencePath\tWorkflowKey\tApplicability\tEvidenceStage\tDecisionReference\tDecisionReason",
        "bim.draw.rectangle\tBIM\tBLT3D / MÔ HÌNH BIM / Chữ nhật\tbim.draw.rectangle\tApplicable\tReferenceCaptured\t\t"
    });
    if (parsed.CatalogComplete || parsed.Records.Count != 1)
        throw new InvalidOperationException("Deterministic parser lost manifest metadata or row count.");
}
```

Invoke `ParserRules()` from `Run()`.

- [ ] **Step 2: Run to prove RED**

Expected: undefined `ParityManifestParser`.

- [ ] **Step 3: Implement exact parsing rules**

```text
line 1: # catalog-complete=true|false
line 2: FeatureId<TAB>Domain<TAB>ReferencePath<TAB>WorkflowKey<TAB>Applicability<TAB>EvidenceStage<TAB>DecisionReference<TAB>DecisionReason
remaining data rows: exactly eight fields
blank lines: ignored
additional comment lines beginning with #: ignored only after catalog metadata
```

Use case-sensitive `Enum.TryParse(..., false, out ...)`; malformed enum names and wrong field counts throw `FormatException`. Construct `FeatureId` for its existing canonical validation and pass records to `ParityManifest` for duplicate validation.

- [ ] **Step 4: Run to prove GREEN**

Expected: parser and prior tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/QS3D.Core/Features/ParityManifestParser.cs tests/QS3D.Core.SmokeTests/ParityManifestSmoke.cs
git commit -m "feat(parity): parse deterministic manifest"
```

---

### Task 4: Add workflow binding/safety registry

**Files:**
- Create: `src/QS3D.Core/Features/ParityWorkflowRegistry.cs`
- Create: `tests/QS3D.Core.SmokeTests/ParityWorkflowRegistrySmoke.cs`
- Modify: `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs`

**Interfaces:**
- Produces: `ParityWorkflowBinding`, `ParityWorkflowRegistry`, surface/kind/requirement enums.

- [ ] **Step 1: Write failing registry smoke**

```csharp
using System;
using QS3D.Core.Features;

namespace QS3D.Core.SmokeTests
{
    internal static class ParityWorkflowRegistrySmoke
    {
        internal static void Run()
        {
            Throws<ArgumentException>(() => new ParityWorkflowBinding(
                new FeatureId("bim.draw.rectangle"), "bim.draw.rectangle",
                ParityWorkflowKind.SemanticMutation,
                ParityWorkflowSurface.Ui | ParityWorkflowSurface.Mcp,
                ParityWorkflowRequirement.ActiveDocument | ParityWorkflowRequirement.Project));

            var id=new FeatureId("bim.draw.rectangle");
            var safe=new ParityWorkflowBinding(id,"bim.draw.rectangle",
                ParityWorkflowKind.SemanticMutation,
                ParityWorkflowSurface.Ui | ParityWorkflowSurface.Mcp,
                ParityWorkflowRequirement.ActiveDocument | ParityWorkflowRequirement.Project |
                ParityWorkflowRequirement.AtomicMutation | ParityWorkflowRequirement.Audit);
            var registry=new ParityWorkflowRegistry(new[]{safe});
            if(!ReferenceEquals(safe,registry.GetRequired(id)))
                throw new InvalidOperationException("Registry returned the wrong binding.");

            Throws<InvalidOperationException>(() => new ParityWorkflowRegistry(new[]{safe,safe}));
        }

        private static void Throws<T>(Action action) where T:Exception
        {
            try { action(); } catch(T) { return; }
            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }
    }
}
```

Add `ParityWorkflowRegistrySmoke.Run();` near `ParityManifestSmoke.Run()`.

- [ ] **Step 2: Run to prove RED**

Expected: compile failure for undefined workflow types.

- [ ] **Step 3: Implement registry contracts**

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
```

Required binding/registry API:

```csharp
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

For `SemanticMutation`, require exactly this minimum safety set:

```csharp
var minimum = ParityWorkflowRequirement.ActiveDocument |
              ParityWorkflowRequirement.Project |
              ParityWorkflowRequirement.AtomicMutation |
              ParityWorkflowRequirement.Audit;
if ((requirements & minimum) != minimum)
    throw new ArgumentException("Semantic mutation workflows require ActiveDocument, Project, AtomicMutation and Audit.", nameof(requirements));
```

Also reject blank keys, `ParityWorkflowSurface.None`, duplicate `FeatureId`, and duplicate workflow keys case-insensitively.

- [ ] **Step 4: Run to prove GREEN**

Expected: full Core smoke suite passes.

- [ ] **Step 5: Commit**

```bash
git add src/QS3D.Core/Features/ParityWorkflowRegistry.cs tests/QS3D.Core.SmokeTests/ParityWorkflowRegistrySmoke.cs tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs
git commit -m "feat(parity): add workflow registry"
```

---

### Task 5: Seed the checked-in manifest and prove it blocks premature closure

**Files:**
- Create: `docs/BLT3D-PARITY-MANIFEST.tsv`
- Modify: `tests/QS3D.Core.SmokeTests/ParityManifestSmoke.cs`

**Interfaces:**
- Consumes: TSV parser.
- Produces: auditable initial catalog anchors with `catalog-complete=false`.

- [ ] **Step 1: Create the initial manifest**

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

- [ ] **Step 2: Add repository-file smoke**

Add `using System.IO;` and:

```csharp
private static void RepositoryManifestBlocksPrematureClosure()
{
    var path=Path.Combine("docs","BLT3D-PARITY-MANIFEST.tsv");
    if(!File.Exists(path)) throw new InvalidOperationException("Missing parity manifest: " + path);
    var manifest=ParityManifestParser.Parse(File.ReadAllLines(path));
    if(manifest.Records.Count < 20) throw new InvalidOperationException("Parity manifest lost approved domain anchors.");
    if(manifest.GetClosureReport().CanClaimFullParity) throw new InvalidOperationException("Seed manifest must not claim full parity.");
}
```

Invoke it from `ParityManifestSmoke.Run()`.

- [ ] **Step 3: Run Core smoke**

```powershell
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Expected: exit 0 and final `ALL PASS`.

- [ ] **Step 4: Commit**

```bash
git add docs/BLT3D-PARITY-MANIFEST.tsv tests/QS3D.Core.SmokeTests/ParityManifestSmoke.cs
git commit -m "test(parity): seed manifest closure guard"
```

---

### Task 6: Exact-head verification before P1 PR merge

**Files:**
- No new files unless a defect is found in one of the P1-owned paths listed in this plan.

**Interfaces:**
- Produces: truthful exact-head Core/V25/V26/preflight evidence.

- [ ] **Step 1: Build Core**

```powershell
dotnet build src/QS3D.Core/QS3D.Core.csproj -c Release
```

Expected: exit 0.

- [ ] **Step 2: Run all Core smoke tests**

```powershell
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Expected: exit 0 and `ALL PASS`.

- [ ] **Step 3: Build V25**

```powershell
dotnet build src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj -c Release
```

Expected: PASS when required local references are present; otherwise record the repository-defined LOCAL_ONLY state rather than claiming PASS.

- [ ] **Step 4: Build V26**

```powershell
dotnet build src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj -c Release
```

Expected: PASS when required V26 references are present; otherwise record LOCAL_ONLY truthfully.

- [ ] **Step 5: Run ownership/collision preflights on the dedicated P1 carrier**

```powershell
python scripts/preflight-agent-reservation-v2.py
python scripts/preflight-agent-lane-collision.py
```

Expected: both exit 0. If either reports overlapping ownership, reconcile the carrier instead of bypassing the guard.

- [ ] **Step 6: If verification exposed a P1-owned defect, fix it with a new RED→GREEN cycle and stage only the known P1 paths**

```bash
git status --short
git add src/QS3D.Core/Features/ParityEvidenceContracts.cs src/QS3D.Core/Features/ParityManifest.cs src/QS3D.Core/Features/ParityManifestParser.cs src/QS3D.Core/Features/ParityWorkflowRegistry.cs docs/BLT3D-PARITY-MANIFEST.tsv tests/QS3D.Core.SmokeTests/ParityManifestSmoke.cs tests/QS3D.Core.SmokeTests/ParityWorkflowRegistrySmoke.cs tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs
git diff --cached --check
git commit -m "fix(parity): close P1 verification gap"
```

If `git status --short` is empty, skip this step and do not create an empty commit.

## P1 Completion Gate

P1 is complete only when there is one canonical `FeatureId`; duplicate feature/workflow identities fail closed; incomplete catalog and any applicable row below `V25V26ParityPass` block full parity; host-boundary N/A requires explicit decision evidence; semantic mutations require ActiveDocument + Project + AtomicMutation + Audit; the checked-in TSV parses and remains honestly incomplete; Core smoke is green; V25/V26 evidence is truthful; and all production changes are merged through a dedicated Reservation-v2 Issue/branch/PR.