using FluentValidation;
using PrDeploy.Api.Models.Auth;

namespace PrDeploy.Api.Models.DeployEnvironments.Inputs.Validators
{
    public class DeployStateComparisonInputValidator : AbstractValidator<DeployStateComparisonInput>
    {
        public DeployStateComparisonInputValidator()
        {
            RuleFor(x => x.Owner).NotEmpty();
            RuleFor(x => x.Repo).NotEmpty();
            RuleFor(x => x.SourceEnvironment).NotEmpty();
            RuleFor(x => x.TargetEnvironment).NotEmpty();
        }
    }
}
