using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Navigation;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserSelectionRootMembershipSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var project = new ProjectState("P-BROWSER-ROOT-HASH", "Browser root membership smoke");
            project.Elements.Add(new ProjectElement("E1", ElementCategory.Beam));
            project.Elements.Add(new ProjectElement("E2", ElementCategory.Column));
            var root = ProjectBrowserPlanner.Build(project, ProjectBrowserGrouping.Category);

            var reveal = ProjectBrowserSelectionPlanner.PlanReveal(root, new[] { "e1" }, "E1");
            True(reveal.HasSelection, "case-insensitive root membership");
            Equal(1, reveal.SelectedElementIds.Count, "selected count");
            Equal("e1", reveal.SelectedElementIds[0], "selected spelling");
            Equal("e1", reveal.PrimaryElementId, "primary canonical selection instance");
            Equal(1, reveal.TargetNodePaths.Count, "target path count");

            Throws<InvalidOperationException>(
                () => ProjectBrowserSelectionPlanner.PlanReveal(root, new[] { "MISSING" }),
                "missing root membership");
        }

        private static void True(bool value, string label)
        {
            if (!value) throw new Exception("ProjectBrowserSelectionRootMembershipSmoke expected true: " + label + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("ProjectBrowserSelectionRootMembershipSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action, string label) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("ProjectBrowserSelectionRootMembershipSmoke " + label + ": expected " + typeof(TException).Name + ".");
        }
    }
}
