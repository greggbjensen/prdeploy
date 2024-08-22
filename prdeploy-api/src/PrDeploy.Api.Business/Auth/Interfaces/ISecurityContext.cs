namespace PrDeploy.Api.Business.Auth.Interfaces;

public interface ISecurityContext
{
    string UserToken { get; }
    string Login { get; }
}