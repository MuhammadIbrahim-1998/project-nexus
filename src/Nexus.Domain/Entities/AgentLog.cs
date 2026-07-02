using Nexus.Domain.Common;
using Nexus.Domain.Enums;

namespace Nexus.Domain.Entities;

public class AgentLog : BaseEntity
{
    public AgentType AgentType { get; set; }
    public AgentRunStatus Status { get; set; }
    public DateTime RunAt { get; set; } = DateTime.UtcNow;
    public int? DurationMs { get; set; }
    public string? Result { get; set; }
    public string? ErrorMessage { get; set; }

    public int? JobId { get; set; }
    public Job? Job { get; set; }
}