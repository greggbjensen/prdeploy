namespace PrDeploy.Api.Models.Settings.Compare
{
    /// <summary>
    /// Represents owner repo value data used by the application.
    /// </summary>
    public class OwnerRepoValue<T>
    {
        /// <summary>
        /// Gets or sets the owner.
        /// </summary>
        /// <value>Current owner value.</value>
        public T? Owner { get; set; }
        /// <summary>
        /// Gets or sets the repo.
        /// </summary>
        /// <value>Current repo value.</value>
        public T? Repo { get; set; }
    }
}
