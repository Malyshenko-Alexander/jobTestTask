using PageAnalyzer.Models;

namespace PageAnalyzer.Services;

public interface IAnalyzeService
{
    Task<AnalyzeResponse> AnalyzeAsync(AnalyzeRequest request);
}
