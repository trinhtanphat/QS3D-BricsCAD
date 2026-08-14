using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectZoneUpdateFailureAtomicitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var project = new ProjectState("ZONE-ATOMIC", "Zone update atomicity");
            var zone = ProjectZoneService.Create(project, "ZONE-1", "Khu A");

            var beforeName = zone.Name;
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;
            var beforeActiveZoneId = project.ActiveZoneId;
            var beforeZoneCount = project.Zones.Count;

            Throws<ArgumentException>(() => ProjectZoneService.Update(project, zone.Id, "Khu\nB"));

            Equal(beforeName, zone.Name, "zone name after rejected update");
            Equal(beforeVersion, project.ChangeVersion, "project change version after rejected update");
            Equal(beforeUpdatedUtc, project.UpdatedUtc, "project timestamp after rejected update");
            Equal(beforeActiveZoneId, project.ActiveZoneId, "active Zone after rejected update");
            Equal(beforeZoneCount, project.Zones.Count, "Zone count after rejected update");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("ProjectZoneUpdateFailureAtomicitySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("ProjectZoneUpdateFailureAtomicitySmoke expected " + typeof(TException).Name + ".");
        }
    }
}
