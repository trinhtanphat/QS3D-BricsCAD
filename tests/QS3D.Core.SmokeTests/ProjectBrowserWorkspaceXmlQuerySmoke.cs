using System;
using QS3D.Core.Navigation;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserWorkspaceXmlQuerySmoke
    {
        internal static void Run()
        {
            RejectsForbiddenXmlControlCharacter();
            PreservesLegalUnicodeRoundTrip();
        }

        private static void RejectsForbiddenXmlControlCharacter()
        {
            try
            {
                _ = new ProjectBrowserWorkspaceState(query: "wall\u0001door");
            }
            catch (ArgumentException ex)
            {
                if (!string.Equals(ex.ParamName, "value", StringComparison.Ordinal))
                    throw new InvalidOperationException("Workspace query XML validation must identify the normalized value argument.", ex);
                if (ex.InnerException == null)
                    throw new InvalidOperationException("Workspace query XML validation must preserve the XML validation cause.", ex);
                return;
            }

            throw new InvalidOperationException("Workspace state accepted a query containing a forbidden XML control character.");
        }

        private static void PreservesLegalUnicodeRoundTrip()
        {
            const string query = "Café 漢字 – cửa";
            var store = new ProjectBrowserWorkspaceStateStore();
            var state = new ProjectBrowserWorkspaceState(query: "  " + query + "  ");
            if (!string.Equals(state.Query, query, StringComparison.Ordinal))
                throw new InvalidOperationException("Workspace query canonicalization changed legal Unicode text.");

            var serialized = store.Serialize(state);
            var loaded = store.Deserialize(serialized);
            if (!string.Equals(loaded.Query, query, StringComparison.Ordinal))
                throw new InvalidOperationException("Workspace query legal Unicode text did not round-trip through persisted XML.");
        }
    }
}