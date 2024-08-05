using FluentValidation;

namespace PrDeploy.Api.Models.Auth.Validators
{
    public class AccessTokenRequestValidator : AbstractValidator<AccessTokenRequest>
    {
        public AccessTokenRequestValidator()
        {
            RuleFor(x => x.ClientId).NotEmpty();
            RuleFor(x => x.Code).NotEmpty();
            RuleFor(x => x.RedirectUrl).NotEmpty();
        }
    }
}
