using System;
using System.IO;
using System.Threading.Tasks;
using Jot.Models;
using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;
using PdfSharpCore.Fonts;
using Markdig;
using System.Text.RegularExpressions;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using System.Collections.Generic;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MarkdigCodeBlock = Markdig.Syntax.CodeBlock;

namespace Jot.Services
{
    public class PdfExportService
    {
        private const double PageWidth = 595; // A4 width in points
        private const double PageHeight = 842; // A4 height in points
        private const double Margin = 50;
        private const double ContentWidth = PageWidth - 2 * Margin;
        private const double ContentHeight = PageHeight - 2 * Margin;

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

                var pdf = new PdfDocument();
                var page = pdf.AddPage();
                page.Width = PageWidth;
                page.Height = PageHeight;
                
                var graphics = XGraphics.FromPdfPage(page);
                
                // Fill page with white background
                graphics.DrawRectangle(XBrushes.White, 0, 0, PageWidth, PageHeight);

                double yPosition = Margin;

                // Draw title
                var titleFont = new XFont("Segoe UI", 20, XFontStyle.Bold);
                graphics.DrawString(document.Title, titleFont, XBrushes.Black, Margin, yPosition);
                yPosition += 35;

                // Draw metadata
                var metadataFont = new XFont("Segoe UI", 9, XFontStyle.Regular);
                var metadataText = $"Created: {document.CreatedAt:yyyy-MM-dd HH:mm} | Modified: {document.ModifiedAt:yyyy-MM-dd HH:mm}";
                graphics.DrawString(metadataText, metadataFont, XBrushes.Gray, Margin, yPosition);
                yPosition += 20;

                // Draw separator line
                graphics.DrawLine(new XPen(XColor.FromArgb(225, 225, 225), 1), Margin, yPosition, PageWidth - Margin, yPosition);
                yPosition += 25;

                // Parse markdown and render as preview
                yPosition = await RenderMarkdownAsPreview(graphics, document.Content, yPosition, pdf);

                graphics.Dispose();

