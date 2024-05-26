using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text;

namespace AzureCommunicationServicesSample
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var host = CreateHostBuilder(args).Build();

            using var serviceScope = host.Services.CreateScope();
            var services = serviceScope.ServiceProvider;

            try
            {
                var app = services.GetRequiredService<App>();
                app.SendEmailUsingSmtp();
                await app.SendEmailUsingEmailClientAsync();
                return 0;
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred.");
                return 1;
            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? string.Empty;

            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    config.AddJsonFile("appsettings.json");
                    config.AddJsonFile($"appsettings.{env}.json", true);
                    config.AddUserSecrets<Program>();
                    config.AddCommandLine(args);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<AzureCommunicationServiceSmtpSettings>(hostContext.Configuration.GetSection(nameof(AzureCommunicationServiceSmtpSettings)));
                    services.Configure<AzureCommunicationServiceSettings>(hostContext.Configuration.GetSection(nameof(AzureCommunicationServiceSettings)));
                    services.Configure<ExecuteSettings>(hostContext.Configuration.GetSection(nameof(ExecuteSettings)));
                    services.AddTransient<App>();

                    var sp = services.BuildServiceProvider();
                    var configuration = sp.GetService<IConfiguration>();

                    var secretClient = new SecretClient(
                       new Uri($"https://{configuration["KeyVaultName"]}.vault.azure.net/"),
                       new DefaultAzureCredential());

                    configuration["AzureCommunicationServiceSettings:ConnectionString"] = secretClient.GetSecret("communicationservices-connectionstring").Value.Value;
                    configuration["AzureCommunicationServiceSmtpSettings:Password"] = secretClient.GetSecret("communicationservices-smtp-password").Value.Value;
                })
                .UseConsoleLifetime();
        }
    }
}
