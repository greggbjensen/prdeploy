using FluentValidation;

namespace PrDeploy.Api.Business.Options.Validation
{
    /// <summary>
    /// Validates the git Hub Auth Options Validator before it is used.
    /// </summary>
    public class GitHubAuthOptionsValidator : AbstractValidator<GitHubAuthOptions>
    {
        /// <summary>
        /// Initializes a git Hub Auth Options Validator with the supplied dependencies.
        /// </summary>
        public GitHubAuthOptionsValidator()
        {
            RuleFor(x => x.Authority).NotEmpty();
            RuleFor(x => x.ClientId).NotEmpty();
            RuleFor(x => x.ClientSecret).NotEmpty();
        }
    }
}
