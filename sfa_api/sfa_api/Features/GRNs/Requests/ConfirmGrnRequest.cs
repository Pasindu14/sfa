namespace sfa_api.Features.GRNs.Requests;

public record ConfirmGrnRequest(
    DateTime ReceivedAt,
    string? Notes = null
);
