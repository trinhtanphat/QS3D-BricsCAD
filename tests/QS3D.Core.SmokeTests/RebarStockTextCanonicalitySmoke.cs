using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarStockTextCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            CutIdControlCharactersFailClosedWithoutEcho();
            GroupIdControlCharactersFailClosedWithoutEcho();
            GradeControlCharactersFailClosedWithoutEcho();
            ExistingWhitespaceCanonicalityRemainsClosed();
            DuplicateCutIdentityRemainsCaseInsensitive();
            CanonicalInputsRemainUnchanged();
        }

        private static void CutIdControlCharactersFailClosedWithoutEcho()
        {
            const string hostile = "CUT\n01";
            var error = Capture<ArgumentException>(() => new RebarCutRequirement(hostile, 1.25d, 2));
            NotContains(hostile, error.Message, "Malformed cut identity diagnostics must not echo hostile raw input.");
        }

        private static void GroupIdControlCharactersFailClosedWithoutEcho()
        {
            const string hostile = "GROUP\t01";
            var error = Capture<ArgumentException>(() => Demand(hostile, "B500", new RebarCutRequirement("CUT-01", 1.25d, 2)));
            NotContains(hostile, error.Message, "Malformed group identity diagnostics must not echo hostile raw input.");
        }

        private static void GradeControlCharactersFailClosedWithoutEcho()
        {
            const string hostile = "B500\rX";
            var error = Capture<ArgumentException>(() => Demand("GROUP-01", hostile, new RebarCutRequirement("CUT-01", 1.25d, 2)));
            NotContains(hostile, error.Message, "Malformed grade diagnostics must not echo hostile raw input.");
        }

        private static void ExistingWhitespaceCanonicalityRemainsClosed()
        {
            Capture<ArgumentException>(() => new RebarCutRequirement(" CUT-01", 1.25d, 2));
            Capture<ArgumentException>(() => Demand("GROUP-01 ", "B500", new RebarCutRequirement("CUT-01", 1.25d, 2)));
            Capture<ArgumentException>(() => Demand("GROUP-01", " B500", new RebarCutRequirement("CUT-01", 1.25d, 2)));
        }

        private static void DuplicateCutIdentityRemainsCaseInsensitive()
        {
            var error = Capture<ArgumentException>(() =>
                Demand(
                    "GROUP-01",
                    "B500",
                    new RebarCutRequirement("CUT-01", 1.25d, 1),
                    new RebarCutRequirement("cut-01", 1.50d, 1)));
            Contains("unique (case-insensitive)", error.Message, "Duplicate cut identity semantics changed unexpectedly.");
        }

        private static void CanonicalInputsRemainUnchanged()
        {
            var cut = new RebarCutRequirement("CUT-01", 1.25d, 2);
            var demand = Demand("GROUP-01", "B500", cut);

            Equal("CUT-01", cut.CutId, "Canonical cut identity changed unexpectedly.");
            Equal("GROUP-01", demand.GroupId, "Canonical group identity changed unexpectedly.");
            Equal("B500", demand.Grade, "Canonical grade changed unexpectedly.");
            Equal(1, demand.RequiredCuts.Count, "Canonical cut collection changed unexpectedly.");
            Equal(2L, demand.RequiredCutCount, "Canonical cut quantity changed unexpectedly.");
        }

        private static RebarStockDemand Demand(string groupId, string grade, params RebarCutRequirement[] cuts)
        {
            return new RebarStockDemand(
                groupId,
                grade,
                16d,
                12d,
                cuts,
                new RebarCutAllowancePolicy());
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

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message + " Actual: " + actual);
        }

        private static void NotContains(string unexpected, string actual, string message)
        {
            if (actual != null && actual.IndexOf(unexpected, StringComparison.Ordinal) >= 0)
                throw new InvalidOperationException(message + " Actual: " + actual);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
