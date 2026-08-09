using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.Core.Domain;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class WallSolidBuilder
    {
        public static int BuildSelectedLineWalls(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            var selection = document.Editor.SelectImplied();
            if (selection.Status != PromptStatus.OK || selection.Value == null) return 0;
            var sourceIds = selection.Value.GetObjectIds();
            if (sourceIds.Length == 0) return 0;
            var created = 0;

            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                foreach (var id in sourceIds)
                {
                    var line = transaction.GetObject(id, OpenMode.ForRead, false) as Line;
                    if (line == null) continue;
                    var sourceHandle = line.Handle.ToString();
                    var element = project.Elements.FirstOrDefault(x => x.Category == ElementCategory.ArchitecturalWall && x.SourceHandles.Any(h => string.Equals(h, sourceHandle, StringComparison.OrdinalIgnoreCase)));
                    if (element == null) continue;
                    var family = project.FindFamily(element.FamilyId);
                    var thicknessM = Number(element, family, "ThicknessM", .2);
                    var heightM = Number(element, family, "HeightM", 3.6);
                    var bottomOffsetM = Number(element, family, "BottomOffsetM", 0d);
                    var dx = line.EndPoint.X - line.StartPoint.X; var dy = line.EndPoint.Y - line.StartPoint.Y;
                    var length = Math.Sqrt(dx * dx + dy * dy);
                    if (length <= 1e-6 || thicknessM <= 0 || heightM <= 0) continue;
                    var thickness = thicknessM * 1000d; var height = heightM * 1000d; var angle = Math.Atan2(dy, dx);
                    var mid = new Point3d((line.StartPoint.X + line.EndPoint.X) / 2d, (line.StartPoint.Y + line.EndPoint.Y) / 2d, line.StartPoint.Z + bottomOffsetM * 1000d + height / 2d);
                    var solid = new Solid3d();
                    try
                    {
                        solid.SetDatabaseDefaults(document.Database);
                        solid.CreateBox(length, thickness, height);
                        solid.TransformBy(Matrix3d.Rotation(angle, Vector3d.ZAxis, Point3d.Origin));
                        solid.TransformBy(Matrix3d.Displacement(new Vector3d(mid.X, mid.Y, mid.Z)));
                        solid.Layer = line.Layer;
                        modelSpace.AppendEntity(solid); transaction.AddNewlyCreatedDBObject(solid, true);
                        var generatedHandle = solid.Handle.ToString();
                        element.Properties["GeneratedSolidHandle"] = generatedHandle;
                        if (!element.SourceHandles.Any(x => string.Equals(x, generatedHandle, StringComparison.OrdinalIgnoreCase))) element.SourceHandles.Add(generatedHandle);
                        element.Properties["LengthM"] = (length / 1000d).ToString("R", CultureInfo.InvariantCulture);
                        element.Properties["ThicknessM"] = thicknessM.ToString("R", CultureInfo.InvariantCulture);
                        element.Properties["HeightM"] = heightM.ToString("R", CultureInfo.InvariantCulture);
                        created++;
                    }
                    catch { solid.Dispose(); throw; }
                }
                transaction.Commit();
            }
            document.Editor.Regen(); project.Touch(); return created;
        }

        private static double Number(ProjectElement element, ProjectFamily? family, string name, double fallback)
        {
            if (element.Properties.TryGetValue(name, out var value) && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var direct)) return direct;
            if (family != null && family.Properties.TryGetValue(name, out value) && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var inherited)) return inherited;
            return fallback;
        }
    }
}
