using FluentValidation;

namespace PrDeploy.Api.Models.General.Inputs.Validators
{
    /// <summary>
    /// Validates the repository Query Input Validator before it is used.
    /// </summary>
    public class RepositoryQueryInputValidator : AbstractValidator<RepoQueryInput>
    {
        /// <summary>
        /// Initializes a repository Query Input Validator with the supplied dependencies.
        /// </summary>
        public RepositoryQueryInputValidator()
        {
            RuleFor(x => x.Owner).NotEmpty();
            RuleFor(x => x.Repo).NotEmpty();
        }
    }
}
