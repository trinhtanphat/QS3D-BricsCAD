using System;
using System.Collections.Generic;
using QS3D.Core.Cost;
using QS3D.Core.Mep;

namespace QS3D.Core.SmokeTests
{
    internal static class MepTbqProjectionSmoke
    {
        internal static void Run()
        {
            ProjectsCanonicalMetricsAndPreservesWorkspace();
            StableProjectionAndCsv();
            EmptyMetricsDoNotCreateRows();
            UnrepresentableMetricFailsClosed();
        }

        private static void ProjectsCanonicalMetricsAndPreservesWorkspace()
        {
            var service = new MepTbqProjectionService();
            var current = CreateState();
            var groups = new MepQuantityService().Aggregate(new[]
            {
                new MepElement("D-1", MepElementKind.Duct, "Supply", "500x300", "L01", 2, 2.5d, 4d, 1d),
                new MepElement("D-2", MepElementKind.Duct, "Supply", "500x300", "L01", 3, 1.5d, 1d, 0.5d),
                new MepElement("E-1", MepElementKind.Equipment, "HVAC", "AHU", "L02", 1),
                new MepElement("F-0", MepElementKind.Fixture, "Drainage", "FloorDrain", "L02", 0)
            });

            var result = service.Project(current, groups);
            Equal(5, result.ProjectedBillItemCount, "projected MEP bill row count");
            Equal(3, result.ReportRows.Count, "MEP report group count");
            Equal(6, result.State.BillItems.Count, "preserved plus projected bill item count");

            Equal(true, ContainsCode(result.State.BillItems, "KEEP"), "non-MEP bill item preservation");
            Equal(false, ContainsCode(result.State.BillItems, "QS3D.MEP.OLD.COUNT"), "stale MEP-owned bill row replacement");

            var ductCount = FindItem(result.State.BillItems, "Duct", "ea");
            var ductLength = FindItem(result.State.BillItems, "Duct", "m");
            var ductArea = FindItem(result.State.BillItems, "Duct", "m2");
            var ductVolume = FindItem(result.State.BillItems, "Duct", "m3");
            Equal(5m, ductCount.Quantity, "COUNT uses QuantityCount, not ElementCount");
            Equal(4m, ductLength.Quantity, "duct length projection");
            Equal(5m, ductArea.Quantity, "duct area projection");
            Equal(1.5m, ductVolume.Quantity, "duct volume projection");

            var equipmentCount = FindItem(result.State.BillItems, "Equipment", "ea");
            Equal(1m, equipmentCount.Quantity, "equipment count projection");
            Equal(false, ContainsDescription(result.State.BillItems, "Fixture"), "zero-metric group must not create bill rows");

            for (var i = 0; i < result.State.BillItems.Count; i++)
            {
                var item = result.State.BillItems[i];
                if (!MepTbqProjectionService.IsOwnedItem(item.ItemCode)) continue;
                Equal(0m, item.UnitRate, "projected MEP rate remains zero");
                Equal("MEP", item.TradeCode, "projected MEP trade code");
            }

            Equal(current.Currency, result.State.Currency, "currency preservation");
            Equal(current.CfaM2, result.State.CfaM2, "CFA preservation");
            Equal(current.AdjustmentRatioPercent, result.State.AdjustmentRatioPercent, "adjustment ratio preservation");
            Equal(current.MarkupRatioPercent, result.State.MarkupRatioPercent, "markup ratio preservation");
            Equal(current.BuildUpRates.Count, result.State.BuildUpRates.Count, "build-up count preservation");
            Equal(current.BuildUpRates[0].RateCode, result.State.BuildUpRates[0].RateCode, "build-up identity preservation");
            Equal(current.RateReferences.Edges.Count, result.State.RateReferences.Edges.Count, "rate reference preservation");
            Equal(current.Library.LibraryId, result.State.Library.LibraryId, "BQ library id preservation");
            Equal(current.Library.Entries.Count, result.State.Library.Entries.Count, "BQ library entry preservation");
            Equal(current.Library.Entries[0].ItemCode, result.State.Library.Entries[0].ItemCode, "BQ library item preservation");
        }