                // Save the PDF
                pdf.Save(filePath);
                pdf.Close();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error exporting PDF: {ex.Message}");
                return false;
            }
        }

        private async Task<double> RenderMarkdownAsPreview(XGraphics graphics, string content, double yPosition, PdfDocument pdf)
        {
            if (string.IsNullOrEmpty(content))
                return yPosition;

            // Parse markdown using Markdig
            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();
            
            var document = Markdown.Parse(content, pipeline);
            
            foreach (var block in document)
            {
                yPosition = await RenderBlock(graphics, block, yPosition, pdf);
            }
            
            return yPosition;
        }

        private async Task<double> RenderBlock(XGraphics graphics, Block block, double yPosition, PdfDocument pdf)
        {
            // Check if we need a new page
            if (yPosition > PageHeight - Margin - 100)
            {
                graphics.Dispose();
                var newPage = pdf.AddPage();
                newPage.Width = PageWidth;
                newPage.Height = PageHeight;
                graphics = XGraphics.FromPdfPage(newPage);
                graphics.DrawRectangle(XBrushes.White, 0, 0, PageWidth, PageHeight);
                yPosition = Margin;
            }

            switch (block)
            {
                case HeadingBlock heading:
                    yPosition = RenderHeading(graphics, heading, yPosition);
                    break;
                    
                case ParagraphBlock paragraph:
                    yPosition = RenderParagraph(graphics, paragraph, yPosition);
                    break;
                    
                case ListBlock list:
                    yPosition = RenderList(graphics, list, yPosition);
                    break;
                    
                case MarkdigCodeBlock codeBlock:
                    yPosition = RenderCodeBlock(graphics, codeBlock, yPosition);
                    break;
                    
                case QuoteBlock quote:
                    yPosition = RenderQuote(graphics, quote, yPosition);
                    break;
                    
                case ThematicBreakBlock:
                    yPosition = RenderThematicBreak(graphics, yPosition);
                    break;
                    
                default:
                    // Handle other block types as paragraphs
                    yPosition = RenderGenericBlock(graphics, block, yPosition);
                    break;
            }
            
            return yPosition;
        }

        private double RenderHeading(XGraphics graphics, HeadingBlock heading, double yPosition)
        {
            var fontSize = heading.Level switch
            {
                1 => 24,
                2 => 20,
                3 => 16,
                4 => 14,
                5 => 12,
                6 => 11,
                _ => 12
            };
            
            var topMargin = heading.Level <= 2 ? 20 : 15;
            var bottomMargin = heading.Level <= 2 ? 10 : 8;
            
            yPosition += topMargin;
            
            var font = new XFont("Segoe UI", fontSize, XFontStyle.Bold);
            var text = ExtractPlainText(heading.Inline);
            
            yPosition = DrawWrappedText(graphics, text, font, XBrushes.Black, Margin, yPosition, ContentWidth);
            yPosition += bottomMargin;
            
            return yPosition;
        }

        private double RenderParagraph(XGraphics graphics, ParagraphBlock paragraph, double yPosition)
        {
            yPosition += 5; // Small top margin
            
            var font = new XFont("Segoe UI", 11, XFontStyle.Regular);
            var text = ExtractPlainText(paragraph.Inline);
            
            if (!string.IsNullOrWhiteSpace(text))
            {
                yPosition = DrawWrappedText(graphics, text, font, XBrushes.Black, Margin, yPosition, ContentWidth);
            }
            
            yPosition += 12; // Paragraph spacing
            return yPosition;
        }

        private double RenderList(XGraphics graphics, ListBlock list, double yPosition)
        {
            yPosition += 5;
            
            var font = new XFont("Segoe UI", 11, XFontStyle.Regular);
            int itemNumber = 1;
            
            foreach (var item in list)
            {
                if (item is ListItemBlock listItem)
                {
                    string bullet;
                    if (list.IsOrdered)
                    {
                        bullet = $"{itemNumber}. ";
                        itemNumber++;
                    }
                    else
                    {
                        bullet = "• ";
                    }
                    
                    // Draw bullet
                    graphics.DrawString(bullet, font, XBrushes.Black, Margin + 10, yPosition);
                    
                    // Draw item content
                    var itemText = ExtractPlainText(listItem);
                    if (!string.IsNullOrWhiteSpace(itemText))
                    {
                        var bulletWidth = graphics.MeasureString(bullet, font).Width;
                        yPosition = DrawWrappedText(graphics, itemText, font, XBrushes.Black, 
                            Margin + 10 + bulletWidth, yPosition, ContentWidth - bulletWidth - 10);
                    }
                    
                    yPosition += 18; // List item spacing
                }
            }
            
            yPosition += 8; // List bottom margin
            return yPosition;
        }

        private double RenderCodeBlock(XGraphics graphics, MarkdigCodeBlock codeBlock, double yPosition)
        {
            yPosition += 10;
            
            var codeFont = new XFont("Consolas", 9, XFontStyle.Regular);
            var codeLines = new List<string>();
            
            // Extract code lines
            for (int i = 0; i < codeBlock.Lines.Count; i++)
            {
                codeLines.Add(codeBlock.Lines.Lines[i].ToString());
            }
            
            if (codeLines.Count > 0)
            {
                // Calculate background size
                var maxWidth = 0.0;
                var totalHeight = 0.0;
                
                foreach (var line in codeLines)
                {
                    var lineSize = graphics.MeasureString(line, codeFont);
                    if (lineSize.Width > maxWidth) maxWidth = lineSize.Width;
                    totalHeight += lineSize.Height + 2;
                }
                
                var backgroundRect = new XRect(Margin, yPosition - 8, 
                    Math.Min(maxWidth + 20, ContentWidth), totalHeight + 16);
                graphics.DrawRectangle(new XSolidBrush(XColor.FromArgb(246, 248, 250)), backgroundRect);
                graphics.DrawRectangle(new XPen(XColor.FromArgb(208, 215, 222), 1), backgroundRect);
                
                // Draw code text
                foreach (var line in codeLines)
                {
                    graphics.DrawString(line, codeFont, XBrushes.Black, Margin + 10, yPosition);
                    yPosition += codeFont.Height + 2;
                }
                
                yPosition += 10;
            }
            
            return yPosition;
        }

        private double RenderQuote(XGraphics graphics, QuoteBlock quote, double yPosition)
        {
            yPosition += 10;
            
            var quoteFont = new XFont("Segoe UI", 11, XFontStyle.Italic);
            var text = ExtractPlainText(quote);
            
            if (!string.IsNullOrWhiteSpace(text))
            {
                // Draw quote border line
                var textHeight = MeasureTextHeight(graphics, text, quoteFont, ContentWidth - 20);
                graphics.DrawLine(new XPen(XColor.FromArgb(208, 215, 222), 4), 
                    Margin, yPosition - 5, Margin, yPosition + textHeight + 5);
                
                // Draw quote background
                var backgroundRect = new XRect(Margin, yPosition - 5, ContentWidth, textHeight + 10);
                graphics.DrawRectangle(new XSolidBrush(XColor.FromArgb(246, 248, 250)), backgroundRect);
                
                // Draw quote text
                yPosition = DrawWrappedText(graphics, text, quoteFont, XBrushes.DarkGray, 
                    Margin + 15, yPosition, ContentWidth - 20);
                
                yPosition += 10;
            }
            
            return yPosition;
        }

        private double RenderThematicBreak(XGraphics graphics, double yPosition)
        {
            yPosition += 15;
            graphics.DrawLine(new XPen(XColor.FromArgb(208, 215, 222), 1), 
                Margin, yPosition, PageWidth - Margin, yPosition);
            yPosition += 15;
            return yPosition;
        }

        private double RenderGenericBlock(XGraphics graphics, Block block, double yPosition)
        {
            var text = ExtractPlainText(block);
            if (!string.IsNullOrWhiteSpace(text))
            {
                var font = new XFont("Segoe UI", 11, XFontStyle.Regular);
                yPosition = DrawWrappedText(graphics, text, font, XBrushes.Black, Margin, yPosition, ContentWidth);
                yPosition += 12;
            }
            return yPosition;
        }

        private string ExtractPlainText(ContainerInline? inline)
        {
            if (inline == null) return string.Empty;
            
            var result = "";
            foreach (var item in inline)
            {
                result += ExtractPlainTextFromInline(item);
            }
            return result;
        }

        private string ExtractPlainText(Block block)
        {
            if (block is LeafBlock leafBlock && leafBlock.Inline != null)
            {
                return ExtractPlainText(leafBlock.Inline);
            }
            
            if (block is ContainerBlock containerBlock)
            {
                var result = "";
                foreach (var child in containerBlock)
                {
                    result += ExtractPlainText(child) + " ";
                }
                return result.Trim();
            }
            
            return "";
        }

        private string ExtractPlainTextFromInline(Inline inline)
        {
            return inline switch
            {
                LiteralInline literal => literal.Content.ToString(),
                CodeInline code => code.Content,
                EmphasisInline emphasis => ExtractPlainText(emphasis),
                LinkInline link => ExtractPlainText(link),
                _ => ""
            };
        }

        private double DrawWrappedText(XGraphics graphics, string text, XFont font, XBrush brush, 
            double x, double y, double maxWidth)
        {
            if (string.IsNullOrEmpty(text))
                return y;

            var words = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var currentLine = "";
            var lineHeight = font.Height + 2;

            foreach (var word in words)
            {
                var testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                var testWidth = graphics.MeasureString(testLine, font).Width;

                if (testWidth > maxWidth && !string.IsNullOrEmpty(currentLine))
                {
                    // Draw current line
                    graphics.DrawString(currentLine, font, brush, x, y);
                    y += lineHeight;
                    currentLine = word;
                }
                else
                {
                    currentLine = testLine;
                }
            }

            // Draw the last line
            if (!string.IsNullOrEmpty(currentLine))
            {
                graphics.DrawString(currentLine, font, brush, x, y);
                y += lineHeight;
            }

            return y;
        }

        private double MeasureTextHeight(XGraphics graphics, string text, XFont font, double maxWidth)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            
            var words = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var currentLine = "";
            var lineHeight = font.Height + 2;
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
                
                // Get the current window
                var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
                InitializeWithWindow.Initialize(savePicker, hwnd);

                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("PDF Document", new[] { ".pdf" });
                
                // Clean the suggested filename
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

        private string ProcessInlineFormatting(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // Process HTML color spans first (preserve them temporarily)
            var colorSpans = new Dictionary<string, string>();
            var colorSpanPattern = @"<span style=""color:\s*([^""]+)"">([^<]+)</span>";
            var colorMatches = Regex.Matches(text, colorSpanPattern);
            
            for (int i = 0; i < colorMatches.Count; i++)
            {
                var placeholder = $"__COLORSPAN_{i}__";
                colorSpans[placeholder] = colorMatches[i].Groups[2].Value; // Keep just the text for now
                text = text.Replace(colorMatches[i].Value, placeholder);
            }

            // Process inline formatting in order of priority
            // Bold: **text** or __text__
            text = Regex.Replace(text, @"\*\*(.*?)\*\*", "$1");
            text = Regex.Replace(text, @"__(.*?)__", "$1");
            
            // Italic: *text* or _text_ (but not if already processed as bold)
            text = Regex.Replace(text, @"(?<!\*)\*([^*]+?)\*(?!\*)", "$1");
            text = Regex.Replace(text, @"(?<!_)_([^_]+?)_(?!_)", "$1");
            
            // Strikethrough: ~~text~~
            text = Regex.Replace(text, @"~~(.*?)~~", "$1");
            
            // Inline code: `text`
            text = Regex.Replace(text, @"`([^`]+?)`", "$1");
            
            // Links: [text](url) -> text
            text = Regex.Replace(text, @"\[([^\]]+?)\]\([^\)]+?\)", "$1");
            
            // Images: ![alt](url) -> [Image: alt]
            text = Regex.Replace(text, @"!\[([^\]]*?)\]\([^\)]+?\)", "[Image: $1]");
            
            // Restore color spans (for now just as plain text, color formatting could be added later)
            foreach (var span in colorSpans)
            {
                text = text.Replace(span.Key, span.Value);
            }
            
            // Remove any remaining markdown artifacts
            text = text.Replace("\\*", "*").Replace("\\_", "_");

            return text;
        }
    }
}