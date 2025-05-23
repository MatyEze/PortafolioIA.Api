

using Microsoft.AspNetCore.Http;

namespace Application.Dto
{
    public class ProcessFileRequest
    {
        public IFormFile File { get; set; } = null!;
        public string BrokerKey { get; set; } = string.Empty;
    }
}