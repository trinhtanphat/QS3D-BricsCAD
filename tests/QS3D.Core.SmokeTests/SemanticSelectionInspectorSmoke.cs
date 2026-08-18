using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Selection;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSelectionInspectorSmoke
    {
        private const int MaxSelection = 100000;

        public static void Run()
        {
            CommonAndMixedValuesAreStable();
            SelectedIdsMustBeCanonical();
            QuantitySignedZeroProjectionIsCanonical();
            NegativeInjectedQuantityFailsClosed();
            QuantityKeysMustBeCanonical();
            PropertyKeysMustBeCanonical();
            ReferencePresenceCountsActualAssignments();
            FamilyDefaultsParticipateInEffectiveValues();
            InternalOwnershipPropertiesStayHidden();
            MissingSelectionFailsClosed();
            MissingSemanticReferenceFailsClosed();
            FamilyCategoryMismatchFailsClosed();
            DuplicateProjectIdentityFailsClosed();
            OversizedKnownCountsRejectBeforeEnumeration();
            NegativeKnownCountsRejectBeforeEnumeration();
            ExactBoundKnownCountReachesEnumeration();
            EmptySelectionIsSupported();
        }

        private static void CommonAndMixedValuesAreStable()
        {
            var project = BuildProject();
            var result = SemanticSelectionInspector.Inspect(project, new[] { "B-002", "B-001" });
            Equal(2, result.Count);
            Equal("B-001", result.ElementIds[0]);
            Equal("B-002", result.ElementIds[1]);
            Equal(false, result.HasMixedCategories);
            Equal(false, result.Family.IsMixed);
            Equal("FAM-B", result.Family.Value);
            Equal(false, result.Floor.IsMixed);
            Equal("F-02", result.Floor.Value);
            Equal(true, result.Zone.IsMixed);

            var thickness = result.Properties.Single(x => x.Name == "ThicknessM");
            Equal(false, thickness.IsMixed);
            Equal("0.3", thickness.Value);
            Equal(2, thickness.PresentCount);

            var mark = result.Properties.Single(x => x.Name == "Mark");
            Equal(true, mark.IsMixed);
            Equal(null, mark.Value);

            var note = result.Properties.Single(x => x.Name == "Note");
            Equal(true, note.IsMixed);
            Equal(1, note.PresentCount);

            var length = result.Quantities.Single(x => x.Name == "LengthM");
            Equal(true, length.IsMixed);
            Equal(2, length.PresentCount);
        }

        private static void SelectedIdsMustBeCanonical()
        {
            var project = BuildProject();
            var version = project.ChangeVersion;
            var canonical = SemanticSelectionInspector.Inspect(project, new[] { "b-001" });
            Equal(1, canonical.Count);
            Equal("B-001", canonical.ElementIds[0]);
            Equal(version, project.ChangeVersion);

            RejectPaddedSelectedId(project, " B-001 ");
            RejectPaddedSelectedId(project, "\tB-001");
            RejectPaddedSelectedId(project, "B-001\n");
            Equal(version, project.ChangeVersion);
        }

        private static void RejectPaddedSelectedId(ProjectState project, string selectedId)
        {
            try
            {
                SemanticSelectionInspector.Inspect(project, new[] { selectedId });
            }
            catch (ArgumentException ex)
            {
                if (ex.Message.IndexOf("leading or trailing whitespace", StringComparison.Ordinal) < 0)
                    throw new Exception("Unexpected padded selected semantic-id rejection: " + ex.Message);
                return;
            }

            throw new Exception("Padded selected semantic IDs must fail closed instead of aliasing canonical project elements.");
        }

        private static void QuantitySignedZeroProjectionIsCanonical()
        {
            var project = BuildProject();
            var negativeZero = BitConverter.Int64BitsToDouble(long.MinValue);
            var negativeElement = project.FindElement("B-001")!;
            var positiveElement = project.FindElement("B-002")!;
            negativeElement.Quantities["LengthM"] = negativeZero;
            var version = project.ChangeVersion;

            var single = SemanticSelectionInspector.Inspect(project, new[] { "B-001" })
                .Quantities.Single(x => x.Name == "LengthM");
            Equal(false, single.IsMixed);
            Equal(1, single.PresentCount);
            if (!single.Value.HasValue) throw new Exception("Signed-zero quantity projection lost the selected value.");
            Equal(0L, BitConverter.DoubleToInt64Bits(single.Value.Value));
            Equal(long.MinValue, BitConverter.DoubleToInt64Bits(negativeElement.Quantities["LengthM"]));
            Equal(version, project.ChangeVersion);

            positiveElement.Quantities["LengthM"] = 0d;
            var combined = SemanticSelectionInspector.Inspect(project, new[] { "B-002", "B-001" })
                .Quantities.Single(x => x.Name == "LengthM");
            Equal(false, combined.IsMixed);
            Equal(2, combined.PresentCount);
            if (!combined.Value.HasValue) throw new Exception("Equivalent signed zeros must retain a canonical shared quantity value.");
            Equal(0L, BitConverter.DoubleToInt64Bits(combined.Value.Value));
            Equal(long.MinValue, BitConverter.DoubleToInt64Bits(negativeElement.Quantities["LengthM"]));
            Equal(0L, BitConverter.DoubleToInt64Bits(positiveElement.Quantities["LengthM"]));
            Equal(version, project.ChangeVersion);
        }

        private static void NegativeInjectedQuantityFailsClosed()
        {
            var project = BuildProject();
            var element = project.FindElement("B-001")!;
            element.Quantities["LengthM"] = -1d;
            var version = project.ChangeVersion;

            MustFail(
                () => SemanticSelectionInspector.Inspect(project, new[] { element.Id }),
                "Negative quantity state injected through the mutable quantity dictionary must fail closed at the semantic selection boundary.");

            Equal(-1d, element.Quantities["LengthM"]);
            Equal(version, project.ChangeVersion);
        }

        private static void QuantityKeysMustBeCanonical()
        {
            var blankProject = BuildProject();
            var blankElement = blankProject.FindElement("B-001")!;
            blankElement.Quantities["   "] = 1d;
            var blankVersion = blankProject.ChangeVersion;
            MustFail(
                () => SemanticSelectionInspector.Inspect(blankProject, new[] { blankElement.Id }),
                "Whitespace-only quantity names must fail closed at the semantic selection boundary.");
            Equal(true, blankElement.Quantities.ContainsKey("   "));
            Equal(blankVersion, blankProject.ChangeVersion);

            var paddedProject = BuildProject();
            var paddedElement = paddedProject.FindElement("B-001")!;
            paddedElement.Quantities[" LengthM "] = 9d;
            var paddedVersion = paddedProject.ChangeVersion;
            MustFail(
                () => SemanticSelectionInspector.Inspect(paddedProject, new[] { paddedElement.Id }),
                "Padded quantity names must fail closed at the semantic selection boundary.");
            Equal(true, paddedElement.Quantities.ContainsKey(" LengthM "));
            Equal(paddedVersion, paddedProject.ChangeVersion);

            var canonical = SemanticSelectionInspector.Inspect(BuildProject(), new[] { "B-001" })
                .Quantities.Single(x => x.Name == "LengthM");
            Equal("LengthM", canonical.Name);
            Equal(1, canonical.PresentCount);
        }

        private static void PropertyKeysMustBeCanonical()
        {
            var blankElementProject = BuildProject();
            var blankElement = blankElementProject.FindElement("B-001")!;
            blankElement.Properties["   "] = "legacy";
            var blankElementVersion = blankElementProject.ChangeVersion;
            MustFail(
                () => SemanticSelectionInspector.Inspect(blankElementProject, new[] { blankElement.Id }),
                "Whitespace-only element property keys must fail closed at the semantic selection boundary.");
            Equal(true, blankElement.Properties.ContainsKey("   "));
            Equal(blankElementVersion, blankElementProject.ChangeVersion);

            var paddedElementProject = BuildProject();
            var paddedElement = paddedElementProject.FindElement("B-001")!;
            paddedElement.Properties[" Mark "] = "legacy";
            var paddedElementVersion = paddedElementProject.ChangeVersion;
            MustFail(
                () => SemanticSelectionInspector.Inspect(paddedElementProject, new[] { paddedElement.Id }),
                "Padded element property keys must fail closed at the semantic selection boundary.");
            Equal(true, paddedElement.Properties.ContainsKey(" Mark "));
            Equal(paddedElementVersion, paddedElementProject.ChangeVersion);

            var blankFamilyProject = BuildProject();
            var blankFamily = blankFamilyProject.FindFamily("FAM-B")!;
            blankFamily.Properties["   "] = "legacy";
            var blankFamilyVersion = blankFamilyProject.ChangeVersion;
            MustFail(
                () => SemanticSelectionInspector.Inspect(blankFamilyProject, new[] { "B-001" }),
                "Whitespace-only Family property keys must fail closed before inherited property projection.");
            Equal(true, blankFamily.Properties.ContainsKey("   "));
            Equal(blankFamilyVersion, blankFamilyProject.ChangeVersion);

            var paddedFamilyProject = BuildProject();
            var paddedFamily = paddedFamilyProject.FindFamily("FAM-B")!;
            paddedFamily.Properties[" FireRating "] = "R90";
            var paddedFamilyVersion = paddedFamilyProject.ChangeVersion;
            MustFail(
                () => SemanticSelectionInspector.Inspect(paddedFamilyProject, new[] { "B-001" }),
                "Padded Family property keys must fail closed before inherited property projection.");
            Equal(true, paddedFamily.Properties.ContainsKey(" FireRating "));
            Equal(paddedFamilyVersion, paddedFamilyProject.ChangeVersion);

            var canonical = SemanticSelectionInspector.Inspect(BuildProject(), new[] { "B-001" });
            Equal("R60", canonical.Properties.Single(x => x.Name == "FireRating").Value);
            Equal("B1", canonical.Properties.Single(x => x.Name == "Mark").Value);
        }

        private static void ReferencePresenceCountsActualAssignments()
        {
            var project = BuildProject();
            project.Elements[0].ZoneId = string.Empty;
            var partial = SemanticSelectionInspector.Inspect(project, new[] { "B-001", "B-002" });
            Equal(true, partial.Zone.IsMixed);
            Equal(1, partial.Zone.PresentCount);
            Equal(null, partial.Zone.Value);

            project.Elements[1].ZoneId = "   ";
            var unassigned = SemanticSelectionInspector.Inspect(project, new[] { "B-001", "B-002" });
            Equal(false, unassigned.Zone.IsMixed);
            Equal(0, unassigned.Zone.PresentCount);
            Equal(string.Empty, unassigned.Zone.Value);
        }

        private static void FamilyDefaultsParticipateInEffectiveValues()
        {
            var project = BuildProject();
            var result = SemanticSelectionInspector.Inspect(project, new[] { "B-001", "B-002" });

            var fireRating = result.Properties.Single(x => x.Name == "FireRating");
            Equal(false, fireRating.IsMixed);
            Equal("R60", fireRating.Value);
            Equal(2, fireRating.PresentCount);

            var material = result.Properties.Single(x => x.Name == "Material");
            Equal(true, material.IsMixed);
            Equal(null, material.Value);
            Equal(2, material.PresentCount);
        }

        private static void InternalOwnershipPropertiesStayHidden()
        {
            var project = BuildProject();
            project.Elements[0].Properties["GeneratedSolidHandle"] = "AB12";
            project.Elements[0].Properties[ProjectElement.GeneratedGeometryStateKey] = "stale";
            project.Elements[0].Properties["PhysicalOpeningCutHandle"] = "CD34";
            project.Elements[0].Properties["QS3D.PhysicalOpeningCutOpeningIds"] = "T1|T2";
            project.Families[0].Properties["GeneratedFamilyHandle"] = "EF56";
            var result = SemanticSelectionInspector.Inspect(project, new[] { project.Elements[0].Id });
            if (result.Properties.Any(x => x.Name.IndexOf("Handle", StringComparison.OrdinalIgnoreCase) >= 0))
                throw new Exception("Property inspector must not expose native ownership handles.");
            if (result.Properties.Any(x => x.Name.StartsWith("QS3D.Generated", StringComparison.OrdinalIgnoreCase)))
                throw new Exception("Property inspector must not expose internal generated-state keys as editable semantic properties.");
            if (result.Properties.Any(x => x.Name.StartsWith("PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase)))
                throw new Exception("Property inspector must not expose legacy physical opening cut ownership state.");
            if (result.Properties.Any(x => x.Name.StartsWith("QS3D.PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase)))
                throw new Exception("Property inspector must not expose namespaced physical opening cut ownership state.");
        }

        private static void MissingSelectionFailsClosed()
        {
            var project = BuildProject();
            MustFail(
                () => SemanticSelectionInspector.Inspect(project, new[] { "E-404" }),
                "Missing selected semantic IDs must fail closed.");
        }

        private static void MissingSemanticReferenceFailsClosed()
        {
            var project = BuildProject();
            project.Elements[0].FloorId = "F-404";
            MustFail(
                () => SemanticSelectionInspector.Inspect(project, new[] { project.Elements[0].Id }),
                "Missing selected floor references must fail closed.");
        }

        private static void FamilyCategoryMismatchFailsClosed()
        {
            var project = BuildProject();
            project.Families[0].Category = ElementCategory.Column;
            MustFail(
                () => SemanticSelectionInspector.Inspect(project, new[] { project.Elements[0].Id }),
                "Selected element/family category mismatch must fail closed.");
        }

        private static void DuplicateProjectIdentityFailsClosed()
        {
            var project = BuildProject();
            project.Elements.Add(new ProjectElement("b-001", ElementCategory.Beam, "FAM-B", "F-02", "Z-A"));
            MustFail(
                () => SemanticSelectionInspector.Inspect(project, new[] { "B-001" }),
                "Duplicate project element IDs must fail closed before inspection.");
        }

        private static void OversizedKnownCountsRejectBeforeEnumeration()
        {
            var generic = new GenericKnownCountSource(MaxSelection + 1);
            KnownCountRejectedBeforeEnumeration(generic, () => generic.EnumeratorRequested, "supports at most");

            var readOnly = new ReadOnlyKnownCountSource(MaxSelection + 1);
            KnownCountRejectedBeforeEnumeration(readOnly, () => readOnly.EnumeratorRequested, "supports at most");

            var nonGeneric = new NonGenericKnownCountSource(MaxSelection + 1);
            KnownCountRejectedBeforeEnumeration(nonGeneric, () => nonGeneric.EnumeratorRequested, "supports at most");
        }

        private static void NegativeKnownCountsRejectBeforeEnumeration()
        {
            var generic = new GenericKnownCountSource(-1);
            KnownCountRejectedBeforeEnumeration(generic, () => generic.EnumeratorRequested, "invalid negative known count");

            var readOnly = new ReadOnlyKnownCountSource(-1);
            KnownCountRejectedBeforeEnumeration(readOnly, () => readOnly.EnumeratorRequested, "invalid negative known count");

            var nonGeneric = new NonGenericKnownCountSource(-1);
            KnownCountRejectedBeforeEnumeration(nonGeneric, () => nonGeneric.EnumeratorRequested, "invalid negative known count");
        }

        private static void ExactBoundKnownCountReachesEnumeration()
        {
            var project = BuildProject();
            var beforeVersion = project.ChangeVersion;
            var source = new GenericKnownCountSource(MaxSelection);
            try
            {
                SemanticSelectionInspector.Inspect(project, source);
            }
            catch (ExactBoundEnumerationReachedException)
            {
                Equal(true, source.EnumeratorRequested);
                Equal(beforeVersion, project.ChangeVersion);
                return;
            }

            throw new Exception("Exact-bound semantic selection Count must reach enumeration instead of failing the known-count preflight.");
        }

        private static void KnownCountRejectedBeforeEnumeration(
            IEnumerable<string> source,
            Func<bool> enumeratorRequested,
            string expectedMessageFragment)
        {
            var project = BuildProject();
            var beforeVersion = project.ChangeVersion;
            try
            {
                SemanticSelectionInspector.Inspect(project, source);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedMessageFragment, StringComparison.Ordinal) < 0)
                    throw new Exception("Unexpected semantic selection known-count rejection: " + ex.Message);
                Equal(false, enumeratorRequested());
                Equal(beforeVersion, project.ChangeVersion);
                return;
            }

            throw new Exception("Expected semantic selection known-count source to fail before enumeration.");
        }

        private sealed class GenericKnownCountSource : ICollection<string>
        {
            internal GenericKnownCountSource(int count) => Count = count;

            public int Count { get; }
            public bool IsReadOnly => true;
            internal bool EnumeratorRequested { get; private set; }

            public IEnumerator<string> GetEnumerator()
            {
                EnumeratorRequested = true;
                if (Count == MaxSelection) throw new ExactBoundEnumerationReachedException();
                throw new Exception("Rejected generic known-count semantic selection source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(string item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(string item) => false;
            public void CopyTo(string[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(string item) => throw new NotSupportedException();
        }

        private sealed class ReadOnlyKnownCountSource : IReadOnlyCollection<string>
        {
            internal ReadOnlyKnownCountSource(int count) => Count = count;

            public int Count { get; }
            internal bool EnumeratorRequested { get; private set; }

            public IEnumerator<string> GetEnumerator()
            {
                EnumeratorRequested = true;
                throw new Exception("Rejected read-only known-count semantic selection source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class NonGenericKnownCountSource : IEnumerable<string>, ICollection
        {
            internal NonGenericKnownCountSource(int count) => Count = count;

            public int Count { get; }
            public bool IsSynchronized => false;
            public object SyncRoot => this;
            internal bool EnumeratorRequested { get; private set; }

            public IEnumerator<string> GetEnumerator()
            {
                EnumeratorRequested = true;
                throw new Exception("Rejected non-generic known-count semantic selection source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private sealed class ExactBoundEnumerationReachedException : Exception
        {
        }

        private static void EmptySelectionIsSupported()
        {
            var result = SemanticSelectionInspector.Inspect(BuildProject(), Array.Empty<string>());
            Equal(0, result.Count);
            Equal(0, result.Categories.Count);
            Equal(false, result.Family.IsMixed);
            Equal(null, result.Family.Value);
            Equal(0, result.Properties.Count);
            Equal(0, result.Quantities.Count);
        }

        private static ProjectState BuildProject()
        {
            var project = new ProjectState("P-PROP", "Property Inspector Smoke");
            project.Floors.Add(new FloorDefinition("F-02", "L02", 3.6d));
            project.Zones.Add(new ZoneDefinition("Z-A", "Zone A"));
            project.Zones.Add(new ZoneDefinition("Z-B", "Zone B"));
            var family = new ProjectFamily("FAM-B", "Beam 300x500", ElementCategory.Beam);
            family.Properties["FireRating"] = "R60";
            family.Properties["Material"] = "C30";
            project.Families.Add(family);

            var first = new ProjectElement("B-002", ElementCategory.Beam, "FAM-B", "F-02", "Z-B");
            first.SetProperty("ThicknessM", "0.3");
            first.SetProperty("Mark", "B2");
            first.SetProperty("Note", "Edge");
            first.SetProperty("Material", "C35");
            first.SetQuantity("LengthM", 5d);

            var second = new ProjectElement("B-001", ElementCategory.Beam, "FAM-B", "F-02", "Z-A");
            second.SetProperty("ThicknessM", "0.3");
            second.SetProperty("Mark", "B1");
            second.SetQuantity("LengthM", 6d);

            project.Elements.Add(first);
            project.Elements.Add(second);
            return project;
        }

        private static void MustFail(Action action, string message)
        {
            var failed = false;
            try { action(); }
            catch (InvalidOperationException) { failed = true; }
            if (!failed) throw new Exception(message);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected '" + expected + "' but got '" + actual + "'.");
        }
    }
}
