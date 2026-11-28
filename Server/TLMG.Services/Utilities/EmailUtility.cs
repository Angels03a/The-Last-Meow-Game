using log4net;
using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Xml;
using System.Xml.Linq;

namespace TLMG.Services.Utilities
{
    public class EmailUtility
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(EmailUtility));

        private readonly string _smtpServer;
        private readonly int _port;
        private readonly string _senderEmail;
        private readonly string _appPassword;

        public EmailUtility()
        {
            const string configurationFileName = "EmailConfig.xml";
            const string smtpServerNodeName = "SmtpServer";
            const string smtpPortNodeName = "SmtpPort";
            const string senderEmailNodeName = "SenderEmail";
            const string appPasswordNodeName = "AppPassword";

            string configurationPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configurationFileName);

            try
            {
                XDocument configurationDocument = XDocument.Load(configurationPath);
                XElement rootElement = configurationDocument.Root;

                if (rootElement != null)
                {
                    XElement smtpServerElement = rootElement.Element(smtpServerNodeName);
                    XElement smtpPortElement = rootElement.Element(smtpPortNodeName);
                    XElement senderEmailElement = rootElement.Element(senderEmailNodeName);
                    XElement appPasswordElement = rootElement.Element(appPasswordNodeName);

                    if (smtpServerElement != null && smtpPortElement != null && senderEmailElement 
                        != null && appPasswordElement != null)
                    {
                        _smtpServer = smtpServerElement.Value;
                        _port = int.Parse(smtpPortElement.Value);
                        _senderEmail = senderEmailElement.Value;
                        _appPassword = appPasswordElement.Value;
                    }
                    else
                    {
                        _log.Error($"Faltan elementos XML obligatorios en {configurationFileName}.");
                    }
                }
            }
            catch (FileNotFoundException fileNotFoundException)
            {
                _log.Error($"No se encontró el archivo de configuración en {configurationPath}.", fileNotFoundException);
            }
            catch (DirectoryNotFoundException directoryNotFoundException)
            {
                _log.Error($"No se encontró el directorio de configuración de {configurationPath}.", directoryNotFoundException);
            }
            catch (XmlException xmlException)
            {
                _log.Error($"El archivo {configurationPath} contiene XML mal formado.", xmlException);
            }
            catch (FormatException formatException)
            {
                _log.Error($"El valor de SmtpPort en {configurationFileName} debe ser un entero.", formatException);
            }
        }

        public bool SendEmail(string recipientEmail, string subject, string body)
        {
            const string senderDisplayName = "The Last Meow";
            bool isOperationSendEmailSuccessful = false;

            try
            {
                using (MailMessage mailMessage = new MailMessage())
                {
                    mailMessage.From = new MailAddress(_senderEmail, senderDisplayName);
                    mailMessage.To.Add(recipientEmail);
                    mailMessage.Subject = subject;
                    mailMessage.Body = body;
                    mailMessage.IsBodyHtml = true;

                    using (SmtpClient smtpClient = new SmtpClient(_smtpServer, _port))
                    {
                        smtpClient.Credentials = new NetworkCredential(_senderEmail, _appPassword);
                        smtpClient.EnableSsl = true;
                        smtpClient.Send(mailMessage);
                    }
                }

                isOperationSendEmailSuccessful = true;
            }
            catch (SmtpFailedRecipientException smtpFailedRecipientException)
            {
                _log.ErrorFormat("El servidor SMTP rechazó al destinatario. Código SMTP: {0}.",
                    (int)smtpFailedRecipientException.StatusCode);
            }
            catch (SmtpException smtpException)
            {
                _log.ErrorFormat("Falló la comunicación o autenticación con el servidor SMTP. Código SMTP: {0}.",
                    (int)smtpException.StatusCode);
            }

            return isOperationSendEmailSuccessful;
        }
    }
}
