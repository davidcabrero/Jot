using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jot.Models;

namespace Jot.Services
{
    /// <summary>
    /// Servicio para imprimir documentos
    /// </summary>
    public class PrintService
    {
        /// <summary>
        /// Imprime un documento generando HTML y abriéndolo en el navegador
        /// </summary>
        public async Task<bool> PrintDocumentAsync(Document document)
        {
            try
            {
                // Crear el servicio de exportación HTML
                var htmlService = new HtmlExportService();
                var html = htmlService.GenerateHtmlFromDocument(document);

                // Agregar estilos de impresión optimizados
                var printHtml = html.Replace("</head>", @"
<style>
    @media print {
        body {
            margin: 1in;
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
        }
        @page {
            margin: 1in;
        }
    }
    @media screen {
        body {
            max-width: 8.5in;
            margin: 20px auto;
            padding: 20px;
            background: white;
            box-shadow: 0 0 10px rgba(0,0,0,0.1);
        }
    }
</style>
<script>
    // Auto-abrir diálogo de impresión
    window.onload = function() {
        setTimeout(function() {
            window.print();
        }, 500);
    };
</script>
</head>");

                // Crear archivo temporal HTML
                var tempFileName = $"Jot_Print_{SanitizeFileName(document.Title)}_{DateTime.Now:yyyyMMdd_HHmmss}.html";
                var tempPath = Path.Combine(Path.GetTempPath(), tempFileName);

                await File.WriteAllTextAsync(tempPath, printHtml);

                // Abrir en el navegador predeterminado
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true
                };

                System.Diagnostics.Process.Start(psi);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error printing document: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Imprime un documento de texto plano
        /// </summary>
        public async Task<bool> PrintPlainTextDocumentAsync(Document document)
        {
            try
            {
                // Crear HTML simple para texto plano
                var html = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>{EscapeHtml(document.Title)}</title>
    <style>
        @media print {{
            body {{
                margin: 1in;
                font-family: 'Consolas', 'Courier New', monospace;
                font-size: 11pt;
                line-height: 1.5;
            }}
            @page {{
                margin: 1in;
            }}
            .header {{
                border-bottom: 2px solid #333;
                padding-bottom: 10px;
                margin-bottom: 20px;
            }}
            h1 {{
                margin: 0;
                font-size: 18pt;
                color: #333;
            }}
            .metadata {{
                font-size: 9pt;
                color: #666;
                margin-top: 5px;
            }}
            .content {{
                white-space: pre-wrap;
                word-wrap: break-word;
            }}
        }}
        @media screen {{
            body {{
                max-width: 8.5in;
                margin: 20px auto;
                padding: 40px;
                background: white;
                box-shadow: 0 0 15px rgba(0,0,0,0.1);
                font-family: 'Consolas', 'Courier New', monospace;
                font-size: 11pt;
                line-height: 1.5;
            }}
            .header {{
                border-bottom: 2px solid #333;
                padding-bottom: 10px;
                margin-bottom: 20px;
            }}
            h1 {{
                margin: 0;
                font-size: 18pt;
                color: #333;
            }}
            .metadata {{
                font-size: 9pt;
                color: #666;
                margin-top: 5px;
            }}
            .content {{
                white-space: pre-wrap;
                word-wrap: break-word;
            }}
        }}
    </style>
    <script>
        window.onload = function() {{
            setTimeout(function() {{
                window.print();
            }}, 500);
        }};
    </script>
</head>
<body>
    <div class='header'>
        <h1>{EscapeHtml(document.Title)}</h1>
        <div class='metadata'>
            Created: {document.CreatedAt:dd/MM/yyyy HH:mm} | Modified: {document.ModifiedAt:dd/MM/yyyy HH:mm}
        </div>
    </div>
    <div class='content'>{EscapeHtml(document.Content ?? "")}</div>
    <div style='margin-top: 30px; padding-top: 10px; border-top: 1px solid #ccc; font-size: 8pt; color: #999; text-align: center;'>
        Printed from Jot - Modern Note Taking
    </div>
</body>
</html>";

                // Crear archivo temporal HTML
                var tempFileName = $"Jot_Print_{SanitizeFileName(document.Title)}_{DateTime.Now:yyyyMMdd_HHmmss}.html";
                var tempPath = Path.Combine(Path.GetTempPath(), tempFileName);

                await File.WriteAllTextAsync(tempPath, html);

                // Abrir en el navegador predeterminado
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true
                };

                System.Diagnostics.Process.Start(psi);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error printing plain text document: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Limpia el nombre del archivo para que sea válido
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "document";

            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(fileName
                .Where(c => !invalidChars.Contains(c))
                .ToArray());

            if (sanitized.Length > 100)
                sanitized = sanitized.Substring(0, 100);

            return string.IsNullOrWhiteSpace(sanitized) ? "document" : sanitized;
        }

        /// <summary>
        /// Escapa caracteres HTML especiales
        /// </summary>
        private string EscapeHtml(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
        }
    }
}
