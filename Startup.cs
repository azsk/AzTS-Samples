using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(AzTS_Extended.Startup))]

namespace AzTS_Extended
{
    using System;
    using System.IO;
    using System.Net.Http;
    using Microsoft.AzSK.ATS.EndpointProvider;
    using Microsoft.AzSK.ATS.Extensions;
    using Microsoft.AzSK.ATS.Extensions.Authentication;
    using Microsoft.AzSK.ATS.Extensions.Authorization;
    using Microsoft.AzSK.ATS.Extensions.ConfigurationProvider;
    using Microsoft.AzSK.ATS.Extensions.EventProcessor;
    using Microsoft.AzSK.ATS.Extensions.ExceptionProvider;
    using Microsoft.AzSK.ATS.Extensions.Graph;
    using Microsoft.AzSK.ATS.Extensions.HttpHelper;
    using Microsoft.AzSK.ATS.Extensions.Models;
    using Microsoft.AzSK.ATS.Extensions.PollyPolicyHelper;
    using Microsoft.AzSK.ATS.Extensions.Storage;
    using Microsoft.AzSK.ATS.ProcessSubscriptions.Processors;
    using Microsoft.AzSK.ATS.ProcessSubscriptions.Repositories;
    using Microsoft.AzSK.ATS.ProcessSubscriptions.Repositories.ResourceRepositories;
    using Microsoft.AzSK.ATS.ProcessSubscriptions.Repositories.SubscriptionRespositories;
    using Microsoft.AzSK.ATS.WorkItemProcessor.Processors.ControlProcessors;
    using Microsoft.AzSK.ATS.WorkItemProcessor.Repositories;
    using Microsoft.AzSK.ATS.WorkItemProcessor.Repositories.SubscriptionRespositories;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.FeatureManagement;
    using Polly.Registry;

    class Startup : FunctionsStartup
    {
        private static IPolicyRegistry<string> _policyRegistry;
        private IConfiguration _configuration;

        /// <summary>
        /// Configuration load for function apps.
        /// </summary>
        /// <param name="builder">Function config builder instance.</param>
        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            FunctionsHostBuilderContext context = builder.GetContext();

            // Get configurations paths
            string configurationRootPath = Path.Combine(context.ApplicationRootPath, "Configurations");
            var temp = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            
            if (string.IsNullOrWhiteSpace(configurationRootPath))
            {
                throw new ArgumentException("App setting not found.");
            }

