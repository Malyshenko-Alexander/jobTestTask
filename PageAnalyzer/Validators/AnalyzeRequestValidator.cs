using FluentValidation;
using PageAnalyzer.Models;

namespace PageAnalyzer.Validators;

public class AnalyzeRequestValidator : AbstractValidator<AnalyzeRequest>
{
    public AnalyzeRequestValidator()
    {
        RuleFor(x => x.Selector)
            .NotEmpty()
            .WithMessage("selector is required and cannot be empty");

        RuleFor(x => x.Attribute)
            .NotEmpty()
            .WithMessage("attribute is required and cannot be empty");

        RuleFor(x => x.UrlB64)
            .NotEmpty()
            .WithMessage("url_b64 is required");

        RuleFor(x => x.EncryptedTextBytesB64)
            .NotEmpty()
            .WithMessage("encrypted_text_bytes_b64 is required");

        RuleFor(x => x.KeyBytesB64)
            .NotEmpty()
            .WithMessage("key_bytes_b64 is required");

        RuleFor(x => x.PageB64)
            .NotEmpty()
            .WithMessage("page_b64 is required");
    }
}
