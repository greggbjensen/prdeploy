using FluentValidation;

namespace PrDeploy.Api.Models.Auth.Validators
{
    /// <summary>
    /// Validates the access Token Request Validator before it is used.
    /// </summary>
    public class AccessTokenRequestValidator : AbstractValidator<AccessTokenRequest>
    {
        /// <summary>
        /// Initializes a access Token Request Validator with the supplied dependencies.
        /// </summary>
        public AccessTokenRequestValidator()
        {
            RuleFor(x => x.ClientId).NotEmpty();
            RuleFor(x => x.Code).NotEmpty();
            RuleFor(x => x.RedirectUrl).NotEmpty();
        }
    }
}
