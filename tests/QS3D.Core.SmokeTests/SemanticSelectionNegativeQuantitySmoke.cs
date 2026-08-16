using System;
using QS3D.Core.Domain;
using QS3D.Core.Selection;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSelectionNegativeQuantitySmoke
    {
        public static void Run()
        {
            var project = new ProjectState("P-NEG-QTY", "Negative Quantity Selection Smoke");
            project.Floors.Add(new FloorDefinition("F-01", "L01", 0d));
            project.Zones.Add(new ZoneDefinition("Z-A", "Zone A"));
            project.Families.Add(new ProjectFamily("FAM-B", "Beam", ElementCategory.Beam));

            var element = new ProjectElement("B-001", ElementCategory.Beam, "FAM-B", "F-01", "Z-A");
            element.SetQuantity("LengthM", 6d);
            project.Elements.Add(element);

            // Public dictionary mutation can bypass ProjectElement.SetQuantity's non-negative invariant.
            element.Quantities["LengthM"] = -0.25d;
            var version = project.ChangeVersion;

            var failedClosed = false;
            try
            {
                SemanticSelectionInspector.Inspect(project, new[] { element.Id });
            }
            catch (InvalidOperationException)
            {
                failedClosed = true;
            }

            if (!failedClosed)
                throw new Exception("Semantic selection must fail closed when mutable quantity state contains a negative value.");
            if (element.Quantities["LengthM"] != -0.25d)
                throw new Exception("Semantic selection validation must not mutate malformed quantity state.");
            if (project.ChangeVersion != version)
                throw new Exception("Semantic selection validation must remain read-only when rejecting malformed quantity state.");
        }
    }
}
