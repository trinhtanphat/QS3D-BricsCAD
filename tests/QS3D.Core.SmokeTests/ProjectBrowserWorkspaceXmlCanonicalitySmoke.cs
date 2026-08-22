using System;
using System.IO;
using QS3D.Core.Navigation;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserWorkspaceXmlCanonicalitySmoke
    {
        internal static void Run()
        {
            CanonicalXmlStillLoads();
            XmlDeclarationStillLoads();
            RejectsItemCData();
            RejectsRootWhitespaceCData();
            RejectsDocumentComment();
            RejectsDocumentProcessingInstruction();
        }

        private static void CanonicalXmlStillLoads()
        {
            var store = new ProjectBrowserWorkspaceStateStore();
            var canonical = CanonicalXml(store);
            var loaded = store.Deserialize(canonical);
            Require(loaded.FloorIds.Count == 1 && loaded.FloorIds[0] == "F1",
                "Canonical Project Browser workspace XML did not round-trip its floor filter.");
        }

        private static void XmlDeclarationStillLoads()
        {
            var store = new ProjectBrowserWorkspaceStateStore();
            var loaded = store.Deserialize("<?xml version=\"1.0\"?>" + CanonicalXml(store));
            Require(loaded.FloorIds.Count == 1 && loaded.FloorIds[0] == "F1",
                "XML declaration changed Project Browser workspace state.");
        }

        private static void RejectsItemCData()
        {
            var store = new ProjectBrowserWorkspaceStateStore();
            var canonical = CanonicalXml(store);
            var mutated = canonical.Replace("<Id>F1</Id>", "<Id><![CDATA[F1]]></Id>", StringComparison.Ordinal);
            Require(!string.Equals(mutated, canonical, StringComparison.Ordinal),
                "Project Browser item CDATA fixture did not mutate canonical XML.");
            Throws<InvalidDataException>(() => store.Deserialize(mutated));
        }

        private static void RejectsRootWhitespaceCData()
        {
            var store = new ProjectBrowserWorkspaceStateStore();
            var canonical = CanonicalXml(store);
            var mutated = canonical.Replace("<FloorIds>", "<![CDATA[ ]]><FloorIds>", StringComparison.Ordinal);
            Require(!string.Equals(mutated, canonical, StringComparison.Ordinal),
                "Project Browser root CDATA fixture did not mutate canonical XML.");
            Throws<InvalidDataException>(() => store.Deserialize(mutated));
        }

        private static void RejectsDocumentComment()
        {
            var store = new ProjectBrowserWorkspaceStateStore();
            Throws<InvalidDataException>(() => store.Deserialize("<!--non-canonical-->" + CanonicalXml(store)));
        }

        private static void RejectsDocumentProcessingInstruction()
        {
            var store = new ProjectBrowserWorkspaceStateStore();
            Throws<InvalidDataException>(() => store.Deserialize("<?qs3d non-canonical?>" + CanonicalXml(store)));
        }

        private static string CanonicalXml(ProjectBrowserWorkspaceStateStore store) =>
            store.Serialize(new ProjectBrowserWorkspaceState(floorIds: new[] { "F1" }));

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }
    }
}
