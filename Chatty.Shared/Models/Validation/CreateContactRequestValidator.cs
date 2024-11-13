using FluentValidation;
using Chatty.Shared.Models.Contacts;

namespace Chatty.Shared.Models.Validation;

public sealed class CreateContactRequestValidator : AbstractValidator<CreateContactRequest>
{
    public CreateContactRequestValidator()
    {
        RuleFor(x => x.ContactUserId)
            .NotEmpty();
    }
}