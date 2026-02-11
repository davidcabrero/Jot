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
        public Dictionary<string, PythonCell> PythonCells { get; set; } = new Dictionary<string, PythonCell>();
        public List<string> AttachedImages { get; set; } = new List<string>();

        // 🔄 Historial de Versiones
        [JsonIgnore]
        public List<DocumentVersion> VersionHistory { get; set; } = new List<DocumentVersion>();

        // 🔐 Cifrado
        public bool IsEncrypted { get; set; } = false;
        public string EncryptedContent { get; set; } = "";
        public string PasswordHash { get; set; } = ""; // SHA256 hash

        // ☁️ Sincronización
        public string CloudSyncId { get; set; } = "";
        public DateTime? LastSyncedAt { get; set; }
        public CloudProvider CloudProvider { get; set; } = CloudProvider.None;
        public bool IsSyncEnabled { get; set; } = false;

        // 📎 Archivos Adjuntos
        public List<DocumentAttachment> Attachments { get; set; } = new List<DocumentAttachment>();

        // 🔗 Enlaces entre Documentos
        public List<string> LinkedDocumentIds { get; set; } = new List<string>(); // IDs de documentos enlazados
        public List<string> BackLinks { get; set; } = new List<string>(); // IDs de documentos que enlazan a este
    }

    public enum DocumentType
    {
        Markdown,
        RichText,
        Code,
        Quiz
    }

    public enum CloudProvider
    {
        None,
        OneDrive,
        GoogleDrive,
        Dropbox
    }

    // 🔄 Modelo de Versión
    public class DocumentVersion
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = "User";
        public string ChangeDescription { get; set; } = "Auto-saved version";
        public long SizeInBytes { get; set; }
    }

    // 📎 Modelo de Adjunto
    public class DocumentAttachment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = ""; // Ruta local o URL
        public AttachmentType Type { get; set; } = AttachmentType.File;
        public long SizeInBytes { get; set; }
        public DateTime AttachedAt { get; set; } = DateTime.Now;
        public string MimeType { get; set; } = "";
    }

    public enum AttachmentType
    {
        File,
        Image,
        Audio,
        Video,
        Pdf,
        Link
    }

    public class PythonCell
    {
        public string Id { get; set; } = "";
        public string Code { get; set; } = "";
        public string Output { get; set; } = "";
        public DateTime ExecutedAt { get; set; }
        public string InterpreterName { get; set; } = "";
        public List<string> PlotFiles { get; set; } = new List<string>();
        public bool IsExecutable { get; set; } = true;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
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