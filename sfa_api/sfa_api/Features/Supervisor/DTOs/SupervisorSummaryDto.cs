namespace sfa_api.Features.Supervisor.DTOs;

public record SupervisorSummaryDto(
    int TotalReps,
    int AssignedReps,
    int BillsToday,
    int NonBillingsToday);
