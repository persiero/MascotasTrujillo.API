using System.Net;
using System.Net.Mail;

namespace MascotasTrujillo.API.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task EnviarCorreoAsync(string destino, string asunto, string cuerpoHtml)
        {
            string host = _configuration["Smtp:Host"]!;
            int port = int.Parse(_configuration["Smtp:Port"]!);
            string user = _configuration["Smtp:User"]!;
            string password = _configuration["Smtp:Password"]!;
            string fromName = _configuration["Smtp:FromName"]!;
            string fromEmail = _configuration["Smtp:FromEmail"]!;

            using var client = new SmtpClient(host, port)
            {
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(user, password),
                EnableSsl = true
            };

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = asunto,
                Body = cuerpoHtml,
                IsBodyHtml = true
            };

            message.To.Add(destino);

            await client.SendMailAsync(message);
        }
    }
}