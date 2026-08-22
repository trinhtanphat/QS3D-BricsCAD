using System;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectRebarShapeSmoke
    {
        public static void Run()
        {
            BuildsLShapeFromElementProperties();
            StraightShapeNeedsNoLegMetadata();
            MismatchedLegTotalIsRejected();
        }

        private static void BuildsLShapeFromElementProperties()
        {
            var project = new ProjectState("shape-plan", "Shape Plan");
            var element = new ProjectElement("B1", ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            element.Properties["RebarNotation"] = "2D16";
            element.Properties["RebarCuttingLengthM"] = "3";
            element.Properties["RebarShapeCode"] = "11";
            element.Properties["RebarShapeLegsM"] = "2;1";
            project.Elements.Add(element);

            var plan = ProjectRebarShapePlanner.Build(project).Single();
            Equal("B1", plan.ElementId);
            Equal(2, plan.Quantity);
            Near(16d, plan.DiameterMm);
            Equal("11", plan.Path.ShapeCode);
            Equal(3, plan.Path.Points.Count);
            Near(2d, plan.Path.Points[1].X);
            Near(0d, plan.Path.Points[1].Y);
            Near(2d, plan.Path.Points[2].X);
            Near(1d, plan.Path.Points[2].Y);
        }

        private static void StraightShapeNeedsNoLegMetadata()
        {
            var project = new ProjectState("shape-straight", "Shape Straight");
            var element = new ProjectElement("S1", ElementCategory.Slab, string.Empty, string.Empty, string.Empty);
            element.Properties["RebarNotation"] = "1D12";
            element.Properties["RebarCuttingLengthM"] = "2.5";
            project.Elements.Add(element);

            var plan = ProjectRebarShapePlanner.Build(project).Single();
            Equal("00", plan.Path.ShapeCode);
            Equal(2, plan.Path.Points.Count);
            Near(0d, plan.Path.Points[0].X);
            Near(2.5d, plan.Path.Points[1].X);
        }

        private static void MismatchedLegTotalIsRejected()
        {
            var project = new ProjectState("shape-invalid", "Shape Invalid");
            var element = new ProjectElement("B2", ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            element.Properties["RebarNotation"] = "2D16";
            element.Properties["RebarCuttingLengthM"] = "3";
            element.Properties["RebarShapeCode"] = "11";
            element.Properties["RebarShapeLegsM"] = "1;1";
            project.Elements.Add(element);
            Throws<InvalidOperationException>(() => ProjectRebarShapePlanner.Build(project));
        }

        private static void Near(double expected, double actual, double tolerance = 1e-9)
        {
            if (Math.Abs(expected - actual) > tolerance) throw new Exception("Expected " + expected + ", got " + actual);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual);
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
