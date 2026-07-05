namespace ClaimsFence;

internal static class ClaimFenceGuard
{
    internal static string RequireText(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be null or empty.", paramName);
        }

        return value;
    }

    internal static string[] RequireTextArray(string[] values, string paramName)
    {
        if (values is null || values.Length == 0)
        {
            throw new ArgumentException("At least one value must be supplied.", paramName);
        }

        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Values must not contain null or empty entries.", paramName);
            }
        }

        return values.ToArray();
    }
}
