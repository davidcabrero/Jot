using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Jot.Models
{
    public class Document
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = "Untitled";
        public string Content { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime ModifiedAt { get; set; } = DateTime.Now;
        public List<string> Tags { get; set; } = new List<string>();
        public string FolderPath { get; set; } = "";
        public DocumentType Type { get; set; } = DocumentType.Markdown;
    }

    public enum DocumentType
    {
        Markdown,
        RichText,
        Code,
        Quiz
    }

    public class QuizQuestion
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Question { get; set; } = "";
        public List<string> Options { get; set; } = new List<string>();
        public int CorrectAnswerIndex { get; set; } = 0;
        public string Explanation { get; set; } = "";
    }

    public class CodeBlock
    {
        public string Language { get; set; } = "text";
        public string Code { get; set; } = "";
        public bool IsCollapsible { get; set; } = false;
        public bool IsCollapsed { get; set; } = false;
    }
}