using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrDeploy.Api.Business.Options;
using PrDeploy.Api.Business.Stores.Interfaces;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PrDeploy.Api.Business.Stores
{
    public class ParameterStore : IParameterStore
    {
        private const int MaxHistoryPerPage = 50;
        private static readonly SemaphoreSlim Semaphore = new(1);
        private readonly IAmazonSimpleSystemsManagement _amazonSsm;
        private readonly ILogger<ParameterStore> _logger;
        private readonly AwsExtendedOptions _awsOptions;


        public ParameterStore(IAmazonSimpleSystemsManagement amazonSsm, 
            ILogger<ParameterStore> logger, IOptions<AwsExtendedOptions> awsOptions)
        {
            _awsOptions = awsOptions.Value;
            _amazonSsm = amazonSsm;
            _logger = logger;
        }

        public async Task<T> GetAsync<T>(string name)
        {
            var value = await GetParameterValueAsync<T>($"{_awsOptions.SecretPathPrefix}/{name.ToUpperInvariant()}");
            return value;
        }

        public async Task<T> GetAsync<T>(string owner, string repo, string name)
        {
            var value = await GetParameterValueAsync<T>($"{_awsOptions.SecretPathPrefix}/{owner}/{repo}/{name.ToUpperInvariant()}");
            return value;
        }

        public async Task<T> GetHistoricalAsync<T>(string owner, string repo, string name, int historical)
        {
            var path = $"{_awsOptions.SecretPathPrefix}/{owner}/{repo}/{name.ToUpperInvariant()}";
            if (historical <= 0)
            {
                return await GetParameterValueAsync<T>(path);
            }

            var value = default(T);
            try
            {
                // History is returned oldest first, so all pages are needed to find the latest values.
                var history = new List<ParameterHistory>();
                string? nextToken = null;
                do
                {
                    var response = await _amazonSsm.GetParameterHistoryAsync(new GetParameterHistoryRequest
                    {
                        Name = path,
                        WithDecryption = true,
                        MaxResults = MaxHistoryPerPage,
                        NextToken = nextToken
                    });

                    if (response.Parameters != null)
                    {
                        history.AddRange(response.Parameters);
                    }

                    nextToken = response.NextToken;
                } while (!string.IsNullOrEmpty(nextToken));

                if (history.Count > historical)
                {
                    value = Deserialize<T>(history[history.Count - (historical + 1)].Value);
                }
            }
            catch (ParameterNotFoundException)
            {
                // Do nothing.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unable to get parameter history for {path}.");
            }

            return value!;
        }

        public async Task<T> GetAsync<T>(string owner, string name)
        {
            var value = await GetParameterValueAsync<T>($"{_awsOptions.SecretPathPrefix}/{owner}/{name.ToUpperInvariant()}");
            return value;
        }

        public async Task SetAsync<T>(string name, T value, bool isSecure = false)
        {
            await SetParameterValueAsync($"{_awsOptions.SecretPathPrefix}/{name.ToUpperInvariant()}", value, isSecure);
        }

        public async Task SetAsync<T>(string owner, string repo, string name, T value, bool isSecure = false)
        {
            await SetParameterValueAsync($"{_awsOptions.SecretPathPrefix}/{owner}/{repo}/{name.ToUpperInvariant()}", value, isSecure);
        }

        public async Task SetAsync<T>(string owner, string name, T value, bool isSecure = false)
        {
            await SetParameterValueAsync($"{_awsOptions.SecretPathPrefix}/{owner}/{name.ToUpperInvariant()}", value, isSecure);
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
                value = Deserialize<T>(parameterResponse.Parameter.Value);
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

        private static T Deserialize<T>(string valueString)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
            return deserializer.Deserialize<T>(valueString);
        }

        private async Task SetParameterValueAsync<T>(string path, T value, bool isSecure)
        {
            // Only allow one update at a time to prevent TooManyUpdates from Parameter store.
            await Semaphore.WaitAsync();

            try
            {
                _logger.LogInformation($"Updating parameter store at {path}");
                var serializer = new SerializerBuilder()
                    .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitEmptyCollections | DefaultValuesHandling.OmitNull)
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
            finally
            {
                Semaphore.Release();
            }
        }
    }
}
