using MediatR;

namespace Nexus.Application.Features.Jobs.Commands.CreateJob;

public record CreateJobCommand : IRequest<int>
{
    public string Title { get; init; } = default!;
    public string Company { get; init; } = default!;
    public string? Description { get; init; }
    public string Source { get; init; } = default!;
    public string? SourceUrl { get; init; }
    public string? Url { get; init; }
    public string? Location { get; init; }
    public bool IsRemote { get; init; } = true;
    public string? SalaryInfo { get; init; }
}