namespace VPay.Gateway.Common.UserManagement.Models;

public record ValidateServiceResult(long UserId, string DisplayName, IReadOnlyCollection<Permission> Permissions, AccessGroup? AccessGroup);

public record Permission(long Id, string Name, string DisplayName);

public record AccessGroup(IReadOnlyCollection<string?> ClientFilter, IReadOnlyCollection<string?> BillingEntityFilter, IReadOnlyCollection<string?> BillingEntityExclusions)
{
    // NOTE: There's currently a bug in the User Management API where all of the client codes are returned
    // as a single, comma-delimited value within the array.  This will fix that, but continue to work when
    // that bug is fixed.
    public IReadOnlyCollection<string?> ClientFilter { get; } = ClientFilter
        .SelectMany(x => string.IsNullOrWhiteSpace(x) ? new[] { x } : x.Split(',', System.StringSplitOptions.TrimEntries | System.StringSplitOptions.RemoveEmptyEntries))
        .ToList()
        .AsReadOnly();
}
