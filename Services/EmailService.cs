using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace fekon_repository_dataservice.Services
{
    public class EmailService : IEmailSender
    {
        private readonly ILogger<EmailService> logger;
        public EmailService(ILogger<EmailService> logger)
        {
            this.logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            string fromMail = GetSmtpMailConfig("SmtpEmailAcc");
            string fromPassword = GetSmtpMailConfig("SmtpEmailPass");
            string smtpHost = GetSmtpMailConfig("SmtpHost");

            MailMessage message = new();
            message.From = new MailAddress(fromMail);
            message.Subject = subject;
            message.To.Add(new MailAddress(email));
            message.Body = "<html><body> " + htmlMessage + " </body></html>";
            message.IsBodyHtml = true;

            SmtpClient smtpClient = new(smtpHost)
            {
                Port = 587,
                Credentials = new NetworkCredential(fromMail, fromPassword),
                EnableSsl = true,
            };
            logger.LogError("Sending email");
            await smtpClient.SendMailAsync(message);
        }

        private static string GetSmtpMailConfig(string section)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                            .AddEnvironmentVariables();
            IConfiguration config = builder.Build();
            string value = config[section];

            return value;
        }
    }
}
