using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectQuantityAggregationPrecisionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            LargeFirstRepresentableAggregateIsPreserved();
            SmallFirstRepresentableAggregateIsPreserved();
            MetricsAndGroupsRemainIsolated();
            HomogeneousMassUsesCompensatedAggregation();
            MissingMassKeepsGroupedMassUnknown();
            DetailRowsRemainElementIsolated();
            FinalUnrepresentableAggregateFailsClosed();
            NonFiniteInputStillFailsClosed();
        }

        private static void LargeFirstRepresentableAggregateIsPreserved()
        {
            var project = Project("PQ-PREC-LARGE");
            AddElement(project, "q1", "q", 1e16d);
            AddElement(project, "q2", "q", 1d);
            AddElement(project, "q3", "q", 1d);

            var row = Single(project);
            Equal(3, row.Count, "large-first count");
            Equal(3, row.ElementIds.Count, "large-first provenance count");
            Equal(10000000000000002d, row.LengthM, "large-first length");
            Equal(10000000000000002d, row.GrossConcreteM3, "large-first gross concrete");
            Equal(10000000000000002d, row.NetConcreteM3, "large-first net concrete");
            Equal(10000000000000002d, row.FormworkM2, "large-first formwork");
        }

        private static void SmallFirstRepresentableAggregateIsPreserved()
        {
            var project = Project("PQ-PREC-SMALL");
            AddElement(project, "q1", "q", 1d);
            AddElement(project, "q2", "q", 1d);
            AddElement(project, "q3", "q", 1e16d);

            var row = Single(project);
            Equal(10000000000000002d, row.LengthM, "small-first length");
            Equal(10000000000000002d, row.GrossConcreteM3, "small-first gross concrete");
        }

        private static void MetricsAndGroupsRemainIsolated()
        {
            var project = Project("PQ-PREC-GROUPS");
            project.Families.Add(new ProjectFamily("q2", "Quantity 2", ElementCategory.CustomQuantity));
            AddElement(project, "q1", "q", 10d, formwork: 2d);
            AddElement(project, "q2", "q", 5d, formwork: 3d);
            AddElement(project, "q3", "q2", 7d, formwork: 11d);

            var rows = ProjectQuantityReportBuilder.Group(project);
            Equal(2, rows.Count, "isolated group count");
            Equal(15d, rows[0].LengthM, "first-group length");
            Equal(5d, rows[0].FormworkM2, "first-group formwork");
            Equal(7d, rows[1].LengthM, "second-group length");
            Equal(11d, rows[1].FormworkM2, "second-group formwork");
        }

        private static void HomogeneousMassUsesCompensatedAggregation()
        {
            var project = Project("PQ-PREC-MASS");
            AddElement(project, "q1", "q", 1d, massKg: 1e16d);
            AddElement(project, "q2", "q", 1d, massKg: 1d);
            AddElement(project, "q3", "q", 1d, massKg: 1d);

            var row = Single(project);
            Equal(10000000000000002d, row.MassKg, "homogeneous compensated mass");
        }

        private static void MissingMassKeepsGroupedMassUnknown()
        {
            var project = Project("PQ-PREC-MASS-NULL");
            AddElement(project, "q1", "q", 1d, massKg: 10d);
            AddElement(project, "q2", "q", 1d);

            var row = Single(project);
            Equal<double?>(null, row.MassKg, "missing mass remains unknown");
        }

        private static void DetailRowsRemainElementIsolated()
        {
            var project = Project("PQ-PREC-DETAIL");
            AddElement(project, "q1", "q", 1e16d);
            AddElement(project, "q2", "q", 1d);
            var rows = ProjectQuantityReportBuilder.Detail(project);
            Equal(2, rows.Count, "detail row count");
            Equal(1e16d, rows[0].LengthM, "detail first length");
            Equal(1d, rows[1].LengthM, "detail second length");
        }

        private static void FinalUnrepresentableAggregateFailsClosed()
        {
            var project = Project("PQ-PREC-LOSS");
            AddElement(project, "q1", "q", 9007199254740992d);
            AddElement(project, "q2", "q", 1d);
            Throws<OverflowException>(() => ProjectQuantityReportBuilder.Group(project));
        }

        private static void NonFiniteInputStillFailsClosed()
        {
            var project = Project("PQ-PREC-NAN");
            AddElement(project, "q1", "q", double.NaN);
            Throws<InvalidOperationException>(() => ProjectQuantityReportBuilder.Group(project));
        }

        private static ProjectState Project(string id)
        {
            var project = new ProjectState(id, "Project quantity precision smoke");
            project.Floors.Add(new FloorDefinition("f1", "Floor 1", 0d));
            project.Zones.Add(new ZoneDefinition("z", "Zone"));
            project.Families.Add(new ProjectFamily("q", "Quantity", ElementCategory.CustomQuantity));
            return project;
        }

        private static void AddElement(ProjectState project, string id, string familyId, double value, double? formwork = null, double? massKg = null)
        {
            var element = new ProjectElement(id, ElementCategory.CustomQuantity, familyId, "f1", "z");
            element.Quantities["LengthM"] = value;
            element.Quantities["GrossConcreteM3"] = value;
            element.Quantities["NetConcreteM3"] = value;
            element.Quantities["GrossFormworkM2"] = formwork ?? value;
            element.Quantities["NetFormworkM2"] = formwork ?? value;
            element.Quantities["FormworkM2"] = formwork ?? value;
            if (massKg.HasValue) element.Quantities["WeightKg"] = massKg.Value;
            project.Elements.Add(element);
        }

        private static QuantityReportRow Single(ProjectState project)
        {
            var rows = ProjectQuantityReportBuilder.Group(project);
            Equal(1, rows.Count, "single grouped row");
            return rows[0];
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("ProjectQuantityAggregationPrecisionSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("ProjectQuantityAggregationPrecisionSmoke expected " + typeof(TException).Name + ".");
        }
    }
}
