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
                        _log.Error($"Configuration file {configurationFileName} is missing required XML elements.");
                    }
                }
            }
            catch (FileNotFoundException fileNotFoundException)
            {
                _log.Error($"Configuration file not found at path: {configurationPath}", fileNotFoundException);
            }
            catch (DirectoryNotFoundException directoryNotFoundException)
            {
                _log.Error($"Configuration directory not found for path: {configurationPath}", directoryNotFoundException);
            }
            catch (XmlException xmlException)
            {
                _log.Error($"Malformed XML content in configuration file: {configurationPath}", xmlException);
            }
            catch (FormatException formatException)
            {
                _log.Error($"SmtpPort element value in {configurationFileName} is not a valid integer.", formatException);
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
                _log.Error($"SMTP Error: Recipient '{recipientEmail}' was rejected.", smtpFailedRecipientException);
            }
            catch (SmtpException smtpException)
            {
                _log.Error($"SMTP Error: Connection or authentication failure with the mail server for " +
                    $"'{recipientEmail}'.", smtpException);
            }

            return isOperationSendEmailSuccessful;
        }
    }
}