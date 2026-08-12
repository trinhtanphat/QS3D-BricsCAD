using System;
using System.IO;
using System.Xml.Linq;
using QS3D.Core.Navigation;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserWorkspaceBooleanCanonicalitySmoke
    {
        public static void Run()
        {
            var store = new ProjectBrowserWorkspaceStateStore();

            var falseXml = store.Serialize(new ProjectBrowserWorkspaceState(dirtyOnly: false));
            var falseState = store.Deserialize(falseXml);
            Equal(false, falseState.DirtyOnly);

            var trueXml = store.Serialize(new ProjectBrowserWorkspaceState(dirtyOnly: true));
            var trueState = store.Deserialize(trueXml);
            Equal(true, trueState.DirtyOnly);

            var mixedCaseTrue = XDocument.Parse(trueXml);
            mixedCaseTrue.Root!.SetAttributeValue("dirtyOnly", "True");
            Throws<InvalidDataException>(() => store.Deserialize(mixedCaseTrue.ToString(SaveOptions.DisableFormatting)));

            var upperFalse = XDocument.Parse(falseXml);
            upperFalse.Root!.SetAttributeValue("dirtyOnly", "FALSE");
            Throws<InvalidDataException>(() => store.Deserialize(upperFalse.ToString(SaveOptions.DisableFormatting)));
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
