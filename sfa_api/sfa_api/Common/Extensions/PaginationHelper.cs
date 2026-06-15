namespace sfa_api.Common.Extensions;

/// <summary>
/// Normalizes client-supplied paging input so no endpoint can request a negative offset
/// or pull an unbounded number of rows in a single response.
/// </summary>
public static class PaginationHelper
{
    public const int DefaultMaxPageSize = 200;

    /// <summary>
    /// Clamps <paramref name="page"/> to a minimum of 1 and <paramref name="pageSize"/> to
    /// the range [1, <paramref name="maxPageSize"/>], and returns the resulting zero-based skip.
    /// </summary>
    public static (int Page, int PageSize, int Skip) Normalize(
        int page, int pageSize, int maxPageSize = DefaultMaxPageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = Math.Clamp(pageSize, 1, maxPageSize);
        return (page, pageSize, (page - 1) * pageSize);
    }

    /// <summary>
    /// Clamps page/pageSize without computing skip — for callers that want to reassign
    /// their own parameters: <c>(page, pageSize) = PaginationHelper.Clamp(page, pageSize);</c>
    /// </summary>
    public static (int Page, int PageSize) Clamp(
        int page, int pageSize, int maxPageSize = DefaultMaxPageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = Math.Clamp(pageSize, 1, maxPageSize);
        return (page, pageSize);
    }
}
