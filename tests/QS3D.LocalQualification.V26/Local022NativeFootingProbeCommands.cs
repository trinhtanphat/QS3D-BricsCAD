using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.LocalQualification.V26
{
    /// <summary>
    /// Automation-only LOCAL-022 native evidence probe.  It loads the frozen product unchanged
    /// and reaches its non-public V26 product authoring seams only by exact reflection.  It is deliberately
    /// not a replacement geometry implementation and writes only sanitized phase markers.
    /// </summary>
    public sealed class Local022NativeFootingProbeCommands
    {
        private const string Schema = "QS3D_LOCAL022_V26_NATIVE_V1";
        private const string RunIdVariable = "QS3D_LOCAL022_V26_RUN_ID";
        private const string RootVariable = "QS3D_LOCAL022_V26_ROOT";
        private const string DrawingVariable = "QS3D_LOCAL022_V26_DRAWING";
        private const string ProductVariable = "QS3D_LOCAL022_V26_PRODUCT_DLL";
        private const string ProbeVariable = "QS3D_LOCAL022_V26_PROBE_DLL";
        private const string PhaseVariable = "QS3D_LOCAL022_V26_PHASE";
        private const double Tolerance = 1e-7d;
        private static readonly object Sync = new object();
        private static readonly object VoidResult = new object();
        private static RunState? _state;

        [CommandMethod("QL22RUN", CommandFlags.Modal)]
        public void Run() => Execute("run", RunPhase);

        [CommandMethod("QL22SAVED", CommandFlags.Modal)]
        public void Saved() => Execute("saved", SavedPhase);

        [CommandMethod("QL22REOPEN", CommandFlags.Modal)]
        public void Reopen() => Execute("reopen", ReopenPhase);

        private static IDictionary<string, bool> RunPhase(Context context)
        {
            RequireMeterDrawing(context.Document);
            var project = GetOrCreateProject(context.Document);
            var semanticBaseline = project.Elements.Count;
            var familyBaseline = project.Families.Count;
            var nativeBaseline = CountModelSpaceEntities(context.Document);
            var family = CreateSingleFootingFamily(project, context.RunId);

            var genericRejected = RejectGenericFoundationPlacement(context.Document, project, family, context.RunId);

            var box = new SingleFootingDimensions(2d, 2d, 2d, 2d, 1d, 0d);
            ApplyFamily(family, box);
            ProjectFamilyActivationService.SetActive(project, family.Id);
            Place(context.Document, new Point3d(10d, 10d, 0d));
            var boxElement = RequireSingleFootingElement(project, family.Id, null);
            VerifySolid(context.Document, boxElement, box, new Point3d(10d, 10d, 0d), "box");

            var tapered = new SingleFootingDimensions(3d, 2d, 1d, 1d, 1d, 1d);
            ApplyFamily(family, tapered);
            ProjectFamilyActivationService.SetActive(project, family.Id);
            Place(context.Document, new Point3d(20d, 10d, 0d));
            var taperedElement = RequireSingleFootingElement(project, family.Id, boxElement.Id);
            VerifySolid(context.Document, taperedElement, tapered, new Point3d(20d, 10d, 0d), "tapered");

            var previousBox = OwnedHandle(boxElement);
            var previousTapered = OwnedHandle(taperedElement);
            var boxElementId = boxElement.Id;
            var taperedElementId = taperedElement.Id;
            var edited = new SingleFootingDimensions(4d, 2d, 2d, 1d, 1d, 1d);
            var regenerated = Regenerate(context.Document, project, family, edited);
            if (regenerated != 2) throw new ProbeException("regeneration_count");
            RequireErased(context.Document, previousBox);
            RequireErased(context.Document, previousTapered);
            boxElement = RequireElementById(project, boxElementId);
            taperedElement = RequireElementById(project, taperedElementId);
            if (string.Equals(OwnedHandle(boxElement), previousBox, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(OwnedHandle(taperedElement), previousTapered, StringComparison.OrdinalIgnoreCase))
                throw new ProbeException("generated_handle_not_replaced");
            VerifySolid(context.Document, boxElement, edited, new Point3d(10d, 10d, 0d), "edited_box");
            VerifySolid(context.Document, taperedElement, edited, new Point3d(20d, 10d, 0d), "edited_tapered");

            RequireTotalDeltas(context.Document, project, semanticBaseline, familyBaseline, nativeBaseline);
            WriteContinuity(context, project, semanticBaseline, familyBaseline, nativeBaseline);
            lock (Sync) _state = new RunState(context.Document, project.ProjectId, family.Id, context.RunId);
            RequireMcpMutationBoundaryPaused(context.Product);
            return Checks(
                "active_disposable_drawing", true,
                "host_major_26", true,
                "product_location_exact", true,
                "mcp_mutation_boundary_paused", true,
                "meter_units", true,
                "box_placement", true,
                "tapered_repeated_placement", true,
                "solid_mass_volume_extents", true,
                "generated_ownership", true,
                "family_regeneration", true,
                "former_generated_handle_erased", true,
                "generic_foundation_rejected_before_mutation", genericRejected,
                "exact_native_semantic_cardinality", true);
        }

        private static IDictionary<string, bool> SavedPhase(Context context)
        {
            RequireMeterDrawing(context.Document);
            var project = GetOrCreateProject(context.Document);
            RunState? state;
            lock (Sync) state = _state;
            if (state == null || !ReferenceEquals(state.Document, context.Document) ||
                !string.Equals(state.RunId, context.RunId, StringComparison.Ordinal) ||
                !string.Equals(state.ProjectId, project.ProjectId, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(state.FamilyId, ExpectedFamilyId(context.RunId), StringComparison.OrdinalIgnoreCase))
                throw new ProbeException("saved_session_identity");
            var continuity = RequireContinuity(context, project);
            RequireTotalDeltas(context.Document, project, continuity.SemanticBaseline, continuity.FamilyBaseline, continuity.NativeBaseline);
            VerifyExpectedPersistedState(context.Document, project, context.RunId, "saved");
            if (!File.Exists(ProjectPath(context.Document))) throw new ProbeException("sidecar_missing");
            RequireMcpMutationBoundaryPaused(context.Product);
            return Checks(
                "active_disposable_drawing", true,
                "mcp_mutation_boundary_paused", true,
                "sidecar_exists_after_qs3dsave", true,
                "native_database_still_open", true,
                "saved_semantic_native_state", true,
                "saved_exact_cardinality", true);
        }

        private static IDictionary<string, bool> ReopenPhase(Context context)
        {
            RequireMeterDrawing(context.Document);
            lock (Sync)
                if (_state != null) throw new ProbeException("reopen_not_fresh_process");
            var project = GetOrCreateProject(context.Document);
            var continuity = RequireContinuity(context, project);
            RequireTotalDeltas(context.Document, project, continuity.SemanticBaseline, continuity.FamilyBaseline, continuity.NativeBaseline);
            VerifyExpectedPersistedState(context.Document, project, context.RunId, "reopened");
            RequireMcpMutationBoundaryPaused(context.Product);
            return Checks(
                "active_disposable_drawing", true,
                "mcp_mutation_boundary_paused", true,
                "cold_project_bind", true,
                "reopened_semantic_identity", true,
                "reopened_generated_solids_live", true,
                "reopened_dimensions_volume_extents", true,
                "reopened_exact_cardinality", true);
        }

        private static Context BindContext(string expectedPhase)
        {
            var runId = RequireNonce(Environment.GetEnvironmentVariable(RunIdVariable));
            var root = RequirePath(Environment.GetEnvironmentVariable(RootVariable), "root_missing");
            var drawing = RequirePath(Environment.GetEnvironmentVariable(DrawingVariable), "drawing_missing");
            var productPath = RequirePath(Environment.GetEnvironmentVariable(ProductVariable), "product_path_missing");
            var probePath = RequirePath(Environment.GetEnvironmentVariable(ProbeVariable), "probe_path_missing");
            var phase = (Environment.GetEnvironmentVariable(PhaseVariable) ?? string.Empty).Trim().ToLowerInvariant();
            if (!string.Equals(phase, expectedPhase, StringComparison.Ordinal) &&
                !(string.Equals(expectedPhase, "saved", StringComparison.Ordinal) && string.Equals(phase, "run", StringComparison.Ordinal))) throw new ProbeException("phase_mismatch");
            if (!IsChildPath(root, drawing)) throw new ProbeException("drawing_outside_root");
            var document = Application.DocumentManager.MdiActiveDocument ?? throw new ProbeException("active_document_missing");
            var documentPath = RequirePath(document.Name, "drawing_not_saved");
            if (!SamePath(documentPath, drawing)) throw new ProbeException("drawing_identity_mismatch");
            if (!File.Exists(drawing)) throw new ProbeException("drawing_missing_on_disk");
            RequireHostMajor26();
            var product = ProductAssembly();
            if (!SamePath(product.Location, productPath)) throw new ProbeException("product_location_mismatch");
            if (!string.Equals(product.GetName().Name, "QS3D.BricsCAD.V26", StringComparison.Ordinal)) throw new ProbeException("product_identity_mismatch");
            var core = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(assembly =>
                string.Equals(assembly.GetName().Name, "QS3D.Core", StringComparison.OrdinalIgnoreCase))
                ?? throw new ProbeException("core_not_loaded");
            var expectedCore = Path.Combine(Path.GetDirectoryName(productPath) ?? throw new ProbeException("product_root_missing"), "QS3D.Core.dll");
            if (!SamePath(core.Location, expectedCore)) throw new ProbeException("core_location_mismatch");
            if (!SamePath(Assembly.GetExecutingAssembly().Location, probePath)) throw new ProbeException("probe_location_mismatch");
            PauseMcpMutationBoundary(product);
            return new Context(document, runId, root, drawing, productPath, product);
        }

        private static void PauseMcpMutationBoundary(Assembly product)
        {
            InvokeStatic(
                product,
                "QS3D.BricsCAD.V25.McpDesktopControlSession",
                "PauseFromLocalUser",
                new[] { typeof(string) },
                "LOCAL-022 native qualification; MCP testing is paused");
            RequireMcpMutationBoundaryPaused(product);
        }

        private static void RequireMcpMutationBoundaryPaused(Assembly product)
        {
            var agent = product.GetType("QS3D.BricsCAD.V25.McpCadAgentRuntime", true)
                ?? throw new ProbeException("mcp_agent_type_missing");
            var session = product.GetType("QS3D.BricsCAD.V25.McpDesktopControlSession", true)
                ?? throw new ProbeException("mcp_session_type_missing");
            var stopped = agent.GetProperty("AutomationStopped", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var enabled = session.GetProperty("IsEnabled", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var consent = session.GetProperty("ConsentState", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (stopped == null || enabled == null || consent == null ||
                !Convert.ToBoolean(stopped.GetValue(null, null), CultureInfo.InvariantCulture) ||
                Convert.ToBoolean(enabled.GetValue(null, null), CultureInfo.InvariantCulture) ||
                !string.Equals(Convert.ToString(consent.GetValue(null, null), CultureInfo.InvariantCulture), "PAUSED", StringComparison.Ordinal))
                throw new ProbeException("mcp_mutation_boundary_not_paused");
        }

        private static ProjectFamily CreateSingleFootingFamily(ProjectState project, string nonce)
        {
            var family = ProjectFamilyService.Create(project, ExpectedFamilyId(nonce), "LOCAL-022 Footing", ElementCategory.Foundation);
            ApplyFamily(family, new SingleFootingDimensions(2d, 2d, 2d, 2d, 1d, 0d));
            return family;
        }

        private static void ApplyFamily(ProjectFamily family, SingleFootingDimensions dimensions) =>
            InvokeStatic(ProductAssembly(), "QS3D.BricsCAD.V25.SingleFootingContract", "Apply", new[] { typeof(ProjectFamily), typeof(SingleFootingDimensions) }, family, dimensions);

        private static bool RejectGenericFoundationPlacement(Document document, ProjectState project, ProjectFamily activeFooting, string nonce)
        {
            var before = project.Elements.Count;
            var nativeBefore = CountModelSpaceEntities(document);
            var generic = ProjectFamilyService.Create(project, "local022-generic-" + nonce, "LOCAL-022 Generic", ElementCategory.Foundation);
            var isSingleFooting = Convert.ToBoolean(
                InvokeStatic(ProductAssembly(), "QS3D.BricsCAD.V25.SingleFootingContract", "IsSingleFooting", new[] { typeof(ProjectFamily) }, generic),
                CultureInfo.InvariantCulture);
            if (isSingleFooting) throw new ProbeException("generic_family_misclassified");
            ProjectFamilyActivationService.SetActive(project, generic.Id);
            var rejected = false;
            try
            {
                var type = ProductAssembly().GetType("QS3D.BricsCAD.V25.SingleFootingCommands", true)
                    ?? throw new ProbeException("reflection_type_missing");
                var method = type.GetMethod(
                    "PlaceActiveSingleFootingAt",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(Document), typeof(Point3d) },
                    null) ?? throw new ProbeException("reflection_member_missing");
                method.Invoke(null, new object[] { document, new Point3d(30d, 10d, 0d) });
            }
            catch (TargetInvocationException error) when (
                error.InnerException is InvalidOperationException &&
                string.Equals(
                    error.InnerException.Message,
                    "Chọn Móng → Móng đơn và một Family Móng đơn trước khi đặt theo tọa độ.",
                    StringComparison.Ordinal))
            {
                rejected = true;
            }
            finally { ProjectFamilyActivationService.SetActive(project, activeFooting.Id); }
            if (!rejected || project.Elements.Count != before || CountModelSpaceEntities(document) != nativeBefore)
                throw new ProbeException("generic_family_not_refused");
            return true;
        }

        private static void WriteContinuity(
            Context context,
            ProjectState project,
            int semanticBaseline,
            int familyBaseline,
            int nativeBaseline)
        {
            var path = ContinuityPath(context);
            var temporary = path + ".tmp";
            if (File.Exists(path) || File.Exists(temporary)) throw new ProbeException("continuity_preexists");
            var content = string.Join("\n", new[]
            {
                "schema=QS3D_LOCAL022_V26_CONTINUITY_V1",
                "digest=" + ContinuityDigest(context.RunId, project.ProjectId),
                "semantic_baseline=" + semanticBaseline.ToString(CultureInfo.InvariantCulture),
                "family_baseline=" + familyBaseline.ToString(CultureInfo.InvariantCulture),
                "native_baseline=" + nativeBaseline.ToString(CultureInfo.InvariantCulture)
            });
            File.WriteAllText(temporary, content, new UTF8Encoding(false));
            File.Move(temporary, path);
        }

        private static ContinuityState RequireContinuity(Context context, ProjectState project)
        {
            var path = ContinuityPath(context);
            if (!File.Exists(path)) throw new ProbeException("continuity_missing");
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
            {
                var separator = line.IndexOf('=');
                if (separator <= 0) throw new ProbeException("continuity_format");
                var key = line.Substring(0, separator);
                if (values.ContainsKey(key))
                    throw new ProbeException("continuity_format");
                values.Add(key, line.Substring(separator + 1));
            }
            if (values.Count != 5 || !values.TryGetValue("schema", out var schema) ||
                !string.Equals(schema, "QS3D_LOCAL022_V26_CONTINUITY_V1", StringComparison.Ordinal) ||
                !values.TryGetValue("digest", out var actual) ||
                !string.Equals(actual, ContinuityDigest(context.RunId, project.ProjectId), StringComparison.Ordinal))
                throw new ProbeException("project_identity_changed");
            return new ContinuityState(
                ParseNonNegative(values, "semantic_baseline"),
                ParseNonNegative(values, "family_baseline"),
                ParseNonNegative(values, "native_baseline"));
        }

        private static int ParseNonNegative(IDictionary<string, string> values, string key)
        {
            if (!values.TryGetValue(key, out var raw) ||
                !int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value < 0)
                throw new ProbeException("continuity_count_invalid");
            return value;
        }

        private static string ContinuityPath(Context context)
        {
            var drawingRoot = Path.GetDirectoryName(context.Drawing) ?? throw new ProbeException("drawing_root_missing");
            var path = Path.GetFullPath(Path.Combine(drawingRoot, "local022-continuity.private"));
            if (!IsChildPath(context.Root, path)) throw new ProbeException("continuity_path_invalid");
            return path;
        }

        private static string ContinuityDigest(string runId, string projectId)
        {
            using (var hasher = SHA256.Create())
                return string.Concat(hasher.ComputeHash(Encoding.UTF8.GetBytes(runId + "\0" + projectId)).Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
        }

        private static int CountModelSpaceEntities(Document document)
        {
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var table = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(table[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                var count = 0;
                foreach (ObjectId id in modelSpace)
                {
                    var entity = transaction.GetObject(id, OpenMode.ForRead, true) as Entity;
                    if (entity != null && !entity.IsErased) count++;
                }
                return count;
            }
        }

        private static void RequireTotalDeltas(
            Document document,
            ProjectState project,
            int semanticBaseline,
            int familyBaseline,
            int nativeBaseline)
        {
            if (project.Elements.Count != semanticBaseline + 2 ||
                project.Families.Count != familyBaseline + 2 ||
                CountModelSpaceEntities(document) != nativeBaseline + 4)
                throw new ProbeException("unexpected_runtime_residue");
        }

        private static string Place(Document document, Point3d center) =>
            Convert.ToString(InvokeStatic(ProductAssembly(), "QS3D.BricsCAD.V25.SingleFootingCommands", "PlaceActiveSingleFootingAt", new[] { typeof(Document), typeof(Point3d) }, document, center), CultureInfo.InvariantCulture) ?? throw new ProbeException("placement_no_handle");

        private static int Regenerate(Document document, ProjectState project, ProjectFamily family, SingleFootingDimensions dimensions) =>
            Convert.ToInt32(InvokeStatic(ProductAssembly(), "QS3D.BricsCAD.V25.SingleFootingRegenerationService", "ApplyFamilyDimensions", new[] { typeof(Document), typeof(ProjectState), typeof(ProjectFamily), typeof(SingleFootingDimensions) }, document, project, family, dimensions), CultureInfo.InvariantCulture);

        private static ProjectState GetOrCreateProject(Document document) =>
            (ProjectState)InvokeStatic(ProductAssembly(), "QS3D.BricsCAD.V25.ProjectContextCoordinator", "GetOrCreate", new[] { typeof(Document) }, document);

        private static string ProjectPath(Document document) =>
            Convert.ToString(InvokeStatic(ProductAssembly(), "QS3D.BricsCAD.V25.ProjectContextCoordinator", "GetProjectPath", new[] { typeof(Document) }, document), CultureInfo.InvariantCulture) ?? throw new ProbeException("sidecar_path_missing");

        private static object InvokeStatic(Assembly assembly, string typeName, string methodName, Type[] signature, params object[] values)
        {
            var type = assembly.GetType(typeName, true) ?? throw new ProbeException("reflection_type_missing");
            var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, signature, null);
            if (method == null) throw new ProbeException("reflection_member_missing");
            try
            {
                var result = method.Invoke(null, values);
                if (method.ReturnType == typeof(void)) return VoidResult;
                return result ?? throw new ProbeException("reflection_null_result");
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null) { throw new ProbeException("PRODUCT_" + NormalizeCode(ex.InnerException.GetType().Name)); }
        }

        private static ProjectElement RequireSingleFootingElement(ProjectState project, string familyId, string? excludeElementId)
        {
            var matches = SingleFootingElements(project).Where(x =>
                string.Equals(x.FamilyId, familyId, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(x.Id, excludeElementId, StringComparison.OrdinalIgnoreCase)).Take(2).ToList();
            if (matches.Count != 1) throw new ProbeException("semantic_owner_not_unique");
            return matches[0];
        }

        private static IEnumerable<ProjectElement> SingleFootingElements(ProjectState project) => project.Elements.Where(element =>
            element.Category == ElementCategory.Foundation &&
            element.Properties.TryGetValue("CategoryCode", out var category) &&
            string.Equals(category, "Foundation.SingleFooting", StringComparison.OrdinalIgnoreCase));

        private static ProjectElement RequireElementById(ProjectState project, string id) =>
            project.Elements.SingleOrDefault(element => string.Equals(element.Id, id, StringComparison.OrdinalIgnoreCase))
            ?? throw new ProbeException("semantic_element_missing");

        private static void VerifyExpectedPersistedState(Document document, ProjectState project, string runId, string stage)
        {
            var familyId = ExpectedFamilyId(runId);
            var family = project.FindFamily(familyId);
            if (family == null || !SameDimensions(ReadDimensions(family), ExpectedEditedDimensions()))
                throw new ProbeException(stage + "_family_dimensions");
            var elements = SingleFootingElements(project).ToList();
            if (elements.Count != 2) throw new ProbeException(stage + "_element_count");
            var remainingCenters = ExpectedCenters().ToList();
            foreach (var element in elements)
            {
                if (!string.Equals(element.FamilyId, familyId, StringComparison.OrdinalIgnoreCase))
                    throw new ProbeException(stage + "_family_identity");
                var dimensions = ReadDimensions(element);
                if (!SameDimensions(dimensions, ExpectedEditedDimensions()))
                    throw new ProbeException(stage + "_dimensions");
                var center = ReadFootprintCenter(document, element);
                var matches = remainingCenters.Where(point => SamePoint(point, center)).ToList();
                if (matches.Count != 1) throw new ProbeException(stage + "_center_identity");
                remainingCenters.Remove(matches[0]);
                VerifySolid(document, element, dimensions, center, stage);
            }
            if (remainingCenters.Count != 0) throw new ProbeException(stage + "_center_cardinality");
        }

        private static void VerifySolid(Document document, ProjectElement element, SingleFootingDimensions dimensions, Point3d center, string stage)
        {
            VerifyFootprint(document, element, dimensions, center, stage);
            var handle = OwnedHandle(element);
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var id = document.Database.GetObjectId(false, ParseHandle(handle), 0);
                var solid = transaction.GetObject(id, OpenMode.ForRead, false) as Solid3d;
                if (solid == null || solid.IsErased) throw new ProbeException(stage + "_solid_missing");
                InvokeStatic(ProductAssembly(), "QS3D.BricsCAD.V25.Cad.GeneratedGeometryService", "RequireMatchingOwnership", new[] { typeof(Entity), typeof(ProjectState), typeof(ProjectElement), typeof(string) }, solid, FindOwningProject(element), element, "local022");
                var actualVolume = Math.Abs(solid.MassProperties.Volume);
                if (!Near(actualVolume, dimensions.VolumeM3)) throw new ProbeException(stage + "_solid_volume");
                var extents = solid.GeometricExtents;
                if (!Near(extents.MinPoint.X, center.X - dimensions.L1M / 2d) || !Near(extents.MaxPoint.X, center.X + dimensions.L1M / 2d) ||
                    !Near(extents.MinPoint.Y, center.Y - dimensions.W1M / 2d) || !Near(extents.MaxPoint.Y, center.Y + dimensions.W1M / 2d) ||
                    !Near(extents.MinPoint.Z, center.Z) || !Near(extents.MaxPoint.Z, center.Z + dimensions.TotalHeightM))
                    throw new ProbeException(stage + "_solid_extents");
                transaction.Commit();
            }
            if (!element.Properties.TryGetValue("GeneratedSolidMode", out var mode) || !string.Equals(mode, "SingleFootingLoft", StringComparison.Ordinal)) throw new ProbeException(stage + "_ownership_mode");
            if (!element.Properties.TryGetValue("VolumeM3", out var raw) || !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var semanticVolume) || !Near(semanticVolume, dimensions.VolumeM3)) throw new ProbeException(stage + "_semantic_volume");
        }

        private static void VerifyFootprint(Document document, ProjectElement element, SingleFootingDimensions dimensions, Point3d center, string stage)
        {
            var handle = element.SourceHandles.SingleOrDefault() ?? throw new ProbeException(stage + "_source_handle");
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var id = document.Database.GetObjectId(false, ParseHandle(handle), 0);
                var source = transaction.GetObject(id, OpenMode.ForRead, false) as Polyline;
                if (source == null || source.IsErased || !source.Closed || source.NumberOfVertices != 4)
                    throw new ProbeException(stage + "_source_geometry");
                var extents = source.GeometricExtents;
                if (!Near(extents.MinPoint.X, center.X - dimensions.L1M / 2d) ||
                    !Near(extents.MaxPoint.X, center.X + dimensions.L1M / 2d) ||
                    !Near(extents.MinPoint.Y, center.Y - dimensions.W1M / 2d) ||
                    !Near(extents.MaxPoint.Y, center.Y + dimensions.W1M / 2d) ||
                    !Near(source.Elevation, center.Z))
                    throw new ProbeException(stage + "_source_extents");
            }
        }

        private static Point3d ReadFootprintCenter(Document document, ProjectElement element)
        {
            var handle = element.SourceHandles.SingleOrDefault() ?? throw new ProbeException("source_handle_missing");
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var id = document.Database.GetObjectId(false, ParseHandle(handle), 0);
                var polyline = transaction.GetObject(id, OpenMode.ForRead, false) as Polyline;
                if (polyline == null || polyline.IsErased || !polyline.Closed || polyline.NumberOfVertices != 4) throw new ProbeException("footprint_invalid");
                var minX = double.PositiveInfinity; var maxX = double.NegativeInfinity; var minY = double.PositiveInfinity; var maxY = double.NegativeInfinity;
                for (var i = 0; i < polyline.NumberOfVertices; i++) { var point = polyline.GetPoint2dAt(i); minX = Math.Min(minX, point.X); maxX = Math.Max(maxX, point.X); minY = Math.Min(minY, point.Y); maxY = Math.Max(maxY, point.Y); }
                return new Point3d((minX + maxX) / 2d, (minY + maxY) / 2d, polyline.Elevation);
            }
        }

        private static SingleFootingDimensions ReadDimensions(ProjectElement element)
        {
            double Value(string key) => element.Properties.TryGetValue(key, out var raw) && double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : throw new ProbeException("dimension_missing");
            return new SingleFootingDimensions(Value("SINGLE_FOOTING_L1"), Value("SINGLE_FOOTING_W1"), Value("SINGLE_FOOTING_L2"), Value("SINGLE_FOOTING_W2"), Value("SINGLE_FOOTING_H1"), Value("SINGLE_FOOTING_H2"));
        }

        private static SingleFootingDimensions ReadDimensions(ProjectFamily family)
        {
            double Value(string key) => family.Properties.TryGetValue(key, out var raw) && double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : throw new ProbeException("family_dimension_missing");
            return new SingleFootingDimensions(Value("SINGLE_FOOTING_L1"), Value("SINGLE_FOOTING_W1"), Value("SINGLE_FOOTING_L2"), Value("SINGLE_FOOTING_W2"), Value("SINGLE_FOOTING_H1"), Value("SINGLE_FOOTING_H2"));
        }

        private static string OwnedHandle(ProjectElement element) => element.Properties.TryGetValue("GeneratedSolidHandle", out var handle) && !string.IsNullOrWhiteSpace(handle) ? handle.Trim() : throw new ProbeException("generated_handle_missing");

        private static void RequireErased(Document document, string handle)
        {
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var id = document.Database.GetObjectId(false, ParseHandle(handle), 0);
                var entity = transaction.GetObject(id, OpenMode.ForRead, true) as Entity;
                if (entity == null || !entity.IsErased) throw new ProbeException("former_handle_not_erased");
            }
        }

        private static void Execute(string phase, Func<Context, IDictionary<string, bool>> action)
        {
            Context? context = null;
            IDictionary<string, bool> checks = new Dictionary<string, bool>(StringComparer.Ordinal);
            var status = "PASS"; var stage = phase + "_bind"; var errorCode = "NONE";
            try { context = BindContext(phase); stage = phase + "_execute"; checks = action(context); stage = phase; }
            catch (ProbeException ex) { status = "FAIL"; errorCode = ex.Code; WriteFailureDiagnostic(phase, ex); }
            catch (System.Exception ex) { status = "FAIL"; errorCode = NormalizeCode("UNEXPECTED_" + ex.GetType().Name); WriteFailureDiagnostic(phase, ex); }
            try { WriteMarker(context ?? ContextFromEnvironment(phase), phase, status, stage, errorCode, checks); }
            catch { try { Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nLOCAL-022 marker write failed."); } catch { } }
        }

        private static Context ContextFromEnvironment(string phase) => new Context(null!, RequireNonce(Environment.GetEnvironmentVariable(RunIdVariable)), RequirePath(Environment.GetEnvironmentVariable(RootVariable), "root_missing"), RequirePath(Environment.GetEnvironmentVariable(DrawingVariable), "drawing_missing"), RequirePath(Environment.GetEnvironmentVariable(ProductVariable), "product_path_missing"), null!);

        private static void WriteFailureDiagnostic(string phase, System.Exception error)
        {
            // Bounded type/HResult/method metadata only: no exception messages,
            // argument values, source filenames, drawing paths or credentials.
            try
            {
                var root = RequirePath(Environment.GetEnvironmentVariable(RootVariable), "root_missing");
                var path = Path.Combine(root, "phase-" + phase + "-diagnostic.private.txt");
                if (!IsChildPath(root, path) || File.Exists(path)) return;
                var lines = new List<string>();
                for (System.Exception? current = error; current != null && lines.Count < 40; current = current.InnerException)
                {
                    lines.Add("type=" + NormalizeCode(current.GetType().FullName ?? "UNKNOWN"));
                    lines.Add("hresult=" + current.HResult.ToString("X8", CultureInfo.InvariantCulture));
                    foreach (var frame in (new StackTrace(current, false).GetFrames() ?? Array.Empty<StackFrame>()).Take(8))
                    {
                        var method = frame.GetMethod();
                        if (method != null) lines.Add("method=" + NormalizeCode((method.DeclaringType?.FullName ?? "UNKNOWN") + "_" + method.Name));
                    }
                }
                File.WriteAllLines(path, lines, new UTF8Encoding(false));
            }
            catch { /* Diagnostic failure cannot replace the native verdict. */ }
        }

        private static void WriteMarker(Context context, string phase, string status, string stage, string errorCode, IDictionary<string, bool> checks)
        {
            var marker = Path.Combine(context.Root, "phase-" + phase + ".json");
            if (!IsChildPath(context.Root, marker) || File.Exists(marker)) throw new ProbeException("marker_path_invalid");
            var body = "{\"schema\":\"" + Schema + "\",\"run_id\":\"" + context.RunId + "\",\"phase\":\"" + phase + "\",\"status\":\"" + status + "\",\"stage\":\"" + stage + "\",\"error_code\":\"" + errorCode + "\",\"checks\":{" + string.Join(",", checks.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => "\"" + x.Key + "\":" + (x.Value ? "true" : "false"))) + "}}";
            var temporary = marker + ".tmp";
            File.WriteAllText(temporary, body, new UTF8Encoding(false));
            File.Move(temporary, marker);
        }

        private static Dictionary<string, bool> Checks(params object[] values)
        {
            var result = new Dictionary<string, bool>(StringComparer.Ordinal);
            for (var index = 0; index < values.Length; index += 2) result.Add((string)values[index], (bool)values[index + 1]);
            return result;
        }

        private static ProjectState FindOwningProject(ProjectElement element)
        {
            var document = Application.DocumentManager.MdiActiveDocument ?? throw new ProbeException("active_document_missing");
            var project = GetOrCreateProject(document);
            if (!project.Elements.Any(candidate => ReferenceEquals(candidate, element))) throw new ProbeException("ownership_project_mismatch");
            return project;
        }

        private static void RequireMeterDrawing(Document document) { if ((int)document.Database.Insunits != 6) throw new ProbeException("drawing_units_not_meters"); }
        private static void RequireHostMajor26()
        {
            using (var process = Process.GetCurrentProcess())
            {
                var module = process.MainModule;
                if (module == null || module.FileVersionInfo.FileMajorPart != 26)
                    throw new ProbeException("host_major_not_26");
            }
        }
        private static Assembly ProductAssembly() => AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(x => string.Equals(x.GetName().Name, "QS3D.BricsCAD.V26", StringComparison.OrdinalIgnoreCase)) ?? throw new ProbeException("product_not_loaded");
        private static string RequireNonce(string? value) { var text = (value ?? string.Empty).Trim(); if (text.Length != 32 || text.Any(ch => !Uri.IsHexDigit(ch))) throw new ProbeException("run_id_invalid"); return text.ToLowerInvariant(); }
        private static string RequirePath(string? value, string code) { var text = (value ?? string.Empty).Trim(); if (text.Length == 0 || !Path.IsPathRooted(text)) throw new ProbeException(code); return Path.GetFullPath(text); }
        private static bool SamePath(string left, string right) => string.Equals(Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar), Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
        private static bool IsChildPath(string root, string candidate) { var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar; return Path.GetFullPath(candidate).StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase); }
        private static bool Near(double left, double right) => Math.Abs(left - right) <= Tolerance;
        private static bool SameDimensions(SingleFootingDimensions left, SingleFootingDimensions right) => Near(left.L1M, right.L1M) && Near(left.W1M, right.W1M) && Near(left.L2M, right.L2M) && Near(left.W2M, right.W2M) && Near(left.H1M, right.H1M) && Near(left.H2M, right.H2M);
        private static bool SamePoint(Point3d left, Point3d right) => Near(left.X, right.X) && Near(left.Y, right.Y) && Near(left.Z, right.Z);
        private static string ExpectedFamilyId(string runId) => "local022-" + runId;
        private static SingleFootingDimensions ExpectedEditedDimensions() => new SingleFootingDimensions(4d, 2d, 2d, 1d, 1d, 1d);
        private static Point3d[] ExpectedCenters() => new[] { new Point3d(10d, 10d, 0d), new Point3d(20d, 10d, 0d) };
        private static Handle ParseHandle(string value)
        {
            if (!long.TryParse((value ?? string.Empty).Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var parsed) || parsed <= 0)
                throw new ProbeException("handle_invalid");
            return new Handle(parsed);
        }
        private static string NormalizeCode(string value)
        {
            var builder = new StringBuilder();
            var previousUnderscore = false;
            foreach (var character in value ?? string.Empty)
            {
                if (char.IsLetterOrDigit(character)) { builder.Append(char.ToUpperInvariant(character)); previousUnderscore = false; }
                else if (!previousUnderscore && builder.Length > 0) { builder.Append('_'); previousUnderscore = true; }
            }
            return builder.ToString().Trim('_').Length == 0 ? "UNEXPECTED" : builder.ToString().Trim('_');
        }

        private sealed class Context
        {
            public Context(Document document, string runId, string root, string drawing, string productPath, Assembly product) { Document = document; RunId = runId; Root = root; Drawing = drawing; ProductPath = productPath; Product = product; }
            public Document Document { get; }
            public string RunId { get; }
            public string Root { get; }
            public string Drawing { get; }
            public string ProductPath { get; }
            public Assembly Product { get; }
        }
        private sealed class RunState { public RunState(Document document, string projectId, string familyId, string runId) { Document = document; ProjectId = projectId; FamilyId = familyId; RunId = runId; } public Document Document { get; } public string ProjectId { get; } public string FamilyId { get; } public string RunId { get; } }
        private sealed class ContinuityState { public ContinuityState(int semanticBaseline, int familyBaseline, int nativeBaseline) { SemanticBaseline = semanticBaseline; FamilyBaseline = familyBaseline; NativeBaseline = nativeBaseline; } public int SemanticBaseline { get; } public int FamilyBaseline { get; } public int NativeBaseline { get; } }
        private sealed class ProbeException : System.Exception { public ProbeException(string code) : base(code) { Code = NormalizeCode(code); } public string Code { get; } }
    }
}
