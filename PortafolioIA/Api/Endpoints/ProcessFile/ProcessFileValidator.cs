using FastEndpoints;
using FluentValidation;

namespace Api.Endpoints.ProcessFile
{
    public class ProcessFileValidator : Validator<ProcessFileRequest>
    {
        private static readonly string[] AllowedExtensions = { ".xlsx", ".xls", ".csv" };
        private static readonly string[] SupportedBrokers = { "IOL", "BALANZ", "BULL" };
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB

        public ProcessFileValidator()
        {
            RuleFor(x => x.File)
                .NotNull()
                .WithMessage("El archivo es requerido")
                .Must(BeAValidFile)
                .WithMessage("El archivo no es válido o está vacío")
                .Must(HaveValidExtension)
                .WithMessage($"El archivo debe tener una de las siguientes extensiones: {string.Join(", ", AllowedExtensions)}")
                .Must(BeWithinSizeLimit)
                .WithMessage($"El archivo no puede exceder {MaxFileSizeBytes / (1024 * 1024)}MB");

            RuleFor(x => x.BrokerKey)
                .NotEmpty()
                .WithMessage("La clave del broker es requerida")
                .MaximumLength(50)
                .WithMessage("La clave del broker no puede exceder 50 caracteres")
                .Must(BeSupportedBroker)
                .WithMessage($"El broker debe ser uno de los siguientes: {string.Join(", ", SupportedBrokers)}");
        }

        private static bool BeAValidFile(IFormFile? file)
        {
            return file != null && file.Length > 0;
        }

        private static bool HaveValidExtension(IFormFile? file)
        {
            if (file == null) return false;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return AllowedExtensions.Contains(extension);
        }

        private static bool BeWithinSizeLimit(IFormFile? file)
        {
            return file == null || file.Length <= MaxFileSizeBytes;
        }

        private static bool BeSupportedBroker(string brokerKey)
        {
            return !string.IsNullOrEmpty(brokerKey) &&
                   SupportedBrokers.Contains(brokerKey.ToUpperInvariant());
        }
    }
}