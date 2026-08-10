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
        private sealed class PendingUpdate
        {
            public ProjectElement Element { get; set; } = null!;
            public string PreviousHandle { get; set; } = string.Empty;
            public string GeneratedHandle { get; set; } = string.Empty;
            public double LengthM { get; set; }
            public double ThicknessM { get; set; }
            public double HeightM { get; set; }
        }

        public static int BuildSelectedLineWalls(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            var selection = document.Editor.SelectImplied();
            if (selection.Status != PromptStatus.OK || selection.Value == null) return 0;
            var sourceIds = selection.Value.GetObjectIds();
            if (sourceIds.Length == 0) return 0;
            var pending = new List<PendingUpdate>();

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
                    var thicknessM = Number(element, family, "ThicknessM", .2d);
                    var heightM = Number(element, family, "HeightM", 3.6d);
                    var bottomOffsetM = Number(element, family, "BottomOffsetM", 0d);
                    var dx = line.EndPoint.X - line.StartPoint.X;
                    var dy = line.EndPoint.Y - line.StartPoint.Y;
                    var length = Math.Sqrt(dx * dx + dy * dy);
                    if (length <= 1e-6 || thicknessM <= 0d || heightM <= 0d) continue;

                    var thickness = CadUnitService.MetersToDrawingUnits(document, thicknessM);
                    var height = CadUnitService.MetersToDrawingUnits(document, heightM);
                    var bottomOffset = CadUnitService.MetersToDrawingUnits(document, bottomOffsetM);
                    var angle = Math.Atan2(dy, dx);
                    var mid = new Point3d((line.StartPoint.X + line.EndPoint.X) / 2d, (line.StartPoint.Y + line.EndPoint.Y) / 2d, line.StartPoint.Z + bottomOffset + height / 2d);

                    var solid = new Solid3d();
                    try
                    {
                        solid.SetDatabaseDefaults(document.Database);
                        solid.CreateBox(length, thickness, height);
                        solid.TransformBy(Matrix3d.Displacement(new Vector3d(-length / 2d, -thickness / 2d, -height / 2d)));
                        solid.TransformBy(Matrix3d.Rotation(angle, Vector3d.ZAxis, Point3d.Origin));
                        solid.TransformBy(Matrix3d.Displacement(new Vector3d(mid.X, mid.Y, mid.Z)));
                        solid.Layer = line.Layer;

                        var previousHandle = GeneratedGeometryService.PrepareReplacement(document, transaction, element);
                        modelSpace.AppendEntity(solid);
                        transaction.AddNewlyCreatedDBObject(solid, true);
                        pending.Add(new PendingUpdate
                        {
                            Element = element,
                            PreviousHandle = previousHandle,
                            GeneratedHandle = solid.Handle.ToString(),
                            LengthM = CadUnitService.DrawingUnitsToMeters(document, length),
                            ThicknessM = thicknessM,
                            HeightM = heightM
                        });
                    }
                    catch
                    {
                        solid.Dispose();
                        throw;
                    }
                }
                transaction.Commit();
            }

            foreach (var update in pending)
            {
                GeneratedGeometryService.CommitReplacement(update.Element, update.PreviousHandle, update.GeneratedHandle, ElementCategory.ArchitecturalWall);
                update.Element.Properties["LengthM"] = update.LengthM.ToString("R", CultureInfo.InvariantCulture);
                update.Element.Properties["ThicknessM"] = update.ThicknessM.ToString("R", CultureInfo.InvariantCulture);
                update.Element.Properties["HeightM"] = update.HeightM.ToString("R", CultureInfo.InvariantCulture);
            }

            if (pending.Count > 0)
            {
                document.Editor.Regen();
                project.Touch();
            }
            return pending.Count;
        }

        private static double Number(ProjectElement element, ProjectFamily? family, string name, double fallback)
        {
            if (element.Properties.TryGetValue(name, out var value) && TryFinite(value, out var direct)) return direct;
            if (family != null && family.Properties.TryGetValue(name, out value) && TryFinite(value, out var inherited)) return inherited;
            return fallback;
        }

        private static bool TryFinite(string value, out double number)
        {
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number)) return false;
            return !double.IsNaN(number) && !double.IsInfinity(number);
        }
    }
}
