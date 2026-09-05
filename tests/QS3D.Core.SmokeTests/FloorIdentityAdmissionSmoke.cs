using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class FloorIdentityAdmissionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsUnicodeCanonicalAliasAtCreate();
            RejectsUnicodeCanonicalAliasInPersistedProjectValidation();
        }

        private static void RejectsUnicodeCanonicalAliasAtCreate()
        {
            const string composedId = "LEVEL-\u00C9";
            const string decomposedId = "LEVEL-E\u0301";

            var composedOwner = FloorGeneratedIdentityPlanner.BuildOwnerToken(composedId);
            var decomposedOwner = FloorGeneratedIdentityPlanner.BuildOwnerToken(decomposedId);
            Equal(composedOwner, decomposedOwner, "canonical owner token precondition");

            var project = new ProjectState("P-FLOOR-UNICODE-ADMISSION", "Floor Unicode admission regression");
            ProjectFloorService.Create(project, composedId, "Composed floor", 0d);
            Equal(composedId, project.Floors[0].Id, "stored caller spelling");
            var beforeRejectedAlias = project.ChangeVersion;

            Throws<InvalidOperationException>(() =>
                ProjectFloorService.Create(project, decomposedId, "Decomposed floor", 3d));

            Equal(1, project.Floors.Count, "floor count after rejected alias");
            Equal(beforeRejectedAlias, project.ChangeVersion, "change version after rejected alias");
        }

        private static void RejectsUnicodeCanonicalAliasInPersistedProjectValidation()
        {
            const string composedId = "LEVEL-\u00C9";
            const string decomposedId = "LEVEL-E\u0301";

            var project = new ProjectState("P-FLOOR-UNICODE-PERSISTED", "Persisted floor Unicode regression");
            project.Floors.Add(new FloorDefinition(composedId, "Composed floor", 0d));
            project.Floors.Add(new FloorDefinition(decomposedId, "Decomposed floor", 3d));
            var beforeValidation = project.ChangeVersion;

            Throws<InvalidOperationException>(() =>
                ProjectFloorService.ReferenceCount(project, composedId));

            Equal(2, project.Floors.Count, "persisted floor count after rejected validation");
            Equal(beforeValidation, project.ChangeVersion, "change version after rejected persisted aliases");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(
                    "FloorIdentityAdmissionSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
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

            throw new InvalidOperationException(
                "FloorIdentityAdmissionSmoke expected " + typeof(TException).Name + ".");
        }
    }
}
