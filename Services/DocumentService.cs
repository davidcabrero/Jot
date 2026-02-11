using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Jot.Models;
using Newtonsoft.Json;
using Windows.Storage;

namespace Jot.Services
{
    public class DocumentService
    {
        private readonly string _documentsFolder;
        private readonly VersionHistoryService _versionHistoryService;
        private readonly EncryptionService _encryptionService;
        private readonly CloudSyncService _cloudSyncService;
        private readonly DocumentLinksService _documentLinksService;
        private readonly AttachmentService _attachmentService;

        public DocumentService()
        {
            var localFolder = ApplicationData.Current.LocalFolder.Path;
            _documentsFolder = Path.Combine(localFolder, "Documents");
            Directory.CreateDirectory(_documentsFolder);

            // Inicializar servicios avanzados
            _versionHistoryService = new VersionHistoryService();
            _encryptionService = new EncryptionService();
            _cloudSyncService = new CloudSyncService();
            _documentLinksService = new DocumentLinksService();
            _attachmentService = new AttachmentService();
        }

        public VersionHistoryService VersionHistory => _versionHistoryService;
        public EncryptionService Encryption => _encryptionService;
        public CloudSyncService CloudSync => _cloudSyncService;
        public DocumentLinksService DocumentLinks => _documentLinksService;
        public AttachmentService Attachments => _attachmentService;

        public async Task<List<Document>> LoadAllDocumentsAsync()
        {
            var documents = new List<Document>();

            try
            {
                var files = Directory.GetFiles(_documentsFolder, "*.json");

                foreach (var file in files)
                {
                    var content = await File.ReadAllTextAsync(file);
                    var document = JsonConvert.DeserializeObject<Document>(content);
                    if (document != null)
                    {
                        documents.Add(document);
                    }
                }

                // Sort by modified date, most recent first
                documents.Sort((a, b) => b.ModifiedAt.CompareTo(a.ModifiedAt));

                // Actualizar backlinks
                _documentLinksService.UpdateAllBacklinks(documents);
            }
            catch (Exception ex)
            {
                // Log error - for now just continue with empty list
                System.Diagnostics.Debug.WriteLine($"Error loading documents: {ex.Message}");
            }

            return documents;
        }

        public async Task SaveDocumentAsync(Document document)
        {
            try
            {
                document.ModifiedAt = DateTime.Now;

                // Auto-guardar versión si ha pasado suficiente tiempo
                var versions = await _versionHistoryService.GetVersionHistoryAsync(document.Id);
                if (versions.Count == 0 || 
                    (DateTime.Now - versions[0].CreatedAt).TotalMinutes >= 5)
                {
                    await _versionHistoryService.SaveVersionAsync(document, "Auto-save");
                }

                // Sincronizar con la nube si está habilitado
                if (document.IsSyncEnabled)
                {
                    await _cloudSyncService.SyncDocumentAsync(document);
                }

                var filePath = Path.Combine(_documentsFolder, $"{document.Id}.json");
                var json = JsonConvert.SerializeObject(document, Formatting.Indented);
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving document: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteDocumentAsync(Guid documentId)
        {
            try
            {
                // Eliminar historial de versiones
                await _versionHistoryService.DeleteVersionHistoryAsync(documentId);

                // Eliminar archivo
                var filePath = Path.Combine(_documentsFolder, $"{documentId}.json");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting document: {ex.Message}");
                throw;
            }
        }

        public async Task<Document?> LoadDocumentAsync(Guid documentId)
        {
            try
            {
                var filePath = Path.Combine(_documentsFolder, $"{documentId}.json");
                if (File.Exists(filePath))
                {
                    var content = await File.ReadAllTextAsync(filePath);
                    return JsonConvert.DeserializeObject<Document>(content);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading document: {ex.Message}");
            }

            return null;
        }

        public async Task UpdateDocumentLinksAsync(Document document, List<Document> allDocuments)
        {
            _documentLinksService.UpdateDocumentLinks(document, allDocuments);
            _documentLinksService.UpdateAllBacklinks(allDocuments);
        }
    }
}