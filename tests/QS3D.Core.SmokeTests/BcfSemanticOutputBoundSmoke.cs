using System;
using System.Collections.Generic;
using System.IO;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class BcfSemanticOutputBoundSmoke
    {
        internal static void Run()
        {
            IndividuallyBoundedFieldsCannotExceedAggregateSemanticOutput();
        }

        private static void IndividuallyBoundedFieldsCannotExceedAggregateSemanticOutput()
        {
            var field = new string('T', BcfIssueExchangeSerializer.MaxFreeTextCharacters);
            var topics = new List<BcfTopic>();
            for (var index = 1; index <= 129; index++)
            {
                topics.Add(
                    new BcfTopic(
                        new Guid(index, 0, 0, new byte[8]).ToString("D"),
                        field,
                        "Open",
                        "Coordination",
                        field,
                        "qa@qs3d",
                        new DateTime(2026, 8, 25, 0, 0, 0, DateTimeKind.Utc),
                        Array.Empty<BcfComment>(),
                        Array.Empty<BcfViewpoint>()));
            }

            var exchange = BcfIssueExchange.Create(topics);
            try
            {
                BcfIssueExchangeSerializer.Serialize(exchange);
            }
            catch (InvalidDataException exception)
            {
                if (!string.Equals(exception.Message, "BCF payload exceeds the bounded semantic XML size.", StringComparison.Ordinal))
                    throw new Exception("Aggregate BCF serializer overflow must fail through the semantic XML size contract.", exception);
                return;
            }

            throw new Exception("Individually valid BCF free-text fields must not aggregate into serializer output above the semantic XML size contract.");
        }
    }
}
