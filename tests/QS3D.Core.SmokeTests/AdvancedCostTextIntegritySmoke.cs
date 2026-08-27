using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class AdvancedCostTextIntegritySmoke
    {
        internal static void Run()
        {
            ResourceDescriptionFailsClosed();
            TenderDescriptionFailsClosed();
            BidderFailsClosed();
            ValidUnicodeAndInteriorSpacesArePreserved();
        }

        private static void ResourceDescriptionFailsClosed()
        {
            AssertArgument("description", () => Resource(" Leading"), "surrounding whitespace");
            AssertArgument("description", () => Resource("Trailing "), "surrounding whitespace");
            AssertArgument("description", () => Resource("Bad\tControl"), "control characters");
            AssertArgument("description", () => Resource("Bad" + LoneHighSurrogate()), "malformed UTF-16");
            AssertArgument("description", () => Resource(LoneLowSurrogate() + "Bad"), "malformed UTF-16");
        }

        private static void TenderDescriptionFailsClosed()
        {
            AssertArgument("description", () => Requirement(" Leading"), "surrounding whitespace");
            AssertArgument("description", () => Requirement("Trailing "), "surrounding whitespace");
            AssertArgument("description", () => Requirement("Bad\nControl"), "control characters");
            AssertArgument("description", () => Requirement("Bad" + LoneHighSurrogate()), "malformed UTF-16");
            AssertArgument("description", () => Requirement(LoneLowSurrogate() + "Bad"), "malformed UTF-16");
        }

        private static void BidderFailsClosed()
        {
            AssertArgument("bidder", () => Bid(" Leading"), "surrounding whitespace");
            AssertArgument("bidder", () => Bid("Trailing "), "surrounding whitespace");
            AssertArgument("bidder", () => Bid("Bad\rControl"), "control characters");
            AssertArgument("bidder", () => Bid("Bad" + LoneHighSurrogate()), "malformed UTF-16");
            AssertArgument("bidder", () => Bid(LoneLowSurrogate() + "Bad"), "malformed UTF-16");
        }

        private static void ValidUnicodeAndInteriorSpacesArePreserved()
        {
            const string resourceText = "Cốt thép 😀 梁 grade 500";
            const string tenderText = "Ván khuôn 😀 khu vực A";
            const string bidderText = "Nhà thầu 😀 Việt Nam";

            var resource = Resource(resourceText);
            var requirement = Requirement(tenderText);
            var bid = Bid(bidderText);

            Equal(resourceText, resource.Description, "Resource description must preserve valid Unicode and interior spaces exactly.");
            Equal(tenderText, requirement.Description, "Tender description must preserve valid Unicode and interior spaces exactly.");
            Equal(bidderText, bid.Bidder, "Bidder must preserve valid Unicode and interior spaces exactly.");
        }

        private static CostResourceComponent Resource(string description) =>
            new CostResourceComponent("RES-TEXT", description, "kg", 1m, 1m);

        private static TenderRequirement Requirement(string description) =>
            new TenderRequirement("ITEM-TEXT", description, "m2", 1m);

        private static TenderBid Bid(string bidder) =>
            new TenderBid("BID-TEXT", bidder, "VND", Array.Empty<TenderQuoteLine>());

        private static string LoneHighSurrogate() => new string(new[] { '\uD800' });
        private static string LoneLowSurrogate() => new string(new[] { '\uDC00' });

        private static void AssertArgument(string parameterName, Action action, string expectedMessage)
        {
            try
            {
                action();
            }
            catch (ArgumentException ex)
            {
                Equal(parameterName, ex.ParamName, "Advanced cost text rejection must identify the exact parameter.");
                if (ex.Message.IndexOf(expectedMessage, StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException(
                        "Advanced cost text rejection did not report the expected invariant. Expected fragment='" +
                        expectedMessage + "', actual='" + ex.Message + "'.");
                return;
            }

            throw new InvalidOperationException("Advanced cost text boundary accepted non-canonical or malformed input.");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(
                    message + " Expected=" + expected + ", actual=" + actual + ".");
        }
    }

    internal static class AdvancedCostTextIntegrityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            AdvancedCostTextIntegritySmoke.Run();
        }
    }
}
