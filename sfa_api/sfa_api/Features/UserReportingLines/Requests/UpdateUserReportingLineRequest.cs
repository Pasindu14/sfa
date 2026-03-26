namespace sfa_api.Features.UserReportingLines.Requests;

public class UpdateUserReportingLineRequest
{
    public int ReportsToUserId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
}
