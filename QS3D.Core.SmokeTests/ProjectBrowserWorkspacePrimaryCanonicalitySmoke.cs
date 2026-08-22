using System;
using System.IO;
using System.Xml.Linq;
using QS3D.Core.Navigation;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserWorkspacePrimaryCanonicalitySmoke
    {
        public static void Run()
        {
            var store = new ProjectBrowserWorkspaceStateStore();

            var canonical = store.Serialize(new ProjectBrowserWorkspaceState(
                selectedElementIds: new[] { "E-1" },
                primaryElementId: "E-1"));
            var canonicalState = store.Deserialize(canonical);
            Equal("E-1", canonicalState.PrimaryElementId);

            var empty = store.Serialize(new ProjectBrowserWorkspaceState());
            Equal(string.Empty, store.Deserialize(empty).PrimaryElementId);

            var blankWithSelection = XDocument.Parse(canonical);
            blankWithSelection.Root!.SetAttributeValue("primaryElementId", string.Empty);
            Throws<InvalidDataException>(() => store.Deserialize(blankWithSelection.ToString(SaveOptions.DisableFormatting)));

            var caseVaried = XDocument.Parse(canonical);
            caseVaried.Root!.SetAttributeValue("primaryElementId", "e-1");
            Throws<InvalidDataException>(() => store.Deserialize(caseVaried.ToString(SaveOptions.DisableFormatting)));
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected '" + expected + "', got '" + actual + "'.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
