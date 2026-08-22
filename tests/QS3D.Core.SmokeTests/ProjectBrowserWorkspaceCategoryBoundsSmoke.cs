using System;
using System.Collections.Generic;
using QS3D.Core.Domain;
using QS3D.Core.Navigation;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserWorkspaceCategoryBoundsSmoke
    {
        internal static void Run()
        {
            var normal = new ProjectBrowserWorkspaceState(categories: new[]
            {
                ElementCategory.StructuralWall,
                ElementCategory.Beam,
                ElementCategory.StructuralWall
            });
            if (normal.Categories.Count != 2 ||
                normal.Categories[0] != ElementCategory.Beam ||
                normal.Categories[1] != ElementCategory.StructuralWall)
                throw new InvalidOperationException("Workspace categories must remain sorted and deduplicated.");

            var atLimit = new ProjectBrowserWorkspaceState(categories: Repeat(ElementCategory.Beam, 10000));
            if (atLimit.Categories.Count != 1 || atLimit.Categories[0] != ElementCategory.Beam)
                throw new InvalidOperationException("Workspace category input at the existing filter limit must remain valid.");

            Throws<InvalidOperationException>(
                () => new ProjectBrowserWorkspaceState(categories: EscapesBound()),
                "workspace category enumeration bound");

            Throws<ArgumentOutOfRangeException>(
                () => new ProjectBrowserWorkspaceState(categories: new[] { (ElementCategory)int.MaxValue }),
                "workspace undefined category guard");
        }

        private static IEnumerable<ElementCategory> Repeat(ElementCategory value, int count)
        {
            for (var index = 0; index < count; index++) yield return value;
        }

        private static IEnumerable<ElementCategory> EscapesBound()
        {
            for (var index = 0; index <= 10000; index++) yield return ElementCategory.Beam;
            throw new EnumerationEscapedBoundException();
        }

        private static void Throws<TException>(Action action, string label) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex) when (!(ex is EnumerationEscapedBoundException))
            {
                return;
            }

            throw new InvalidOperationException(label + ": expected " + typeof(TException).Name + " before enumeration escaped the bound.");
        }

        private sealed class EnumerationEscapedBoundException : InvalidOperationException
        {
        }
    }
}
