using CdrAuthServer.Domain.Extensions;
using CdrAuthServer.IntegrationTests.Interfaces;
using CdrAuthServer.IntegrationTests.Services;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Enums;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace CdrAuthServer.IntegrationTests
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public static void ConfigureServices(IServiceCollection services)
        {
            //Setup config
            var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
              .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true)
             .AddEnvironmentVariables()
             .Build();

            //Setting up logger early so we can catch any startup issues
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration: configuration)
                .CreateBootstrapLogger();

            Log.Information($"---Logger has been configured within {nameof(Startup.ConfigureServices)}.---");

            Log.Information($"Registering project specific services.");
            services.AddSingleton<IDataHolderIntrospectionService, DataHolderIntrospectionService>();
            services.AddSingleton<IDataHolderCDRArrangementRevocationService, DataHolderCDRArrangementRevocationService>();
            services.AddSingleton<IAuthorizationService, AuthorizationService>();

            //Common startup
            services.AddMvc().AddCdrNewtonsoftJson();

            services.AddTestAutomationServices(configuration);
            services.AddTestAutomationSettings(opt =>
            {
                opt.IS_AUTH_SERVER = true;

                opt.INDUSTRY = Industry.BANKING;
                opt.SCOPE = ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Constants.Scopes.ScopeBanking;

                opt.DH_MTLS_GATEWAY_URL = configuration["URL:DH_MTLS_Gateway"] ?? "";
                opt.DH_TLS_AUTHSERVER_BASE_URL = configuration["URL:DH_TLS_AuthServer"] ?? "";
                opt.DH_TLS_PUBLIC_BASE_URL = configuration["URL:DH_TLS_Public"] ?? "";
                opt.REGISTER_MTLS_URL = configuration["URL:Register_MTLS"] ?? "";
                // Connection strings
                opt.DATAHOLDER_CONNECTIONSTRING = configuration["ConnectionStrings:DataHolder"] ?? "";
                opt.AUTHSERVER_CONNECTIONSTRING = configuration["ConnectionStrings:AuthServer"] ?? "";
                opt.REGISTER_CONNECTIONSTRING = configuration["ConnectionStrings:Register"] ?? "";

                // Seed-data offset
                opt.SEEDDATA_OFFSETDATES = configuration["SeedData:OffsetDates"] == "true";

                opt.MDH_INTEGRATION_TESTS_HOST = configuration["URL:MDH_INTEGRATION_TESTS_HOST"] ?? "";
                opt.MDH_HOST = configuration["URL:MDH_HOST"] ?? "";

                opt.CDRAUTHSERVER_SECUREBASEURI = configuration["URL:CDRAuthServer_SecureBaseUri"] ?? "";

                // Playwright settings.
                opt.RUNNING_IN_CONTAINER = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER")?.ToUpper() == "TRUE";
                opt.CREATE_MEDIA = configuration.GetValue<bool>("CreateMedia");
                opt.TEST_TIMEOUT = configuration.GetValue<int>("TestTimeout");
                opt.MEDIA_FOLDER = configuration["MediaFolder"] ?? "";
            });

            services.AddTestAutomationAuthServerSettings(opt =>
            {
                opt.CDRAUTHSERVER_BASEURI = configuration["URL:CDRAuthServer_BaseUri"] ?? "";
                opt.STANDALONE = configuration.GetValue<bool>("Standalone");
                opt.XTLSCLIENTCERTTHUMBPRINT = configuration["XTlsClientCertThumbprint"] ?? "";
                opt.XTLSADDITIONALCLIENTCERTTHUMBPRINT = configuration["XTlsAdditionalClientCertThumbprint"] ?? "";
                opt.ACCESSTOKENLIFETIMESECONDS = Convert.ToInt32(configuration["AccessTokenLifetimeSeconds"]);
                opt.HEADLESSMODE = configuration.GetValue<bool>("HeadlessMode");
                opt.JARM_ENCRYPTION_ON = (Environment.GetEnvironmentVariable("USE_JARM_ENCRYPTION") ?? "false").ToUpper() == "TRUE";
            });

        }
    }
}
