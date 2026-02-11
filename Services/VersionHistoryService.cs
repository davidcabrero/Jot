using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jot.Models;
using Newtonsoft.Json;

namespace Jot.Services
{
    /// <summary>
    /// Servicio para gestionar el historial de versiones de documentos
    /// </summary>
    public class VersionHistoryService
    {
        private readonly string _versionsDirectory;
        private const int MaxVersionsPerDocument = 50; // Límite de versiones guardadas
        private const int AutoSaveIntervalMinutes = 5; // Auto-guardar cada 5 minutos

        public VersionHistoryService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _versionsDirectory = Path.Combine(appData, "Jot", "Versions");
            
            if (!Directory.Exists(_versionsDirectory))
            {
                Directory.CreateDirectory(_versionsDirectory);
            }
        }

        /// <summary>
        /// Guarda una nueva versión del documento
        /// </summary>
        public async Task<DocumentVersion> SaveVersionAsync(Document document, string changeDescription = "Auto-saved version")
        {
            try
            {
                var version = new DocumentVersion
                {
                    Title = document.Title,
                    Content = document.Content,
                    CreatedAt = DateTime.Now,
                    ChangeDescription = changeDescription,
                    SizeInBytes = System.Text.Encoding.UTF8.GetByteCount(document.Content ?? "")
                };

                // Crear directorio para el documento si no existe
                var docVersionsDir = Path.Combine(_versionsDirectory, document.Id.ToString());
                if (!Directory.Exists(docVersionsDir))
                {
                    Directory.CreateDirectory(docVersionsDir);
                }

                // Guardar versión en archivo JSON
                var versionFile = Path.Combine(docVersionsDir, $"{version.Id}.json");
                var json = JsonConvert.SerializeObject(version, Formatting.Indented);
                await File.WriteAllTextAsync(versionFile, json);

                // Limpiar versiones antiguas si exceden el límite
                await CleanupOldVersionsAsync(document.Id);

                return version;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving version: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Obtiene todas las versiones de un documento
        /// </summary>
        public async Task<List<DocumentVersion>> GetVersionHistoryAsync(Guid documentId)
        {
            try
            {
                var docVersionsDir = Path.Combine(_versionsDirectory, documentId.ToString());
                if (!Directory.Exists(docVersionsDir))
                {
                    return new List<DocumentVersion>();
                }

                var versions = new List<DocumentVersion>();
                var files = Directory.GetFiles(docVersionsDir, "*.json");

                foreach (var file in files)
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(file);
                        var version = JsonConvert.DeserializeObject<DocumentVersion>(json);
                        if (version != null)
                        {
                            versions.Add(version);
                        }
                    }
                    catch
                    {
                        // Ignorar archivos corruptos
                    }
                }

                return versions.OrderByDescending(v => v.CreatedAt).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting version history: {ex.Message}");
                return new List<DocumentVersion>();
            }
        }

        /// <summary>
        /// Restaura una versión específica del documento
        /// </summary>
        public async Task<Document> RestoreVersionAsync(Document currentDocument, DocumentVersion version)
        {
            try
            {
                // Guardar versión actual antes de restaurar
                await SaveVersionAsync(currentDocument, "Before restore");

                // Restaurar contenido de la versión
                currentDocument.Title = version.Title;
                currentDocument.Content = version.Content;
                currentDocument.ModifiedAt = DateTime.Now;

                return currentDocument;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error restoring version: {ex.Message}");
                return currentDocument;
            }
        }

        /// <summary>
        /// Elimina versiones antiguas si exceden el límite
        /// </summary>
        private async Task CleanupOldVersionsAsync(Guid documentId)
        {
            try
            {
                var versions = await GetVersionHistoryAsync(documentId);
                if (versions.Count <= MaxVersionsPerDocument)
                {
                    return;
                }

                // Eliminar las versiones más antiguas
                var versionsToDelete = versions.Skip(MaxVersionsPerDocument).ToList();
                var docVersionsDir = Path.Combine(_versionsDirectory, documentId.ToString());

                foreach (var version in versionsToDelete)
                {
                    var versionFile = Path.Combine(docVersionsDir, $"{version.Id}.json");
                    if (File.Exists(versionFile))
                    {
                        File.Delete(versionFile);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cleaning up old versions: {ex.Message}");
            }
        }

        /// <summary>
        /// Compara dos versiones y devuelve las diferencias
        /// </summary>
        public string GetDifferences(DocumentVersion version1, DocumentVersion version2)
        {
            // Implementación simple de diff
            var changes = new List<string>();

            if (version1.Title != version2.Title)
            {
                changes.Add($"Title changed from '{version1.Title}' to '{version2.Title}'");
            }

            var content1Lines = version1.Content.Split('\n');
            var content2Lines = version2.Content.Split('\n');

            var addedLines = content2Lines.Length - content1Lines.Length;
            if (addedLines > 0)
            {
                changes.Add($"+{addedLines} lines added");
            }
            else if (addedLines < 0)
            {
                changes.Add($"{Math.Abs(addedLines)} lines removed");
            }

            return string.Join("\n", changes);
        }

        /// <summary>
        /// Elimina todo el historial de versiones de un documento
        /// </summary>
        public async Task DeleteVersionHistoryAsync(Guid documentId)
        {
            try
            {
                var docVersionsDir = Path.Combine(_versionsDirectory, documentId.ToString());
                if (Directory.Exists(docVersionsDir))
                {
                    Directory.Delete(docVersionsDir, true);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting version history: {ex.Message}");
            }
        }
    }
}
