using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.LocalQualification.MultiRegion
{
    /// <summary>
    /// LOCAL-005 test-only native probe. Setup creates only synthetic source loops
    /// and semantic ownership. The real mutation is executed by the runner through
    /// the public QS3DSLABREBAR3DMULTI command; this probe never calls the builder.
    /// </summary>
    public sealed class Local005NativeMultiRegionProbeCommands
    {
        private const string Schema = "QS3D_LOCAL005_NATIVE_V1";
        private const string RunIdVariable = "QS3D_LOCAL005_RUN_ID";
        private const string RootVariable = "QS3D_LOCAL005_ROOT";
        private const string DrawingVariable = "QS3D_LOCAL005_DRAWING";
        private const string ProductVariable = "QS3D_LOCAL005_PRODUCT_DLL";
        private const string ProbeVariable = "QS3D_LOCAL005_PROBE_DLL";
        private const string OwnerSlot = "GeneratedSlabMeshHandles";
        private const string NativeRegApp = "QS3D_REBAR";
        private const string RegionRegApp = "QS3D_REBAR_REGION";
        private const double Tolerance = 1e-7d;
        private static readonly object DiagnosticGate = new object();
        private static bool ProductionExceptionDiagnosticArmed;
        private static string? ProductionExceptionDiagnostic;

        [CommandMethod("QL005DUMPEX", CommandFlags.Modal)]
        public void DumpProductionExceptionDiagnostic()
        {
            var context = Bind();
            string diagnostic;
            lock (DiagnosticGate)
            {
                diagnostic = ProductionExceptionDiagnostic ?? string.Empty;
                if (ProductionExceptionDiagnosticArmed) AppDomain.CurrentDomain.FirstChanceException -= CaptureProductionException;
                ProductionExceptionDiagnosticArmed = false;
                ProductionExceptionDiagnostic = null;
            }
            var path = Path.GetFullPath(Path.Combine(context.Root, "private", "local005-production-exception.private.txt"));
            if (!IsChild(context.Root, path) || File.Exists(path)) throw new ProbeException("production_exception_diagnostic_path_invalid");
            File.WriteAllText(path, diagnostic.Length == 0 ? "NONE\n" : diagnostic, new UTF8Encoding(false));
        }

        [CommandMethod("QL005SETUP", CommandFlags.Modal)]
        public void Setup() => Execute("setup", SetupPhase);

        [CommandMethod("QL005VERIFY", CommandFlags.Modal)]
        public void Verify() => Execute("run", c => VerifyPhase(c, true));

        [CommandMethod("QL005SAVED", CommandFlags.Modal)]
        public void Saved() => Execute("saved", SavedPhase);

        [CommandMethod("QL005REOPEN", CommandFlags.Modal)]
        public void Reopen() => Execute("reopen", ReopenPhase);

        private static void ArmProductionExceptionDiagnostic(Context context)
        {
            _ = context;
            lock (DiagnosticGate)
            {
                if (!ProductionExceptionDiagnosticArmed) AppDomain.CurrentDomain.FirstChanceException += CaptureProductionException;
                ProductionExceptionDiagnosticArmed = true;
                ProductionExceptionDiagnostic = string.Empty;
            }
        }

        private static void CaptureProductionException(object? sender, FirstChanceExceptionEventArgs args)
        {
            try
            {
                var error = args.Exception;
                var stack = error.StackTrace ?? string.Empty;
                if (stack.IndexOf("QS3D.BricsCAD.V25.MultiRegionRebarCommands", StringComparison.Ordinal) < 0) return;
                lock (DiagnosticGate)
                {
                    if (!ProductionExceptionDiagnosticArmed || !string.IsNullOrEmpty(ProductionExceptionDiagnostic)) return;
                    var message = (error.Message ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');
                    ProductionExceptionDiagnostic = (error.GetType().FullName ?? error.GetType().Name) + "\n" + message + "\n" + stack;
                }
            }
            catch { }
        }

        private static IDictionary<string, bool> SetupPhase(Context context)
        {
            RequireMeters(context.Document);
            var project = GetProject(context.Document);
            if (project.Elements.Count != 0) throw new ProbeException("fixture_project_not_empty");

            var ids = CreateLoops(context.Document);
            var element = new ProjectElement(ElementId(context.RunId), ElementCategory.Slab);
            foreach (var id in ids) element.SourceHandles.Add(id.Handle.ToString());
            element.Properties["ThicknessM"] = "0.30";
            element.Properties["RebarSlabXNotation"] = "D12@1000";
            element.Properties["RebarSlabYNotation"] = "D12@1000";
            element.Properties["RebarSlabCoverM"] = "0.05";
            element.Properties["RebarSlabFaces"] = "Bottom";
            element.Properties["RebarSlabXClosestToFace"] = "true";
            project.Elements.Add(element);
            project.Touch();
            ArmProductionExceptionDiagnostic(context);
            context.Document.Editor.SetImpliedSelection(ids);

            return Checks(
                "active_disposable_drawing", true,
                "host_major_25", true,
                "product_location_exact", true,
                "meter_units", true,
                "synthetic_two_disjoint_regions", true,
                "synthetic_one_hole", true,
                "single_semantic_slab_owner", true,
                "implied_selection_seeded", true);
        }

        private static IDictionary<string, bool> VerifyPhase(Context context, bool createContinuity)
        {
            RequireMeters(context.Document);
            var project = GetProject(context.Document);
            var element = RequireElement(project, context.RunId);
            VerifySourceLoops(context.Document, element);

            var handles = GeneratedHandles(element);
            if (PositiveInt(element, "GeneratedSlabMeshCount") != handles.Count) throw new ProbeException("generated_count_mismatch");
            if (Text(element, "GeneratedSlabMeshMultiRegionCount") != "2") throw new ProbeException("region_count_mismatch");
            if (Text(element, "GeneratedSlabMeshMultiRegionBarCount") != handles.Count.ToString(CultureInfo.InvariantCulture)) throw new ProbeException("region_bar_count_mismatch");
            if (!string.Equals(Text(element, "GeneratedSlabMeshMultiRegionMode"), "PolygonMultiRegionGlobalXY", StringComparison.Ordinal)) throw new ProbeException("mode_mismatch");
            var sourceManifest = Text(element, "GeneratedSlabMeshMultiRegionSourceManifest");
            var generatedManifest = Text(element, "GeneratedSlabMeshMultiRegionGeneratedManifest");
            var fingerprint = Text(element, "GeneratedSlabMeshMultiRegionTopologyFingerprint");
            if (sourceManifest.Length < 8 || generatedManifest.Length < 8 || fingerprint.Length < 8) throw new ProbeException("manifest_or_fingerprint_missing");

            var regions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var left = 0;
            var right = 0;
            using (var transaction = context.Document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var handle in handles)
                {
                    var solid = ResolveEntity(context.Document.Database, transaction, handle) as Solid3d;
                    if (solid == null) throw new ProbeException("generated_not_solid3d");
                    RequireOwnership(solid, NativeRegApp, project, element, false, regions);
                    RequireOwnership(solid, RegionRegApp, project, element, true, regions);
                    var extents = solid.GeometricExtents;
                    Finite(extents);
                    var x = (extents.MinPoint.X + extents.MaxPoint.X) / 2d;
                    if (x < 12d) left++;
                    else if (x > 12d) right++;
                    else throw new ProbeException("bar_between_regions");
                    if (extents.MaxPoint.X > 3.25d && extents.MinPoint.X < 4.75d &&
                        extents.MaxPoint.Y > 2.25d && extents.MinPoint.Y < 3.75d)
                        throw new ProbeException("bar_intrudes_hole_interior");
                }
                transaction.Commit();
            }
            if (regions.Count != 2 || left == 0 || right == 0) throw new ProbeException("region_materialization_incomplete");
            RequireHealthNoErrors(context.Document, project);

            var digest = Digest(project.ProjectId + "\0" + element.Id + "\0" +
                string.Join(";", handles.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)) + "\0" + fingerprint);
            if (createContinuity) WriteContinuity(context, digest); else RequireContinuity(context, digest);

            return Checks(
                "active_disposable_drawing", true,
                "real_product_output_present", true,
                "exact_generated_aggregate_count", true,
                "two_regions_materialized", true,
                "hole_interior_excluded", true,
                "native_rebar_ownership", true,
                "native_region_ownership", true,
                "source_manifest_present", true,
                "generated_manifest_present", true,
                "topology_fingerprint_present", true,
                "runtime_health_no_errors", true);
        }

        private static IDictionary<string, bool> SavedPhase(Context context)
        {
            var checks = VerifyPhase(context, false);
            if (!File.Exists(ProjectPath(context.Document))) throw new ProbeException("sidecar_missing_after_qs3dsave");
            checks["sidecar_exists_after_qs3dsave"] = true;
            checks["native_database_still_open"] = true;
            return checks;
        }

        private static IDictionary<string, bool> ReopenPhase(Context context)
        {
            var checks = VerifyPhase(context, false);
            checks["cold_reopen_project_bind"] = true;
            checks["reopened_source_handles_live"] = true;
            checks["reopened_generated_handles_live"] = true;
            checks["reopened_ownership_and_topology_stable"] = true;
            return checks;
        }

        private static ObjectId[] CreateLoops(Document document)
        {
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var table = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var model = (BlockTableRecord)transaction.GetObject(table[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                var ids = new[]
                {
                    Rectangle(model, transaction, 0d, 0d, 10d, 8d),
                    Rectangle(model, transaction, 3d, 2d, 5d, 4d),
                    Rectangle(model, transaction, 14d, 0d, 22d, 8d)
                };
                transaction.Commit();
                return ids;
            }
        }

        private static ObjectId Rectangle(BlockTableRecord model, Transaction transaction, double minX, double minY, double maxX, double maxY)
        {
            var polyline = new Polyline(4) { Closed = true, Elevation = 0d };
            try
            {
                polyline.AddVertexAt(0, new Point2d(minX, minY), 0d, 0d, 0d);
                polyline.AddVertexAt(1, new Point2d(maxX, minY), 0d, 0d, 0d);
                polyline.AddVertexAt(2, new Point2d(maxX, maxY), 0d, 0d, 0d);
                polyline.AddVertexAt(3, new Point2d(minX, maxY), 0d, 0d, 0d);
                var id = model.AppendEntity(polyline);
                transaction.AddNewlyCreatedDBObject(polyline, true);
                polyline = null!;
                return id;
            }
            finally { polyline?.Dispose(); }
        }

        private static void VerifySourceLoops(Document document, ProjectElement element)
        {
            if (element.SourceHandles.Count != 3) throw new ProbeException("source_handle_count");
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var extents = new List<Extents3d>();
                foreach (var handle in element.SourceHandles)
                {
                    var polyline = ResolveEntity(document.Database, transaction, handle) as Polyline;
                    if (polyline == null || !polyline.Closed || polyline.NumberOfVertices != 4 || Math.Abs(polyline.Elevation) > Tolerance) throw new ProbeException("source_loop_invalid");
                    extents.Add(polyline.GeometricExtents);
                }
                if (!HasRect(extents, 0d,0d,10d,8d) || !HasRect(extents, 3d,2d,5d,4d) || !HasRect(extents,14d,0d,22d,8d)) throw new ProbeException("source_geometry_changed");
                transaction.Commit();
            }
        }

        private static bool HasRect(IEnumerable<Extents3d> values, double minX, double minY, double maxX, double maxY) =>
            values.Any(x => Near(x.MinPoint.X,minX) && Near(x.MinPoint.Y,minY) && Near(x.MaxPoint.X,maxX) && Near(x.MaxPoint.Y,maxY));

        private static void RequireOwnership(Entity entity, string regApp, ProjectState project, ProjectElement element, bool regionMarker, ISet<string> regions)
        {
            using (var data = entity.GetXDataForApplication(regApp))
            {
                if (data == null) throw new ProbeException(regionMarker ? "region_ownership_missing" : "native_ownership_missing");
                var values = data.AsArray();
                var minimum = regionMarker ? 6 : 5;
                if (values.Length < minimum || !Same(values[0].Value, regApp) || !Same(values[1].Value, "1") ||
                    !Same(values[2].Value, Identity("p1:", project.ProjectId)) || !Same(values[3].Value, Identity("e1:", element.Id)) ||
                    !SameIgnoreCase(values[4].Value, OwnerSlot))
                    throw new ProbeException(regionMarker ? "region_ownership_mismatch" : "native_ownership_mismatch");
                if (regionMarker)
                {
                    var region = (Convert.ToString(values[5].Value, CultureInfo.InvariantCulture) ?? string.Empty).Trim();
                    if (region.Length == 0 || region.Any(char.IsControl)) throw new ProbeException("region_id_invalid");
                    regions.Add(region);
                }
            }
        }

        private static void RequireHealthNoErrors(Document document, ProjectState project)
        {
            var type = ProductAssembly().GetType("QS3D.BricsCAD.V25.Cad.GeneratedMultiRegionRebarRuntimeHealthService", true) ?? throw new ProbeException("health_type_missing");
            var method = type.GetMethod("Inspect", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(Document), typeof(ProjectState) }, null) ?? throw new ProbeException("health_method_missing");
            object? raw;
            try { raw = method.Invoke(null, new object[] { document, project }); }
            catch (TargetInvocationException) { throw new ProbeException("health_inspect_failed"); }
            var items = raw as IEnumerable ?? throw new ProbeException("health_result_invalid");
            foreach (var item in items)
            {
                if (item == null) throw new ProbeException("health_issue_null");
                var property = item.GetType().GetProperty("Severity", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var severity = property == null ? string.Empty : Convert.ToString(property.GetValue(item, null), CultureInfo.InvariantCulture) ?? string.Empty;
                if (string.Equals(severity, "Error", StringComparison.OrdinalIgnoreCase)) throw new ProbeException("runtime_health_error");
            }
        }

        private static ProjectElement RequireElement(ProjectState project, string runId)
        {
            var matches = project.Elements.Where(x => x != null && string.Equals(x.Id, ElementId(runId), StringComparison.OrdinalIgnoreCase)).Take(2).ToList();
            if (matches.Count != 1 || matches[0].Category != ElementCategory.Slab) throw new ProbeException("semantic_owner_missing_or_ambiguous");
            return matches[0];
        }

        private static List<string> GeneratedHandles(ProjectElement element)
        {
            var handles = Text(element, OwnerSlot).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length != 0).ToList();
            if (handles.Count == 0 || handles.Distinct(StringComparer.OrdinalIgnoreCase).Count() != handles.Count) throw new ProbeException("generated_handle_aggregate_invalid");
            return handles;
        }

        private static Entity ResolveEntity(Database database, Transaction transaction, string raw)
        {
            if (!long.TryParse((raw ?? string.Empty).Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value) || value <= 0) throw new ProbeException("handle_invalid");
            ObjectId id;
            try { id = database.GetObjectId(false, new Handle(value), 0); } catch { throw new ProbeException("handle_unresolved"); }
            var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
            if (entity == null || entity.IsErased) throw new ProbeException("entity_missing");
            return entity;
        }

        private static string Text(ProjectElement element, string key)
        {
            if (!element.Properties.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value)) throw new ProbeException("property_missing_" + Normalize(key));
            return value.Trim();
        }

        private static int PositiveInt(ProjectElement element, string key)
        {
            if (!int.TryParse(Text(element,key), NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value <= 0) throw new ProbeException("property_invalid_" + Normalize(key));
            return value;
        }

        private static void RequireMeters(Document document) { if ((int)document.Database.Insunits != 6) throw new ProbeException("drawing_not_meters"); }
        private static void Finite(Extents3d e) { foreach (var v in new[]{e.MinPoint.X,e.MinPoint.Y,e.MinPoint.Z,e.MaxPoint.X,e.MaxPoint.Y,e.MaxPoint.Z}) if (double.IsNaN(v)||double.IsInfinity(v)) throw new ProbeException("non_finite_extents"); }
        private static bool Near(double a,double b) => Math.Abs(a-b) <= Tolerance;
        private static bool Same(object value,string expected) => string.Equals(Convert.ToString(value,CultureInfo.InvariantCulture),expected,StringComparison.Ordinal);
        private static bool SameIgnoreCase(object value,string expected) => string.Equals(Convert.ToString(value,CultureInfo.InvariantCulture),expected,StringComparison.OrdinalIgnoreCase);
        private static string ElementId(string runId) => "local005-slab-" + runId;

        private static ProjectState GetProject(Document document) => (ProjectState)Invoke("QS3D.BricsCAD.V25.ProjectContextCoordinator","GetOrCreate",new[]{typeof(Document)},document);
        private static string ProjectPath(Document document) => Convert.ToString(Invoke("QS3D.BricsCAD.V25.ProjectContextCoordinator","GetProjectPath",new[]{typeof(Document)},document),CultureInfo.InvariantCulture) ?? throw new ProbeException("sidecar_path_missing");
        private static object Invoke(string typeName,string methodName,Type[] signature,params object[] values)
        {
            var type = ProductAssembly().GetType(typeName,true) ?? throw new ProbeException("reflection_type_missing");
            var method = type.GetMethod(methodName,BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic,null,signature,null) ?? throw new ProbeException("reflection_member_missing");
            try { return method.Invoke(null,values) ?? throw new ProbeException("reflection_null_result"); }
            catch (TargetInvocationException) { throw new ProbeException("product_reflection_failed"); }
        }

        private static Assembly ProductAssembly()
        {
            var expected = RequiredPath(Environment.GetEnvironmentVariable(ProductVariable),"product_path_missing");
            var assembly = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(x => string.Equals(x.GetName().Name,"QS3D.BricsCAD.V25",StringComparison.OrdinalIgnoreCase)) ?? throw new ProbeException("product_not_loaded");
            if (!SamePath(assembly.Location,expected)) throw new ProbeException("product_location_mismatch");
            return assembly;
        }

        private static Context Bind()
        {
            var runId = Nonce(Environment.GetEnvironmentVariable(RunIdVariable));
            var root = RequiredPath(Environment.GetEnvironmentVariable(RootVariable),"root_missing");
            var drawing = RequiredPath(Environment.GetEnvironmentVariable(DrawingVariable),"drawing_missing");
            var probe = RequiredPath(Environment.GetEnvironmentVariable(ProbeVariable),"probe_path_missing");
            if (!IsChild(root,drawing)) throw new ProbeException("drawing_outside_root");
            var document = Application.DocumentManager.MdiActiveDocument ?? throw new ProbeException("active_document_missing");
            if (!SamePath(document.Name,drawing) || !File.Exists(drawing)) throw new ProbeException("drawing_identity_mismatch");
            if (!SamePath(Assembly.GetExecutingAssembly().Location,probe)) throw new ProbeException("probe_location_mismatch");
            var module = Process.GetCurrentProcess().MainModule;
            if (module == null || module.FileVersionInfo.FileMajorPart != 25) throw new ProbeException("host_major_mismatch");
            ProductAssembly();
            return new Context(document,runId,root);
        }

        private static void Execute(string phase,Func<Context,IDictionary<string,bool>> action)
        {
            Context? context = null;
            try { context=Bind(); WriteMarker(context,phase,"PASS",phase,"NONE",action(context)); }
            catch(System.Exception error)
            {
                if(context!=null) try { WriteMarker(context,phase,"FAIL",phase,"ERR_"+Normalize(error is ProbeException?error.Message:error.GetType().Name),new Dictionary<string,bool>()); } catch { }
                throw;
            }
        }

        private static void WriteContinuity(Context context,string digest)
        {
            var path=ContinuityPath(context); if(File.Exists(path)) throw new ProbeException("continuity_preexists");
            File.WriteAllText(path,"schema=QS3D_LOCAL005_CONTINUITY_V1\ndigest="+digest+"\n",new UTF8Encoding(false));
        }
        private static void RequireContinuity(Context context,string digest)
        {
            var expected="schema=QS3D_LOCAL005_CONTINUITY_V1\ndigest="+digest+"\n";
            if(!File.Exists(ContinuityPath(context)) || !string.Equals(File.ReadAllText(ContinuityPath(context),Encoding.UTF8),expected,StringComparison.Ordinal)) throw new ProbeException("continuity_changed");
        }
        private static string ContinuityPath(Context context) { var p=Path.GetFullPath(Path.Combine(context.Root,"private","local005-continuity.private")); if(!IsChild(context.Root,p)) throw new ProbeException("continuity_path_invalid"); return p; }

        private static IDictionary<string,bool> Checks(params object[] values)
        {
            var result=new Dictionary<string,bool>(StringComparer.Ordinal); if(values.Length%2!=0) throw new ArgumentException("checks");
            for(var i=0;i<values.Length;i+=2) result.Add((string)values[i],(bool)values[i+1]); return result;
        }
        private static void WriteMarker(Context context,string phase,string status,string stage,string code,IDictionary<string,bool> checks)
        {
            var path=Path.GetFullPath(Path.Combine(context.Root,"phase-"+phase+".json")); if(!IsChild(context.Root,path)||File.Exists(path)) throw new ProbeException("marker_path_invalid");
            var temp=path+".tmp"; var body=string.Join(",",checks.OrderBy(x=>x.Key,StringComparer.Ordinal).Select(x=>"\""+x.Key+"\":"+(x.Value?"true":"false")));
            var json="{\"schema\":\""+Schema+"\",\"run_id\":\""+context.RunId+"\",\"phase\":\""+phase+"\",\"status\":\""+status+"\",\"stage\":\""+stage+"\",\"error_code\":\""+code+"\",\"checks\":{"+body+"}}";
            File.WriteAllText(temp,json,new UTF8Encoding(false)); File.Move(temp,path);
        }

        private static string Identity(string prefix,string value) => prefix+Digest(value.Trim());
        private static string Digest(string value) { using(var sha=SHA256.Create()) return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(value)).Select(x=>x.ToString("x2",CultureInfo.InvariantCulture))); }
        private static string Nonce(string? raw) { var n=(raw??string.Empty).Trim().ToLowerInvariant(); if(n.Length!=32||n.Any(x=>!(x>='0'&&x<='9')&&!(x>='a'&&x<='f'))) throw new ProbeException("run_id_invalid"); return n; }
        private static string RequiredPath(string? raw,string code) { if(string.IsNullOrWhiteSpace(raw)) throw new ProbeException(code); var value=raw!; return Path.GetFullPath(value.Trim()); }
        private static bool SamePath(string a,string b) => string.Equals(Path.GetFullPath(a),Path.GetFullPath(b),StringComparison.OrdinalIgnoreCase);
        private static bool IsChild(string root,string path) { var r=Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar)+Path.DirectorySeparatorChar; return Path.GetFullPath(path).StartsWith(r,StringComparison.OrdinalIgnoreCase); }
        private static string Normalize(string raw) { var b=new StringBuilder(); foreach(var c in (raw??string.Empty).ToUpperInvariant()){ if(b.Length>=64) break; b.Append((c>='A'&&c<='Z')||(c>='0'&&c<='9')?c:'_'); } var n=b.ToString().Trim('_'); return n.Length==0?"UNKNOWN":n; }

        private sealed class Context
        {
            public Context(Document document,string runId,string root){Document=document;RunId=runId;Root=root;}
            public Document Document{get;} public string RunId{get;} public string Root{get;}
        }
        private sealed class ProbeException:InvalidOperationException { public ProbeException(string code):base(code){} }
    }
}
