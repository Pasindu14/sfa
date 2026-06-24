namespace sfa_api.Features.Users.Requests;

public class UpdateUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? DeviceId { get; set; }
    public int? DistributorId { get; set; }

    // Optimistic concurrency token — client echoes the value it last read
    public uint RowVersion { get; set; }
}
