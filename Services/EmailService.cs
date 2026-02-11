using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Jot.Models;

namespace Jot.Services
{
    /// <summary>
    /// Servicio para enviar documentos por correo electrónico
    /// </summary>
    public class EmailService
    {
        /// <summary>
        /// Crea un archivo .eml con el documento adjunto y lo abre en el cliente de correo predeterminado
        /// </summary>
        public async Task<bool> SendViaDefaultClientAsync(Document document, string toEmail)
        {
            try
            {
                // Crear nombre de archivo seguro
                var safeFileName = SanitizeFileName(document.Title);

                // Crear archivo temporal del documento
                var tempFolder = Path.GetTempPath();
                var documentFileName = $"{safeFileName}.md";
                var documentPath = Path.Combine(tempFolder, documentFileName);
                await File.WriteAllTextAsync(documentPath, document.Content ?? "");

                // Crear archivo .eml
                var emlFileName = $"Email_{safeFileName}_{DateTime.Now:yyyyMMdd_HHmmss}.eml";
                var emlPath = Path.Combine(tempFolder, emlFileName);

                // Leer el documento como base64 para adjuntarlo
                var documentBytes = await File.ReadAllBytesAsync(documentPath);
                var documentBase64 = Convert.ToBase64String(documentBytes);

                // Crear contenido del archivo .eml en formato MIME
                var boundary = "----=_NextPart_" + Guid.NewGuid().ToString("N");
                var emlContent = new System.Text.StringBuilder();

                // Headers del email
                emlContent.AppendLine($"To: {toEmail}");
                emlContent.AppendLine($"Subject: Documento: {document.Title}");
                emlContent.AppendLine($"From: ");
                emlContent.AppendLine("MIME-Version: 1.0");
                emlContent.AppendLine($"Content-Type: multipart/mixed; boundary=\"{boundary}\"");
                emlContent.AppendLine();

                // Cuerpo del mensaje
                emlContent.AppendLine($"--{boundary}");
                emlContent.AppendLine("Content-Type: text/plain; charset=utf-8");
                emlContent.AppendLine("Content-Transfer-Encoding: 8bit");
                emlContent.AppendLine();
                emlContent.AppendLine($"Documento: {document.Title}");
                emlContent.AppendLine();
                emlContent.AppendLine($"Creado: {document.CreatedAt:dd/MM/yyyy HH:mm}");
                emlContent.AppendLine($"Modificado: {document.ModifiedAt:dd/MM/yyyy HH:mm}");
                emlContent.AppendLine();
                emlContent.AppendLine("El documento está adjunto en este email.");
                emlContent.AppendLine();
                emlContent.AppendLine("Enviado desde Jot - Modern Note Taking");
                emlContent.AppendLine();

                // Adjunto
                emlContent.AppendLine($"--{boundary}");
                emlContent.AppendLine($"Content-Type: text/markdown; name=\"{documentFileName}\"");
                emlContent.AppendLine("Content-Transfer-Encoding: base64");
                emlContent.AppendLine($"Content-Disposition: attachment; filename=\"{documentFileName}\"");
                emlContent.AppendLine();

                // Dividir base64 en líneas de 76 caracteres (estándar MIME)
                for (int i = 0; i < documentBase64.Length; i += 76)
                {
                    int length = Math.Min(76, documentBase64.Length - i);
                    emlContent.AppendLine(documentBase64.Substring(i, length));
                }

                emlContent.AppendLine();
                emlContent.AppendLine($"--{boundary}--");

                // Guardar archivo .eml
                await File.WriteAllTextAsync(emlPath, emlContent.ToString());

                // Abrir el archivo .eml con el cliente de correo predeterminado
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = emlPath,
                    UseShellExecute = true
                };

                System.Diagnostics.Process.Start(psi);

                // Limpiar archivo temporal del documento (el .eml se limpiará solo)
                try
                {
                    await Task.Delay(2000); // Esperar 2 segundos antes de limpiar
                    if (File.Exists(documentPath))
                    {
                        File.Delete(documentPath);
                    }
                }
                catch
                {
                    // Ignorar errores al limpiar archivos temporales
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening email client: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Envía el documento directamente mediante SMTP
        /// </summary>
        public async Task<bool> SendViaSmtpAsync(
            Document document, 
            string toEmail, 
            string fromEmail, 
            string smtpServer, 
            int smtpPort, 
            string username, 
            string password,
            bool useSSL = true,
            EmailFormat format = EmailFormat.Markdown)
        {
            try
            {
                using var message = new MailMessage();
                message.From = new MailAddress(fromEmail);
                message.To.Add(new MailAddress(toEmail));
                message.Subject = document.Title;

                // Configurar el cuerpo según el formato
                switch (format)
                {
                    case EmailFormat.Markdown:
                        message.Body = $"Documento adjunto: {document.Title}\n\nCreado: {document.CreatedAt:dd/MM/yyyy HH:mm}\nÚltima modificación: {document.ModifiedAt:dd/MM/yyyy HH:mm}";
                        message.IsBodyHtml = false;

                        // Adjuntar como archivo .md
                        var mdContent = System.Text.Encoding.UTF8.GetBytes(document.Content ?? "");
                        var mdStream = new MemoryStream(mdContent);
                        message.Attachments.Add(new Attachment(mdStream, $"{SanitizeFileName(document.Title)}.md", "text/markdown"));
                        break;

                    case EmailFormat.Html:
                        // Convertir Markdown a HTML y enviar como archivo adjunto
                        var htmlService = new HtmlExportService();
                        var html = htmlService.GenerateHtmlFromDocument(document);

                        message.Body = $"Documento HTML adjunto: {document.Title}\n\nAbre el archivo adjunto para ver el documento formateado.\n\nCreado: {document.CreatedAt:dd/MM/yyyy HH:mm}\nÚltima modificación: {document.ModifiedAt:dd/MM/yyyy HH:mm}";
                        message.IsBodyHtml = false;

                        // Adjuntar como archivo .html
                        var htmlContent = System.Text.Encoding.UTF8.GetBytes(html);
                        var htmlStream = new MemoryStream(htmlContent);
                        message.Attachments.Add(new Attachment(htmlStream, $"{SanitizeFileName(document.Title)}.html", "text/html"));
                        break;

                    case EmailFormat.PlainText:
                        // Texto plano sin formato
                        message.Body = $"Documento: {document.Title}\n\n{document.Content}";
                        message.IsBodyHtml = false;
                        break;
                }

                // Añadir información adicional
                message.Body += $"\n\n---\nEnviado desde Jot\nCreado: {document.CreatedAt:dd/MM/yyyy HH:mm}\nÚltima modificación: {document.ModifiedAt:dd/MM/yyyy HH:mm}";

                // Configurar cliente SMTP
                using var smtpClient = new SmtpClient(smtpServer, smtpPort);
                smtpClient.EnableSsl = useSSL;
                smtpClient.Credentials = new NetworkCredential(username, password);
                smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;

                // Enviar
                await smtpClient.SendMailAsync(message);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending email via SMTP: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Envía usando Gmail (configuración predefinida)
        /// </summary>
        public async Task<bool> SendViaGmailAsync(Document document, string toEmail, string gmailAddress, string gmailPassword, EmailFormat format = EmailFormat.Html)
        {
            return await SendViaSmtpAsync(
                document,
                toEmail,
                gmailAddress,
                "smtp.gmail.com",
                587,
                gmailAddress,
                gmailPassword,
                true,
                format
            );
        }

        /// <summary>
        /// Envía usando Outlook (configuración predefinida)
        /// </summary>
        public async Task<bool> SendViaOutlookAsync(Document document, string toEmail, string outlookAddress, string outlookPassword, EmailFormat format = EmailFormat.Html)
        {
            return await SendViaSmtpAsync(
                document,
                toEmail,
                outlookAddress,
                "smtp-mail.outlook.com",
                587,
                outlookAddress,
                outlookPassword,
                true,
                format
            );
        }

        /// <summary>
        /// Limpia el nombre del archivo para que sea válido
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "document";

            // Eliminar caracteres no válidos para nombres de archivo
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(fileName
                .Where(c => !invalidChars.Contains(c))
                .ToArray());

            // Limitar longitud
            if (sanitized.Length > 100)
                sanitized = sanitized.Substring(0, 100);

            return string.IsNullOrWhiteSpace(sanitized) ? "document" : sanitized;
        }
    }

    /// <summary>
    /// Formato del email a enviar
    /// </summary>
    public enum EmailFormat
    {
        Markdown,
        Html,
        PlainText
    }
}
