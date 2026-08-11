using System;
using System.Collections.Generic;
using QS3D.Core.Domain;
using QS3D.Core.Navigation;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserQueryOptionBoundsSmoke
    {
        internal static void Run()
        {
            var empty = new ProjectBrowserQueryOptions();
            Equal(0, empty.Categories.Count, "empty categories");
            Equal(0, empty.FloorIds.Count, "empty floor ids");
            Equal(0, empty.ZoneIds.Count, "empty zone ids");

            var normal = new ProjectBrowserQueryOptions(
                query: "wall",
                dirtyOnly: true,
                categories: new[] { ElementCategory.ArchitecturalWall },
                floorIds: new[] { "F1" },
                zoneIds: new[] { "Z1" });
            Equal(ElementCategory.ArchitecturalWall, normal.Categories[0], "normal category");
            Equal("F1", normal.FloorIds[0], "normal floor id");
            Equal("Z1", normal.ZoneIds[0], "normal zone id");

            Throws<InvalidOperationException>(() => new ProjectBrowserQueryOptions(categories: CategoriesPastBound()), "category enumeration bound");
            Throws<InvalidOperationException>(() => new ProjectBrowserQueryOptions(floorIds: StringsPastBound("F")), "floor enumeration bound");
            Throws<InvalidOperationException>(() => new ProjectBrowserQueryOptions(zoneIds: StringsPastBound("Z")), "zone enumeration bound");
        }

        private static IEnumerable<ElementCategory> CategoriesPastBound()
        {
            for (var i = 0; i <= 10000; i++) yield return ElementCategory.ArchitecturalWall;
            throw new EnumerationEscapedBoundException();
        }

        private static IEnumerable<string> StringsPastBound(string prefix)
        {
            for (var i = 0; i <= 10000; i++) yield return prefix + i;
            throw new EnumerationEscapedBoundException();
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void Throws<TException>(Action action, string label) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException(label + ": expected " + typeof(TException).Name + ".");
        }

        private sealed class EnumerationEscapedBoundException : Exception
        {
        }
    }
}