using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarNotationIntegerOverflowSmoke
    {
        public static void Run()
        {
            OversizedQuantityIsFormatError();
            OversizedSetCountIsFormatError();
            MultipliedQuantityOverflowIsFormatError();
            Int32MaxQuantityStillParses();
        }

        private static void OversizedQuantityIsFormatError()
        {
            ThrowsFormat(() => RebarNotationParser.Parse("2147483648D16"));
        }

        private static void OversizedSetCountIsFormatError()
        {
            ThrowsFormat(() => RebarNotationParser.Parse("2147483648x1D16"));
        }

        private static void MultipliedQuantityOverflowIsFormatError()
        {
            ThrowsFormat(() => RebarNotationParser.Parse("2147483647x2D16"));
        }

        private static void Int32MaxQuantityStillParses()
        {
            var groups = RebarNotationParser.Parse("2147483647D16");
            if (groups.Count != 1 || groups[0].Quantity != int.MaxValue || Math.Abs(groups[0].DiameterMm - 16d) > 1e-12d)
                throw new InvalidOperationException("Representable maximum rebar quantity no longer parses correctly.");
        }

        private static void ThrowsFormat(Action action)
        {
            try
            {
                action();
            }
            catch (FormatException)
            {
                return;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Expected FormatException but got " + ex.GetType().Name + ".", ex);
            }
            throw new InvalidOperationException("Expected FormatException.");
        }
    }

    internal static class RebarNotationIntegerOverflowSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RebarNotationIntegerOverflowSmoke.Run();
        }
    }
}