        private static void StableProjectionAndCsv()
        {
            var service = new MepTbqProjectionService();
            var current = CreateState();
            var forward = new[]
            {
                new MepElement("P-1", MepElementKind.Pipe, "CHW", "DN50", "L02", 2, 3.25d),
                new MepElement("C-1", MepElementKind.CableTray, "Power", "300W", "L01", 1, 5d, 1.25d)
            };
            var reverse = new[] { forward[1], forward[0] };
            var groupsA = new MepQuantityService().Aggregate(forward);
            var groupsB = new MepQuantityService().Aggregate(reverse);

            var first = service.Project(current, groupsA);
            var second = service.Project(current, groupsB);
            Equal(service.SerializeCsv(groupsA), service.SerializeCsv(groupsB), "deterministic MEP CSV serialization");

            var firstOwned = OwnedItems(first.State.BillItems);
            var secondOwned = OwnedItems(second.State.BillItems);
            Equal(firstOwned.Count, secondOwned.Count, "stable projected row count");
            for (var i = 0; i < firstOwned.Count; i++)
            {
                Equal(firstOwned[i].ItemCode, secondOwned[i].ItemCode, "stable projected row identity " + i);
                Equal(firstOwned[i].Quantity, secondOwned[i].Quantity, "stable projected row quantity " + i);
                Equal(firstOwned[i].Unit, secondOwned[i].Unit, "stable projected row unit " + i);
            }
        }

        private static void EmptyMetricsDoNotCreateRows()
        {
            var service = new MepTbqProjectionService();
            var current = CreateState();
            var groups = new MepQuantityService().Aggregate(new[]
            {
                new MepElement("ZERO", MepElementKind.Accessory, "Fire", "Valve", "L03", 0)
            });
            var result = service.Project(current, groups);
            Equal(0, result.ProjectedBillItemCount, "zero-metric projected bill row count");
            Equal(1, result.State.BillItems.Count, "only non-MEP bill item remains after zero projection");
            Equal("KEEP", result.State.BillItems[0].ItemCode, "non-MEP row remains after zero projection");
        }

        private static void UnrepresentableMetricFailsClosed()
        {
            var service = new MepTbqProjectionService();
            var current = CreateState();
            var groups = new MepQuantityService().Aggregate(new[]
            {
                new MepElement("HUGE", MepElementKind.Pipe, "CHW", "DN100", "L99", 1, double.MaxValue)
            });
            Throws<OverflowException>(() => service.Project(current, groups), "MEP metric outside decimal range");
        }

        private static TbqProjectWorkspaceState CreateState()
        {
            return new TbqProjectWorkspaceState(
                "VND",
                1000m,
                new[]
                {
                    new TbqBillItem("KEEP", "Existing concrete", "m3", "Structure", 2m, 100m, "R-CONC"),
                    new TbqBillItem("QS3D.MEP.OLD.COUNT", "Stale MEP row", "ea", "MEP", 99m, 99m)
                },
                new[] { new BuildUpRateSnapshot("R-CONC", 100m) },
                new[] { new RateReferenceEdge("R-CONC", RateReferenceTargetKind.BillItem, "KEEP") },
                "PROJECT",
                new[] { new BqLibraryEntry("KEEP", "Existing concrete", "m3", "Structure/Concrete", 100m) },
                10m,
                5m);
        }

        private static TbqBillItem FindItem(IReadOnlyList<TbqBillItem> items, string kind, string unit)
        {
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (MepTbqProjectionService.IsOwnedItem(item.ItemCode) &&
                    item.Description.IndexOf(kind, StringComparison.Ordinal) >= 0 &&
                    string.Equals(item.Unit, unit, StringComparison.Ordinal))
                    return item;
            }
            throw new InvalidOperationException("Missing projected MEP " + kind + " row with unit " + unit + ".");
        }

        private static bool ContainsCode(IReadOnlyList<TbqBillItem> items, string itemCode)
        {
            for (var i = 0; i < items.Count; i++)
                if (string.Equals(items[i].ItemCode, itemCode, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static bool ContainsDescription(IReadOnlyList<TbqBillItem> items, string token)
        {
            for (var i = 0; i < items.Count; i++)
                if (items[i].Description.IndexOf(token, StringComparison.Ordinal) >= 0) return true;
            return false;
        }

        private static List<TbqBillItem> OwnedItems(IReadOnlyList<TbqBillItem> items)
        {
            var result = new List<TbqBillItem>();
            for (var i = 0; i < items.Count; i++)
                if (MepTbqProjectionService.IsOwnedItem(items[i].ItemCode)) result.Add(items[i]);
            return result;
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void Throws<T>(Action action, string label) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException(label + ": expected " + typeof(T).Name + ".");
        }
    }
}
