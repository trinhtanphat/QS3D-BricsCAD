using System;
using System.Collections.Generic;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class AutoRoomSourceHandleResourceBoundSmoke
    {
        internal static void Run()
        {
            LazyOverLimitEnumerationFailsAtFirstExcessEntry();
            ExactLimitRemainsValid();
            CanonicalNormalizationRemainsStable();
        }

        private static void LazyOverLimitEnumerationFailsAtFirstExcessEntry()
        {
            var enumerated = 0;

            IEnumerable<string> Handles()
            {
                while (true)
                {
                    enumerated++;
                    if (enumerated > 5001)
                        throw new Exception("NormalizeSourceHandles enumerated past the first over-limit entry.");
                    yield return "H" + enumerated;
                }
            }

            try
            {
                AutoRoomLifecycle.NormalizeSourceHandles(Handles());
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("cannot exceed 5000 input entries", StringComparison.Ordinal) < 0)
                    throw new Exception("Auto Room source-handle bound did not preserve the expected diagnostic.");
                if (enumerated != 5001)
                    throw new Exception("Auto Room source-handle bound did not fail at entry 5001.");
                return;
            }

            throw new Exception("Auto Room source-handle normalization must reject more than 5000 input entries.");
        }

        private static void ExactLimitRemainsValid()
        {
            var handles = new List<string>(5000);
            for (var index = 0; index < 5000; index++)
                handles.Add("H" + index.ToString("D4"));

            var normalized = AutoRoomLifecycle.NormalizeSourceHandles(handles);
            var tokens = normalized.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length != 5000 || tokens[0] != "H0000" || tokens[4999] != "H4999")
                throw new Exception("Exactly 5000 Auto Room source handles must remain valid and deterministically ordered.");
        }

        private static void CanonicalNormalizationRemainsStable()
        {
            var normalized = AutoRoomLifecycle.NormalizeSourceHandles(
                new[] { " b2 ", "", "A1", "a1", "  " });
            if (!string.Equals(normalized, "A1;B2", StringComparison.Ordinal))
                throw new Exception("Auto Room source-handle canonical normalization changed.");
        }
    }
}
