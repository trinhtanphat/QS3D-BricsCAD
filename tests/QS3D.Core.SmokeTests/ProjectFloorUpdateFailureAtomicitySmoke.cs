using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFloorUpdateFailureAtomicitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var project = new ProjectState("FLOOR-ATOMIC", "Floor update atomicity");
            var floor = ProjectFloorService.Create(project, "FLOOR-1", "Tầng 1", 0d);

            var beforeName = floor.Name;
            var beforeElevation = floor.ElevationM;
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;
            var beforeActiveFloorId = project.ActiveFloorId;
            var beforeFloorCount = project.Floors.Count;

            Throws<ArgumentException>(() => ProjectFloorService.Update(project, floor.Id, "Tầng\n2", 3.6d));

            Equal(beforeName, floor.Name, "floor name after rejected update");
            Equal(beforeElevation, floor.ElevationM, "floor elevation after rejected update");
            Equal(beforeVersion, project.ChangeVersion, "project change version after rejected update");
            Equal(beforeUpdatedUtc, project.UpdatedUtc, "project timestamp after rejected update");
            Equal(beforeActiveFloorId, project.ActiveFloorId, "active Floor after rejected update");
            Equal(beforeFloorCount, project.Floors.Count, "Floor count after rejected update");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("ProjectFloorUpdateFailureAtomicitySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
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

            throw new InvalidOperationException("ProjectFloorUpdateFailureAtomicitySmoke expected " + typeof(TException).Name + ".");
        }
    }
}
