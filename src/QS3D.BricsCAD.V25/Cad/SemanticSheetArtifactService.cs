using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Bricscad.ApplicationServices;
using QS3D.Core.Audit;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class SemanticSheetArtifactService
    {
        private const int MaxViews = 128;
        private const string GeneratedSolidHandleKey = "GeneratedSolidHandle";

        public static string Build(
            Document document,
            ProjectState project,
            SemanticSheetPlan sheet,
            IEnumerable<SemanticViewPlan> availableViews,
            IEnumerable<SemanticTitleBlockParameterDefinition> titleBlockMappings)
        {
            return BuildOrRefresh(document, project, sheet, availableViews, titleBlockMappings, refresh: false);
        }

        public static string Refresh(
            Document document,
            ProjectState project,
            SemanticSheetPlan sheet,
            IEnumerable<SemanticViewPlan> availableViews,
            IEnumerable<SemanticTitleBlockParameterDefinition> titleBlockMappings)
        {
            return BuildOrRefresh(document, project, sheet, availableViews, titleBlockMappings, refresh: true);
        }

        public static void Remove(Document document, ProjectState project, string sheetId, string layoutName)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            var normalizedSheetId = Required(sheetId, nameof(sheetId));
            var normalizedLayoutName = Required(layoutName, nameof(layoutName));
            if (!ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument))
                throw new InvalidOperationException("Semantic sheet remove yêu cầu DWG đích vẫn là MdiActiveDocument.");

            var rollback = ProjectStateSnapshot.Capture(project);
            var auditCommitted = false;
            try
            {
                using (document.LockDocument())
                {
                    var layoutId = RequireLayoutId(document.Database, normalizedLayoutName);
                    ValidateOwnedLayoutForRemove(document.Database, layoutId, project.ProjectId, normalizedSheetId);

                    AuditTrail.ForProject(project).Record(
                        "documentation.semantic-sheet.remove",
                        normalizedSheetId,
                        "layout=" + normalizedLayoutName);
                    auditCommitted = true;

                    var manager = LayoutManager.Current;
                    if (string.Equals(manager.CurrentLayout, normalizedLayoutName, StringComparison.OrdinalIgnoreCase))
                        manager.CurrentLayout = "Model";
                    manager.DeleteLayout(normalizedLayoutName);
                }
            }
            catch (Exception operationError)
            {
                var layoutStillExists = true;
                try
                {
                    using (document.LockDocument())
                        layoutStillExists = !TryGetLayoutId(document.Database, normalizedLayoutName).IsNull;
                }
                catch
                {
                    layoutStillExists = !auditCommitted;
                }

                if (layoutStillExists)
                {
                    try { rollback.Restore(project); }
                    catch (Exception restoreError)
                    {
                        throw new InvalidOperationException(
                            "Semantic sheet remove failed before layout deletion and project rollback also failed.",
                            new AggregateException(operationError, restoreError));
                    }
                }
                throw;
            }
        }

        public static string LayoutNameFor(SemanticSheetPlan sheet)
        {
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));
            return LayoutNameForNumber(sheet.Number);
        }

        public static string LayoutNameForNumber(string sheetNumber)
        {
            var number = Required(sheetNumber, nameof(sheetNumber));
            var builder = new StringBuilder(number.Length + 8);
            var pendingDash = false;
            foreach (var ch in number)
            {
                if (char.IsLetterOrDigit(ch) || ch == '_' || ch == '-')
                {
                    if (pendingDash && builder.Length > 0) builder.Append('-');
                    builder.Append(ch);
                    pendingDash = false;
                }
                else
                {
                    pendingDash = true;
                }
                if (builder.Length >= 48) break;
            }
            var component = builder.ToString().Trim('-');
            if (component.Length == 0) throw new InvalidOperationException("Semantic sheet number cannot produce a safe layout name.");
            return "QS3D-" + component;
        }

        private static string BuildOrRefresh(
            Document document,
            ProjectState project,
            SemanticSheetPlan sheet,
            IEnumerable<SemanticViewPlan> availableViews,
            IEnumerable<SemanticTitleBlockParameterDefinition> titleBlockMappings,
            bool refresh)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));
            if (availableViews == null) throw new ArgumentNullException(nameof(availableViews));
            if (titleBlockMappings == null) throw new ArgumentNullException(nameof(titleBlockMappings));
            if (!ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument))
                throw new InvalidOperationException("Semantic sheet materialization yêu cầu DWG đích vẫn là MdiActiveDocument.");

            var views = BuildViewIndex(availableViews);
            foreach (var placement in sheet.Placements)
                if (!views.ContainsKey(placement.ViewId))
                    throw new InvalidOperationException("Semantic sheet placement references unavailable view id: " + placement.ViewId + ".");
            var mappings = titleBlockMappings.ToArray();
            var layoutName = LayoutNameFor(sheet);
            var rollback = ProjectStateSnapshot.Capture(project);
            var newlyCreatedLayout = false;
            var cadCommitted = false;

            try
            {
                using (document.LockDocument())
                {
                    var existingLayoutId = TryGetLayoutId(document.Database, layoutName);
                    ObjectId layoutId;
                    if (refresh)
                    {
                        if (existingLayoutId.IsNull)
                            throw new InvalidOperationException("Semantic sheet layout does not exist for refresh: " + layoutName + ".");
                        layoutId = existingLayoutId;
                        ValidateOwnedLayoutForRefresh(document.Database, layoutId, project.ProjectId, sheet.Id);
                    }
                    else
                    {
                        if (!existingLayoutId.IsNull)
                            throw new InvalidOperationException("Layout " + layoutName + " already exists. Use QS3DSHEETREFRESH only if it is QS3D-owned.");
                        layoutId = LayoutManager.Current.CreateLayout(layoutName);
                        if (layoutId.IsNull || !layoutId.IsValid)
                            throw new InvalidOperationException("BricsCAD did not return a valid layout id for " + layoutName + ".");
                        newlyCreatedLayout = true;
                    }

                    using (var transaction = document.Database.TransactionManager.StartTransaction())
                    {
                        var layout = transaction.GetObject(layoutId, OpenMode.ForWrite, false) as Layout;
                        if (layout == null)
                            throw new InvalidOperationException("Could not open semantic sheet Layout: " + layoutName + ".");
                        var paperSpace = transaction.GetObject(layout.BlockTableRecordId, OpenMode.ForWrite, false) as BlockTableRecord;
                        if (paperSpace == null)
                            throw new InvalidOperationException("Could not open PaperSpace BlockTableRecord for semantic sheet " + sheet.Id + ".");

                        if (refresh)
                        {
                            SemanticSheetOwnershipService.RequireMatching(layout, project.ProjectId, sheet.Id, SemanticSheetOwnershipService.ArtifactLayout, "refresh semantic sheet layout");
                            SemanticSheetOwnershipService.RequireMatching(paperSpace, project.ProjectId, sheet.Id, SemanticSheetOwnershipService.ArtifactPaperSpace, "refresh semantic sheet paper space");
                            EraseOwnedContent(transaction, paperSpace, project.ProjectId, sheet.Id);
                        }
                        else
                        {
                            SemanticSheetOwnershipService.Mark(document.Database, transaction, layout, project.ProjectId, sheet.Id, SemanticSheetOwnershipService.ArtifactLayout);
                            SemanticSheetOwnershipService.Mark(document.Database, transaction, paperSpace, project.ProjectId, sheet.Id, SemanticSheetOwnershipService.ArtifactPaperSpace);
                            ClaimInitialPaperViewport(document.Database, transaction, paperSpace, project.ProjectId, sheet.Id);
                        }

                        foreach (var placement in sheet.Placements)
                            CreateViewport(document, transaction, paperSpace, project, sheet, placement, views[placement.ViewId]);

                        if (!string.IsNullOrWhiteSpace(sheet.TitleBlockName))
                            CreateTitleBlock(document.Database, transaction, paperSpace, project, sheet, mappings);

                        AuditTrail.ForProject(project).Record(
                            refresh ? "documentation.semantic-sheet.refresh" : "documentation.semantic-sheet.build",
                            sheet.Id,
                            "layout=" + layoutName + " • viewports=" + sheet.Placements.Count.ToString(CultureInfo.InvariantCulture) +
                            " • titleBlock=" + (sheet.TitleBlockName ?? string.Empty));
                        transaction.Commit();
                        cadCommitted = true;
                    }
                }
            }
            catch (Exception operationError)
            {
                if (!cadCommitted)
                {
                    Exception? restoreError = null;
                    try { rollback.Restore(project); } catch (Exception ex) { restoreError = ex; }
                    Exception? cleanupError = null;
                    if (newlyCreatedLayout)
                    {
                        try { CleanupFailedNewLayout(document, layoutName); } catch (Exception ex) { cleanupError = ex; }
                    }
                    if (restoreError != null || cleanupError != null)
                    {
                        var failures = new List<Exception> { operationError };
                        if (restoreError != null) failures.Add(restoreError);
                        if (cleanupError != null) failures.Add(cleanupError);
                        throw new InvalidOperationException(
                            "Semantic sheet materialization failed and rollback/cleanup was not fully successful.",
                            new AggregateException(failures));
                    }
                }
                throw;
            }

            return layoutName;
        }

        private static void ClaimInitialPaperViewport(
            Database database,
            Transaction transaction,
            BlockTableRecord paperSpace,
            string projectId,
            string sheetId)
        {
            foreach (ObjectId id in paperSpace)
            {
                var entity = transaction.GetObject(id, OpenMode.ForWrite, false) as Entity;
                if (entity == null || entity.IsErased) continue;
                if (SemanticSheetOwnershipService.HasMarker(entity))
                    throw new InvalidOperationException("New semantic sheet layout unexpectedly contains pre-owned QS3D_SHEET content.");
                if (!(entity is Viewport))
                    throw new InvalidOperationException("New semantic sheet layout contains unexpected non-viewport content before QS3D materialization.");
                SemanticSheetOwnershipService.Mark(database, transaction, entity, projectId, sheetId, SemanticSheetOwnershipService.ArtifactPaperViewport);
            }
        }

        private static void EraseOwnedContent(
            Transaction transaction,
            BlockTableRecord paperSpace,
            string projectId,
            string sheetId)
        {
            var erase = new List<Entity>();
            foreach (ObjectId id in paperSpace)
            {
                var entity = transaction.GetObject(id, OpenMode.ForWrite, false) as Entity;
                if (entity == null || entity.IsErased) continue;
                if (!SemanticSheetOwnershipService.HasMarker(entity)) continue;
                if (!SemanticSheetOwnershipService.TryRead(entity, out var ownerProject, out var ownerSheet, out var artifact, out var viewId))
                    throw new InvalidOperationException("Refusing semantic sheet refresh because PaperSpace contains malformed QS3D_SHEET ownership metadata.");
                if (!string.Equals(ownerProject, projectId, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(ownerSheet, sheetId, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Refusing semantic sheet refresh because PaperSpace contains a QS3D_SHEET artifact owned by another project/sheet.");
                if (string.Equals(artifact, SemanticSheetOwnershipService.ArtifactPaperViewport, StringComparison.Ordinal)) continue;
                if (string.Equals(artifact, SemanticSheetOwnershipService.ArtifactViewport, StringComparison.Ordinal))
                {
                    if (!(entity is Viewport) || string.IsNullOrWhiteSpace(viewId))
                        throw new InvalidOperationException("Semantic sheet owned viewport metadata/type is inconsistent.");
                    erase.Add(entity);
                    continue;
                }
                if (string.Equals(artifact, SemanticSheetOwnershipService.ArtifactTitleBlock, StringComparison.Ordinal))
                {
                    if (!(entity is BlockReference))
                        throw new InvalidOperationException("Semantic sheet owned title-block metadata/type is inconsistent.");
                    erase.Add(entity);
                    continue;
                }
                throw new InvalidOperationException("Unexpected owned semantic sheet artifact inside PaperSpace: " + artifact + ".");
            }
            foreach (var entity in erase) entity.Erase();
        }

        private static void CreateViewport(
            Document document,
            Transaction transaction,
            BlockTableRecord paperSpace,
            ProjectState project,
            SemanticSheetPlan sheet,
            SemanticSheetPlacementPlan placement,
            SemanticViewPlan view)
        {
            var model = ReadViewExtents(document.Database, transaction, project, view);
            var paperWidth = MmToDrawingUnits(document, placement.WidthMm, view.Id + "/viewport width");
            var paperHeight = MmToDrawingUnits(document, placement.HeightMm, view.Id + "/viewport height");
            var centerX = MmToDrawingUnits(document, placement.Xmm + placement.WidthMm * 0.5d, view.Id + "/viewport center X");
            var centerY = MmToDrawingUnits(document, placement.Ymm + placement.HeightMm * 0.5d, view.Id + "/viewport center Y");

            var modelWidth = Math.Max(model.MaxPoint.X - model.MinPoint.X, 1e-9d);
            var modelHeight = Math.Max(model.MaxPoint.Y - model.MinPoint.Y, 1e-9d);
            var modelCenter = new Point3d(
                (model.MinPoint.X + model.MaxPoint.X) * 0.5d,
                (model.MinPoint.Y + model.MaxPoint.Y) * 0.5d,
                (model.MinPoint.Z + model.MaxPoint.Z) * 0.5d);
            var customScale = Math.Min(paperWidth / modelWidth, paperHeight / modelHeight) * 0.9d;
            if (!Finite(customScale) || !(customScale > 0d))
                throw new InvalidOperationException("Semantic sheet viewport scale is not finite/positive for view " + view.Id + ".");

            var viewport = new Viewport
            {
                CenterPoint = new Point3d(centerX, centerY, 0d),
                Width = paperWidth,
                Height = paperHeight,
                ViewDirection = Vector3d.ZAxis,
                ViewTarget = modelCenter,
                ViewCenter = Point2d.Origin,
                ViewHeight = modelHeight * 1.1d,
                CustomScale = customScale,
                On = true
            };
            viewport.SetDatabaseDefaults(document.Database);
            paperSpace.AppendEntity(viewport);
            transaction.AddNewlyCreatedDBObject(viewport, true);
            SemanticSheetOwnershipService.Mark(document.Database, transaction, viewport, project.ProjectId, sheet.Id, SemanticSheetOwnershipService.ArtifactViewport, view.Id);
            viewport.Locked = true;
        }

        private static Extents3d ReadViewExtents(
            Database database,
            Transaction transaction,
            ProjectState project,
            SemanticViewPlan view)
        {
            if (view.ElementIds.Count == 0)
                throw new InvalidOperationException("Semantic sheet cannot materialize empty view id " + view.Id + ".");
            var elements = BuildElementIndex(project);
            var hasExtents = false;
            var min = Point3d.Origin;
            var max = Point3d.Origin;
            foreach (var elementId in view.ElementIds)
            {
                if (!elements.TryGetValue(elementId, out var element))
                    throw new InvalidOperationException("Semantic view references missing project element id during materialization: " + elementId + ".");
                var handles = AuthoritativeGeometryHandles(element);
                if (handles.Count == 0)
                    throw new InvalidOperationException("Semantic view element has no authoritative live geometry handle: " + element.Id + ".");
                foreach (var handle in handles)
                {
                    var entity = transaction.GetObject(ResolveHandle(database, handle, "semantic sheet view " + view.Id), OpenMode.ForRead, false) as Entity;
                    if (entity == null || entity.IsErased)
                        throw new InvalidOperationException("Semantic sheet view geometry is missing/erased for handle " + handle + ".");
                    Extents3d extents;
                    try { extents = entity.GeometricExtents; }
                    catch (Exception error)
                    {
                        throw new InvalidOperationException("Could not read GeometricExtents for semantic sheet view handle " + handle + ".", error);
                    }
                    if (!hasExtents)
                    {
                        min = extents.MinPoint;
                        max = extents.MaxPoint;
                        hasExtents = true;
                    }
                    else
                    {
                        min = new Point3d(Math.Min(min.X, extents.MinPoint.X), Math.Min(min.Y, extents.MinPoint.Y), Math.Min(min.Z, extents.MinPoint.Z));
                        max = new Point3d(Math.Max(max.X, extents.MaxPoint.X), Math.Max(max.Y, extents.MaxPoint.Y), Math.Max(max.Z, extents.MaxPoint.Z));
                    }
                }
            }
            if (!hasExtents || !FinitePoint(min) || !FinitePoint(max))
                throw new InvalidOperationException("Semantic sheet view has no finite model extents: " + view.Id + ".");
            return new Extents3d(min, max);
        }

        private static Dictionary<string, ProjectElement> BuildElementIndex(ProjectState project)
        {
            var result = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null) throw new InvalidOperationException("Project contains a null semantic element.");
                if (result.ContainsKey(element.Id))
                    throw new InvalidOperationException("Project contains duplicate semantic element id during sheet materialization: " + element.Id + ".");
                result.Add(element.Id, element);
            }
            return result;
        }

        private static IReadOnlyList<string> AuthoritativeGeometryHandles(ProjectElement element)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in element.SourceHandles)
            {
                var handle = (raw ?? string.Empty).Trim();
                if (handle.Length > 0 && seen.Add(handle)) result.Add(handle);
            }
            if (element.Properties.TryGetValue(GeneratedSolidHandleKey, out var generatedRaw))
            {
                var generated = (generatedRaw ?? string.Empty).Trim();
                if (generated.Length > 0 && seen.Add(generated)) result.Add(generated);
            }
            return result.AsReadOnly();
        }

        private static void CreateTitleBlock(
            Database database,
            Transaction transaction,
            BlockTableRecord paperSpace,
            ProjectState project,
            SemanticSheetPlan sheet,
            IReadOnlyList<SemanticTitleBlockParameterDefinition> mappings)
        {
            var blockName = Required(sheet.TitleBlockName, nameof(sheet.TitleBlockName));
            var blockTable = transaction.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;
            if (blockTable == null || !blockTable.Has(blockName))
                throw new InvalidOperationException("Semantic sheet title-block definition does not exist in the drawing: " + blockName + ".");
            var blockId = blockTable[blockName];
            var definition = transaction.GetObject(blockId, OpenMode.ForRead, false) as BlockTableRecord;
            if (definition == null || definition.IsLayout)
                throw new InvalidOperationException("Semantic sheet title-block name does not resolve to a reusable block definition: " + blockName + ".");

            var parameterMap = SemanticTitleBlockParameterMapBuilder.Build(sheet, mappings);
            var values = parameterMap.Values.ToDictionary(x => x.DestinationTag, x => x.Value, StringComparer.OrdinalIgnoreCase);
            var reference = new BlockReference(Point3d.Origin, blockId);
            reference.SetDatabaseDefaults(database);
            paperSpace.AppendEntity(reference);
            transaction.AddNewlyCreatedDBObject(reference, true);
            SemanticSheetOwnershipService.Mark(database, transaction, reference, project.ProjectId, sheet.Id, SemanticSheetOwnershipService.ArtifactTitleBlock);

            var seenTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ObjectId id in definition)
            {
                var definitionAttribute = transaction.GetObject(id, OpenMode.ForRead, false) as AttributeDefinition;
                if (definitionAttribute == null) continue;
                var tag = (definitionAttribute.Tag ?? string.Empty).Trim();
                if (tag.Length == 0) continue;
                if (!seenTags.Add(tag))
                    throw new InvalidOperationException("Title-block definition contains duplicate attribute tag: " + tag + ".");
                if (definitionAttribute.Constant) continue;

                var attribute = new AttributeReference();
                attribute.SetAttributeFromBlock(definitionAttribute, reference.BlockTransform);
                if (values.TryGetValue(tag, out var value)) attribute.TextString = value;
                reference.AttributeCollection.AppendAttribute(attribute);
                transaction.AddNewlyCreatedDBObject(attribute, true);
            }
        }

        private static Dictionary<string, SemanticViewPlan> BuildViewIndex(IEnumerable<SemanticViewPlan> availableViews)
        {
            var result = new Dictionary<string, SemanticViewPlan>(StringComparer.OrdinalIgnoreCase);
            using (var enumerator = availableViews.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (result.Count >= MaxViews)
                        throw new InvalidOperationException("Native semantic sheet materialization supports at most " + MaxViews + " available views per operation.");
                    var view = enumerator.Current ?? throw new InvalidOperationException("Available semantic view cannot be null.");
                    if (result.ContainsKey(view.Id))
                        throw new InvalidOperationException("Available semantic views contain duplicate id: " + view.Id + ".");
                    result.Add(view.Id, view);
                }
            }
            return result;
        }

        private static void ValidateOwnedLayoutForRefresh(Database database, ObjectId layoutId, string projectId, string sheetId)
        {
            using (var transaction = database.TransactionManager.StartOpenCloseTransaction())
            {
                var layout = transaction.GetObject(layoutId, OpenMode.ForRead, false) as Layout ?? throw new InvalidOperationException("Semantic sheet layout id no longer resolves to Layout.");
                SemanticSheetOwnershipService.RequireMatching(layout, projectId, sheetId, SemanticSheetOwnershipService.ArtifactLayout, "refresh semantic sheet layout");
                var paper = transaction.GetObject(layout.BlockTableRecordId, OpenMode.ForRead, false) as BlockTableRecord ?? throw new InvalidOperationException("Semantic sheet Layout no longer resolves to PaperSpace.");
                SemanticSheetOwnershipService.RequireMatching(paper, projectId, sheetId, SemanticSheetOwnershipService.ArtifactPaperSpace, "refresh semantic sheet paper space");
                foreach (ObjectId id in paper)
                {
                    var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                    if (entity == null || entity.IsErased || !SemanticSheetOwnershipService.HasMarker(entity)) continue;
                    if (!SemanticSheetOwnershipService.TryRead(entity, out var ownerProject, out var ownerSheet, out var artifact, out var viewId))
                        throw new InvalidOperationException("Semantic sheet PaperSpace contains malformed QS3D_SHEET ownership metadata.");
                    if (!string.Equals(ownerProject, projectId, StringComparison.OrdinalIgnoreCase) || !string.Equals(ownerSheet, sheetId, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Semantic sheet PaperSpace contains mixed QS3D_SHEET ownership; refusing refresh.");
                    ValidateArtifactEntityType(entity, artifact, viewId);
                }
                transaction.Commit();
            }
        }

        private static void ValidateOwnedLayoutForRemove(Database database, ObjectId layoutId, string projectId, string sheetId)
        {
            using (var transaction = database.TransactionManager.StartOpenCloseTransaction())
            {
                var layout = transaction.GetObject(layoutId, OpenMode.ForRead, false) as Layout ?? throw new InvalidOperationException("Semantic sheet layout id no longer resolves to Layout.");
                SemanticSheetOwnershipService.RequireMatching(layout, projectId, sheetId, SemanticSheetOwnershipService.ArtifactLayout, "remove semantic sheet layout");
                var paper = transaction.GetObject(layout.BlockTableRecordId, OpenMode.ForRead, false) as BlockTableRecord ?? throw new InvalidOperationException("Semantic sheet Layout no longer resolves to PaperSpace.");
                SemanticSheetOwnershipService.RequireMatching(paper, projectId, sheetId, SemanticSheetOwnershipService.ArtifactPaperSpace, "remove semantic sheet paper space");
                foreach (ObjectId id in paper)
                {
                    var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                    if (entity == null || entity.IsErased) continue;
                    if (!SemanticSheetOwnershipService.HasMarker(entity))
                        throw new InvalidOperationException("Refusing semantic sheet remove because the generated layout contains unowned live PaperSpace content: " + entity.Handle + ".");
                    if (!SemanticSheetOwnershipService.TryRead(entity, out var ownerProject, out var ownerSheet, out var artifact, out var viewId))
                        throw new InvalidOperationException("Refusing semantic sheet remove because PaperSpace contains malformed QS3D_SHEET metadata.");
                    if (!string.Equals(ownerProject, projectId, StringComparison.OrdinalIgnoreCase) || !string.Equals(ownerSheet, sheetId, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Refusing semantic sheet remove because PaperSpace contains a QS3D_SHEET artifact owned by another project/sheet.");
                    ValidateArtifactEntityType(entity, artifact, viewId);
                }
                transaction.Commit();
            }
        }

        private static void ValidateArtifactEntityType(Entity entity, string artifact, string viewId)
        {
            if (string.Equals(artifact, SemanticSheetOwnershipService.ArtifactPaperViewport, StringComparison.Ordinal))
            {
                if (!(entity is Viewport) || !string.IsNullOrWhiteSpace(viewId))
                    throw new InvalidOperationException("Owned PaperViewport marker/type is inconsistent.");
                return;
            }
            if (string.Equals(artifact, SemanticSheetOwnershipService.ArtifactViewport, StringComparison.Ordinal))
            {
                if (!(entity is Viewport) || string.IsNullOrWhiteSpace(viewId))
                    throw new InvalidOperationException("Owned semantic Viewport marker/type is inconsistent.");
                return;
            }
            if (string.Equals(artifact, SemanticSheetOwnershipService.ArtifactTitleBlock, StringComparison.Ordinal))
            {
                if (!(entity is BlockReference) || !string.IsNullOrWhiteSpace(viewId))
                    throw new InvalidOperationException("Owned semantic TitleBlock marker/type is inconsistent.");
                return;
            }
            throw new InvalidOperationException("Unexpected QS3D_SHEET artifact in PaperSpace: " + artifact + ".");
        }

        private static ObjectId TryGetLayoutId(Database database, string layoutName)
        {
            using (var transaction = database.TransactionManager.StartOpenCloseTransaction())
            {
                var dictionary = transaction.GetObject(database.LayoutDictionaryId, OpenMode.ForRead, false) as DBDictionary;
                if (dictionary == null || !dictionary.Contains(layoutName)) return ObjectId.Null;
                var id = dictionary.GetAt(layoutName);
                transaction.Commit();
                return id;
            }
        }

        private static ObjectId RequireLayoutId(Database database, string layoutName)
        {
            var id = TryGetLayoutId(database, layoutName);
            if (id.IsNull || !id.IsValid)
                throw new InvalidOperationException("Semantic sheet layout does not exist: " + layoutName + ".");
            return id;
        }

        private static void CleanupFailedNewLayout(Document document, string layoutName)
        {
            using (document.LockDocument())
            {
                var manager = LayoutManager.Current;
                var id = TryGetLayoutId(document.Database, layoutName);
                if (id.IsNull) return;
                if (string.Equals(manager.CurrentLayout, layoutName, StringComparison.OrdinalIgnoreCase)) manager.CurrentLayout = "Model";
                manager.DeleteLayout(layoutName);
            }
        }

        private static ObjectId ResolveHandle(Database database, string text, string label)
        {
            var canonical = CadHandleService.NormalizeHexHandle(text);
            if (canonical == null || !long.TryParse(canonical, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
                throw new InvalidOperationException(label + " Handle không hợp lệ: " + text + ".");
            try
            {
                var id = database.GetObjectId(false, new Handle(value), 0);
                if (!id.IsNull && id.IsValid) return id;
            }
            catch (Exception error)
            {
                throw new InvalidOperationException("Không resolve được " + label + " Handle: " + text + ".", error);
            }
            throw new InvalidOperationException("Không resolve được " + label + " Handle: " + text + ".");
        }

        private static double MmToDrawingUnits(Document document, double millimeters, string label)
        {
            if (!Finite(millimeters) || millimeters < 0d) throw new InvalidOperationException(label + " must be finite and non-negative.");
            var value = CadUnitService.MetersToDrawingUnits(document, millimeters / 1000d);
            if (!Finite(value) || value < 0d) throw new InvalidOperationException(label + " could not be converted to finite drawing units.");
            return value;
        }

        private static string Required(string? value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", name);
            return value!.Trim();
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static bool FinitePoint(Point3d value) => Finite(value.X) && Finite(value.Y) && Finite(value.Z);
    }
}