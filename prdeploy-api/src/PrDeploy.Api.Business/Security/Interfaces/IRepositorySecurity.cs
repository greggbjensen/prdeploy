namespace PrDeploy.Api.Business.Security.Interfaces;

public interface IRepositorySecurity
{
    Task GuardAsync(string owner, string repo);
    Task<bool> HasAccessAsync(string owner, string repo);
}