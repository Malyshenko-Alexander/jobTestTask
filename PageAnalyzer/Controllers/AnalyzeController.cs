using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using PageAnalyzer.Models;
using PageAnalyzer.Services;

namespace PageAnalyzer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyzeController : ControllerBase
{
    private readonly IAnalyzeService _analyzeService;
    private readonly IValidator<AnalyzeRequest> _validator;

    public AnalyzeController(
        IAnalyzeService analyzeService,
        IValidator<AnalyzeRequest> validator)
    {
        _analyzeService = analyzeService;
        _validator = validator;
    }

    [HttpPost]
    public async Task<ActionResult<AnalyzeResponse>> Post([FromBody] AnalyzeRequest? request)
    {
        if (request is null)
        {
            return Ok(new AnalyzeResponse
            {
                IsError = 1,
                ErrorCode = "VALIDATION_ERROR",
                ErrorMessage = "Request body is required"
            });
        }

        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return Ok(new AnalyzeResponse
            {
                IsError = 1,
                ErrorCode = "VALIDATION_ERROR",
                ErrorMessage = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage))
            });
        }

        var response = await _analyzeService.AnalyzeAsync(request);
        return Ok(response);
    }
}
