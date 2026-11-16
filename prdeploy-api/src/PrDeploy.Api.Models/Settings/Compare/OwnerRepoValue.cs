namespace PrDeploy.Api.Models.Settings.Compare
{
    /// <summary>
    /// Represents the owner Repo Value used by the application.
    /// </summary>
    public class OwnerRepoValue<T>
    {
        /// <summary>
        /// Gets or sets the Owner.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public T? Owner { get; set; }
        /// <summary>
        /// Gets or sets the Repo.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public T? Repo { get; set; }
    }
}
