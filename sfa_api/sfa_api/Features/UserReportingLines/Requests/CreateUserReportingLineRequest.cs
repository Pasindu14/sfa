namespace sfa_api.Features.UserReportingLines.Requests;

public class CreateUserReportingLineRequest
{
    public int UserId { get; set; }
    public int ReportsToUserId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
}
