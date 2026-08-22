using System;
using System.IO;
using System.Xml.Linq;
using QS3D.Core.Navigation;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserWorkspaceQueryCanonicalitySmoke
    {
        public static void Run()
        {
            var store = new ProjectBrowserWorkspaceStateStore();

            var canonical = store.Serialize(new ProjectBrowserWorkspaceState(query: "beam"));
            Equal("beam", store.Deserialize(canonical).Query);

            var empty = store.Serialize(new ProjectBrowserWorkspaceState(query: string.Empty));
            Equal(string.Empty, store.Deserialize(empty).Query);

            var padded = XDocument.Parse(canonical);
            padded.Root!.SetAttributeValue("query", "  beam  ");
            Throws<InvalidDataException>(() => store.Deserialize(padded.ToString(SaveOptions.DisableFormatting)));

            var whitespace = XDocument.Parse(canonical);
            whitespace.Root!.SetAttributeValue("query", "   \t   ");
            Throws<InvalidDataException>(() => store.Deserialize(whitespace.ToString(SaveOptions.DisableFormatting)));
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
