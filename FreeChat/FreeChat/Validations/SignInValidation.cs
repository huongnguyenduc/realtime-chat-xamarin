using System;
using FreeChat.ViewModels;
using FluentValidation;
namespace FreeChat.Validations
{
    public class SignInValidation : AbstractValidator<SignInViewModel>
    {
        public SignInValidation()
        {
            RuleFor(x => x.Password).NotNull().WithMessage(x => x.SignInError = "Password is missing");
            RuleFor(x => x.Email).NotNull().EmailAddress().WithMessage(x => x.SignInError = "Email is not valid");
        }
    }
}

