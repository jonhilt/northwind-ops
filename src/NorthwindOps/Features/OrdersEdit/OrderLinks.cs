namespace NorthwindOps.Features.OrdersEdit;

/// <summary>
/// Links between the Orders list and an order's page. The list's URL (sort, page, status filter) rides along
/// as returnUrl, so saving or going back lands you on exactly the view you came from.
/// </summary>
public static class OrderLinks
{
    public const string List = "orders";

    /// <summary>A base-relative link to an order, remembering the list view it was opened from.</summary>
    public static string Detail(int orderId, string? listUrl)
    {
        var returnUrl = SafeReturnUrl(listUrl);
        return returnUrl == List
            ? $"orders/{orderId}"
            : $"orders/{orderId}?returnUrl={Uri.EscapeDataString(returnUrl)}";
    }

    /// <summary>
    /// Only ever sends people back to the Orders list. Anything else (another site, another page,
    /// a protocol-relative "//evil.example") falls back to the plain list, so returnUrl can't be used as an open redirect.
    /// </summary>
    public static string SafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrEmpty(returnUrl) || returnUrl.Contains('\\') || returnUrl.Contains("//", StringComparison.Ordinal))
        {
            return List;
        }

        return returnUrl == List || returnUrl.StartsWith(List + "?", StringComparison.Ordinal) ? returnUrl : List;
    }
}
