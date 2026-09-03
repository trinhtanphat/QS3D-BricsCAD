using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.Reporting
{
    public static class ProjectQuantityReportBuilder
    {
        internal const int MaxSelectionElementIds = 10000;

        public static IReadOnlyList<QuantityReportRow> Group(ProjectState project) => Build(project, null, false);

        public static IReadOnlyList<QuantityReportRow> Group(ProjectState project, IEnumerable<string> elementIds)
        {
            if (elementIds == null) throw new ArgumentNullException(nameof(elementIds));
            return Build(project, elementIds, false);
        }

        public static IReadOnlyList<QuantityReportRow> Detail(ProjectState project) => Build(project, null, true);

        public static IReadOnlyList<QuantityReportRow> Detail(ProjectState project, IEnumerable<string> elementIds)
        {
            if (elementIds == null) throw new ArgumentNullException(nameof(elementIds));
            return Build(project, elementIds, true);
        }

        private static IReadOnlyList<QuantityReportRow> Build(ProjectState project, IEnumerable<string>? elementIds, bool detail)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            ReportingProjectIdentityGuard.RequireUniqueElementIds(project, detail ? "Quantity detail report" : "Quantity report");
            RoomFinishIdentityService.ValidateProject(project);
            var selectedIds = ResolveSelection(project, elementIds);

            var reportVersion = project.ChangeVersion;
            var elements = project.Elements.ToList();
            var floorInstances = project.Floors.ToList();
            var zoneInstances = project.Zones.ToList();
            var familyInstances = project.Families.ToList();
            var drawingFingerprint = project.DrawingFingerprint;
            EnsureProjectRevision(project, reportVersion, elements, floorInstances, zoneInstances, familyInstances, drawingFingerprint);

            var floors = floorInstances.ToDictionary(x => x.Id, x => x.Name, StringComparer.OrdinalIgnoreCase);
            var zones = zoneInstances.ToDictionary(x => x.Id, x => x.Name, StringComparer.OrdinalIgnoreCase);
            var families = familyInstances.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
            var rows = new Dictionary<string, QuantityReportRow>(StringComparer.OrdinalIgnoreCase);
            var accumulators = new Dictionary<string, QuantityReportAggregateState>(StringComparer.OrdinalIgnoreCase);
            var noteValues = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            var order = new List<string>();

            foreach (var element in elements)
            {
                EnsureProjectRevision(project, reportVersion, elements, floorInstances, zoneInstances, familyInstances, drawingFingerprint);
                var elementId = element.Id.Trim();
                if (selectedIds != null && !selectedIds.Contains(elementId)) continue;
                if (AutoRoomLifecycle.IsExcludedFromQuantity(project, element)) continue;
                var floorId = ReportingProjectIdentityGuard.NormalizeReferenceId(element.FloorId);
                var zoneId = ReportingProjectIdentityGuard.NormalizeReferenceId(element.ZoneId);
                var familyId = ReportingProjectIdentityGuard.NormalizeReferenceId(element.FamilyId);
                var floor = floors.TryGetValue(floorId, out var floorName) ? floorName : floorId;
                var zone = zones.TryGetValue(zoneId, out var zoneName) ? zoneName : zoneId;
                families.TryGetValue(familyId, out var family);
                if (family != null && family.Category != element.Category)
                    throw new InvalidOperationException("Quantity report element " + element.Id + " category " + element.Category + " does not match Family " + family.Id + " category " + family.Category + ". Repair the Family relation before reporting.");
                if (family != null) familyId = family.Id;
                var familyName = family != null ? family.Name : familyId;
                var elementName = FirstInstanceProperty(element, "Name", "TenCauKien");
                if (elementName.Length == 0) elementName = familyName;
                var material = Effective(element, family, "Material");
                var note = Effective(element, family, "Note");
                if (note.Length == 0) note = Effective(element, family, "GhiChu");
                var densityKgM3 = EffectiveDensity(element, family);
                var massKg = EffectiveMass(element, densityKgM3);
                var category = element.Category.ToString();
                var key = detail
                    ? "ELEMENT\u001f" + elementId
                    : CanonicalGroupKey(floorId, zoneId, category, familyId, material, DensityKey(densityKgM3));
                var created = false;
                QuantityReportAggregateState aggregate;
                if (!rows.TryGetValue(key, out var row))
                {
                    row = new QuantityReportRow
                    {
                        Floor = floor,
                        Zone = zone,
                        Category = category,
                        FamilyId = familyId,
                        FamilyName = familyName,
                        ElementName = detail ? elementName : familyName,
                        Material = material,
                        Note = note,
                        DensityKgM3 = densityKgM3,
                        DrawingFingerprint = drawingFingerprint
                    };
                    rows[key] = row;
                    aggregate = new QuantityReportAggregateState();
                    accumulators.Add(key, aggregate);
                    var distinctNotes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (note.Length != 0) distinctNotes.Add(note);
                    noteValues.Add(key, distinctNotes);
                    order.Add(key);
                    created = true;
                }
                else
                {
                    aggregate = accumulators[key];
                    if (note.Length != 0 && noteValues[key].Add(note))
                        row.Note = AppendText(row.Note, note);
                }

                aggregate.MassKg.Add(massKg, element.Id + "/MassKg");
                row.Count = QuantityReportMath.AddCount(row.Count, 1);
                row.ElementIds.Add(elementId);
                AddHandles(row.SourceHandles, SourceHandleResolver.Resolve(project, new[] { elementId }));

                var hasGrossEvidence = HasAnyQuantity(element, "GrossConcreteM3", "GrossVolumeM3");
                var hasNetEvidence = HasAnyQuantity(element, "NetConcreteM3", "NetVolumeM3");
                var hasDeductionEvidence = element.Quantities.ContainsKey("DeductionM3") || (hasGrossEvidence && hasNetEvidence);
                var hasGrossFormworkEvidence = element.Quantities.ContainsKey("GrossFormworkM2");
                var hasFormworkDeductionEvidence = HasAnyQuantity(element, "ConcreteContactDeductionM2", "FormworkDeductionM2");
                var hasNetFormworkEvidence = HasAnyQuantity(element, "NetFormworkM2", "FormworkM2");
                var hasFormworkEvidence = hasNetFormworkEvidence;
                var hasLengthEvidence = element.Quantities.ContainsKey("LengthM");
                var hasWidthEvidence = element.Quantities.ContainsKey("WidthM");
                var hasHeightEvidence = element.Quantities.ContainsKey("HeightM");
                var hasOuterPerimeterEvidence = element.Category == ElementCategory.Room
                    ? HasAnyQuantity(element, "OuterPerimeterM", "PerimeterM")
                    : element.Quantities.ContainsKey("OuterPerimeterM");
                var hasInnerPerimeterEvidence = element.Category == ElementCategory.Skirting
                    ? HasAnyQuantity(element, "InnerPerimeterM", "PerimeterM")
                    : element.Quantities.ContainsKey("InnerPerimeterM");
                var hasDoorAreaEvidence = element.Category == ElementCategory.Door || element.Category == ElementCategory.WallOpening
                    ? element.Quantities.ContainsKey("OpeningAreaM2")
                    : element.Quantities.ContainsKey("DoorAreaM2");
                var hasSideAreaEvidence = element.Category == ElementCategory.WallFinish
                    ? HasAnyQuantity(element, "NetFinishAreaM2", "SideAreaM2")
                    : element.Quantities.ContainsKey("SideAreaM2");
                var hasBottomAreaEvidence = element.Category == ElementCategory.FloorFinish || element.Category == ElementCategory.Waterproofing
                    ? HasAnyQuantity(element, "BottomAreaM2", "AreaM2")
                    : element.Quantities.ContainsKey("BottomAreaM2");
                var hasTopAreaEvidence = element.Category == ElementCategory.CeilingFinish
                    ? HasAnyQuantity(element, "TopAreaM2", "AreaM2")
                    : element.Quantities.ContainsKey("TopAreaM2");
                var hasOtherAreaEvidence = HasAnyQuantity(element, "OtherAreaM2", "MeasuredSurfaceAreaM2");

                row.HasGrossConcreteM3Evidence = AggregateEvidence(row.HasGrossConcreteM3Evidence, hasGrossEvidence, created);
                row.HasDeductionM3Evidence = AggregateEvidence(row.HasDeductionM3Evidence, hasDeductionEvidence, created);
                row.HasNetConcreteM3Evidence = AggregateEvidence(row.HasNetConcreteM3Evidence, hasNetEvidence, created);
                row.HasGrossFormworkM2Evidence = AggregateEvidence(row.HasGrossFormworkM2Evidence, hasGrossFormworkEvidence, created);
                row.HasConcreteContactDeductionM2Evidence = AggregateEvidence(row.HasConcreteContactDeductionM2Evidence, hasFormworkDeductionEvidence, created);
                row.HasNetFormworkM2Evidence = AggregateEvidence(row.HasNetFormworkM2Evidence, hasNetFormworkEvidence, created);
                row.HasFormworkM2Evidence = AggregateEvidence(row.HasFormworkM2Evidence, hasFormworkEvidence, created);
                row.HasLengthMEvidence = AggregateEvidence(row.HasLengthMEvidence, hasLengthEvidence, created);
                row.HasWidthMEvidence = AggregateEvidence(row.HasWidthMEvidence, hasWidthEvidence, created);
                row.HasHeightMEvidence = AggregateEvidence(row.HasHeightMEvidence, hasHeightEvidence, created);
                row.HasOuterPerimeterMEvidence = AggregateEvidence(row.HasOuterPerimeterMEvidence, hasOuterPerimeterEvidence, created);
                row.HasInnerPerimeterMEvidence = AggregateEvidence(row.HasInnerPerimeterMEvidence, hasInnerPerimeterEvidence, created);
                row.HasDoorAreaM2Evidence = AggregateEvidence(row.HasDoorAreaM2Evidence, hasDoorAreaEvidence, created);
                row.HasSideAreaM2Evidence = AggregateEvidence(row.HasSideAreaM2Evidence, hasSideAreaEvidence, created);
                row.HasBottomAreaM2Evidence = AggregateEvidence(row.HasBottomAreaM2Evidence, hasBottomAreaEvidence, created);
                row.HasTopAreaM2Evidence = AggregateEvidence(row.HasTopAreaM2Evidence, hasTopAreaEvidence, created);
                row.HasOtherAreaM2Evidence = AggregateEvidence(row.HasOtherAreaM2Evidence, hasOtherAreaEvidence, created);

                var gross = QFirst(element, "GrossConcreteM3", "GrossVolumeM3");
                var net = QFirstOrFallback(element, gross, "NetConcreteM3", "NetVolumeM3");
                var grossFormwork = Q(element, "GrossFormworkM2");
                var formworkDeduction = QFirst(element, "ConcreteContactDeductionM2", "FormworkDeductionM2");
                var netFormwork = QFirst(element, "NetFormworkM2", "FormworkM2");
                aggregate.GrossConcreteM3.Add(gross, element.Id + "/GrossConcreteM3");
                aggregate.NetConcreteM3.Add(net, element.Id + "/NetConcreteM3");
                aggregate.DeductionM3.Add(Q(element, "DeductionM3", Math.Max(0d, gross - net)), element.Id + "/DeductionM3");
                aggregate.GrossFormworkM2.Add(grossFormwork, element.Id + "/GrossFormworkM2");
                aggregate.ConcreteContactDeductionM2.Add(formworkDeduction, element.Id + "/ConcreteContactDeductionM2");
                aggregate.NetFormworkM2.Add(netFormwork, element.Id + "/NetFormworkM2");
                aggregate.FormworkM2.Add(netFormwork, element.Id + "/FormworkM2");
                aggregate.LengthM.Add(Q(element, "LengthM"), element.Id + "/LengthM");
                aggregate.WidthM.Add(Q(element, "WidthM"), element.Id + "/WidthM");
                aggregate.HeightM.Add(Q(element, "HeightM"), element.Id + "/HeightM");
                aggregate.OuterPerimeterM.Add(
                    element.Category == ElementCategory.Room ? QFirst(element, "OuterPerimeterM", "PerimeterM") : Q(element, "OuterPerimeterM"),
                    element.Id + "/OuterPerimeterM");
                aggregate.InnerPerimeterM.Add(
                    element.Category == ElementCategory.Skirting ? QFirst(element, "InnerPerimeterM", "PerimeterM") : Q(element, "InnerPerimeterM"),
                    element.Id + "/InnerPerimeterM");
                aggregate.DoorAreaM2.Add(
                    element.Category == ElementCategory.Door || element.Category == ElementCategory.WallOpening ? Q(element, "OpeningAreaM2") : Q(element, "DoorAreaM2"),
                    element.Id + "/DoorAreaM2");
                aggregate.SideAreaM2.Add(
                    element.Category == ElementCategory.WallFinish ? QFirst(element, "NetFinishAreaM2", "SideAreaM2") : Q(element, "SideAreaM2"),
                    element.Id + "/SideAreaM2");
                aggregate.BottomAreaM2.Add(
                    element.Category == ElementCategory.FloorFinish || element.Category == ElementCategory.Waterproofing ? QFirst(element, "BottomAreaM2", "AreaM2") : Q(element, "BottomAreaM2"),
                    element.Id + "/BottomAreaM2");
                aggregate.TopAreaM2.Add(
                    element.Category == ElementCategory.CeilingFinish ? QFirst(element, "TopAreaM2", "AreaM2") : Q(element, "TopAreaM2"),
                    element.Id + "/TopAreaM2");
                aggregate.OtherAreaM2.Add(QFirst(element, "OtherAreaM2", "MeasuredSurfaceAreaM2"), element.Id + "/OtherAreaM2");
                EnsureProjectRevision(project, reportVersion, elements, floorInstances, zoneInstances, familyInstances, drawingFingerprint);
            }

            EnsureProjectRevision(project, reportVersion, elements, floorInstances, zoneInstances, familyInstances, drawingFingerprint);
            foreach (var key in order)
            {
                var row = rows[key];
                var aggregate = accumulators[key];
                row.GrossConcreteM3 = aggregate.GrossConcreteM3.Value("GrossConcreteM3");
                row.DeductionM3 = aggregate.DeductionM3.Value("DeductionM3");
                row.NetConcreteM3 = aggregate.NetConcreteM3.Value("NetConcreteM3");
                row.GrossFormworkM2 = aggregate.GrossFormworkM2.Value("GrossFormworkM2");
                row.ConcreteContactDeductionM2 = aggregate.ConcreteContactDeductionM2.Value("ConcreteContactDeductionM2");
                row.NetFormworkM2 = aggregate.NetFormworkM2.Value("NetFormworkM2");
                row.FormworkM2 = aggregate.FormworkM2.Value("FormworkM2");
                row.LengthM = aggregate.LengthM.Value("LengthM");
                row.WidthM = aggregate.WidthM.Value("WidthM");
                row.HeightM = aggregate.HeightM.Value("HeightM");
                row.OuterPerimeterM = aggregate.OuterPerimeterM.Value("OuterPerimeterM");
                row.InnerPerimeterM = aggregate.InnerPerimeterM.Value("InnerPerimeterM");
                row.DoorAreaM2 = aggregate.DoorAreaM2.Value("DoorAreaM2");
                row.SideAreaM2 = aggregate.SideAreaM2.Value("SideAreaM2");
                row.BottomAreaM2 = aggregate.BottomAreaM2.Value("BottomAreaM2");
                row.TopAreaM2 = aggregate.TopAreaM2.Value("TopAreaM2");
                row.OtherAreaM2 = aggregate.OtherAreaM2.Value("OtherAreaM2");
                row.MassKg = aggregate.MassKg.Value("MassKg");
            }
            return order.Select(x => rows[x]).ToList().AsReadOnly();
        }

        private sealed class QuantityReportAggregateState
        {
            internal CompensatedValue GrossConcreteM3 { get; } = new CompensatedValue();
            internal CompensatedValue DeductionM3 { get; } = new CompensatedValue();
            internal CompensatedValue NetConcreteM3 { get; } = new CompensatedValue();
            internal CompensatedValue GrossFormworkM2 { get; } = new CompensatedValue();
            internal CompensatedValue ConcreteContactDeductionM2 { get; } = new CompensatedValue();
            internal CompensatedValue NetFormworkM2 { get; } = new CompensatedValue();
            internal CompensatedValue FormworkM2 { get; } = new CompensatedValue();
            internal CompensatedValue LengthM { get; } = new CompensatedValue();
            internal CompensatedValue WidthM { get; } = new CompensatedValue();
            internal CompensatedValue HeightM { get; } = new CompensatedValue();
            internal CompensatedValue OuterPerimeterM { get; } = new CompensatedValue();
            internal CompensatedValue InnerPerimeterM { get; } = new CompensatedValue();
            internal CompensatedValue DoorAreaM2 { get; } = new CompensatedValue();
            internal CompensatedValue SideAreaM2 { get; } = new CompensatedValue();
            internal CompensatedValue BottomAreaM2 { get; } = new CompensatedValue();
            internal CompensatedValue TopAreaM2 { get; } = new CompensatedValue();
            internal CompensatedValue OtherAreaM2 { get; } = new CompensatedValue();
            internal NullableCompensatedValue MassKg { get; } = new NullableCompensatedValue();
        }

        private sealed class NullableCompensatedValue
        {
            private readonly CompensatedValue _value = new CompensatedValue();
            private bool _observed;
            private bool _allPresent = true;

            internal void Add(double? value, string label)
            {
                _observed = true;
                if (!value.HasValue)
                {
                    _allPresent = false;
                    return;
                }
                if (_allPresent) _value.Add(value.Value, label);
            }

            internal double? Value(string quantity)
            {
                return _observed && _allPresent ? _value.Value(quantity) : (double?)null;
            }
        }

        private sealed class CompensatedValue
        {
            private double _sum;
            private double _compensation;

            internal void Add(double value, string label)
            {
                QuantityReportMath.Finite(_sum, label);
                QuantityReportMath.Finite(_compensation, label + "/compensation");
                var incoming = QuantityReportMath.NonNegative(value, label);
                var result = _sum + incoming;
                if (double.IsNaN(result) || double.IsInfinity(result))
                    throw new OverflowException("Quantity report aggregate overflow: " + label);

                var correction = Math.Abs(_sum) >= Math.Abs(incoming)
                    ? (_sum - result) + incoming
                    : (incoming - result) + _sum;
                var nextCompensation = _compensation + correction;
                if (double.IsNaN(nextCompensation) || double.IsInfinity(nextCompensation))
                    throw new OverflowException("Quantity report aggregate compensation overflow: " + label);
                _sum = result == 0d ? 0d : result;
                _compensation = nextCompensation == 0d ? 0d : nextCompensation;
            }

            internal double Value(string quantity)
            {
                QuantityReportMath.Finite(_sum, "aggregate/" + quantity);
                QuantityReportMath.Finite(_compensation, "aggregate/" + quantity + "/compensation");
                var result = _sum + _compensation;
                if (double.IsNaN(result) || double.IsInfinity(result))
                    throw new OverflowException("Quantity report aggregate overflow: " + quantity);
                if (_compensation != 0d && result == _sum && !IsStrictlyBelowHalfUlp(_sum, _compensation))
                    throw new OverflowException("Quantity report aggregate lost a non-zero compensation at floating-point precision: " + quantity);
                if (_sum != 0d && result == _compensation)
                    throw new OverflowException("Quantity report aggregate lost a non-zero accumulated value at floating-point precision: " + quantity);
                return result == 0d ? 0d : result;
            }

            private static bool IsStrictlyBelowHalfUlp(double current, double compensation)
            {
                if (current <= 0d || compensation == 0d) return false;
                var currentBits = BitConverter.DoubleToInt64Bits(current);
                var adjacentBits = compensation > 0d ? currentBits + 1L : currentBits - 1L;
                var adjacent = BitConverter.Int64BitsToDouble(adjacentBits);
                var spacing = Math.Abs(adjacent - current);
                return Math.Abs(compensation) < spacing / 2d;
            }
        }

        private static void EnsureProjectRevision(
            ProjectState project,
            long expectedVersion,
            IReadOnlyList<ProjectElement> elements,
            IReadOnlyList<FloorDefinition> floors,
            IReadOnlyList<ZoneDefinition> zones,
            IReadOnlyList<ProjectFamily> families,
            string drawingFingerprint)
        {
            if (project.ChangeVersion != expectedVersion ||
                !string.Equals(project.DrawingFingerprint, drawingFingerprint, StringComparison.Ordinal) ||
                !SameInstances(project.Elements, elements) ||
                !SameInstances(project.Floors, floors) ||
                !SameInstances(project.Zones, zones) ||
                !SameInstances(project.Families, families))
                throw new InvalidOperationException(
                    "Project changed while the quantity report was being built; recompute the report against the current project state.");
        }

        private static bool SameInstances<T>(IList<T> current, IReadOnlyList<T> snapshot) where T : class
        {
            if (current.Count != snapshot.Count) return false;
            for (var index = 0; index < current.Count; index++)
                if (!ReferenceEquals(current[index], snapshot[index])) return false;
            return true;
        }

        private static HashSet<string>? ResolveSelection(ProjectState project, IEnumerable<string>? elementIds)
        {
            if (elementIds == null) return null;

            var selectionVersion = project.ChangeVersion;
            var knownCount = SnapshotKnownSelectionCount(elementIds);
            if (project.ChangeVersion != selectionVersion)
                throw new InvalidOperationException("Project changed while quantity report element-id Count contracts were being inspected; recompute the selection against the current project state.");

            var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var selectedInstances = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            var observedCount = 0;
            using var enumerator = elementIds.GetEnumerator();
            while (true)
            {
                if (knownCount.HasValue)
                    RequireStableKnownSelectionCount(elementIds, knownCount.Value);

                var moved = enumerator.MoveNext();

                if (knownCount.HasValue)
                    RequireStableKnownSelectionCount(elementIds, knownCount.Value);
                if (!moved) break;

                if (knownCount.HasValue && observedCount >= knownCount.Value)
                    throw SelectionCountMismatch(knownCount.Value, observedCount + 1);
                if (observedCount >= MaxSelectionElementIds)
                    throw SelectionTooLarge();

                var raw = enumerator.Current;

                if (knownCount.HasValue)
                    RequireStableKnownSelectionCount(elementIds, knownCount.Value);

                observedCount++;
                if (string.IsNullOrWhiteSpace(raw))
                    throw new ArgumentException("Quantity report element ids must not be blank.", nameof(elementIds));
                if (!string.Equals(raw, raw.Trim(), StringComparison.Ordinal))
                    throw new ArgumentException("Quantity report element ids must be canonical and must not contain surrounding whitespace. Non-canonical id: " + raw + ".", nameof(elementIds));
                var id = raw;
                if (!selected.Add(id))
                    throw new ArgumentException("Quantity report element ids must be unique. Duplicate id: " + id + ".", nameof(elementIds));
                var element = project.FindElement(id) ?? throw new KeyNotFoundException("Unknown quantity report element: " + id);
                selectedInstances.Add(id, element);
            }

            if (project.ChangeVersion != selectionVersion)
                throw new InvalidOperationException("Project changed while quantity report element ids were being enumerated; recompute the selection against the current project state.");
            if (knownCount.HasValue)
            {
                RequireStableKnownSelectionCount(elementIds, knownCount.Value);
                if (observedCount != knownCount.Value)
                    throw SelectionCountMismatch(knownCount.Value, observedCount);
            }

            ReportingProjectIdentityGuard.RequireUniqueElementIds(project, "Quantity report selection");
            foreach (var selectedInstance in selectedInstances)
            {
                var current = project.FindElement(selectedInstance.Key);
                if (current == null || !ReferenceEquals(current, selectedInstance.Value))
                    throw new InvalidOperationException("Quantity report selection became stale while element ids were being enumerated; recompute the selection against the current project state.");
            }
            return selected;
        }

        private static int? SnapshotKnownSelectionCount(IEnumerable<string> elementIds)
        {
            int? knownCount = null;
            if (elementIds is ICollection<string> genericCollection)
                ObserveKnownSelectionCount(genericCollection.Count, ref knownCount);
            if (elementIds is IReadOnlyCollection<string> readOnlyCollection)
                ObserveKnownSelectionCount(readOnlyCollection.Count, ref knownCount);
            if (elementIds is ICollection nonGenericCollection)
                ObserveKnownSelectionCount(nonGenericCollection.Count, ref knownCount);
            return knownCount;
        }

        private static void RequireStableKnownSelectionCount(IEnumerable<string> elementIds, int expectedCount)
        {
            var currentCount = SnapshotKnownSelectionCount(elementIds);
            if (!currentCount.HasValue || currentCount.Value != expectedCount)
                throw new InvalidOperationException(
                    "Quantity report selection input changed during enumeration; Count changed from " + expectedCount +
                    " to " + (currentCount.HasValue ? currentCount.Value.ToString(CultureInfo.InvariantCulture) : "<unavailable>") + ".");
        }

        private static void ObserveKnownSelectionCount(int count, ref int? knownCount)
        {
            if (count < 0)
                throw new InvalidOperationException("Quantity report selection input reported a negative known count.");
            if (count > MaxSelectionElementIds)
                throw SelectionTooLarge();
            if (knownCount.HasValue && knownCount.Value != count)
                throw new InvalidOperationException(
                    "Quantity report selection input exposes conflicting known counts: " + knownCount.Value + " and " + count + ".");
            knownCount = count;
        }

        private static InvalidOperationException SelectionTooLarge()
        {
            return new InvalidOperationException(
                "Quantity report selection input exceeds the supported bound of " + MaxSelectionElementIds + ".");
        }

        private static InvalidOperationException SelectionCountMismatch(int reportedCount, int observedCount)
        {
            return new InvalidOperationException(
                "Quantity report selection input changed during enumeration; Count reported " + reportedCount +
                " items but enumeration produced " + observedCount + ".");
        }

        private static void AddHandles(IList<string> destination, IEnumerable<string> source)
        {
            foreach (var handle in source.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()))
                if (!destination.Contains(handle, StringComparer.OrdinalIgnoreCase)) destination.Add(handle);
        }

        private static string FirstInstanceProperty(ProjectElement element, params string[] keys)
        {
            foreach (var key in keys)
                if (element.Properties.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            return string.Empty;
        }

        private static string Effective(ProjectElement element, ProjectFamily? family, string key)
        {
            if (element.Properties.TryGetValue(key, out var instance) && !string.IsNullOrWhiteSpace(instance)) return instance.Trim();
            if (family != null && family.Properties.TryGetValue(key, out var inherited) && !string.IsNullOrWhiteSpace(inherited)) return inherited.Trim();
            return string.Empty;
        }

        private static double? EffectiveDensity(ProjectElement element, ProjectFamily? family)
        {
            if (element.Properties.TryGetValue("DensityKgM3", out var instance) && !string.IsNullOrWhiteSpace(instance))
                return PositiveInvariant(instance, element.Id + "/DensityKgM3");
            if (family != null && family.Properties.TryGetValue("DensityKgM3", out var inherited) && !string.IsNullOrWhiteSpace(inherited))
                return PositiveInvariant(inherited, "Family " + family.Id + "/DensityKgM3");
            return null;
        }

        private static double PositiveInvariant(string value, string label)
        {
            if (!double.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ||
                double.IsNaN(parsed) || double.IsInfinity(parsed) || parsed <= 0d)
                throw new InvalidOperationException(label + " must be an invariant finite number greater than zero.");
            return parsed;
        }

        private static double? EffectiveMass(ProjectElement element, double? densityKgM3)
        {
            var explicitMass = OptionalNonNegativeQuantity(element, "WeightKg", "MassKg");
            if (explicitMass.HasValue) return explicitMass;
            if (!densityKgM3.HasValue) return null;

            var volume = OptionalNonNegativeQuantity(
                element,
                "NetConcreteM3",
                "NetVolumeM3",
                "GrossConcreteM3",
                "GrossVolumeM3",
                "VolumeM3",
                "MeasuredVolumeM3");
            if (!volume.HasValue) return null;
            var mass = checked(volume.Value * densityKgM3.Value);
            if (double.IsNaN(mass) || double.IsInfinity(mass))
                throw new OverflowException("Quantity report mass overflow: " + element.Id + "/volume*density.");
            if (mass == 0d && volume.Value > 0d && densityKgM3.Value > 0d)
                throw new InvalidOperationException("Quantity report mass underflow: " + element.Id + "/volume*density rounded positive finite inputs to zero.");
            if (volume.Value != 0d && densityKgM3.Value != 0d)
            {
                if (densityKgM3.Value != 1d && mass == volume.Value)
                    throw new InvalidOperationException("Quantity report mass lost the density contribution at double precision: " + element.Id + "/volume*density.");
                if (volume.Value != 1d && mass == densityKgM3.Value)
                    throw new InvalidOperationException("Quantity report mass lost the volume contribution at double precision: " + element.Id + "/volume*density.");
            }
            return mass;
        }

        private static double? OptionalNonNegativeQuantity(ProjectElement element, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (!element.Quantities.ContainsKey(key)) continue;
                return Q(element, key);
            }
            return null;
        }

        private static string DensityKey(double? densityKgM3) => densityKgM3.HasValue
            ? densityKgM3.Value.ToString("R", CultureInfo.InvariantCulture)
            : "<none>";

        private static string CanonicalGroupKey(params string[] parts)
        {
            return string.Join("|", (parts ?? Array.Empty<string>())
                .Select(part =>
                {
                    var value = part ?? string.Empty;
                    return value.Length.ToString(CultureInfo.InvariantCulture) + ":" + value;
                }));
        }

        private static string AppendText(string current, string value)
        {
            var existing = (current ?? string.Empty).Trim();
            var incoming = (value ?? string.Empty).Trim();
            if (incoming.Length == 0) return existing;
            if (existing.Length == 0) return incoming;
            return existing + " | " + incoming;
        }

        private static bool HasAnyQuantity(ProjectElement element, params string[] keys)
        {
            foreach (var key in keys)
                if (element.Quantities.ContainsKey(key)) return true;
            return false;
        }

        private static bool AggregateEvidence(bool current, bool elementEvidence, bool created) =>
            created ? elementEvidence : current && elementEvidence;

        private static double QFirst(ProjectElement element, params string[] keys)
        {
            foreach (var key in keys)
                if (element.Quantities.ContainsKey(key)) return Q(element, key);
            return 0d;
        }

        private static double QFirstOrFallback(ProjectElement element, double fallback, params string[] keys)
        {
            foreach (var key in keys)
                if (element.Quantities.ContainsKey(key)) return Q(element, key);
            return QuantityReportMath.NonNegative(fallback, element.Id + "/fallback");
        }

        private static double Q(ProjectElement element, string name, double fallback = 0d)
        {
            var value = element.Quantities.TryGetValue(name, out var stored) ? stored : fallback;
            return QuantityReportMath.NonNegative(value, element.Id + "/" + name);
        }
    }
}
