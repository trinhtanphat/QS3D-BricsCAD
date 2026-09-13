using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class TakeoffPackageDuplicateBimEvidenceSmoke
    {
        internal static void Run()
        {
            BlocksDuplicateBimQuantityIdentity();
            AllowsDistinctQuantitiesOnSameIfcElement();
        }

        private static void BlocksDuplicateBimQuantityIdentity()
        {
            var quantity = Bim("GUID-1", "IfcWall", "L01", "03-CONCRETE", "NetVolume", 5d, "m3");
            var result = Build(new[] { quantity, quantity });

            Equal(TakeoffPackageReadiness.Blocked, result.Readiness, "Duplicate BIM quantity evidence must block package admission.");
            True(result.Issues.Any(x => x.Code == "PKG.DUPLICATE_BIM_QUANTITY"), "Duplicate BIM quantity identity must produce PKG.DUPLICATE_BIM_QUANTITY.");
            Equal(0, result.Inventory.Count, "Blocked duplicate BIM evidence must not reach inventory or estimate.");
            Equal(1, result.Sources.Count(x => x.Kind == TakeoffPackageSourceKind.Bim3D && x.Id == "GUID-1"), "One IFC element should still render as one package source card.");
        }

        private static void AllowsDistinctQuantitiesOnSameIfcElement()
        {
            var result = Build(new[]
            {
                Bim("GUID-1", "IfcWall", "L01", "03-CONCRETE", "NetVolume", 5d, "m3"),
                Bim("GUID-1", "IfcWall", "L01", "03-FORMWORK", "SideArea", 12d, "m2"),
                Bim("GUID-1", "IfcWall", "L01", "03-CONCRETE", "NetVolume", 5000d, "L")
            });

            True(!result.Issues.Any(x => x.Code == "PKG.DUPLICATE_BIM_QUANTITY"), "Distinct quantity-name or unit identities on one IFC element must remain valid.");
            Equal(TakeoffPackageReadiness.Ready, result.Readiness, "Distinct IFC quantities must remain publishable.");
            Equal(3, result.Inventory.Count, "Distinct classification/unit rows must reach inventory exactly once each.");
            Equal(1, result.Sources.Count(x => x.Kind == TakeoffPackageSourceKind.Bim3D && x.Id == "GUID-1"), "Multiple quantities from one IFC element must preserve a single source card.");
        }

        private static TakeoffPackageBuildResult Build(IEnumerable<IfcQtoItem> quantities)
        {
            return new AutodeskTakeoffPackageCoordinator().Build(
                new TakeoffPackageDefinition("PKG-1", "Mixed Takeoff", "R1", "Uniclass", "Default"),
                Enumerable.Empty<DrawingSheet2D>(),
                Enumerable.Empty<TakeoffQuantityEvidence2D>(),
                quantities,
                (classification, quantity) => quantity,
                (classification, unit) => 10d);
        }

        private static IfcQtoItem Bim(string guid, string entity, string storey, string classification, string quantityName, double quantity, string unit)
        {
            return new IfcQtoItem(guid, entity, storey, classification, quantityName, quantity, unit);
        }

        private static void True(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }
    }

    internal static class TakeoffPackageDuplicateBimEvidenceRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            TakeoffPackageDuplicateBimEvidenceSmoke.Run();
        }
    }
}
