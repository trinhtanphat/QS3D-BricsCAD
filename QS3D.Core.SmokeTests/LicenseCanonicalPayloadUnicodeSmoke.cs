using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Licensing;

namespace QS3D.Core.SmokeTests
{
    internal static class LicenseCanonicalPayloadUnicodeSmoke
    {
        public static void Run()
        {
            MalformedScalarTokensAreRejected();
            MalformedFeatureTokensAreRejected();
            ValidSupplementaryUnicodeIsDeterministic();
        }

        private static void MalformedScalarTokensAreRejected()
        {
            var first = ValidDocument();
            first.ProductId = "QS3D-\uD800";
            Throws<InvalidDataException>(() => first.CanonicalPayload());

            var second = ValidDocument();
            second.ProductId = "QS3D-\uD801";
            Throws<InvalidDataException>(() => second.CanonicalPayload());

            var low = ValidDocument();
            low.Nonce = "NONCE-\uDC00";
            Throws<InvalidDataException>(() => low.Validate());
        }

        private static void MalformedFeatureTokensAreRejected()
        {
            var license = ValidDocument();
            license.Features.Add("Feature-\uD800");
            Throws<InvalidDataException>(() => license.CanonicalPayload());
        }

        private static void ValidSupplementaryUnicodeIsDeterministic()
        {
            const string scalar = "\uD83E\uDDF1";
            var license = ValidDocument();
            license.CustomerId = "Customer-" + scalar;
            license.Features.Add("Feature-" + scalar);

            var first = license.CanonicalPayload();
            var second = license.CanonicalPayload();
            if (!first.SequenceEqual(second))
                throw new InvalidOperationException("Valid supplementary Unicode produced non-deterministic license canonical payload bytes.");
            if (first.Length == 0)
                throw new InvalidOperationException("Valid supplementary Unicode produced an empty license canonical payload.");
        }

        private static LicenseDocument ValidDocument()
        {
            return new LicenseDocument
            {
                LicenseId = "LIC-UNICODE",
                CustomerId = "CUSTOMER",
                ProductId = "QS3D",
                NotBeforeUtc = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
                ExpiresUtc = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                Nonce = "NONCE"
            };
        }

        private static TException Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Expected " + typeof(TException).Name + " but got " + ex.GetType().Name + ".", ex);
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }

    internal static class LicenseCanonicalPayloadUnicodeSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            LicenseCanonicalPayloadUnicodeSmoke.Run();
        }
    }
}
