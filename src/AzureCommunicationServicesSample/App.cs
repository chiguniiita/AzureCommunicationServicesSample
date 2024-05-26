using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace AzureCommunicationServicesSample
{
    internal class App
    {
        private readonly IConfiguration _configuration;
        private readonly AzureCommunicationServiceSmtpSettings _azureCommunicationServiceSmtpSettings;
        private readonly AzureCommunicationServiceSettings _azureCommunicationServiceSettings;
        private readonly ExecuteSettings _executeSettings;
        private readonly string subject = "Hello, world!";
        private readonly string body = "This message is sent from Azure Communication Service Email.";

        public App(IConfiguration configuration
            , IOptions<AzureCommunicationServiceSmtpSettings> azureCommunicationServiceSmtpSettings
            , IOptions<AzureCommunicationServiceSettings> azureCommunicationServiceSettings
            , IOptions<ExecuteSettings> executeSettings
            )
        {
            _configuration = configuration;
            _azureCommunicationServiceSmtpSettings = azureCommunicationServiceSmtpSettings.Value;
            _azureCommunicationServiceSettings = azureCommunicationServiceSettings.Value;
            _executeSettings = executeSettings.Value;
        }

        public void SendEmailUsingSmtp()
        {
            string smtpHostUrl = "smtp.azurecomm.net";
            var messageSuffix = $"(using {typeof(SmtpClient).FullName})";

            var client = new SmtpClient(smtpHostUrl)
            {
                Port = 587,
                Credentials = new NetworkCredential(_azureCommunicationServiceSmtpSettings.Username, _azureCommunicationServiceSmtpSettings.Password),
                EnableSsl = true
            };

            var message = new MailMessage(_executeSettings.SenderAddress, _executeSettings.RecipientAddress, subject, body + messageSuffix);

            try
            {
                client.Send(message);
                Console.WriteLine("Successfully!" + messageSuffix);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public async Task SendEmailUsingEmailClientAsync()
        {
            var messageSuffix = $"(using {typeof(EmailClient).FullName})";

            var emailClient = new EmailClient(_azureCommunicationServiceSettings.ConnectionString);
            var recipientAddress = new EmailRecipients([new(_executeSettings.RecipientAddress)]);
            var content = new EmailContent(subject: subject)
            {
                PlainText = body + $"(using {typeof(EmailClient).FullName})",
            };
            var emailMessage = new EmailMessage(_executeSettings.SenderAddress, recipientAddress, content);

            try
            {
                var emailSendOperation = await emailClient.SendAsync(WaitUntil.Completed, emailMessage);
                Console.WriteLine($"Successfully!{messageSuffix} ID: {emailSendOperation.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Smtp send failed with the exception: {ex.Message}.");
            }
        }
    }
}
