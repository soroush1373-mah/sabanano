using System.Diagnostics;
using FluentValidation;

namespace CleanArchitecture.Application.ContactMessages.Commands.CreateContactUs;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public class CreateContactUsCommandValidator : AbstractValidator<CreateContactUsCommand>
{
    public CreateContactUsCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("لطفا نام خود را وارد کنید.");

        RuleFor(x => x.Mobile)
            .NotEmpty().WithMessage("لطفا شماره موبایل را وارد کنید.")
            .Matches(@"^09\d{9}$").WithMessage("شماره موبایل باید فقط عدد و ۱۱ رقم باشد، مثل 09121234567.");
    }

    private string GetDebuggerDisplay()
    {
        return ToString();
    }
}