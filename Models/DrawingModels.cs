using System;
using System.Collections.Generic;

namespace Jot.Models
{
    public enum DrawingElementType
    {
        FreeDrawing,
        Line,
        Rectangle,
        Ellipse,
        Arrow,
        Text
    }

    public class DrawingElement
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DrawingElementType Type { get; set; }
        public List<Point> Points { get; set; } = new();
        public string Color { get; set; } = "#000000";
        public double StrokeWidth { get; set; } = 2.0;
        public string? Text { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class Point
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Point() { }

        public Point(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    public class DrawingData
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = "";
        public List<DrawingElement> Elements { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime ModifiedAt { get; set; } = DateTime.Now;
        public double CanvasWidth { get; set; } = 800;
        public double CanvasHeight { get; set; } = 600;
    }
}