using FluentValidation;
using PrDeploy.Api.Models.Auth;

namespace PrDeploy.Api.Models.DeployEnvironments.Inputs.Validators
{
    /// <summary>Validates deploy state comparison input before it is used.</summary>
    public class DeployStateComparisonInputValidator : AbstractValidator<DeployStateComparisonInput>
    {
        /// <summary>Initializes the instance with its required dependencies.</summary>
        public DeployStateComparisonInputValidator()
        {
            RuleFor(x => x.Owner).NotEmpty();
            RuleFor(x => x.Repo).NotEmpty();
            RuleFor(x => x.SourceEnvironment).NotEmpty();
            RuleFor(x => x.TargetEnvironment).NotEmpty();
        }
    }
}
