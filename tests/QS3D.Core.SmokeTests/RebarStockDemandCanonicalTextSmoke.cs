using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarStockDemandCanonicalTextSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            CanonicalTextRemainsAccepted();
            CutIdControlCharacterFailsClosed();
            GroupIdControlCharacterFailsClosed();
            GradeControlCharacterFailsClosed();
            PaddedCutIdentityStillFailsClosed();
            DuplicateCutIdentityRemainsCaseInsensitive();
        }

        private static void CanonicalTextRemainsAccepted()
        {
            var cut = new RebarCutRequirement("CUT-01", 1.25d, 2);
            var demand = Demand("GROUP-01", "B500", new[] { cut });

            Assert(cut.CutId == "CUT-01", "Canonical cut identity must be preserved exactly.");
            Assert(demand.GroupId == "GROUP-01", "Canonical group identity must be preserved exactly.");
            Assert(demand.Grade == "B500", "Canonical grade must be preserved exactly.");
            Assert(demand.RequiredCutCount == 2L, "Canonical text validation must not change stock-demand quantities.");
        }

        private static void CutIdControlCharacterFailsClosed()
        {
            const string hostile = "CUT\n01";
            var error = Capture<ArgumentException>(() => new RebarCutRequirement(hostile, 1d, 1));

            Assert(error.Message.StartsWith("Rebar cut identity must not contain control characters.", StringComparison.Ordinal),
                "Control-bearing cut identity must fail with the canonical diagnostic.");
            Assert(!error.Message.Contains(hostile), "Malformed cut-identity diagnostics must not echo hostile raw input.");
        }

        private static void GroupIdControlCharacterFailsClosed()
        {
            const string hostile = "GROUP\r01";
            var error = Capture<ArgumentException>(() => Demand(hostile, "B500", OneCut()));

            Assert(error.Message.StartsWith("Rebar stock-demand identity must not contain control characters.", StringComparison.Ordinal),
                "Control-bearing group identity must fail with the canonical diagnostic.");
            Assert(!error.Message.Contains(hostile), "Malformed group-identity diagnostics must not echo hostile raw input.");
        }

        private static void GradeControlCharacterFailsClosed()
        {
            const string hostile = "B500\tX";
            var error = Capture<ArgumentException>(() => Demand("GROUP-01", hostile, OneCut()));

            Assert(error.Message.StartsWith("Rebar stock-demand identity must not contain control characters.", StringComparison.Ordinal),
                "Control-bearing grade must fail with the canonical diagnostic.");
            Assert(!error.Message.Contains(hostile), "Malformed grade diagnostics must not echo hostile raw input.");
        }

        private static void PaddedCutIdentityStillFailsClosed()
        {
            var error = Capture<ArgumentException>(() => new RebarCutRequirement(" CUT-01", 1d, 1));
            Assert(error.Message.StartsWith("Rebar cut identity must not contain leading or trailing whitespace.", StringComparison.Ordinal),
                "Existing padded cut-identity rejection must remain unchanged.");
        }

        private static void DuplicateCutIdentityRemainsCaseInsensitive()
        {
            var error = Capture<ArgumentException>(() => Demand(
                "GROUP-01",
                "B500",
                new[]
                {
                    new RebarCutRequirement("CUT-A", 1d, 1),
                    new RebarCutRequirement("cut-a", 2d, 1)
                }));

            Assert(error.Message.Contains("unique (case-insensitive)"),
                "Existing case-insensitive duplicate cut-identity contract must remain unchanged.");
        }

        private static RebarCutRequirement[] OneCut()
        {
            return new[] { new RebarCutRequirement("CUT-01", 1d, 1) };
        }

        private static RebarStockDemand Demand(string groupId, string grade, RebarCutRequirement[] cuts)
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

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
