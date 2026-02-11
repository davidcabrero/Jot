using System;
using System.IO;
using System.Threading.Tasks;
using Jot.Models;
using System.Text.RegularExpressions;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using System.Text;
using System.Linq;
using System.Diagnostics;

namespace Jot.Services
{
    public class HtmlExportService
    {
        public async Task<bool> ExportDocumentToHtmlAsync(Jot.Models.Document document, string? filePath = null)
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

       // Generate HTML content that matches the preview exactly
     var htmlContent = GenerateHtmlFromDocument(document);
        
                // Write to file
    await File.WriteAllTextAsync(filePath, htmlContent, Encoding.UTF8);
     
     // Open the HTML file in default browser
     await OpenHtmlFile(filePath);
      
     System.Diagnostics.Debug.WriteLine($"✅ HTML exported successfully: {filePath}");
    return true;
 }
            catch (Exception ex)
        {
         System.Diagnostics.Debug.WriteLine($"Error exporting HTML: {ex.Message}");
          return false;
            }
        }

    public string GenerateHtmlFromDocument(Jot.Models.Document document)
        {
 var html = new StringBuilder();
            
 // HTML Document structure with Preview-like styling
 html.AppendLine("<!DOCTYPE html>");
       html.AppendLine("<html lang=\"en\">");
         html.AppendLine("<head>");
      html.AppendLine("    <meta charset=\"UTF-8\">");
  html.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
          html.AppendLine($"    <title>{EscapeHtml(document.Title)}</title>");
      
    // Add CSS that exactly matches the Preview styling
            html.AppendLine(GetPreviewStyleCss());
 
            // Add MathJax for formula rendering
       html.AppendLine("    <script src=\"https://polyfill.io/v3/polyfill.min.js?features=es6\"></script>");
       html.AppendLine("    <script id=\"MathJax-script\" async src=\"https://cdn.jsdelivr.net/npm/mathjax@3/es5/tex-mml-chtml.js\"></script>");
            html.AppendLine("    <script>");
 html.AppendLine("      window.MathJax = {");
      html.AppendLine("   tex: {");
   html.AppendLine("  inlineMath: [['$', '$'], ['\\\\(', '\\\\)']],");
  html.AppendLine("        displayMath: [['$$', '$$'], ['\\\\[', '\\\\]']]");
            html.AppendLine(" },");
            html.AppendLine("   options: {");
            html.AppendLine("       skipHtmlTags: ['script', 'noscript', 'style', 'textarea', 'pre']");
            html.AppendLine("            }");
        html.AppendLine("        };");
            html.AppendLine("    </script>");
          
     html.AppendLine("</head>");
            html.AppendLine("<body>");
       html.AppendLine("    <div class=\"container\">");
   
     // Document header
            html.AppendLine("        <header class=\"document-header\">");
         html.AppendLine($"            <h1 class=\"document-title\">{EscapeHtml(document.Title)}</h1>");
     html.AppendLine("   <div class=\"document-metadata\">");
            html.AppendLine($"                <span>Created: {document.CreatedAt:yyyy-MM-dd HH:mm}</span>");
            html.AppendLine($"<span>Modified: {document.ModifiedAt:yyyy-MM-dd HH:mm}</span>");
    html.AppendLine("     </div>");
         html.AppendLine("        </header>");
            
            // Document content
     html.AppendLine("        <main class=\"document-content\">");
            var contentHtml = ConvertMarkdownToHtml(document.Content ?? "");
      html.AppendLine(contentHtml);
       html.AppendLine("  </main>");
            
            // Footer
    html.AppendLine("        <footer class=\"document-footer\">");
      html.AppendLine($"      <p>Exported from Jot on {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
    html.AppendLine("        </footer>");
            
            html.AppendLine("    </div>");
       html.AppendLine("</body>");
         html.AppendLine("</html>");
      
  return html.ToString();
        }

     private string GetPreviewStyleCss()
        {
            return @"
    <style>
        /* Reset and base styles */
        * {
            margin: 0;
   padding: 0;
            box-sizing: border-box;
     }

        body {
     font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
  line-height: 1.6;
            color: #333;
            background-color: #ffffff;
            max-width: 100%;
       overflow-x: hidden;
        }

        .container {
            max-width: 800px;
  margin: 0 auto;
     padding: 40px 20px;
  background-color: #ffffff;
        }

   /* Document header */
    .document-header {
        border-bottom: 1px solid #e1e1e1;
    padding-bottom: 20px;
    margin-bottom: 30px;
        }

        .document-title {
          font-size: 2.5rem;
  font-weight: 700;
            color: #000;
            margin-bottom: 10px;
line-height: 1.2;
        }

        .document-metadata {
            font-size: 0.9rem;
  color: #666;
  display: flex;
     gap: 20px;
     flex-wrap: wrap;
        }

        /* Content styles matching Preview */
        .document-content {
    font-size: 14px;
            line-height: 1.6;
        }

      /* Headers */
        .document-content h1 {
            font-size: 2rem;
    font-weight: 700;
     color: #000;
     margin: 32px 0 16px 0;
          line-height: 1.25;
        }

        .document-content h2 {
         font-size: 1.625rem;
  font-weight: 700;
      color: #000;
     margin: 26px 0 16px 0;
   line-height: 1.25;
   }

    .document-content h3 {
        font-size: 1.375rem;
   font-weight: 700;
          color: #000;
       margin: 18px 0 10px 0;
     line-height: 1.25;
        }

      .document-content h4 {
            font-size: 1.125rem;
            font-weight: 700;
            color: #000;
          margin: 18px 0 10px 0;
          line-height: 1.25;
        }

        .document-content h5 {
      font-size: 1rem;
        font-weight: 700;
            color: #000;
          margin: 18px 0 10px 0;
            line-height: 1.25;
        }

        .document-content h6 {
   font-size: 0.875rem;
      font-weight: 700;
          color: #000;
       margin: 18px 0 10px 0;
            line-height: 1.25;
        }

        /* Paragraphs */
        .document-content p {
            margin: 6px 0 12px 0;
 color: #000;
        }

     /* Lists */
        .document-content ul, .document-content ol {
          margin: 8px 0;
padding-left: 24px;
        }

        .document-content li {
       margin: 4px 0;
          color: #000;
      }

      .document-content ul li {
   list-style-type: disc;
        }

        .document-content ol li {
            list-style-type: decimal;
        }

    /* Blockquotes */
        .document-content blockquote {
    background-color: #f6f8fa;
            border-left: 4px solid #6495ed;
            margin: 8px 0;
            padding: 8px 15px;
        font-style: italic;
            color: #424242;
   }

    /* Code blocks */
        .document-content pre {
            background-color: #f6f8fa;
      border: 1px solid #e1e4e8;
      border-radius: 6px;
            padding: 16px;
    margin: 12px 0;
            overflow-x: auto;
font-family: 'Cascadia Code', 'Courier New', monospace;
   font-size: 13px;
   line-height: 1.45;
        }

.document-content code {
    background-color: #f6f8fa;
            border-radius: 3px;
     padding: 2px 4px;
  font-family: 'Cascadia Code', 'Courier New', monospace;
       font-size: 85%;
      color: #e83e8c;
        }

        .document-content pre code {
background-color: transparent;
            color: #000;
            padding: 0;
          border-radius: 0;
     }

  /* Task lists */
        .document-content .task-list {
   list-style: none;
         padding-left: 0;
        }

        .document-content .task-list-item {
            display: flex;
            align-items: flex-start;
        gap: 8px;
      margin: 4px 0;
  }

        .document-content .task-checkbox {
   width: 12px;
     height: 12px;
    margin-top: 2px;
            border: 1px solid #808080;
            background-color: #fff;
            flex-shrink: 0;
   position: relative;
 }

        .document-content .task-checkbox.checked {
    background-color: #fff;
   }

        .document-content .task-checkbox.checked::after {
    content: '✓';
   position: absolute;
            top: -2px;
            left: 0;
          font-size: 10px;
     font-weight: bold;
          color: #000;
        }

        .document-content .task-text.completed {
   text-decoration: line-through;
        color: #666;
    }

        /* Images */
        .document-content .image-placeholder {
            background-color: #f0f0f0;
            border: 2px solid #e1e4e8;
 border-radius: 6px;
            padding: 60px 20px;
         margin: 16px 0;
      text-align: center;
 color: #6495ed;
        }

        .document-content .image-icon {
 font-size: 48px;
          margin-bottom: 10px;
display: block;
        }

        .document-content .image-alt {
            font-size: 14px;
     font-weight: bold;
      color: #000;
     margin-bottom: 5px;
        }

        .document-content .image-src {
            font-size: 12px;
            color: #666;
        }

        /* Horizontal rules */
        .document-content hr {
            border: none;
       height: 1px;
            background-color: #e1e4e8;
        margin: 16px 0;
        }

        /* Links */
        .document-content a {
          color: #0366d6;
  text-decoration: none;
        }

        .document-content a:hover {
  text-decoration: underline;
      }

        /* Tables */
        .document-content table {
            border-collapse: collapse;
      width: 100%;
    margin: 16px 0;
        }

        .document-content th,
        .document-content td {
    border: 1px solid #e1e4e8;
            padding: 8px 12px;
       text-align: left;
  }

        .document-content th {
        background-color: #f6f8fa;
 font-weight: 600;
}

  /* Math formulas */
        .document-content .math-display {
            margin: 16px 0;
    text-align: center;
 }

        .document-content .math-inline {
   display: inline;
        }

        /* Footer */
      .document-footer {
            border-top: 1px solid #e1e1e1;
        padding-top: 20px;
            margin-top: 40px;
            text-align: center;
         font-size: 0.8rem;
      color: #666;
        }

        /* Print styles */
        @media print {
            .container {
       max-width: 100%;
    padding: 20px;
    }
            
      .document-header,
   .document-footer {
  border: none;
        }
        }
    </style>";
    }

        private string ConvertMarkdownToHtml(string content)
   {
     if (string.IsNullOrEmpty(content))
         return "";

 var html = new StringBuilder();
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
       
    bool inCodeBlock = false;
   var codeBlockContent = new StringBuilder();
            string codeBlockLanguage = "";
 bool inList = false;
      bool inOrderedList = false;

            foreach (var line in lines)
            {
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
  html.AppendLine($"<pre><code>{EscapeHtml(codeBlockContent.ToString())}</code></pre>");
             codeBlockContent.Clear();
          }
            continue;
            }

            if (inCodeBlock)
   {
      codeBlockContent.AppendLine(line);
          continue;
          }

                // Handle empty lines
        if (string.IsNullOrEmpty(trimmedLine))
        {
    if (inList || inOrderedList)
             {
               if (inList) html.AppendLine("</ul>");
     if (inOrderedList) html.AppendLine("</ol>");
 inList = false;
        inOrderedList = false;
      }
        html.AppendLine("<br>");
           continue;
     }

     // Close lists if we're not in a list item anymore
             if (inList && !IsListItem(trimmedLine))
              {
          html.AppendLine("</ul>");
  inList = false;
    }
     if (inOrderedList && !IsOrderedListItem(trimmedLine))
       {
                html.AppendLine("</ol>");
             inOrderedList = false;
                }

          // Process the line
                html.AppendLine(ProcessMarkdownLine(trimmedLine, ref inList, ref inOrderedList));
         }

    // Close any open lists
            if (inList) html.AppendLine("</ul>");
      if (inOrderedList) html.AppendLine("</ol>");

  // Handle unclosed code block
       if (inCodeBlock && codeBlockContent.Length > 0)
          {
              html.AppendLine($"<pre><code>{EscapeHtml(codeBlockContent.ToString())}</code></pre>");
            }

         return html.ToString();
        }

        private string ProcessMarkdownLine(string line, ref bool inList, ref bool inOrderedList)
        {
   // Images
            var imageMatch = Regex.Match(line, @"!\[([^\]]*)\]\(([^)]+)\)");
            if (imageMatch.Success)
            {
      var altText = imageMatch.Groups[1].Value;
   var imageSrc = imageMatch.Groups[2].Value;
     return $@"<div class=""image-placeholder"">
<span class=""image-icon"">🖼️</span>
    <div class=""image-alt"">{EscapeHtml(altText)}</div>
    <div class=""image-src"">Image: {EscapeHtml(imageSrc)}</div>
</div>";
 }

    // Headers
            if (line.StartsWith("#"))
            {
                var headerMatch = Regex.Match(line, @"^(#{1,6})\s+(.+)$");
            if (headerMatch.Success)
      {
      var level = headerMatch.Groups[1].Value.Length;
    var text = ProcessInlineFormatting(headerMatch.Groups[2].Value.Trim());
        return $"<h{level}>{text}</h{level}>";
            }
            }

          // Quotes
   if (line.StartsWith("> "))
   {
         var content = ProcessInlineFormatting(line.Substring(2));
                return $"<blockquote>{content}</blockquote>";
   }

         // Task lists
      var taskMatch = Regex.Match(line, @"^[-*]\s+\[([ xX])\]\s+(.+)$");
        if (taskMatch.Success)
     {
      bool isChecked = taskMatch.Groups[1].Value.ToLower() == "x";
                string content = ProcessInlineFormatting(taskMatch.Groups[2].Value);
     var checkboxClass = isChecked ? "checked" : "";
     var textClass = isChecked ? "completed" : "";
         
       if (!inList)
       {
       inList = true;
    return $@"<ul class=""task-list"">
    <li class=""task-list-item"">
        <span class=""task-checkbox {checkboxClass}""></span>
        <span class=""task-text {textClass}"">{content}</span>
    </li>";
      }
        else
         {
         return $@"    <li class=""task-list-item"">
<span class=""task-checkbox {checkboxClass}""></span>
        <span class=""task-text {textClass}"">{content}</span>
    </li>";
     }
            }

    // Bullet lists
        if (Regex.IsMatch(line, @"^[-*+]\s+.+"))
            {
 var content = ProcessInlineFormatting(Regex.Replace(line, @"^[-*+]\s+", ""));
    if (!inList)
     {
    inList = true;
     return $"<ul>\n    <li>{content}</li>";
   }
  else
     {
       return $"    <li>{content}</li>";
  }
        }

            // Numbered lists
     var numberedMatch = Regex.Match(line, @"^(\d+)\.\s+(.+)$");
     if (numberedMatch.Success)
          {
       var content = ProcessInlineFormatting(numberedMatch.Groups[2].Value);
         if (!inOrderedList)
  {
    inOrderedList = true;
 return $"<ol>\n    <li>{content}</li>";
     }
    else
      {
           return $"    <li>{content}</li>";
   }
       }

            // Horizontal rules
   if (Regex.IsMatch(line, @"^(---+|___+|\*\*\*+)$"))
     {
         return "<hr>";
         }

            // Math formulas (display)
        if (line.StartsWith("$$") && line.EndsWith("$$"))
        {
          return $"<div class=\"math-display\">{line}</div>";
            }

 // Everything else is a paragraph
            var processedContent = ProcessInlineFormatting(line);
        return $"<p>{processedContent}</p>";
        }

    private string ProcessInlineFormatting(string text)
        {
         if (string.IsNullOrEmpty(text))
     return "";

         // Inline math
         text = Regex.Replace(text, @"\$([^$]+)\$", "<span class=\"math-inline\">$$$1$$</span>");

       // Bold
      text = Regex.Replace(text, @"\*\*(.*?)\*\*", "<strong>$1</strong>");
          text = Regex.Replace(text, @"__(.*?)__", "<strong>$1</strong>");

        // Italic
   text = Regex.Replace(text, @"\*(.*?)\*", "<em>$1</em>");
   text = Regex.Replace(text, @"_(.*?)_", "<em>$1</em>");

            // Strikethrough
            text = Regex.Replace(text, @"~~(.*?)~~", "<del>$1</del>");

       // Highlight
       text = Regex.Replace(text, @"==(.*?)==", "<mark>$1</mark>");

            // Inline code
            text = Regex.Replace(text, @"`(.*?)`", "<code>$1</code>");

    // Links
        text = Regex.Replace(text, @"\[(.*?)\]\((.*?)\)", "<a href=\"$2\">$1</a>");

       return text;
        }

    private bool IsListItem(string line)
        {
   return Regex.IsMatch(line, @"^[-*+]\s+") || Regex.IsMatch(line, @"^[-*]\s+\[([ xX])\]\s+");
        }

        private bool IsOrderedListItem(string line)
        {
   return Regex.IsMatch(line, @"^\d+\.\s+");
   }

  private string EscapeHtml(string text)
      {
      if (string.IsNullOrEmpty(text))
 return "";

   return text
    .Replace("&", "&amp;")
       .Replace("<", "&lt;")
             .Replace(">", "&gt;")
         .Replace("\"", "&quot;")
 .Replace("'", "&#39;");
     }

        private async Task<string?> ShowSaveFileDialog(string suggestedFileName)
        {
            try
  {
         var savePicker = new FileSavePicker();
         
         var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
       InitializeWithWindow.Initialize(savePicker, hwnd);

    savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
 savePicker.FileTypeChoices.Add("HTML Document", new[] { ".html" });
       
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

     private async Task OpenHtmlFile(string filePath)
 {
            try
     {
    var process = new ProcessStartInfo
 {
              FileName = filePath,
   UseShellExecute = true
       };
         Process.Start(process);
            }
     catch (Exception ex)
            {
       System.Diagnostics.Debug.WriteLine($"Error opening HTML file: {ex.Message}");
            }
    }
    }
}