namespace PrDeploy.Api.Business.Services.Interfaces;

public interface IGitHubAppDependency<T>
{
    T Value { get; set; }
}
