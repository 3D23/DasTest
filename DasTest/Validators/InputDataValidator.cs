using DasTest.DTO;
using FluentValidation;

namespace DasTest.Validators
{
    public class InputDataValidator : AbstractValidator<InputData>
    {
        public InputDataValidator()
        { 
            RuleFor(input => input.Selector)
                .NotNull().NotEmpty().WithMessage(ErrorCodes.EmptySelector);
            
            RuleFor(input => input.Attribure)
                .NotNull().NotEmpty().WithMessage(ErrorCodes.EmptyAttribute);
            
            RuleFor(input => input.UrlBase64)
                .NotNull().NotEmpty().Must(BeValidBase64).WithMessage(ErrorCodes.MissingUrl);

            RuleFor(input => input.EncryptedTextBytesBase64)
                .NotNull().NotEmpty().Must(BeValidBase64).WithMessage(ErrorCodes.MissingEncryptedText);

            RuleFor(input => input.KeyBytesBase64)
                .NotNull().NotEmpty().Must(BeValidBase64).WithMessage(ErrorCodes.MissingKey);

            RuleFor(input => input.PageBase64)
                .NotNull().NotEmpty().Must(BeValidBase64).WithMessage(ErrorCodes.MissingPage);
        }

        private static bool BeValidBase64(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            Span<byte> buffer = new byte[value.Length];
            return Convert.TryFromBase64String(value, buffer, out _);
        }
    }
}
