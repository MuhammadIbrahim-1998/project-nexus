using Nexus.Domain.Common;

namespace Nexus.Domain.Entities;

public class ApiUsageLog : BaseEntity
{
    public int? AgentLogId { get; set; }
    public AgentLog? AgentLog { get; set; }

    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;

    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens { get; set; }

    public int ResponseTimeMs { get; set; }
    public decimal? EstimatedCostUsd { get; set; }
}