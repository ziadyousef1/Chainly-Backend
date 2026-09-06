namespace Chainly.Domain.Constants;

public static class DemoAccounts
{
    public static readonly HashSet<string> Emails = new(StringComparer.OrdinalIgnoreCase)
    {
        "manager@chainly.site",
        "customer@chainly.site",
    };
}
