using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jot.Models;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Jot.Services
{
    /// <summary>
    /// Servicio para gestionar archivos adjuntos en documentos
    /// </summary>
    public class AttachmentService
    {
        private readonly string _attachmentsDirectory;
        private const long MaxAttachmentSize = 50 * 1024 * 1024; // 50 MB

        public AttachmentService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _attachmentsDirectory = Path.Combine(appData, "Jot", "Attachments");
            
            if (!Directory.Exists(_attachmentsDirectory))
            {
                Directory.CreateDirectory(_attachmentsDirectory);
            }
        }

        /// <summary>
        /// Adjunta un archivo a un documento
        /// </summary>
        public async Task<DocumentAttachment> AttachFileAsync(Document document, StorageFile file)
        {
            try
            {
                var fileInfo = await file.GetBasicPropertiesAsync();
                
                // Verificar tamaño
                if (fileInfo.Size > MaxAttachmentSize)
                {
                    throw new Exception($"File size exceeds maximum allowed size of {MaxAttachmentSize / (1024 * 1024)} MB");
                }

                // Crear directorio para el documento
                var docAttachmentsDir = Path.Combine(_attachmentsDirectory, document.Id.ToString());
                if (!Directory.Exists(docAttachmentsDir))
                {
                    Directory.CreateDirectory(docAttachmentsDir);
                }

                // Crear adjunto
                var attachment = new DocumentAttachment
                {
                    FileName = file.Name,
                    SizeInBytes = (long)fileInfo.Size,
                    MimeType = file.ContentType,
                    Type = DetermineAttachmentType(file.FileType)
                };

                // Copiar archivo
                var destPath = Path.Combine(docAttachmentsDir, $"{attachment.Id}{Path.GetExtension(file.Name)}");
                await file.CopyAsync(await StorageFolder.GetFolderFromPathAsync(docAttachmentsDir), 
                    $"{attachment.Id}{Path.GetExtension(file.Name)}", 
                    NameCollisionOption.ReplaceExisting);
                
                attachment.FilePath = destPath;

                // Agregar a documento
                document.Attachments.Add(attachment);

                return attachment;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error attaching file: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Adjunta múltiples archivos
        /// </summary>
        public async Task<List<DocumentAttachment>> AttachMultipleFilesAsync(Document document, IReadOnlyList<StorageFile> files)
        {
            var attachments = new List<DocumentAttachment>();
            
            foreach (var file in files)
            {
                var attachment = await AttachFileAsync(document, file);
                if (attachment != null)
                {
                    attachments.Add(attachment);
                }
            }

            return attachments;
        }

        /// <summary>
        /// Elimina un adjunto de un documento
        /// </summary>
        public async Task<bool> RemoveAttachmentAsync(Document document, DocumentAttachment attachment)
        {
            try
            {
                // Eliminar archivo físico
                if (File.Exists(attachment.FilePath))
                {
                    File.Delete(attachment.FilePath);
                }

                // Eliminar de documento
                document.Attachments.Remove(attachment);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing attachment: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Abre un adjunto con la aplicación predeterminada
        /// </summary>
        public async Task<bool> OpenAttachmentAsync(DocumentAttachment attachment)
        {
            try
            {
                if (!File.Exists(attachment.FilePath))
                {
                    return false;
                }

                var file = await StorageFile.GetFileFromPathAsync(attachment.FilePath);
                await Windows.System.Launcher.LaunchFileAsync(file);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening attachment: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Exporta un adjunto a una ubicación específica
        /// </summary>
        public async Task<bool> ExportAttachmentAsync(DocumentAttachment attachment, string destinationPath)
        {
            try
            {
                if (!File.Exists(attachment.FilePath))
                {
                    return false;
                }

                File.Copy(attachment.FilePath, destinationPath, true);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error exporting attachment: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Obtiene el tamaño total de adjuntos de un documento
        /// </summary>
        public long GetTotalAttachmentsSize(Document document)
        {
            return document.Attachments.Sum(a => a.SizeInBytes);
        }

        /// <summary>
        /// Obtiene adjuntos por tipo
        /// </summary>
        public List<DocumentAttachment> GetAttachmentsByType(Document document, AttachmentType type)
        {
            return document.Attachments.Where(a => a.Type == type).ToList();
        }

        /// <summary>
        /// Determina el tipo de adjunto basándose en la extensión
        /// </summary>
        private AttachmentType DetermineAttachmentType(string extension)
        {
            extension = extension.ToLower();

            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".svg", ".webp" };
            var audioExtensions = new[] { ".mp3", ".wav", ".ogg", ".m4a", ".flac" };
            var videoExtensions = new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".webm" };

            if (imageExtensions.Contains(extension))
                return AttachmentType.Image;
            
            if (audioExtensions.Contains(extension))
                return AttachmentType.Audio;
            
            if (videoExtensions.Contains(extension))
                return AttachmentType.Video;
            
            if (extension == ".pdf")
                return AttachmentType.Pdf;

            return AttachmentType.File;
        }

        /// <summary>
        /// Adjunta una imagen desde el portapapeles
        /// </summary>
        public async Task<DocumentAttachment> AttachImageFromClipboardAsync(Document document)
        {
            try
            {
                var dataPackageView = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
                
                if (!dataPackageView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Bitmap))
                {
                    return null;
                }

                var bitmap = await dataPackageView.GetBitmapAsync();
                using (var stream = await bitmap.OpenReadAsync())
                {
                    // Crear directorio
                    var docAttachmentsDir = Path.Combine(_attachmentsDirectory, document.Id.ToString());
                    if (!Directory.Exists(docAttachmentsDir))
                    {
                        Directory.CreateDirectory(docAttachmentsDir);
                    }

                    // Crear adjunto
                    var attachment = new DocumentAttachment
                    {
                        FileName = $"clipboard_image_{DateTime.Now:yyyyMMdd_HHmmss}.png",
                        Type = AttachmentType.Image,
                        MimeType = "image/png"
                    };

                    var destPath = Path.Combine(docAttachmentsDir, $"{attachment.Id}.png");
                    attachment.FilePath = destPath;

                    // Guardar imagen
                    var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream);
                    var pixels = await decoder.GetPixelDataAsync();
                    
                    var file = await (await StorageFolder.GetFolderFromPathAsync(docAttachmentsDir))
                        .CreateFileAsync($"{attachment.Id}.png", CreationCollisionOption.ReplaceExisting);
                    
                    using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        var encoder = await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(
                            Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId, fileStream);
                        
                        encoder.SetPixelData(
                            Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,
                            Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied,
                            decoder.PixelWidth,
                            decoder.PixelHeight,
                            decoder.DpiX,
                            decoder.DpiY,
                            pixels.DetachPixelData());
                        
                        await encoder.FlushAsync();
                    }

                    var fileInfo = await file.GetBasicPropertiesAsync();
                    attachment.SizeInBytes = (long)fileInfo.Size;

                    document.Attachments.Add(attachment);
                    return attachment;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error attaching image from clipboard: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Limpia adjuntos huérfanos (archivos sin documento asociado)
        /// </summary>
        public async Task CleanupOrphanedAttachmentsAsync(List<Document> allDocuments)
        {
            try
            {
                var documentIds = allDocuments.Select(d => d.Id.ToString()).ToHashSet();
                var directories = Directory.GetDirectories(_attachmentsDirectory);

                foreach (var dir in directories)
                {
                    var dirName = Path.GetFileName(dir);
                    if (!documentIds.Contains(dirName))
                    {
                        Directory.Delete(dir, true);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cleaning up orphaned attachments: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene el icono apropiado para un tipo de adjunto
        /// </summary>
        public string GetAttachmentIcon(AttachmentType type)
        {
            return type switch
            {
                AttachmentType.Image => "📷",
                AttachmentType.Audio => "🎵",
                AttachmentType.Video => "🎥",
                AttachmentType.Pdf => "📕",
                AttachmentType.Link => "🔗",
                _ => "📎"
            };
        }

        /// <summary>
        /// Formatea el tamaño de archivo para mostrar
        /// </summary>
        public string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}
