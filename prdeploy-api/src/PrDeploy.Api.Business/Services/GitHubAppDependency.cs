using PrDeploy.Api.Business.Services.Interfaces;

namespace PrDeploy.Api.Business.Services;
public class GitHubAppDependency<T> : IGitHubAppDependency<T>
{
    public GitHubAppDependency(T value)
    {
    }

    public T Value { get; set; }
}
