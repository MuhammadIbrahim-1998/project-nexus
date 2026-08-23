using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nexus.Application.Features.Jobs.Commands.CreateJob;
using Nexus.Application.Features.Jobs.Queries.GetAllJobs;
using Nexus.Application.Features.Jobs.Queries.GetJobSuggestions;

namespace Nexus.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IMediator _mediator;

    public JobsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var jobs = await _mediator.Send(new GetAllJobsQuery());
        return Ok(jobs);
    }

    [HttpGet("{id:int}/suggestions")]
    public async Task<IActionResult> GetSuggestions(int id)
    {
        var result = await _mediator.Send(new GetJobSuggestionsQuery(id));
        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateJobCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetAll), new { id }, new { id });
    }
}