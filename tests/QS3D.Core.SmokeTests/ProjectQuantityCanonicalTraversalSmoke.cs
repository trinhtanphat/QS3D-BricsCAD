using System;
using System.Collections.Generic;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectQuantityCanonicalTraversalSmoke
    {
        internal static void Run()
        {
            var forward = Fixture(false);
            var reverse = Fixture(true);

            var forwardGroup = ProjectQuantityReportBuilder.Group(forward);
            var reverseGroup = ProjectQuantityReportBuilder.Group(reverse);
            Equal(1, forwardGroup.Count, "Forward grouped row count changed.");
            Equal(1, reverseGroup.Count, "Reverse grouped row count changed.");
            EquivalentRow(forwardGroup[0], reverseGroup[0], "grouped");
            SequenceEqual(forwardGroup[0].ElementIds, reverseGroup[0].ElementIds, "Grouped ElementIds must be insertion-order invariant.");
            SequenceEqual(forwardGroup[0].SourceHandles, reverseGroup[0].SourceHandles, "Grouped source handles must be insertion-order invariant.");

            var forwardDetail = ProjectQuantityReportBuilder.Detail(forward);
            var reverseDetail = ProjectQuantityReportBuilder.Detail(reverse);
            Equal(forwardDetail.Count, reverseDetail.Count, "Detail row count changed by insertion order.");
            for (var i = 0; i < forwardDetail.Count; i++)
            {
                EquivalentRow(forwardDetail[i], reverseDetail[i], "detail[" + i + "]");
                SequenceEqual(forwardDetail[i].ElementIds, reverseDetail[i].ElementIds, "Detail semantic order must be insertion-order invariant at index " + i + ".");
            }
        }

        private static ProjectState Fixture(bool reverse)
        {
            var project = new ProjectState("P-QTY-CANONICAL", "Canonical quantity traversal");
            var first = Element("E-A", "HA", "note-a", 1d, 10d);
            var second = Element("E-B", "HB", "note-b", 2d, 20d);

            if (reverse)
            {
                project.Elements.Add(second);
                project.Elements.Add(first);
            }
            else
            {
                project.Elements.Add(first);
                project.Elements.Add(second);
            }

            return project;
        }

        private static ProjectElement Element(string id, string handle, string note, double lengthM, double volumeM3)
        {
            var element = new ProjectElement(id, ElementCategory.Beam);
            element.SourceHandles.Add(handle);
            element.SetProperty("Note", note);
            element.SetQuantity("LengthM", lengthM);
            element.SetQuantity("GrossConcreteM3", volumeM3);
            element.SetQuantity("NetConcreteM3", volumeM3);
            return element;
        }

        private static void EquivalentRow(QuantityReportRow expected, QuantityReportRow actual, string label)
        {
            Equal(expected.Floor, actual.Floor, label + " floor changed.");
            Equal(expected.Zone, actual.Zone, label + " zone changed.");
            Equal(expected.Category, actual.Category, label + " category changed.");
            Equal(expected.FamilyId, actual.FamilyId, label + " family id changed.");
            Equal(expected.FamilyName, actual.FamilyName, label + " family name changed.");
            Equal(expected.Material, actual.Material, label + " material changed.");
            Equal(expected.Note, actual.Note, label + " note concatenation changed with insertion order.");
            Equal(expected.Count, actual.Count, label + " count changed.");
            Equal(expected.LengthM, actual.LengthM, label + " length changed.");
            Equal(expected.GrossConcreteM3, actual.GrossConcreteM3, label + " gross concrete changed.");
            Equal(expected.NetConcreteM3, actual.NetConcreteM3, label + " net concrete changed.");
        }

        private static void SequenceEqual(IList<string> expected, IList<string> actual, string message)
        {
            Equal(expected.Count, actual.Count, message + " Count differs.");
            for (var i = 0; i < expected.Count; i++)
                Equal(expected[i], actual[i], message + " Index=" + i + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + " Actual=" + actual + ".");
        }
    }
}
