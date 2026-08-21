using Nexus.Domain.Enums;

namespace Nexus.Application.Features.Analytics.Dtos;

public class AgentRunDto
{
    public AgentType AgentType { get; set; }
    public AgentRunStatus Status { get; set; }
    public DateTime RunAt { get; set; }
    public int? DurationMs { get; set; }
    public string? Result { get; set; }
}