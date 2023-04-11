using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace PayrollEngine.Client.Test
{
    internal static class DictionaryExtensions
    {
        /// <summary>Find the first invalid attribute</summary>
        /// <param name="expectedAttributes">The expected attribute values</param>
        /// <param name="actualAttributes">The actual attribute values</param>
        /// <returns>A tuple with the attribute name, the expected value and the actual value</returns>
        internal static Tuple<string, object, object> FirstInvalidAttribute(this IDictionary<string, object> expectedAttributes,
            IDictionary<string, object> actualAttributes)
        {
            // no attributes test
            if (expectedAttributes == null || !expectedAttributes.Any())
            {
                return null;
            }

            // missing attributes
            if (actualAttributes == null || !actualAttributes.Any())
            {
                return new(expectedAttributes.First().Key, expectedAttributes.First(), null);
            }

            foreach (var expectedAttribute in expectedAttributes)
            {
                // missing attribute
                if (!actualAttributes.ContainsKey(expectedAttribute.Key))
                {
                    return new(expectedAttribute.Key, expectedAttribute.Value, null);
                }

                // expected attribute value
                var expectedValue = expectedAttribute.Value;
                if (expectedValue is JsonElement expectedJsonValue)
                {
                    expectedValue = expectedJsonValue.GetValue();
                }
                // actual attribute value
                var actualValue = actualAttributes[expectedAttribute.Key];
                if (actualValue is JsonElement actualJsonValue)
                {
                    actualValue = actualJsonValue.GetValue();
                }

                // invalid attribute value
                if (!Equals(expectedValue, actualValue))
                {
                    return new(expectedAttribute.Key, expectedValue, actualValue);
                }
            }
            return null;
        }
    }
}
