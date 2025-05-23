namespace Api.Endpoints.ProcessFile
{
    public class ProcessFileRequest
    {
        public IFormFile File { get; set; } = null!;
        public string BrokerKey { get; set; } = string.Empty;
    }
}