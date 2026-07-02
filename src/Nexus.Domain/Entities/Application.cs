using Nexus.Domain.Common;
using Nexus.Domain.Enums;

namespace Nexus.Domain.Entities;

public class Application : BaseEntity
{
    public int JobId { get; set; }
    public Job Job { get; set; } = default!;

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Draft;
    public DateTime? AppliedDate { get; set; }
    public string? CvVersionUsed { get; set; }
    public string? CoverLetterVersion { get; set; }
    public string? Notes { get; set; }
}