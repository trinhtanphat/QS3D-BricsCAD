using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeRemapUnicodeBoundarySmoke
    {
        internal static void Run()
        {
            ZoneIdBoundaryPreservesSupplementaryScalar();
            FamilyNameBoundaryPreservesSupplementaryScalar();
            BmpBoundaryRemainsStable();
            MalformedUtf16FailsClosed();
        }

        private static void ZoneIdBoundaryPreservesSupplementaryScalar()
        {
            var source = new ProjectState("source", "Source");
            var id = new string('A', 56) + "😀" + new string('B', 8);
            source.Zones.Add(new ZoneDefinition(id, "Source zone"));
            var plan = ProjectInterchangeRemapPlanner.Plan(new ProjectState("target", "Target"), ProjectInterchangeJsonExporter.Build(source));
            var zone = plan.Items.Single(x => x.Kind == InterchangeRemapIdentityKind.Zone);
            True(zone.IdChanged);
            True(zone.TargetId.Length <= 64);
            WellFormed(zone.TargetId);
            True(!zone.TargetId.Contains("\uD83D", StringComparison.Ordinal));
            True(zone.TargetId.EndsWith("-import", StringComparison.Ordinal));
        }

        private static void FamilyNameBoundaryPreservesSupplementaryScalar()
        {
            var target = new ProjectState("target", "Target");
            var source = new ProjectState("source", "Source");
            var name = new string('N', 148) + "😀" + new string('X', 16);
            source.Families.Add(new ProjectFamily("FAMILY", name, ElementCategory.Beam));
            var plan = ProjectInterchangeRemapPlanner.Plan(target, ProjectInterchangeJsonExporter.Build(source));
            var family = plan.Items.Single(x => x.Kind == InterchangeRemapIdentityKind.Family);
            True(family.NameChanged);
            True(family.TargetName.Length <= 160);
            WellFormed(family.TargetName);
            True(family.TargetName.EndsWith(" (Imported)", StringComparison.Ordinal));
        }

        private static void BmpBoundaryRemainsStable()
        {
            var target = new ProjectState("target", "Target");
            target.Zones.Add(new ZoneDefinition("DUP", "Existing"));
            var source = new ProjectState("source", "Source");
            source.Zones.Add(new ZoneDefinition("DUP", "Incoming"));
            var plan = ProjectInterchangeRemapPlanner.Plan(target, ProjectInterchangeJsonExporter.Build(source));
            var zone = plan.Items.Single(x => x.Kind == InterchangeRemapIdentityKind.Zone);
            Equal("DUP-import", zone.TargetId);
            WellFormed(zone.TargetId);
        }

        private static void MalformedUtf16FailsClosed()
        {
            var method = typeof(ProjectInterchangeRemapPlanner).GetMethod("AppendBounded", BindingFlags.NonPublic | BindingFlags.Static);
            True(method != null);
            var malformed = "ABC\uD83DDEF";
            try
            {
                method!.Invoke(null, new object[] { malformed, "-import", 64 });
            }
            catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException)
            {
                return;
            }
            throw new Exception("Expected malformed UTF-16 remap input to fail closed.");
        }

        private static void WellFormed(string value)
        {
            for (var i = 0; i < value.Length; i++)
            {
                if (char.IsHighSurrogate(value[i]))
                {
                    True(i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]));
                    i++;
                }
                else
                {
                    True(!char.IsLowSurrogate(value[i]));
                }
            }
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected condition to be true.");
        }
    }

    internal static class ProjectInterchangeRemapUnicodeBoundarySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectInterchangeRemapUnicodeBoundarySmoke.Run();
    }
}
