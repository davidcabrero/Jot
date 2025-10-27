using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Jot.Models;
using Newtonsoft.Json;
using Windows.Storage;

namespace Jot.Services
{
    public class DrawingService
    {
        private readonly string _drawingsFolder;

        public DrawingService()
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            _drawingsFolder = Path.Combine(localFolder.Path, "Drawings");
            
            // Ensure drawings folder exists
            if (!Directory.Exists(_drawingsFolder))
            {
                Directory.CreateDirectory(_drawingsFolder);
            }
        }

        public async Task<List<DrawingData>> LoadAllDrawingsAsync()
        {
            var drawings = new List<DrawingData>();
            
            try
            {
                var files = Directory.GetFiles(_drawingsFolder, "*.json");
                
                foreach (var file in files)
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(file);
                        var drawing = JsonConvert.DeserializeObject<DrawingData>(json);
                        if (drawing != null)
                        {
                            drawings.Add(drawing);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading drawing from {file}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading drawings: {ex.Message}");
            }
            
            return drawings;
        }

        public async Task SaveDrawingAsync(DrawingData drawing)
        {
            try
            {
                drawing.ModifiedAt = DateTime.Now;
                var json = JsonConvert.SerializeObject(drawing, Formatting.Indented);
                var fileName = $"{drawing.Id}.json";
                var filePath = Path.Combine(_drawingsFolder, fileName);
                
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving drawing: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteDrawingAsync(Guid drawingId)
        {
            try
            {
                var fileName = $"{drawingId}.json";
                var filePath = Path.Combine(_drawingsFolder, fileName);
                
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting drawing: {ex.Message}");
                throw;
            }
        }

        public async Task<DrawingData?> LoadDrawingAsync(Guid drawingId)
        {
            try
            {
                var fileName = $"{drawingId}.json";
                var filePath = Path.Combine(_drawingsFolder, fileName);
                
                if (File.Exists(filePath))
                {
                    var json = await File.ReadAllTextAsync(filePath);
                    return JsonConvert.DeserializeObject<DrawingData>(json);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading drawing: {ex.Message}");
            }
            
            return null;
        }

        public async Task<string> ExportDrawingAsImageAsync(DrawingData drawing, string format = "png")
        {
            // This would be implemented to export drawing as image
            // For now, return a placeholder path
            try
            {
                var fileName = $"drawing_{drawing.Id}_{DateTime.Now:yyyyMMdd_HHmmss}.{format}";
                var tempFolder = ApplicationData.Current.TemporaryFolder;
                var file = await tempFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
                return file.Path;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error exporting drawing: {ex.Message}");
                throw;
            }
        }

        public async Task<DrawingData> CreateDrawingFromMarkdownAsync(string markdownContent)
        {
            var drawing = new DrawingData
            {
                Title = "Diagram from Markdown",
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now
            };

            // Parse markdown content and create drawing elements
            // This is a simplified implementation
            var lines = markdownContent.Split('\n');
            double y = 50;

            foreach (var line in lines)
            {
                if (line.StartsWith("#"))
                {
                    // Create text element for headers
                    var element = new DrawingElement
                    {
                        Type = DrawingElementType.Text,
                        Text = line.TrimStart('#').Trim(),
                        Points = new List<Models.Point> { new Models.Point(50, y) },
                        Color = "#000000",
                        StrokeWidth = 2.0
                    };
                    drawing.Elements.Add(element);
                    y += 40;
                }
                else if (line.Trim().StartsWith("-") || line.Trim().StartsWith("*"))
                {
                    // Create bullet point
                    var bulletElement = new DrawingElement
                    {
                        Type = DrawingElementType.Ellipse,
                        Points = new List<Models.Point> 
                        { 
                            new Models.Point(60, y), 
                            new Models.Point(65, y + 5) 
                        },
                        Color = "#000000",
                        StrokeWidth = 1.0
                    };
                    drawing.Elements.Add(bulletElement);

                    var textElement = new DrawingElement
                    {
                        Type = DrawingElementType.Text,
                        Text = line.Trim().Substring(1).Trim(),
                        Points = new List<Models.Point> { new Models.Point(80, y) },
                        Color = "#000000",
                        StrokeWidth = 1.0
                    };
                    drawing.Elements.Add(textElement);
                    y += 25;
                }
            }

            await SaveDrawingAsync(drawing);
            return drawing;
        }
    }
}