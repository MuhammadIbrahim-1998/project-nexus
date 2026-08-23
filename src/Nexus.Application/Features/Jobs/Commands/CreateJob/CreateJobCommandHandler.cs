using MediatR;
using Nexus.Application.Common.Interfaces;
using Nexus.Domain.Entities;

namespace Nexus.Application.Features.Jobs.Commands.CreateJob;

public class CreateJobCommandHandler : IRequestHandler<CreateJobCommand, int>
{
    private readonly INexusDbContext _db;

    public CreateJobCommandHandler(INexusDbContext db) => _db = db;

    public async Task<int> Handle(CreateJobCommand request, CancellationToken cancellationToken)
    {
        var job = new Job
        {
            Title = request.Title,
            Company = request.Company,
            Description = request.Description,
            Source = request.Source,
            SourceUrl = request.SourceUrl,
            Url = request.Url,
            Location = request.Location,
            IsRemote = request.IsRemote,
            SalaryInfo = request.SalaryInfo
        };

        _db.Jobs.Add(job);
        await _db.SaveChangesAsync(cancellationToken);

        return job.Id;
    }
}