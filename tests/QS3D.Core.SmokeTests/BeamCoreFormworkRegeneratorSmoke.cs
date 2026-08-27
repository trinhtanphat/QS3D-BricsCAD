using System;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class BeamCoreFormworkRegeneratorSmoke
    {
        public static void Run()
        {
            var project = new ProjectState("project-beam-core-formwork", "Beam Core Formwork");
            var beam = new ProjectElement("beam-1", ElementCategory.Beam);
            beam.SetProperty("LengthM", "7.0710678118654755");
            beam.SetProperty("WidthM", "0.3");
            beam.SetProperty("HeightM", "0.5");

            // Seed legacy/rule-projected values to prove Core-only regeneration does
            // not preserve a stale Side+Bottom assumption after source geometry changes.
            beam.SetQuantity("FormworkM2", 9.19238815542512d);
            beam.SetQuantity("NetFormworkM2", 8.80238815542512d);
            beam.SetQuantity("GrossFormworkM2", 9.19238815542512d);
            beam.SetQuantity("ConcreteContactDeductionM2", 0.39d);
            project.Elements.Add(beam);

            new StructuralRegenerator().Regenerate(project, beam);

            ExpectNear(beam, "SideAreaM2", 7.0710678118654755d);
            ExpectNear(beam, "BottomAreaM2", 2.1213203435596424d);
            ExpectNear(beam, "TopAreaM2", 0d);

            Forbid(beam, "FormworkM2");
            Forbid(beam, "NetFormworkM2");
            Forbid(beam, "GrossFormworkM2");
            Forbid(beam, "ConcreteContactDeductionM2");

            Console.WriteLine("PASS Beam Core formwork regeneration is rule-safe");
        }

        private static void ExpectNear(ProjectElement element, string key, double expected)
        {
            double actual;
            if (!element.Quantities.TryGetValue(key, out actual))
                throw new InvalidOperationException(
                    "Beam Core formwork smoke: required quantity " + key + " is missing.");

            if (Math.Abs(actual - expected) > 1e-9d)
                throw new InvalidOperationException(
                    "Beam Core formwork smoke: " + key + " expected " + expected.ToString("R") +
                    ", got " + actual.ToString("R") + ".");
        }

        private static void Forbid(ProjectElement element, string key)
        {
            if (element.Quantities.ContainsKey(key))
                throw new InvalidOperationException(
                    "Beam Core formwork smoke: stale rule-projected quantity " + key +
                    " survived Core regeneration.");
        }
    }
}
