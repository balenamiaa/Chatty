using Chatty.Shared.Models.Contacts;

using FluentValidation;

namespace Chatty.Shared.Models.Validation;

public sealed class CreateContactRequestValidator : AbstractValidator<CreateContactRequest>
{
    public CreateContactRequestValidator()
    {
        RuleFor(x => x.ContactUserId)
            .NotEmpty();
    }
}
