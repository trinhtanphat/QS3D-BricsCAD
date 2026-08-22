using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class BulkEditCanonicalTargetIdSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            SetPropertyRejectsNonCanonicalTargetIdsWithoutMutation();
            AssignFamilyRejectsNonCanonicalTargetIdsWithoutMutation();
        }

        private static void SetPropertyRejectsNonCanonicalTargetIdsWithoutMutation()
        {
            var project = NewRoomProject(out var element, out _);
            var service = new BulkEditService();

            Equal(1, service.SetProperty(project, new[] { "room-1" }, "Material", "Paint"), "canonical SetProperty count");
            Equal("Paint", element.Properties["Material"], "canonical SetProperty value");

            var beforeVersion = project.ChangeVersion;
            Throws<ArgumentException>(() => service.SetProperty(project, new[] { " room-1 " }, "Material", "Tile"));
            Equal("Paint", element.Properties["Material"], "padded SetProperty value");
            Equal(beforeVersion, project.ChangeVersion, "padded SetProperty version");

            Throws<ArgumentException>(() => service.SetProperty(project, new[] { "   " }, "Material", "Tile"));
            Equal("Paint", element.Properties["Material"], "blank SetProperty value");
            Equal(beforeVersion, project.ChangeVersion, "blank SetProperty version");

            Throws<InvalidOperationException>(() => service.SetProperty(project, new[] { "room-1", "ROOM-1" }, "Material", "Tile"));
            Equal("Paint", element.Properties["Material"], "duplicate SetProperty value");
            Equal(beforeVersion, project.ChangeVersion, "duplicate SetProperty version");

            Throws<System.Collections.Generic.KeyNotFoundException>(() => service.SetProperty(project, new[] { "missing-room" }, "Material", "Tile"));
            Equal("Paint", element.Properties["Material"], "missing SetProperty value");
            Equal(beforeVersion, project.ChangeVersion, "missing SetProperty version");
        }

        private static void AssignFamilyRejectsNonCanonicalTargetIdsWithoutMutation()
        {
            var project = NewRoomProject(out var element, out var replacementFamily);
            var service = new BulkEditService();
            var originalFamilyId = element.FamilyId;
            var beforeVersion = project.ChangeVersion;

            Throws<ArgumentException>(() => service.AssignFamily(project, new[] { " room-1 " }, replacementFamily.Id));
            Equal(originalFamilyId, element.FamilyId, "padded AssignFamily family");
            Equal(beforeVersion, project.ChangeVersion, "padded AssignFamily version");

            Equal(1, service.AssignFamily(project, new[] { "room-1" }, replacementFamily.Id), "canonical AssignFamily count");
            Equal(replacementFamily.Id, element.FamilyId, "canonical AssignFamily family");
        }

        private static ProjectState NewRoomProject(out ProjectElement element, out ProjectFamily replacementFamily)
        {
            var project = new ProjectState("P-BULK-CANONICAL", "Bulk canonical target regression");
            var originalFamily = new ProjectFamily("room-old", "Room Old", ElementCategory.Room);
            replacementFamily = new ProjectFamily("room-new", "Room New", ElementCategory.Room);
            project.Families.Add(originalFamily);
            project.Families.Add(replacementFamily);
            element = new ProjectElement("room-1", ElementCategory.Room, originalFamily.Id, "floor-0", "zone-1");
            project.Elements.Add(element);
            return project;
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("BulkEditCanonicalTargetIdSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
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

            throw new InvalidOperationException("BulkEditCanonicalTargetIdSmoke expected " + typeof(TException).Name + ".");
        }
    }
}
