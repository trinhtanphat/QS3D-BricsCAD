using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarNotationBoundsSmoke
    {
        public static void Run()
        {
            MaximumNotationLengthStillParses();
            OversizedNotationFailsClosed();
            MaximumCompoundGroupCountStillParses();
            OversizedCompoundGroupCountFailsClosed();
            OrdinaryCountAndSpacingNotationStillParse();
        }

        private static void MaximumNotationLengthStillParses()
        {
            var notation = "D16" + new string(' ', 4093);
            if (notation.Length != 4096) throw new InvalidOperationException("Rebar notation boundary fixture is invalid.");
            var groups = RebarNotationParser.Parse(notation);
            if (groups.Count != 1 || Math.Abs(groups[0].DiameterMm - 16d) > 1e-12d)
                throw new InvalidOperationException("Maximum supported rebar notation length no longer parses.");
        }

        private static void OversizedNotationFailsClosed()
        {
            var notation = "D16" + new string(' ', 4094);
            if (notation.Length != 4097) throw new InvalidOperationException("Oversized rebar notation fixture is invalid.");
            ThrowsFormat(() => RebarNotationParser.Parse(notation));
        }

        private static void MaximumCompoundGroupCountStillParses()
        {
            var groups = RebarNotationParser.Parse(CompoundNotation(128));
            if (groups.Count != 128) throw new InvalidOperationException("Maximum supported rebar compound group count no longer parses.");
        }

        private static void OversizedCompoundGroupCountFailsClosed()
        {
            ThrowsFormat(() => RebarNotationParser.Parse(CompoundNotation(129)));
        }

        private static void OrdinaryCountAndSpacingNotationStillParse()
        {
            var groups = RebarNotationParser.Parse("2x3D16+D12@200");
            if (groups.Count != 2 || groups[0].Quantity != 6 || Math.Abs(groups[0].DiameterMm - 16d) > 1e-12d)
                throw new InvalidOperationException("Ordinary rebar count notation changed while adding parser bounds.");
            var spacingMm = groups[1].SpacingMm;
            if (!spacingMm.HasValue || Math.Abs(spacingMm.Value - 200d) > 1e-12d || Math.Abs(groups[1].DiameterMm - 12d) > 1e-12d)
                throw new InvalidOperationException("Ordinary rebar spacing notation changed while adding parser bounds.");
        }

        private static string CompoundNotation(int count)
        {
            var parts = new string[count];
            for (var index = 0; index < parts.Length; index++) parts[index] = "D16";
            return string.Join("+", parts);
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

    internal static class RebarNotationBoundsSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RebarNotationBoundsSmoke.Run();
        }
    }
}
