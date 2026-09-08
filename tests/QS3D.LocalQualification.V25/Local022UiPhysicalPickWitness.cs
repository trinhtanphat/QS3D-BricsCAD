using System;

namespace QS3D.LocalQualification
{
    // Host-independent correlation of an independently chosen target with the
    // Editor result observed before production placement. It never reads or
    // constructs a generated solid and cannot change the accepted CAD point.
    internal sealed class PhysicalPickWitness
    {
        internal readonly struct Point
        {
            internal Point(double x, double y, double z) { X = x; Y = y; Z = z; }
            internal double X { get; }
            internal double Y { get; }
            internal double Z { get; }
        }

        private int _sequence;
        private int _semanticBaseline;
        private Point _target;
        private Point? _accepted;

        internal bool IsArmed => _sequence != 0;

        internal void Arm(int sequence, Point target, int semanticBaseline)
        {
            if (IsArmed) throw new InvalidOperationException("pick_already_armed");
            if (sequence < 1 || sequence > 100) throw new InvalidOperationException("pick_sequence_invalid");
            RequireFinite(target);
            if (semanticBaseline < 0) throw new InvalidOperationException("pick_baseline_invalid");
            _sequence = sequence;
            _target = target;
            _semanticBaseline = semanticBaseline;
        }

        internal void Observe(int sequence, Point result, int semanticCount, bool sameContext, bool cursorMatches)
        {
            if (!IsArmed) throw new InvalidOperationException("pick_not_armed");
            if (_accepted.HasValue) throw new InvalidOperationException("pick_duplicate_result");
            if (sequence != _sequence) throw new InvalidOperationException("pick_sequence_mismatch");
            if (!sameContext) throw new InvalidOperationException("pick_context_changed");
            if (!cursorMatches) throw new InvalidOperationException("pick_cursor_mismatch");
            if (semanticCount != _semanticBaseline) throw new InvalidOperationException("pick_geometry_preexists");
            RequireFinite(result);
            if (!Near(result.X, _target.X) || !Near(result.Y, _target.Y) || !Near(result.Z, _target.Z))
                throw new InvalidOperationException("pick_target_mismatch");
            _accepted = result;
        }

        internal Point RequireAccepted()
        {
            if (!_accepted.HasValue) throw new InvalidOperationException("pick_not_observed");
            return _accepted.Value;
        }

        internal void Reset()
        {
            _sequence = 0;
            _semanticBaseline = 0;
            _target = default;
            _accepted = null;
        }

        private static bool Near(double a, double b) => Math.Abs(a - b) <= 1e-7;
        private static void RequireFinite(Point point)
        {
            if (double.IsNaN(point.X) || double.IsInfinity(point.X) ||
                double.IsNaN(point.Y) || double.IsInfinity(point.Y) ||
                double.IsNaN(point.Z) || double.IsInfinity(point.Z))
                throw new InvalidOperationException("pick_nonfinite_point");
        }
    }
}
