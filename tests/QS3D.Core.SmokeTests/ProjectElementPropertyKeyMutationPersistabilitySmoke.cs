using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectElementPropertyKeyMutationPersistabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CanonicalWritesRemainSupported();
            ControlCharacterKeyFailsBeforeMutation();
        }

        private static void CanonicalWritesRemainSupported()
        {
            var element = new ProjectElement("PROP-PERSIST-CANONICAL", ElementCategory.Room);
            element.MarkClean(ElementDirtyFlags.All);

            element.SetProperty("  Comment  ", "ok");

            if (!element.Properties.TryGetValue("Comment", out var value) || !string.Equals(value, "ok", StringComparison.Ordinal))
                throw new Exception("ProjectElement SetProperty did not preserve canonical padded-input normalization.");
            if (element.Properties.Keys.Any(x => !string.Equals(x, "Comment", StringComparison.Ordinal)))
                throw new Exception("ProjectElement SetProperty retained a non-canonical padded property key.");
            if ((element.Dirty & ElementDirtyFlags.Properties) == 0 || (element.Dirty & ElementDirtyFlags.Quantity) == 0)
                throw new Exception("ProjectElement SetProperty did not preserve property/quantity dirty propagation.");

            element.MarkClean(ElementDirtyFlags.All);
            var beforeTimestamp = element.UpdatedUtc;
            element.SetProperty("Comment", "ok");
            if (element.Dirty != ElementDirtyFlags.None || element.UpdatedUtc != beforeTimestamp)
                throw new Exception("ProjectElement SetProperty same-value write stopped being a no-op.");
        }

        private static void ControlCharacterKeyFailsBeforeMutation()
        {
            var element = new ProjectElement("PROP-PERSIST-CONTROL", ElementCategory.Room);
            element.SetProperty("Existing", "keep");
            element.MarkClean(ElementDirtyFlags.All);
            var beforeCount = element.Properties.Count;
            var beforeTimestamp = element.UpdatedUtc;

            try
            {
                element.SetProperty("Broken\u0001Key", "value");
            }
            catch (ArgumentException)
            {
                if (element.Properties.Count != beforeCount)
                    throw new Exception("Rejected control-character property key changed the property count.");
                if (!element.Properties.TryGetValue("Existing", out var existing) || !string.Equals(existing, "keep", StringComparison.Ordinal))
                    throw new Exception("Rejected control-character property key changed existing property state.");
                if (element.Properties.Keys.Any(x => x.Any(char.IsControl)))
                    throw new Exception("Rejected control-character property key was retained in the property map.");
                if (element.Dirty != ElementDirtyFlags.None)
                    throw new Exception("Rejected control-character property key dirtied the element.");
                if (element.UpdatedUtc != beforeTimestamp)
                    throw new Exception("Rejected control-character property key changed the element timestamp.");
                return;
            }

            throw new Exception("ProjectElement SetProperty accepted an embedded control character in the property key.");
        }
    }
}
