using Azure;
using Azure.Communication.Email;

namespace AzureCommunicationServicesSample
{
    internal class Program
    {
        private static string _senderAddress = "";
        private static string _recipientAddress = "";
        private static string _connectionString = "";

        static async Task Main(string[] args)
        {
            var emailClient = new EmailClient(_connectionString);
            var recipientAddress = new EmailRecipients([new(_recipientAddress)]);
            var content = new EmailContent(subject: "Hello, world")
            {
                PlainText = "Test message."
            };
            var emailMessage = new EmailMessage(_senderAddress, recipientAddress, content);
            var emailSendOperation = await emailClient.SendAsync(WaitUntil.Completed, emailMessage);

            Console.WriteLine($"Email send operation ID: {emailSendOperation.Id}");
        }
    }
}
