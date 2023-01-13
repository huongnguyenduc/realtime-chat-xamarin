using System;
using FluentValidation;
using FreeChat.ViewModels;

namespace FreeChat.Validations
{
    public class SignUpValidation : AbstractValidator<SignUpViewModel>
    {
        public SignUpValidation()
        {
            RuleFor(x => x.Name).NotNull().WithMessage(x => x.SignUpError = "Name is missing");
            RuleFor(x => x.Password).NotNull().WithMessage(x => x.SignUpError = "Password is missing");
            RuleFor(x => x.ConfirmPassword).Equal(x => x.Password).WithMessage(x => x.SignUpError = "Passwords must match");
            RuleFor(x => x.Email).NotNull().EmailAddress().WithMessage(x => x.SignUpError = "Email is not valid");
        }
    }
}

