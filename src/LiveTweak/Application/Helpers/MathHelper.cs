using System.Numerics;

namespace LiveTweak.Application.Helpers;

internal static class MathHelper
{

    internal static (bool, string?) IsWithinRange<T>(string value, double? min, double? max) where T : INumber<T>
    {
        if (T.TryParse(value, default, out var parsedValue))
        {
            var converted = Convert.ToDouble(parsedValue);
            if (min.HasValue && !double.IsNaN(min.Value) && converted < min)
            {
                return (false, $"Value below minimum of {min}");
            }

            if (max.HasValue && !double.IsNaN(max.Value) && converted > max)
            {
                return (false, $"Value exceeds maximum of {max}");
            }

            return (true, null);
        }


        return (false, "Invalid number format");
    }
}
