using FluentValidation;

namespace PrDeploy.Api.Business.Options.Validation
{
    public class GitHubAuthOptionsValidator : AbstractValidator<GitHubAuthOptions>
    {
        public GitHubAuthOptionsValidator()
        {
            RuleFor(x => x.Authority).NotEmpty();
            RuleFor(x => x.Audience).NotEmpty();
            RuleFor(x => x.ClientId).NotEmpty();
            RuleFor(x => x.ClientSecret).NotEmpty();
        }
    }
}
