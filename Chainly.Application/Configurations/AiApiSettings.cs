namespace Chainly.Application.Configurations
{
    public class AiApiSettings
    {
        public string BaseUrl { get; set; }
        public string InsightsBaseUrl { get; set; }
        public bool SkipInsightsApiCalls { get; set; }
    }
}
