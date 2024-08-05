using FluentValidation;

namespace PrDeploy.Api.Models.DeployEnvironments.Inputs.Validators
{
    public class EnvironmentsInputValidator : AbstractValidator<EnvironmentsInput>
    {
        public EnvironmentsInputValidator()
        {
            RuleFor(x => x.Owner).NotEmpty();
            RuleFor(x => x.Repo).NotEmpty();
        }
    }
}