            var laQueryFilesPath = Path.Combine(configurationRootPath, "LAQueries");
            _configuration = builder.ConfigurationBuilder
                .AddJsonFile(Path.Combine(configurationRootPath, "appsettings.json"), optional: false, reloadOnChange: true)
                .AddJsonFile(Path.Combine(configurationRootPath, $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json"), optional: false, reloadOnChange: true)
                .AddKeyPerFile(laQueryFilesPath, true)
                .AddEnvironmentVariables()
                .Build();
        }

        /// <summary>
        /// Configure dependencies for application.
        /// </summary>
        /// <param name="builder">Functions host builder instance.</param>
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var services = builder.Services;

            // Add configurations from app settings

            // Enable inbuilt services
            // services.AddHttpClient();
            services.AddLogging();
            services.AddApplicationInsightsTelemetry();
            services.AddFeatureManagement(_configuration);

            // Configurations settings
            // Notes: Configurations read using the options pattern
            var webJobConfigurations = _configuration.GetSection(WebJobConfigurations.ConfigName).Get<WebJobConfigurations>();
            services.Configure<AzureEndpoints>(_configuration.GetSection(string.Concat(EndpointMapping.ConfigName, ":", webJobConfigurations.CloudEnvironmentName)));
            services.Configure<LAConfigurations>(_configuration.GetSection(LAConfigurations.ConfigName));
            services.Configure<GraphConfigurations>(_configuration.GetSection(GraphConfigurations.ConfigName));
            //// Notes: Named options pattern used for mapping configurations with strongly typed properties/classes
            services.Configure<WebJobConfigurations>(_configuration.GetSection(WebJobConfigurations.ConfigName));
            services.Configure<AzureStorageSettings>(_configuration.GetSection(AzureStorageSettings.ConfigName));
            services.Configure<AuthNSettings>(_configuration.GetSection(AuthNSettings.ConfigName));
            services.Configure<AuthzSettings>(_configuration.GetSection(AuthzSettings.ConfigName));
            services.Configure<HttpClientConfig>(_configuration.GetSection(HttpClientConfig.ConfigName));
            services.Configure<AzureSQLSettings>(_configuration.GetSection(AzureSQLSettings.ConfigName));
            services.Configure<AzureControlScanExceptionSettings>(_configuration.GetSection(AzureControlScanExceptionSettings.ConfigName));
            services.Configure<AADClientAppDetails>(_configuration.GetSection(AADClientAppDetails.ConfigName));
            services.Configure<AppMetadata>(_configuration.GetSection(AppMetadata.ConfigName));

            // TODO: Check if this can be added
            services.Configure<RepositorySettings>(_configuration.GetSection(RepositorySettings.ConfigName));

            services.Configure<RuleEngineSettings>(_configuration.GetSection(RuleEngineSettings.ConfigName));

            // Notes: Named options pattern used for mapping configurations with strongly typed properties/classes
            services.Configure<WorkItemProcessorSettings>(_configuration.GetSection(WorkItemProcessorSettings.ConfigName));

            // Helper classes registration

            // services.AddHttpClient().AddPolicyRegistry();
            services.AddSingleton<AzureHttpClientHelper>();
            services.AddSingleton<LAHttpClientHelper>();
            services.AddSingleton<AzureStorageProvider>();
            services.AddSingleton<IBatchProcessor, BatchProcessor>();
            services.AddSingleton<IHttpHelper, HttpClientHelper>();
            services.AddSingleton<IAuthProvider, AADAuthProvider>();
            services.AddSingleton<IEventProcessor, EventProcessor>();
            services.AddSingleton<IStorageProvider, LAStorageProvider>();
            services.AddSingleton<AzureSQLStorageProvider, AzureSQLStorageProvider>();
            services.AddScoped<IAuthzProvider, AzureAuthzProvider>();
            services.AddSingleton<IKeyVaultOperationsProvider, KeyVaultOperationsProvider>();
            services.AddSingleton<IExceptionProvider, AzureControlScanExceptionProvider>();
            services.AddScoped<IGraphProvider, AzureGraphProvider>();
            services.AddScoped<CustomException>();
            services.AddSingleton<AIHttpClientHelper>();
            services.AddSingleton<AutoUpdaterEventProcessor>();

            // Repository classes registration
            services.AddSingleton<IControlConfiguratoinProvider, ControlConfigurationProvider>();
            services.AddScoped<SubscriptionItemProcessor>();
            services.AddScoped<SubscriptionPolicySummary>();
            services.AddScoped<SubscriptionOwnerRepository>();
            services.AddScoped<ARMSubscriptionResourceInventory>();
            services.AddScoped<SubscriptionCoreRepository>();

            services.AddScoped<ServiceControlExceptionProcessor>();
            services.AddScoped<ControlEvaluationProcessor>();
            services.AddScoped<SecureScoreAssessmentRepository>();

            services.AddScoped<SubscriptionPolicyStateRepository>();
            services.AddScoped<SubscriptionPolicyAssignmentsRepository>();
            services.AddScoped<SecureScoreAssessmentRepository>();
            services.AddScoped<ServiceEnricherFactory>();
            services.AddScoped<RepositoryFactory>();
            services.AddScoped<ControlEvaluationProcessorFactory>();
            services.AddScoped<SecurityCenterRepository>();
            services.AddSingleton<ITSRoleDefinition, TSRoleDefinition>();
            services.AddScoped<IEndpointProvider, EndpointProvider>();
            // To handle "Scope disposed{no name, Parent=disposed{no name, Parent=disposed{no name}}} is disposed and scoped instances are disposed and no longer available" exception.
            // TODO: Replace the timespan as need. eg(Timeout.InfiniteTimeSpan or TimeSpan.FromHours(2) for 2 hrs)
            //_policyRegistry = services.AddPolicyRegistry();
            //PolicyHelper.CreateAndRegisterPolicies(_configuration, _policyRegistry);
            services.AddHttpClient(HttpClientConfig.HttpClientName).SetHandlerLifetime(TimeSpan.FromHours(2));

            //    .AddPolicyHandler(httpRequestMessage =>
            //{
            //    return _policyRegistry.Get<Polly.IAsyncPolicy<HttpResponseMessage>>(PolicyHelper.PolicyName.WaitAndRetryPolicy);
            //});


        }
    }
}