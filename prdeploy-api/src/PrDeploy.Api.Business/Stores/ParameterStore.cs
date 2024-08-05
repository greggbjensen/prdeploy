using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using PrDeploy.Api.Business.Stores.Interfaces;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PrDeploy.Api.Business.Stores
{
    public class ParameterStore : IParameterStore
    {
        private const string PrdeployPrefix = "/prdeploy";
        private readonly IAmazonSimpleSystemsManagement _amazonSsm;

        public ParameterStore(IAmazonSimpleSystemsManagement amazonSsm)
        {
            _amazonSsm = amazonSsm;
        }

        public async Task<T> GetAsync<T>(string owner, string repo, string name)
        {
            var value = await GetParameterValueAsync<T>($"{PrdeployPrefix}/{owner}/{repo}/{name.ToUpperInvariant()}");
            return value;
        }

        public async Task<T> GetAsync<T>(string owner, string name)
        {
            var value = await GetParameterValueAsync<T>($"{PrdeployPrefix}/{owner}/{name.ToUpperInvariant()}/");
            return value;
        }

        public async Task SetAsync<T>(string owner, string repo, string name, T value, bool isSecure = false)
        {
            await SetParameterValueAsync($"{PrdeployPrefix}/{owner}/{repo}/{name.ToUpperInvariant()}", value, isSecure);
        }

        public async Task SetAsync<T>(string owner, string name, T value, bool isSecure = false)
        {
            await SetParameterValueAsync($"{PrdeployPrefix}/{owner}/{name.ToUpperInvariant()}", value, isSecure);
        }

        private async Task<T> GetParameterValueAsync<T>(string path)
        {
            var value = default(T);
            var request = new GetParameterRequest
            {
                Name = path,
                WithDecryption = true
            };
            try
            {
                var parameterResponse = await _amazonSsm.GetParameterAsync(request);
                var valueString = parameterResponse.Parameter.Value;
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();
                value = deserializer.Deserialize<T>(valueString);
            }
            catch (ParameterNotFoundException)
            {
                // Do nothing.
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception {ex}");
            }

            return value!;
        }

        private async Task SetParameterValueAsync<T>(string path, T value, bool isSecure)
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
            var yaml = serializer.Serialize(value);
            var request = new PutParameterRequest
            {
                Name = path,
                Type = !isSecure ? ParameterType.String : ParameterType.SecureString,
                Value = yaml,
                Overwrite = true,
                Tier = ParameterTier.Standard
            };
            await _amazonSsm.PutParameterAsync(request);
        }
    }
}
