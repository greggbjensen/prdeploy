using FluentValidation;

namespace PrDeploy.Api.Models.General.Inputs.Validators
{
    public class RepositoryQueryInputValidator : AbstractValidator<RepositoryQueryInput>
    {
        public RepositoryQueryInputValidator()
        {
            RuleFor(x => x.Owner).NotEmpty();
            RuleFor(x => x.Repo).NotEmpty();
        }
    }
}
