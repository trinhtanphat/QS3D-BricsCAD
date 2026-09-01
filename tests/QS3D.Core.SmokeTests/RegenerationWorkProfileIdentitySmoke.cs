using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class RegenerationWorkProfileIdentitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            WorkItemIdentityIsCanonicalizedAndUnicodeSafe();
            ProfileIdentityIsCanonicalizedAndUnicodeSafe();
            HostileIdentityTextFailsAtPublicAdmission();
        }

        private static void WorkItemIdentityIsCanonicalizedAndUnicodeSafe()
        {
            var item = Item("  E-\U0001F680  ");
            Equal("E-\U0001F680", item.ElementId);
            Equal("MiXeD-Case", Item("MiXeD-Case").ElementId);
        }

        private static void ProfileIdentityIsCanonicalizedAndUnicodeSafe()
        {
            var profile = Profile("  P-\U0001F680  ");
            Equal("P-\U0001F680", profile.ProjectId);
            Equal("Project-MiXeD", Profile("Project-MiXeD").ProjectId);
        }

        private static void HostileIdentityTextFailsAtPublicAdmission()
        {
            ThrowsIdentity(() => Item(" "), "elementId", "required");
            ThrowsIdentity(() => Profile("\t"), "projectId", "required");
            ThrowsIdentity(() => Item("E-\u0001-X"), "elementId", "control characters");
            ThrowsIdentity(() => Profile("P-\u000B-X"), "projectId", "control characters");
            ThrowsIdentity(() => Item("E-\uD800-X"), "elementId", "malformed UTF-16");
            ThrowsIdentity(() => Item("E-\uDC00-X"), "elementId", "malformed UTF-16");
            ThrowsIdentity(() => Profile("P-\uD800-X"), "projectId", "malformed UTF-16");
            ThrowsIdentity(() => Profile("P-\uFFFF-X"), "projectId", "XML-invalid");
        }

        private static RegenerationWorkItem Item(string elementId) =>
            new RegenerationWorkItem(
                0,
                elementId,
                ElementCategory.Beam,
                ElementDirtyFlags.None,
                0,
                0,
                0);

        private static RegenerationWorkProfile Profile(string projectId) =>
            new RegenerationWorkProfile(
                projectId,
                0L,
                RegenerationWorkScope.Project,
                Array.Empty<string>(),
                0,
                0,
                Array.Empty<RegenerationWorkItem>(),
                Array.Empty<RegenerationCategoryWork>(),
                0,
                0);

        private static void ThrowsIdentity(Action action, string parameterName, string messageFragment)
        {
            try
            {
                action();
            }
            catch (ArgumentException ex)
            {
                Equal(parameterName, ex.ParamName);
                if (ex.Message.IndexOf(messageFragment, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException("Expected identity diagnostic containing '" + messageFragment + "' but got: " + ex.Message);
                return;
            }
            throw new InvalidOperationException("Expected hostile regeneration identity to fail public admission.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
        }
    }
}
