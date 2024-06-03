using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
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
                //await app.SendEmailUsingEmailClientAsync();
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
            var isDevelopment = env.Equals("Development", StringComparison.OrdinalIgnoreCase);

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
                    var sp = services.BuildServiceProvider();
                    var configuration = sp.GetService<IConfiguration>();

                    services.Configure<AzureCommunicationServiceSmtpSettings>(hostContext.Configuration.GetSection(nameof(AzureCommunicationServiceSmtpSettings)));
                    services.Configure<AzureCommunicationServiceSettings>(hostContext.Configuration.GetSection(nameof(AzureCommunicationServiceSettings)));
                    services.Configure<ExecuteSettings>(hostContext.Configuration.GetSection(nameof(ExecuteSettings)));
                    services.AddTransient(sp =>
                    {
                        var client = new SmtpClient(configuration["AzureCommunicationServiceSmtpSettings:SmtpHostUrl"])
                        {
                            Port = 587,
                            Credentials = new NetworkCredential(configuration["AzureCommunicationServiceSmtpSettings:UserName"], configuration["AzureCommunicationServiceSmtpSettings:Password"]),
                            EnableSsl = true
                        };
                        return client;
                    });
                    services.AddTransient<App>();

                    var secretClient = new SecretClient(
                       new Uri($"https://{configuration["KeyVaultName"]}.vault.azure.net/"),
                       (isDevelopment ? new ClientSecretCredential(configuration["AZURE_TENANT_ID"], configuration["AZURE_CLIENT_ID"], configuration["AZURE_CLIENT_SECRET"]) : new DefaultAzureCredential())
                       );

                    //configuration["AzureCommunicationServiceSettings:ConnectionString"] = secretClient.GetSecret("communicationservices-connectionstring").Value.Value;
                    //configuration["AzureCommunicationServiceSmtpSettings:Password"] = secretClient.GetSecret("communicationservices-smtp-password").Value.Value;
                })
                .UseConsoleLifetime();
        }
    }
}
