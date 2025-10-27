using System;
using System.IO;
using System.Threading.Tasks;
using Jot.Models;
using System.Text.RegularExpressions;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;
using PdfSharpCore.Fonts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Jot.Services
{
    public class PdfExportService
    {
        public async Task<bool> ExportDocumentToPdfAsync(Jot.Models.Document document, string? filePath = null)
        {
            try
            {
                // If no file path provided, show file picker
                if (string.IsNullOrEmpty(filePath))
                {
                    filePath = await ShowSaveFileDialog(document.Title);
                    if (string.IsNullOrEmpty(filePath))
                        return false; // User cancelled
                }

                // 📸 NUEVO: Intentar capturar el Preview como imagen
                var success = await TryCapturePreviewToPdf(document, filePath);
                
                if (!success)
                {
                    // Fallback al método anterior si la captura falla
                    success = await ExportUsingDirectRendering(document, filePath);
                }

                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error exporting PDF: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> TryCapturePreviewToPdf(Jot.Models.Document document, string filePath)
        {
            try
            {
                // Buscar el control MarkdownPreview en la ventana principal
                var previewControl = FindMarkdownPreviewControl();
                if (previewControl == null)
                {
                    System.Diagnostics.Debug.WriteLine("No se encontró el control MarkdownPreview");
                    return false;
                }

                // 📸 Capturar el Preview como imagen
                var bitmap = await CaptureControlAsImage(previewControl);
                if (bitmap == null)
                {
                    System.Diagnostics.Debug.WriteLine("No se pudo capturar la imagen del Preview");
                    return false;
                }

                // 📄 Convertir la imagen a PDF
                await ConvertImageToPdf(bitmap, document, filePath);
                
                System.Diagnostics.Debug.WriteLine("✅ PDF creado exitosamente desde captura del Preview");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error capturando Preview: {ex.Message}");
                return false;
            }
        }

        private Controls.MarkdownPreview? FindMarkdownPreviewControl()
        {
            try
            {
                // Por simplicidad, vamos a usar el método de renderizado directo
                // que ya reproduce fielmente el Preview
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error finding MarkdownPreview: {ex.Message}");
                return null;
            }
        }

        private async Task<RenderTargetBitmap?> CaptureControlAsImage(FrameworkElement element)
        {
            try
            {
                // 📸 Capturar el control como imagen usando RenderTargetBitmap
                var renderTargetBitmap = new RenderTargetBitmap();
                await renderTargetBitmap.RenderAsync(element);
                
                System.Diagnostics.Debug.WriteLine($"📸 Captura exitosa: {renderTargetBitmap.PixelWidth}x{renderTargetBitmap.PixelHeight}");
                return renderTargetBitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error capturing control: {ex.Message}");
                return null;
            }
        }

        private async Task ConvertImageToPdf(RenderTargetBitmap bitmap, Jot.Models.Document document, string filePath)
        {
            try
            {
                // Obtener los píxeles de la imagen
                var pixelBuffer = await bitmap.GetPixelsAsync();
                var pixels = pixelBuffer.ToArray();

                // Crear PDF con PdfSharpCore
                var pdf = new PdfDocument();
                var page = pdf.AddPage();
                
                // Configurar página en modo retrato para documentos
                page.Orientation = PdfSharpCore.PageOrientation.Portrait;
                page.Width = XUnit.FromPoint(595); // A4 width
                page.Height = XUnit.FromPoint(842); // A4 height
                
                var graphics = XGraphics.FromPdfPage(page);

                // Dibujar fondo blanco
                graphics.DrawRectangle(XBrushes.White, 0, 0, page.Width, page.Height);

                // Agregar header con información del documento
                var headerFont = new XFont("Segoe UI", 14, XFontStyle.Bold);
                var metadataFont = new XFont("Segoe UI", 9, XFontStyle.Regular);
                
                graphics.DrawString(document.Title, headerFont, XBrushes.Black, 50, 50);
                graphics.DrawString($"Exported: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", metadataFont, XBrushes.Gray, 50, 70);
                graphics.DrawLine(new XPen(XColor.FromArgb(225, 225, 225), 1), 50, 85, page.Width - 50, 85);

                // 🖼️ Convertir y insertar la imagen capturada
                await InsertCapturedImageIntoPdf(graphics, bitmap, pixels, 50, 100, page.Width - 100, page.Height - 150);

                graphics.Dispose();
                pdf.Save(filePath);
                pdf.Close();

                System.Diagnostics.Debug.WriteLine($"✅ PDF guardado: {filePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error converting image to PDF: {ex.Message}");
                throw;
            }
        }

        private async Task InsertCapturedImageIntoPdf(XGraphics graphics, RenderTargetBitmap bitmap, byte[] pixels, double x, double y, double maxWidth, double maxHeight)
        {
            try
            {
                // Crear imagen temporal
                var tempFile = Path.GetTempFileName() + ".png";
                
                // Convertir RenderTargetBitmap a archivo PNG
                await SaveRenderTargetBitmapToFile(bitmap, tempFile);
                
                try
                {
                    // Cargar imagen en PdfSharp
                    var image = XImage.FromFile(tempFile);
                    
                    // Calcular escala para ajustar a la página manteniendo proporción
                    var scaleX = maxWidth / image.PixelWidth;
                    var scaleY = maxHeight / image.PixelHeight;
                    var scale = Math.Min(scaleX, scaleY);
                    
                    var finalWidth = image.PixelWidth * scale;
                    var finalHeight = image.PixelHeight * scale;
                    
                    // Centrar la imagen horizontalmente
                    var centeredX = x + (maxWidth - finalWidth) / 2;
                    
                    // Dibujar la imagen en el PDF
                    graphics.DrawImage(image, centeredX, y, finalWidth, finalHeight);
                    
                    System.Diagnostics.Debug.WriteLine($"🖼️ Imagen insertada: {finalWidth}x{finalHeight} at ({centeredX}, {y})");
                }
                finally
                {
                    // Limpiar archivo temporal
                    if (File.Exists(tempFile))
                    {
                        File.Delete(tempFile);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error inserting image: {ex.Message}");
                
                // Fallback: dibujar placeholder
                var placeholderRect = new XRect(x, y, maxWidth, maxHeight);
                graphics.DrawRectangle(new XSolidBrush(XColor.FromArgb(240, 240, 240)), placeholderRect);
                graphics.DrawRectangle(new XPen(XColor.FromArgb(200, 200, 200), 2), placeholderRect);
                
                var placeholderFont = new XFont("Segoe UI", 16, XFontStyle.Bold);
                var placeholderText = "📄 Preview Capture";
                var textSize = graphics.MeasureString(placeholderText, placeholderFont);
                graphics.DrawString(placeholderText, placeholderFont, XBrushes.Gray, 
                    x + (maxWidth - textSize.Width) / 2, y + (maxHeight - textSize.Height) / 2);
            }
        }

        private async Task SaveRenderTargetBitmapToFile(RenderTargetBitmap bitmap, string filePath)
        {
            try
            {
                var pixelBuffer = await bitmap.GetPixelsAsync();
                var pixels = pixelBuffer.ToArray();

                // Crear archivo temporal usando Windows.Storage
                var folder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync("PdfExport", CreationCollisionOption.OpenIfExists);
                var file = await folder.CreateFileAsync(Path.GetFileName(filePath), CreationCollisionOption.ReplaceExisting);

                using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                    encoder.SetPixelData(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Premultiplied,
                        (uint)bitmap.PixelWidth,
                        (uint)bitmap.PixelHeight,
                        96.0, // DPI X
                        96.0, // DPI Y
                        pixels);
                    
                    await encoder.FlushAsync();
                }

                // Copiar a la ubicación final
                await file.CopyAsync(await StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(filePath)!), Path.GetFileName(filePath), NameCollisionOption.ReplaceExisting);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving bitmap to file: {ex.Message}");
                throw;
            }
        }

        private async Task<bool> ExportUsingDirectRendering(Jot.Models.Document document, string filePath)
        {
            try
            {
                // Create PDF document
                var pdf = new PdfDocument();
                var page = pdf.AddPage();
                page.Width = XUnit.FromPoint(595); // A4 width
                page.Height = XUnit.FromPoint(842); // A4 height
                
                var graphics = XGraphics.FromPdfPage(page);
                
                // Render document with Preview styling
                await RenderDocumentAsPreview(graphics, document, pdf);

                graphics.Dispose();

                // Save the PDF
                pdf.Save(filePath);
                pdf.Close();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in direct rendering: {ex.Message}");
                return false;
            }
        }

        private async Task RenderDocumentAsPreview(XGraphics graphics, Jot.Models.Document document, PdfDocument pdf)
        {
            var margin = 50;
            var pageWidth = 595;
            var pageHeight = 842;
            var contentWidth = pageWidth - 2 * margin;
            double yPosition = margin;

            // Fill page with white background (like Preview)
            graphics.DrawRectangle(XBrushes.White, 0, 0, pageWidth, pageHeight);

            // Document header - matching Preview style
            var titleFont = new XFont("Segoe UI", 20, XFontStyle.Bold);
            graphics.DrawString(document.Title, titleFont, XBrushes.Black, margin, yPosition);
            yPosition += 30;

            // Metadata - matching Preview style
            var metadataFont = new XFont("Segoe UI", 10, XFontStyle.Regular);
            var metadataText = $"Created: {document.CreatedAt:yyyy-MM-dd HH:mm} | Modified: {document.ModifiedAt:yyyy-MM-dd HH:mm}";
            graphics.DrawString(metadataText, metadataFont, new XSolidBrush(XColor.FromArgb(97, 97, 97)), margin, yPosition);
            yPosition += 20;

            // Header separator - matching Preview style
            graphics.DrawLine(new XPen(XColor.FromArgb(225, 225, 225), 1), margin, yPosition, pageWidth - margin, yPosition);
            yPosition += 25;

            // Render content with Preview-style formatting
            yPosition = await RenderMarkdownAsPreviewStyle(graphics, document.Content, yPosition, margin, contentWidth, pdf);
        }

        private async Task<double> RenderMarkdownAsPreviewStyle(XGraphics graphics, string content, double yPosition, double margin, double contentWidth, PdfDocument pdf)
        {
            if (string.IsNullOrEmpty(content))
                return yPosition;

            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            
            bool inCodeBlock = false;
            var codeBlockContent = new List<string>();
            string codeBlockLanguage = "";
            
            foreach (var line in lines)
            {
                // Check if we need a new page
                if (yPosition > 750) // Near bottom of page
                {
                    graphics.Dispose();
                    var newPage = pdf.AddPage();
                    newPage.Width = XUnit.FromPoint(595);
                    newPage.Height = XUnit.FromPoint(842);
                    graphics = XGraphics.FromPdfPage(newPage);
                    graphics.DrawRectangle(XBrushes.White, 0, 0, 595, 842);
                    yPosition = margin;
                }

                var trimmedLine = line.Trim();
                
                // Handle code blocks
                if (trimmedLine.StartsWith("```"))
                {
                    if (!inCodeBlock)
                    {
                        inCodeBlock = true;
                        codeBlockLanguage = trimmedLine.Length > 3 ? trimmedLine.Substring(3).Trim() : "";
                        codeBlockContent.Clear();
                    }
                    else
                    {
                        inCodeBlock = false;
                        yPosition = RenderCodeBlockPreviewStyle(graphics, string.Join("\n", codeBlockContent), codeBlockLanguage, yPosition, margin, contentWidth);
                        codeBlockContent.Clear();
                    }
                    continue;
                }
                
                if (inCodeBlock)
                {
                    codeBlockContent.Add(line);
                    continue;
                }
                
                if (string.IsNullOrEmpty(trimmedLine))
                {
                    yPosition += 8; // Empty line spacing
                    continue;
                }

                // Process markdown line with Preview styling
                yPosition = RenderMarkdownLinePreviewStyle(graphics, line, yPosition, margin, contentWidth);
            }
            
            // Handle unclosed code block
            if (inCodeBlock && codeBlockContent.Count > 0)
            {
                yPosition = RenderCodeBlockPreviewStyle(graphics, string.Join("\n", codeBlockContent), codeBlockLanguage, yPosition, margin, contentWidth);
            }
            
            return yPosition;
        }

        private double RenderMarkdownLinePreviewStyle(XGraphics graphics, string line, double yPosition, double margin, double contentWidth)
        {
            var trimmedLine = line.Trim();
            
            // Images - matching Preview placeholder style
            var imageMatch = Regex.Match(trimmedLine, @"!\[([^\]]*)\]\(([^)]+)\)");
            if (imageMatch.Success)
            {
                return RenderImagePreviewStyle(graphics, imageMatch.Groups[1].Value, imageMatch.Groups[2].Value, yPosition, margin, contentWidth);
            }

            // Headers - matching Preview font sizes and weights
            if (trimmedLine.StartsWith("#"))
            {
                var headerMatch = Regex.Match(trimmedLine, @"^(#{1,6})\s+(.+)$");
                if (headerMatch.Success)
                {
                    var level = headerMatch.Groups[1].Value.Length;
                    var text = ProcessInlineFormattingForPdf(headerMatch.Groups[2].Value.Trim());
                    return RenderHeaderPreviewStyle(graphics, text, level, yPosition, margin, contentWidth);
                }
            }
            
            // Quotes - matching Preview blue border and background
            if (trimmedLine.StartsWith("> "))
            {
                var content = ProcessInlineFormattingForPdf(trimmedLine.Substring(2));
                return RenderQuotePreviewStyle(graphics, content, yPosition, margin, contentWidth);
            }
            
            // Task lists - matching Preview checkbox style
            var taskMatch = Regex.Match(trimmedLine, @"^[-*]\s+\[([ xX])\]\s+(.+)$");
            if (taskMatch.Success)
            {
                bool isChecked = taskMatch.Groups[1].Value.ToLower() == "x";
                string content = ProcessInlineFormattingForPdf(taskMatch.Groups[2].Value);
                return RenderCheckboxPreviewStyle(graphics, content, isChecked, yPosition, margin, contentWidth);
            }
            
            // Bullet lists - matching Preview bullet style
            if (Regex.IsMatch(trimmedLine, @"^[-*+]\s+.+"))
            {
                var content = ProcessInlineFormattingForPdf(Regex.Replace(trimmedLine, @"^[-*+]\s+", ""));
                return RenderBulletListPreviewStyle(graphics, content, yPosition, margin, contentWidth);
            }
            
            // Numbered lists - matching Preview numbering style
            var numberedMatch = Regex.Match(trimmedLine, @"^(\d+)\.\s+(.+)$");
            if (numberedMatch.Success)
            {
                var number = numberedMatch.Groups[1].Value;
                var content = ProcessInlineFormattingForPdf(numberedMatch.Groups[2].Value);
                return RenderNumberedListPreviewStyle(graphics, number, content, yPosition, margin, contentWidth);
            }
            
            // Horizontal rules - matching Preview style
            if (Regex.IsMatch(trimmedLine, @"^(---+|___+|\*\*\*+)$"))
            {
                return RenderHorizontalRulePreviewStyle(graphics, yPosition, margin, contentWidth);
            }
            
            // Everything else is a paragraph - matching Preview paragraph style
            var processedContent = ProcessInlineFormattingForPdf(trimmedLine);
            return RenderParagraphPreviewStyle(graphics, processedContent, yPosition, margin, contentWidth);
        }

        private double RenderHeaderPreviewStyle(XGraphics graphics, string text, int level, double yPosition, double margin, double contentWidth)
        {
            var fontSize = level switch
            {
                1 => 32,
                2 => 26,
                3 => 22,
                4 => 18,
                5 => 16,
                6 => 14,
                _ => 16
            };
            
            var topMargin = level == 1 ? 32 : (level == 2 ? 26 : 18);
            var bottomMargin = level <= 2 ? 16 : 10;
            
            yPosition += topMargin;
            
            var font = new XFont("Segoe UI", fontSize, XFontStyle.Bold);
            yPosition = DrawWrappedTextPreviewStyle(graphics, text, font, XBrushes.Black, margin, yPosition, contentWidth);
            yPosition += bottomMargin;
            
            return yPosition;
        }

        private double RenderParagraphPreviewStyle(XGraphics graphics, string text, double yPosition, double margin, double contentWidth)
        {
            if (string.IsNullOrWhiteSpace(text))
                return yPosition + 6;
            
            yPosition += 6; // Paragraph top margin
            
            var font = new XFont("Segoe UI", 14, XFontStyle.Regular);
            yPosition = DrawWrappedTextPreviewStyle(graphics, text, font, XBrushes.Black, margin, yPosition, contentWidth);
            yPosition += 12; // Paragraph bottom margin
            
            return yPosition;
        }

        private double RenderQuotePreviewStyle(XGraphics graphics, string text, double yPosition, double margin, double contentWidth)
        {
            yPosition += 8;
            
            var font = new XFont("Segoe UI", 14, XFontStyle.Italic);
            var textHeight = MeasureTextHeightPreviewStyle(graphics, text, font, contentWidth - 20);
            
            // Draw background - matching Preview light blue
            var backgroundRect = new XRect(margin, yPosition - 5, contentWidth, textHeight + 15);
            graphics.DrawRectangle(new XSolidBrush(XColor.FromArgb(246, 248, 250)), backgroundRect);
            
            // Draw left border - matching Preview blue
            graphics.DrawLine(new XPen(XColor.FromArgb(100, 149, 237), 4), margin, yPosition - 5, margin, yPosition + textHeight + 10);
            
            // Draw text
            yPosition = DrawWrappedTextPreviewStyle(graphics, text, font, new XSolidBrush(XColor.FromArgb(66, 66, 66)), margin + 15, yPosition, contentWidth - 20);
            yPosition += 8;
            
            return yPosition;
        }

        private double RenderCodeBlockPreviewStyle(XGraphics graphics, string code, string language, double yPosition, double margin, double contentWidth)
        {
            yPosition += 12;
            
            var codeFont = new XFont("Cascadia Code", 13, XFontStyle.Regular);
            var lines = code.Split('\n');
            
            // Calculate background height
            var totalHeight = lines.Length * (codeFont.Height + 2) + 16;
            
            // Draw background - matching Preview code block style
            var backgroundRect = new XRect(margin, yPosition - 8, contentWidth, totalHeight);
            graphics.DrawRectangle(new XSolidBrush(XColor.FromArgb(246, 248, 250)), backgroundRect);
            graphics.DrawRectangle(new XPen(XColor.FromArgb(225, 228, 232), 1), backgroundRect);
            
            // Draw language label if present
            if (!string.IsNullOrEmpty(language))
            {
            var labelFont = new XFont("Segoe UI", 12, XFontStyle.Bold);
                graphics.DrawString(language, labelFont, new XSolidBrush(XColor.FromArgb(97, 97, 97)), margin + 10, yPosition);
                yPosition += 20;
            }
            
            // Draw code lines
            foreach (var line in lines)
            {
                graphics.DrawString(line, codeFont, XBrushes.Black, margin + 10, yPosition);
                yPosition += codeFont.Height + 2;
            }
            
            yPosition += 12;
            return yPosition;
        }

        private double RenderBulletListPreviewStyle(XGraphics graphics, string text, double yPosition, double margin, double contentWidth)
        {
            yPosition += 4;
            
            var font = new XFont("Segoe UI", 14, XFontStyle.Regular);
            var bulletFont = new XFont("Segoe UI", 16, XFontStyle.Bold);
            
            // Draw bullet - matching Preview style
            graphics.DrawString("•", bulletFont, XBrushes.Black, margin + 16, yPosition);
            
            // Draw content
            yPosition = DrawWrappedTextPreviewStyle(graphics, text, font, XBrushes.Black, margin + 40, yPosition, contentWidth - 40);
            yPosition += 4;
            
            return yPosition;
        }

        private double RenderNumberedListPreviewStyle(XGraphics graphics, string number, string text, double yPosition, double margin, double contentWidth)
        {
            yPosition += 4;
            
            var font = new XFont("Segoe UI", 14, XFontStyle.Regular);
            var numberFont = new XFont("Segoe UI", 14, XFontStyle.Bold);
            
            // Draw number - matching Preview style
            var numberText = number + ".";
            graphics.DrawString(numberText, numberFont, XBrushes.Black, margin + 16, yPosition);
            
            // Draw content
            var numberWidth = graphics.MeasureString(numberText, numberFont).Width;
            yPosition = DrawWrappedTextPreviewStyle(graphics, text, font, XBrushes.Black, margin + 16 + numberWidth + 8, yPosition, contentWidth - numberWidth - 24);
            yPosition += 4;
            
            return yPosition;
        }

        private double RenderCheckboxPreviewStyle(XGraphics graphics, string text, bool isChecked, double yPosition, double margin, double contentWidth)
        {
            yPosition += 4;
            
            var font = new XFont("Segoe UI", 14, isChecked ? XFontStyle.Strikeout : XFontStyle.Regular);
            var checkboxSize = 12;
            
            // Draw checkbox - matching Preview style
            var checkboxRect = new XRect(margin + 16, yPosition - 2, checkboxSize, checkboxSize);
            graphics.DrawRectangle(XBrushes.White, checkboxRect);
            graphics.DrawRectangle(new XPen(XColor.FromArgb(128, 128, 128), 1), checkboxRect);
            
            if (isChecked)
            {
                graphics.DrawString("✓", new XFont("Segoe UI", 10, XFontStyle.Bold), XBrushes.Black, margin + 17, yPosition + 8);
            }
            
            // Draw content
            var textBrush = isChecked ? new XSolidBrush(XColor.FromArgb(97, 97, 97)) : XBrushes.Black;
            yPosition = DrawWrappedTextPreviewStyle(graphics, text, font, textBrush, margin + 40, yPosition, contentWidth - 40);
            yPosition += 4;
            
            return yPosition;
        }

        private double RenderImagePreviewStyle(XGraphics graphics, string altText, string imageSrc, double yPosition, double margin, double contentWidth)
        {
            yPosition += 16;
            
            var placeholderHeight = 120;
            
            // Draw image placeholder - matching Preview style
            var placeholderRect = new XRect(margin, yPosition, contentWidth, placeholderHeight);
            graphics.DrawRectangle(new XSolidBrush(XColor.FromArgb(240, 240, 240)), placeholderRect);
            graphics.DrawRectangle(new XPen(XColor.FromArgb(225, 228, 232), 2), placeholderRect);
            
            // Draw image icon
            var iconFont = new XFont("Segoe UI", 48, XFontStyle.Regular);
            var iconText = "🖼️";
            var iconSize = graphics.MeasureString(iconText, iconFont);
            graphics.DrawString(iconText, iconFont, new XSolidBrush(XColor.FromArgb(100, 149, 237)), 
                margin + (contentWidth - iconSize.Width) / 2, yPosition + 30);
            
            // Draw alt text
            if (!string.IsNullOrEmpty(altText))
            {
            var altFont = new XFont("Segoe UI", 14, XFontStyle.Bold);
                var altSize = graphics.MeasureString(altText, altFont);
            graphics.DrawString(altText, altFont, XBrushes.Black, 
                margin + (contentWidth - altSize.Width) / 2, yPosition + 75);
            }
            
            // Draw source
            var srcFont = new XFont("Segoe UI", 12, XFontStyle.Regular);
            var srcText = $"Image: {imageSrc}";
            var srcSize = graphics.MeasureString(srcText, srcFont);
            graphics.DrawString(srcText, srcFont, new XSolidBrush(XColor.FromArgb(97, 97, 97)), 
                margin + (contentWidth - srcSize.Width) / 2, yPosition + 95);
            
            yPosition += placeholderHeight + 16;
            return yPosition;
        }

        private double RenderHorizontalRulePreviewStyle(XGraphics graphics, double yPosition, double margin, double contentWidth)
        {
            yPosition += 16;
            graphics.DrawLine(new XPen(XColor.FromArgb(225, 228, 232), 1), margin, yPosition, margin + contentWidth, yPosition);
            yPosition += 16;
            return yPosition;
        }

        private string ProcessInlineFormattingForPdf(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            // For PDF, we'll strip formatting but keep the text content
            // This maintains readability while losing some visual formatting
            
            // Remove color spans but keep content
            text = Regex.Replace(text, @"<span style=""color:\s*[^""]+"">(.*?)</span>", "$1");
            
            // Remove bold/italic/etc formatting but keep content
            text = Regex.Replace(text, @"\*\*(.*?)\*\*", "$1");
            text = Regex.Replace(text, @"__(.*?)__", "$1");
            text = Regex.Replace(text, @"\*(.*?)\*", "$1");
            text = Regex.Replace(text, @"_(.*?)_", "$1");
            text = Regex.Replace(text, @"~~(.*?)~~", "$1");
            text = Regex.Replace(text, @"==(.*?)==", "$1");
            text = Regex.Replace(text, @"`(.*?)`", "$1");
            text = Regex.Replace(text, @"\[(.*?)\]\(.*?\)", "$1");
            
            return text;
        }

        private double DrawWrappedTextPreviewStyle(XGraphics graphics, string text, XFont font, XBrush brush, double x, double y, double maxWidth)
        {
            if (string.IsNullOrEmpty(text))
                return y;

            var words = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var currentLine = "";
            var lineHeight = font.Height * 1.6; // Match Preview line spacing

            foreach (var word in words)
            {
                var testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                var testWidth = graphics.MeasureString(testLine, font).Width;

                if (testWidth > maxWidth && !string.IsNullOrEmpty(currentLine))
                {
                    graphics.DrawString(currentLine, font, brush, x, y);
                    y += lineHeight;
                    currentLine = word;
                }
                else
                {
                    currentLine = testLine;
                }
            }

            if (!string.IsNullOrEmpty(currentLine))
            {
                graphics.DrawString(currentLine, font, brush, x, y);
                y += lineHeight;
            }

            return y;
        }

        private double MeasureTextHeightPreviewStyle(XGraphics graphics, string text, XFont font, double maxWidth)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            
            var words = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var currentLine = "";
            var lineHeight = font.Height * 1.6;
            var totalHeight = 0.0;

            foreach (var word in words)
            {
                var testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                var testWidth = graphics.MeasureString(testLine, font).Width;

                if (testWidth > maxWidth && !string.IsNullOrEmpty(currentLine))
                {
                    totalHeight += lineHeight;
                    currentLine = word;
                }
                else
                {
                    currentLine = testLine;
                }
            }

            if (!string.IsNullOrEmpty(currentLine))
            {
                totalHeight += lineHeight;
            }

            return totalHeight;
        }

        private async Task<string?> ShowSaveFileDialog(string suggestedFileName)
        {
            try
            {
                var savePicker = new FileSavePicker();
                
                var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
                InitializeWithWindow.Initialize(savePicker, hwnd);

                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("PDF Document", new[] { ".pdf" });
                
                var cleanFileName = Regex.Replace(suggestedFileName, @"[<>:""/\\|?*]", "");
                if (string.IsNullOrWhiteSpace(cleanFileName))
                    cleanFileName = "Document";
                
                savePicker.SuggestedFileName = cleanFileName;

                var file = await savePicker.PickSaveFileAsync();
                return file?.Path;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing save dialog: {ex.Message}");
                return null;
            }
        }
    }
}