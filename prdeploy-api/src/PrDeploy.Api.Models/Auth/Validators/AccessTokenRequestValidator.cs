using FluentValidation;

namespace PrDeploy.Api.Models.Auth.Validators
{
    /// <summary>
    /// Validates access token request before it is used.
    /// </summary>
    public class AccessTokenRequestValidator : AbstractValidator<AccessTokenRequest>
    {
        /// <summary>
        /// Initializes the instance with its required dependencies.
        /// </summary>
        public AccessTokenRequestValidator()
        {
            RuleFor(x => x.ClientId).NotEmpty();
            RuleFor(x => x.Code).NotEmpty();
            RuleFor(x => x.RedirectUrl).NotEmpty();
        }
    }
}
