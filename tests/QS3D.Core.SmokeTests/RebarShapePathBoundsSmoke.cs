using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarShapePathBoundsSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            MaximumListTextLengthStillParses();
            OversizedListTextFailsClosed();
            MaximumLegCountStillParses();
            OversizedLegCountFailsClosed();
            ExcessTurnCountFailsClosed();
            OrdinaryPresetShapesStillParse();
        }

        private static void MaximumListTextLengthStillParses()
        {
            var legs = "1" + new string(' ', 4095);
            if (legs.Length != 4096) throw new InvalidOperationException("Rebar shape text boundary fixture is invalid.");
            var path = RebarShapePathBuilder.Build("00", 1d, legs);
            if (path.Points.Count != 2 || Math.Abs(path.Points[1].X - 1d) > 1e-12d)
                throw new InvalidOperationException("Maximum supported rebar shape list text no longer parses.");
        }

        private static void OversizedListTextFailsClosed()
        {
            var legs = "1" + new string(' ', 4096);
            if (legs.Length != 4097) throw new InvalidOperationException("Oversized rebar shape text fixture is invalid.");
            Throws<FormatException>(() => RebarShapePathBuilder.Build("00", 1d, legs));
        }

        private static void MaximumLegCountStillParses()
        {
            var legs = string.Join(",", Enumerable.Repeat("1", 32));
            var turns = string.Join(",", Enumerable.Repeat("0", 31));
            var path = RebarShapePathBuilder.Build("CUSTOM", 32d, legs, turns);
            if (path.Points.Count != 33 || Math.Abs(path.Points[path.Points.Count - 1].X - 32d) > 1e-12d)
                throw new InvalidOperationException("Maximum supported rebar shape leg count no longer parses.");
        }

        private static void OversizedLegCountFailsClosed()
        {
            var legs = string.Join(",", Enumerable.Repeat("1", 33));
            var turns = string.Join(",", Enumerable.Repeat("0", 32));
            Throws<InvalidOperationException>(() => RebarShapePathBuilder.Build("CUSTOM", 33d, legs, turns));
        }

        private static void ExcessTurnCountFailsClosed()
        {
            Throws<InvalidOperationException>(() => RebarShapePathBuilder.Build("CUSTOM", 2d, "1,1", "0,0"));
        }

        private static void OrdinaryPresetShapesStillParse()
        {
            var straight = RebarShapePathBuilder.Build(null, 2d);
            var l = RebarShapePathBuilder.Build("L", 2d, "1,1");
            var u = RebarShapePathBuilder.Build("U", 3d, "1,1,1");
            if (straight.Points.Count != 2 || l.Points.Count != 3 || u.Points.Count != 4)
                throw new InvalidOperationException("Ordinary rebar shape presets changed while adding list bounds.");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Expected " + typeof(TException).Name + " but got " + ex.GetType().Name + ".", ex);
            }
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }
}
