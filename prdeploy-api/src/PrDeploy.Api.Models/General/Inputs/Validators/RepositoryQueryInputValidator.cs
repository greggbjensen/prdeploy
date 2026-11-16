using FluentValidation;

namespace PrDeploy.Api.Models.General.Inputs.Validators
{
    /// <summary>
    /// Validates repository query input before it is used.
    /// </summary>
    public class RepositoryQueryInputValidator : AbstractValidator<RepoQueryInput>
    {
        /// <summary>
        /// Initializes the instance with its required dependencies.
        /// </summary>
        public RepositoryQueryInputValidator()
        {
            RuleFor(x => x.Owner).NotEmpty();
            RuleFor(x => x.Repo).NotEmpty();
        }
    }
}
