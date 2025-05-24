using Application.Interfaces;

namespace Infrastructure.Parsing
{
    /// <summary>
    /// Factory para obtener el parser apropiado según el broker
    /// </summary>
    public class ParsingServiceFactory : IFileParsingService
    {
        private readonly List<IFileParsingService> _parsers;

        public ParsingServiceFactory(IEnumerable<IFileParsingService> parsers)
        {
            _parsers = parsers.ToList();
        }

        public bool CanParse(string brokerKey, string fileName)
        {
            return GetParser(brokerKey, fileName) != null;
        }

        public async Task<ParsingResult> ParseFileAsync(Stream fileStream, string fileName, string brokerKey, Guid dataPointId)
        {
            var parser = GetParser(brokerKey, fileName);

            if (parser == null)
            {
                return new ParsingResult
                {
                    Errores = new List<string>
                    {
                        $"No se encontró un parser para el broker '{brokerKey}' y archivo '{fileName}'"
                    }
                };
            }

            return await parser.ParseFileAsync(fileStream, fileName, brokerKey, dataPointId);
        }

        public IEnumerable<string> GetSupportedBrokers()
        {
            return _parsers.SelectMany(p => p.GetSupportedBrokers()).Distinct();
        }

        public IEnumerable<string> GetSupportedExtensions()
        {
            return _parsers.SelectMany(p => p.GetSupportedExtensions()).Distinct();
        }

        private IFileParsingService? GetParser(string brokerKey, string fileName)
        {
            return _parsers.FirstOrDefault(p => p.CanParse(brokerKey, fileName));
        }
    }
}