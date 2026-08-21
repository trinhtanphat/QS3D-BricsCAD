using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Takeoff;

namespace QS3D.Core.SmokeTests
{
    internal static class TakeoffResultHandleCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            var canonical = new TakeoffResult("H1", TakeoffKind.Length, 1d, "m");
            if (!string.Equals(canonical.Handle, "H1", StringComparison.Ordinal))
                throw new InvalidOperationException("Canonical takeoff handle must be preserved exactly.");

            AssertRejected(" H1 ");
            AssertRejected("H1 ");
            AssertRejected(" H1");
            AssertRejected("\tH1");
            AssertRejected("H1\t");
            AssertRejected("\rH1");
            AssertRejected("H1\n");
            AssertRejected("H\u0001X");
            AssertRejected("   ");

            var signedZero = new TakeoffResult("H2", TakeoffKind.Length, -0d, " m ");
            if (BitConverter.DoubleToInt64Bits(signedZero.Value) != BitConverter.DoubleToInt64Bits(0d))
                throw new InvalidOperationException("Takeoff signed-zero canonicalization regressed.");
            if (!string.Equals(signedZero.Unit, "m", StringComparison.Ordinal))
                throw new InvalidOperationException("Takeoff unit normalization regressed.");
        }

        private static void AssertRejected(string handle)
        {
            try
            {
                _ = new TakeoffResult(handle, TakeoffKind.Length, 1d, "m");
            }
            catch (ArgumentException)
            {
                return;
            }

            throw new InvalidOperationException("Non-canonical takeoff handle was accepted.");
        }
    }
}