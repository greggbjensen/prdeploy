using FluentValidation;

namespace PrDeploy.Api.Business.Options.Validation
{
    /// <summary>
    /// Validates git hub auth options before it is used.
    /// </summary>
    public class GitHubAuthOptionsValidator : AbstractValidator<GitHubAuthOptions>
    {
        /// <summary>
        /// Initializes the instance with its required dependencies.
        /// </summary>
        public GitHubAuthOptionsValidator()
        {
            RuleFor(x => x.Authority).NotEmpty();
            RuleFor(x => x.ClientId).NotEmpty();
            RuleFor(x => x.ClientSecret).NotEmpty();
        }
    }
}
