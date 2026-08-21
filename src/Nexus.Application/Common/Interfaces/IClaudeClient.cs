namespace Nexus.Application.Common.Interfaces;

public interface IClaudeClient
{
    Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default);
}
