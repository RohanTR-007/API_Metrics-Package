namespace ApiStats.Dashboard.Models;

public class ErrorRecord
{
    public DateTime UtcTime { get; init; } = DateTime.UtcNow;
    public string Endpoint { get; init; } = "";
    public string Method { get; init; } = "";
    public string Message { get; init; } = "";
    public string StackTrace { get; init; } = "";
    public string MaskedQueryString { get; init; } = "";
    public IDictionary<string, string> MaskedHeaders { get; init; } = new Dictionary<string, string>();
}
