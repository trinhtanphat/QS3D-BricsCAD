using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Rebar;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class XlsxScheduleNumericPreflightSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-xlsx-numeric-preflight-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                RejectDoor(root);
                RejectMaterial(root);
                RejectCurtain(root);
                RejectRoomFinish(root);
                RejectRebar(root);
                ExportFiniteRows(root);
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        private static void RejectDoor(string root)
        {
            var row = DoorRow();
            row.HeightM = double.NaN;
            ExpectPreflight(
                Path.Combine(root, "door-invalid"),
                "HeightM",
                path => DoorOpeningXlsxExporter.Export(path, new[] { row }));
        }

        private static void RejectMaterial(string root)
        {
            var row = MaterialRow();
            row.UnitHint = "m";
            row.AreaM2 = double.PositiveInfinity;
            ExpectPreflight(
                Path.Combine(root, "material-invalid"),
                "AreaM2",
                path => MaterialUsageXlsxExporter.Export(path, new[] { row }));
        }

        private static void RejectCurtain(string root)
        {
            var row = CurtainRow();
            row.FrameLengthM = double.NegativeInfinity;
            ExpectPreflight(
                Path.Combine(root, "curtain-invalid"),
                "FrameLengthM",
                path => CurtainWallXlsxExporter.Export(path, new[] { row }));
        }

        private static void RejectRoomFinish(string root)
        {
            var row = RoomFinishRow();
            row.LengthM = double.NaN;
            ExpectPreflight(
                Path.Combine(root, "room-invalid"),
                "LengthM",
                path => RoomFinishXlsxExporter.Export(path, new[] { row }));
        }

        private static void RejectRebar(string root)
        {
            var row = RebarRow();
            row.WastePercent = double.PositiveInfinity;
            ExpectPreflight(
                Path.Combine(root, "rebar-invalid"),
                "WastePercent",
                path => XlsxRebarScheduleExporter.Export(path, new[] { row }));
        }

        private static void ExpectPreflight(string directory, string field, Action<string> export)
        {
            DeleteDirectory(directory);
            try
            {
                export(Path.Combine(directory, "invalid.xlsx"));
            }
            catch (ArgumentOutOfRangeException ex)
            {
                if (!string.Equals(ex.ParamName, "rows", StringComparison.Ordinal))
                    throw new InvalidOperationException("XLSX numeric preflight must identify the rows argument.", ex);
                if (ex.Message.IndexOf("worksheet row 2", StringComparison.OrdinalIgnoreCase) < 0 ||
                    ex.Message.IndexOf(field, StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException("XLSX numeric preflight must identify worksheet row 2 and field " + field + ".", ex);
                if (Directory.Exists(directory))
                    throw new InvalidOperationException("XLSX numeric preflight touched the filesystem before rejecting " + field + ".");
                return;
            }

            throw new InvalidOperationException("XLSX exporter accepted non-finite field " + field + ".");
        }

        private static void ExportFiniteRows(string root)
        {
            var door = Path.Combine(root, "door-valid.xlsx");
            var material = Path.Combine(root, "material-valid.xlsx");
            var curtain = Path.Combine(root, "curtain-valid.xlsx");
            var room = Path.Combine(root, "room-valid.xlsx");
            var rebar = Path.Combine(root, "rebar-valid.xlsx");

            DoorOpeningXlsxExporter.Export(door, new[] { DoorRow() });
            MaterialUsageXlsxExporter.Export(material, new[] { MaterialRow() });
            CurtainWallXlsxExporter.Export(curtain, new[] { CurtainRow() });
            RoomFinishXlsxExporter.Export(room, new[] { RoomFinishRow() });
            XlsxRebarScheduleExporter.Export(rebar, new[] { RebarRow() });

            foreach (var path in new[] { door, material, curtain, room, rebar })
                if (!File.Exists(path)) throw new InvalidOperationException("Finite XLSX export did not produce " + Path.GetFileName(path) + ".");
        }

        private static DoorOpeningScheduleRow DoorRow()
        {
            var row = new DoorOpeningScheduleRow
            {
                Floor = "L1",
                Category = "Door",
                FamilyName = "D1",
                Material = "Wood",
                WidthM = 0.9d,
                HeightM = 2.1d,
                SillHeightM = 0d,
                ThicknessM = 0.1d,
                Count = 1,
                OpeningAreaM2 = 1.89d,
                HostCount = 1
            };
            row.ElementIds.Add("E-1");
            row.HostIds.Add("H-1");
            return row;
        }

        private static MaterialUsageRow MaterialRow()
        {
            return new MaterialUsageRow
            {
                Floor = "L1",
                MaterialName = "Concrete",
                UnitHint = "m2",
                Component = "Material",
                Category = "ArchitecturalWall",
                FamilyName = "W1",
                ElementCount = 1,
                ElementIds = { "E-1" },
                LengthM = 1d,
                AreaM2 = 2d,
                VolumeM3 = 0.2d,
                MassKg = 10d
            };
        }

        private static CurtainWallScheduleRow CurtainRow()
        {
            var row = new CurtainWallScheduleRow
            {
                Floor = "L1",
                FamilyName = "CW1",
                WallCount = 1,
                TotalWallLengthM = 2d,
                GrossWallAreaM2 = 6d,
                OpeningAreaM2 = 0d,
                NetGlassAreaM2 = 5d,
                FrameFaceAreaM2 = 1d,
                FrameLengthM = 4d,
                PanelCount = 2,
                VerticalFrameCount = 1,
                HorizontalFrameCount = 1,
                MinimumClearPanelWidthM = 0.9d,
                MaximumClearPanelWidthM = 1d,
                MinimumClearPanelHeightM = 2.8d,
                MaximumClearPanelHeightM = 3d
            };
            row.ElementIds.Add("CW-1");
            row.SourceHandles.Add("CW-HANDLE-1");
            return row;
        }

        private static RoomFinishScheduleRow RoomFinishRow()
        {
            return new RoomFinishScheduleRow
            {
                Floor = "L1",
                Room = "R1",
                Category = "FloorFinish",
                FamilyName = "F1",
                Material = "Tile",
                UnitHint = "m2",
                Count = 1,
                PrimaryQuantity = 2d,
                LengthM = 1d,
                AreaM2 = 2d
            };
        }

        private static RebarScheduleRow RebarRow()
        {
            return new RebarScheduleRow
            {
                ElementId = "E-1",
                BarMark = "B1",
                ShapeCode = "00",
                Notation = "1T10",
                DiameterMm = 10d,
                Quantity = 1,
                CuttingLengthM = 1d,
                TotalLengthM = 1d,
                UnitWeightKgM = 0.617d,
                NetWeightKg = 0.617d,
                WastePercent = 0d,
                TotalWeightKg = 0.617d,
                FabricationStatus = "Ready",
                FabricationStandardCode = "STD",
                FabricationDetailingRevision = "R1"
            };
        }

        private static void DeleteDirectory(string directory)
        {
            try
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
