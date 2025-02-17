﻿using Amazon.SimpleSystemsManagement;
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
                var valueString = parameterResponse.Parameter.Value;
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
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
