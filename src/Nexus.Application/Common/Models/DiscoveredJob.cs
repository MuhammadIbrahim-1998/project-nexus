namespace Nexus.Application.Common.Models;

public record DiscoveredJob(
    string Title,
    string Company,
    string Source,
    string? Location,
    bool IsRemote,
    string? SalaryInfo,
    string? Description,
    string? Url,
    string? SourceUrl);
