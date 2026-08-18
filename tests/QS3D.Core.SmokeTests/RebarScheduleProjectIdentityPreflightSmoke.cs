using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarScheduleProjectIdentityPreflightSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RejectsMalformedIdentityOutsideRebarSubsetBeforeFiltering();
        }

        private static void RejectsMalformedIdentityOutsideRebarSubsetBeforeFiltering()
        {
            var project = new ProjectState("rebar-schedule-project-identity-preflight", "Rebar schedule project identity preflight");

            var validRebar = new ProjectElement("R1", ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            validRebar.Properties["RebarNotation"] = "1D12";
            validRebar.Properties["RebarCuttingLengthM"] = "1";
            project.Elements.Add(validRebar);

            const string hostileId = " legacy-id ";
            var unrelated = new ProjectElement("LEGACY", ElementCategory.Wall, string.Empty, string.Empty, string.Empty);
            CorruptElementIdForLegacyStateTest(unrelated, hostileId);
            project.Elements.Add(unrelated);

            var error = Capture<InvalidOperationException>(() => ProjectRebarScheduleBuilder.Build(project));
            Require(error.Message.IndexOf("noncanonical id", StringComparison.OrdinalIgnoreCase) >= 0,
                "Project rebar schedule did not validate semantic identities before filtering the rebar subset.");
            Require(error.Message.IndexOf(hostileId, StringComparison.Ordinal) < 0,
                "Project rebar schedule preflight echoed a hostile noncanonical identity.");
            Require(error.InnerException is ArgumentException,
                "Project rebar schedule did not preserve the canonical identity validation failure as the inner exception.");
        }

        private static void CorruptElementIdForLegacyStateTest(ProjectElement element, string invalidId)
        {
            var field = typeof(ProjectElement).GetField("<Id>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
                throw new InvalidOperationException("ProjectElement Id backing field was not found for malformed-state smoke setup.");

            field.SetValue(element, invalidId);
            Require(string.Equals(element.Id, invalidId, StringComparison.Ordinal),
                "Malformed-state smoke setup did not preserve the intended noncanonical ElementId.");
        }

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
