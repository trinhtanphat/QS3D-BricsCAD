using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarCsvSemanticIdentitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        private static void Run()
        {
            RebarElementIdentityRejectsFormulaPrefixes();
            ProcurementGroupIdentityRejectsFormulaPrefixes();
            RebarExportRejectsBeforeFileSystemMutation();
            ProcurementExportRejectsBeforeFileSystemMutation();
            ValidUnicodeIdentitiesArePreservedExactly();
            PresentationFormulaHardeningRemainsUnchanged();
        }

        private static void RebarElementIdentityRejectsFormulaPrefixes()
        {
            foreach (var prefix in FormulaPrefixes())
            {
                var identity = prefix + "E1";
                Throws<InvalidDataException>(
                    () => RebarCsvExporter.ToCsv(new[] { RebarRow(identity) }),
                    "Rebar CSV must reject formula-leading semantic ElementId " + identity + ".");
            }
        }

        private static void ProcurementGroupIdentityRejectsFormulaPrefixes()
        {
            foreach (var prefix in FormulaPrefixes())
            {
                var identity = prefix + "GROUP";
                Throws<InvalidDataException>(
                    () => RebarProcurementCsvExporter.ToCsv(ProcurementRows(identity)),
                    "Rebar procurement CSV must reject formula-leading GroupId " + identity + ".");
            }
        }

        private static void RebarExportRejectsBeforeFileSystemMutation()
        {
            AssertRejectsBeforeFileSystemMutation(
                path => RebarCsvExporter.Export(path, new[] { RebarRow("=E1") }),
                "Rebar CSV");
        }

        private static void ProcurementExportRejectsBeforeFileSystemMutation()
        {
            AssertRejectsBeforeFileSystemMutation(
                path => RebarProcurementCsvExporter.Export(path, ProcurementRows("@GROUP")),
                "Rebar procurement CSV");
        }

        private static void ValidUnicodeIdentitiesArePreservedExactly()
        {
            const string elementId = "CỐT-ĐAI-α-钢";
            var rebarCsv = RebarCsvExporter.ToCsv(new[] { RebarRow(elementId) });
            Contains(
                rebarCsv,
                "\"" + elementId + "\",",
                "Rebar CSV changed a valid Unicode semantic ElementId.");

            const string groupId = "Nhóm-钢-α-01";
            var procurementCsv = RebarProcurementCsvExporter.ToCsv(ProcurementRows(groupId));
            Contains(
                procurementCsv,
                ",\"" + groupId + "\",\"CB400\",",
                "Rebar procurement CSV changed a valid Unicode GroupId.");
        }

        private static void PresentationFormulaHardeningRemainsUnchanged()
        {
            var row = RebarRow("E1");
            row.BarMark = "=B1";
            row.FabricationStatus = "@READY";
            var csv = RebarCsvExporter.ToCsv(new[] { row });
            Contains(csv, "\"'=B1\"", "Rebar CSV BarMark formula hardening regressed.");
            Contains(csv, "\"'@READY\"", "Rebar CSV presentation-text formula hardening regressed.");
        }

        private static void AssertRejectsBeforeFileSystemMutation(Action<string> export, string label)
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-rebar-csv-identity-" + Guid.NewGuid().ToString("N"));
            try
            {
                var missingPath = Path.Combine(root, "missing", "report.csv");
                Throws<InvalidDataException>(
                    () => export(missingPath),
                    label + " must reject semantic identity before creating a missing destination directory.");
                if (Directory.Exists(root))
                    throw new InvalidOperationException(label + " created filesystem state before semantic identity validation completed.");

                Directory.CreateDirectory(root);
                var existingPath = Path.Combine(root, "existing.csv");
                const string sentinel = "existing-destination";
                File.WriteAllText(existingPath, sentinel);

                Throws<InvalidDataException>(
                    () => export(existingPath),
                    label + " must reject semantic identity before replacing an existing destination.");
                Equal(sentinel, File.ReadAllText(existingPath), label + " changed an existing destination on rejected identity input.");
                Equal(
                    1,
                    Directory.GetFiles(root, "*", SearchOption.AllDirectories).Length,
                    label + " left temporary filesystem residue on rejected identity input.");
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        private static RebarScheduleRow RebarRow(string elementId)
        {
            return new RebarScheduleRow
            {
                ElementId = elementId,
                BarMark = "B1",
                ShapeCode = "00",
                Notation = "1T16",
                DiameterMm = 16d,
                Quantity = 1,
                CuttingLengthM = 2d,
                TotalLengthM = 2d,
                UnitWeightKgM = 1d,
                NetWeightKg = 2d,
                WastePercent = 0d,
                TotalWeightKg = 2d,
                FabricationStatus = "Ready",
                FabricationStandardCode = "STD",
                FabricationDetailingRevision = "R1"
            };
        }

        private static IReadOnlyList<RebarProcurementSummary> ProcurementRows(string groupId)
        {
            var demand = new RebarStockDemand(
                groupId,
                "CB400",
                16d,
                12d,
                new[] { new RebarCutRequirement("CUT-1", 2d, 1) },
                new RebarCutAllowancePolicy());
            return RebarProcurementReportBuilder.Build(new[] { RebarCuttingOptimizer.Plan(demand) });
        }

        private static char[] FormulaPrefixes() => new[] { '=', '+', '-', '@' };

        private static void Contains(string value, string expectedFragment, string message)
        {
            if (value == null || value.IndexOf(expectedFragment, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message + " Missing fragment: " + expectedFragment + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected: " + expected + "; actual: " + actual + ".");
        }

        private static void Throws<TException>(Action action, string message) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(message + " Unexpected exception: " + ex.GetType().FullName + ".", ex);
            }
            throw new InvalidOperationException(message + " Expected exception: " + typeof(TException).FullName + ".");
        }
    }
}
