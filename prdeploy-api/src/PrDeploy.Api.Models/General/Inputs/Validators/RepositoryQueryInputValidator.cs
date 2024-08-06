using FluentValidation;

namespace PrDeploy.Api.Models.General.Inputs.Validators
{
    public class RepositoryQueryInputValidator : AbstractValidator<RepoQueryInput>
    {
        public RepositoryQueryInputValidator()
        {
            RuleFor(x => x.Owner).NotEmpty();
            RuleFor(x => x.Repo).NotEmpty();
        }
    }
}
