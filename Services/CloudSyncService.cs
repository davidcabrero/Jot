using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jot.Models;
using Newtonsoft.Json;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Jot.Services
{
    /// <summary>
    /// Servicio para sincronizar documentos con la nube REAL usando carpetas locales sincronizadas
    /// </summary>
    public class CloudSyncService
    {
        private readonly string _settingsFile;
        private CloudSyncSettings _settings;

        public CloudSyncService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var settingsDir = Path.Combine(appData, "Jot");
            Directory.CreateDirectory(settingsDir);
            _settingsFile = Path.Combine(settingsDir, "CloudSyncSettings.json");

            LoadSettings();
        }

        /// <summary>
        /// Detecta automáticamente carpetas de OneDrive, Google Drive y Dropbox
        /// </summary>
        public Dictionary<CloudProvider, string> DetectCloudFolders()
        {
            var detected = new Dictionary<CloudProvider, string>();
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            // OneDrive - Múltiples ubicaciones posibles
            var oneDrivePaths = new[]
            {
                Path.Combine(userProfile, "OneDrive"),
                Path.Combine(userProfile, "OneDrive - Personal"),
                Environment.GetEnvironmentVariable("OneDrive"),
                Environment.GetEnvironmentVariable("OneDriveConsumer"),
                Environment.GetEnvironmentVariable("OneDriveCommercial")
            };

            foreach (var path in oneDrivePaths.Where(p => !string.IsNullOrEmpty(p)))
            {
                if (Directory.Exists(path))
                {
                    detected[CloudProvider.OneDrive] = path;
                    break;
                }
            }

            // Google Drive - Múltiples ubicaciones
            var googleDrivePaths = new[]
            {
                Path.Combine(userProfile, "Google Drive"),
                Path.Combine(userProfile, "GoogleDrive"),
                @"G:\My Drive",
                @"G:\Mi unidad"
            };

            foreach (var path in googleDrivePaths)
            {
                if (Directory.Exists(path))
                {
                    detected[CloudProvider.GoogleDrive] = path;
                    break;
                }
            }

            // Dropbox
            var dropboxPaths = new[]
            {
                Path.Combine(userProfile, "Dropbox"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Dropbox", "info.json")
            };

            // Buscar Dropbox directamente
            if (Directory.Exists(dropboxPaths[0]))
            {
                detected[CloudProvider.Dropbox] = dropboxPaths[0];
            }
            else if (File.Exists(dropboxPaths[1]))
            {
                // Leer ubicación real de Dropbox desde info.json
                try
                {
                    var json = File.ReadAllText(dropboxPaths[1]);
                    var data = JsonConvert.DeserializeObject<dynamic>(json);
                    if (data?.personal?.path != null)
                    {
                        var dbPath = data.personal.path.ToString();
                        if (Directory.Exists(dbPath))
                        {
                            detected[CloudProvider.Dropbox] = dbPath;
                        }
                    }
                }
                catch { }
            }

            return detected;
        }

        /// <summary>
        /// Permite al usuario seleccionar manualmente una carpeta de sincronización
        /// </summary>
        public async Task<string> SelectCloudFolderAsync()
        {
            try
            {
                var picker = new FolderPicker();
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                picker.FileTypeFilter.Add("*");
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

                var folder = await picker.PickSingleFolderAsync();
                return folder?.Path ?? "";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error selecting folder: {ex.Message}");
                return "";
            }
        }

        /// <summary>
        /// Configura la carpeta de sincronización para un proveedor
        /// </summary>
        public void SetSyncFolder(CloudProvider provider, string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                throw new DirectoryNotFoundException($"Folder not found: {folderPath}");
            }

            _settings.CloudFolders[provider] = folderPath;
            SaveSettings();
        }

        /// <summary>
        /// Sincroniza un documento con la carpeta de nube configurada
        /// </summary>
        public async Task<bool> SyncDocumentAsync(Document document)
        {
            try
            {
                if (!document.IsSyncEnabled || document.CloudProvider == CloudProvider.None)
                {
                    return false;
                }

                if (!_settings.CloudFolders.ContainsKey(document.CloudProvider))
                {
                    System.Diagnostics.Debug.WriteLine($"No sync folder configured for {document.CloudProvider}");
                    return false;
                }

                var cloudFolder = _settings.CloudFolders[document.CloudProvider];
                if (!Directory.Exists(cloudFolder))
                {
                    System.Diagnostics.Debug.WriteLine($"Cloud folder not found: {cloudFolder}");
                    return false;
                }

                // Crear subcarpeta "Jot Documents" en la nube
                var jotFolder = Path.Combine(cloudFolder, "Jot Documents");
                Directory.CreateDirectory(jotFolder);

                // Nombre de archivo seguro
                var safeFileName = SanitizeFileName(document.Title) + ".md";
                var filePath = Path.Combine(jotFolder, safeFileName);

                // Crear contenido del archivo con metadatos
                var content = $@"---
title: {document.Title}
created: {document.CreatedAt:yyyy-MM-dd HH:mm:ss}
modified: {document.ModifiedAt:yyyy-MM-dd HH:mm:ss}
id: {document.Id}
synced: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
---

{document.Content}
";

                // Guardar el archivo en la carpeta de nube
                await File.WriteAllTextAsync(filePath, content);

                // Actualizar metadatos del documento
                document.LastSyncedAt = DateTime.Now;
                document.CloudSyncId = filePath;

                System.Diagnostics.Debug.WriteLine($"✅ Document synced to: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error syncing document: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Descarga un documento desde la carpeta de nube
        /// </summary>
        public async Task<Document> DownloadDocumentAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return null;
                }

                var content = await File.ReadAllTextAsync(filePath);

                // Parsear metadatos y contenido
                var lines = content.Split('\n');
                var document = new Document();
                var contentStartIndex = 0;

                if (lines.Length > 0 && lines[0].Trim() == "---")
                {
                    // Tiene metadatos YAML
                    for (int i = 1; i < lines.Length; i++)
                    {
                        var line = lines[i].Trim();
                        if (line == "---")
                        {
                            contentStartIndex = i + 1;
                            break;
                        }

                        var parts = line.Split(':', 2);
                        if (parts.Length == 2)
                        {
                            var key = parts[0].Trim();
                            var value = parts[1].Trim();

                            switch (key)
                            {
                                case "title":
                                    document.Title = value;
                                    break;
                                case "created":
                                    if (DateTime.TryParse(value, out var created))
                                        document.CreatedAt = created;
                                    break;
                                case "modified":
                                    if (DateTime.TryParse(value, out var modified))
                                        document.ModifiedAt = modified;
                                    break;
                                case "id":
                                    if (Guid.TryParse(value, out var id))
                                        document.Id = id;
                                    break;
                            }
                        }
                    }
                }

                // Contenido real del documento
                document.Content = string.Join("\n", lines.Skip(contentStartIndex + 1));
                return document;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error downloading document: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Lista todos los documentos en la carpeta de nube
        /// </summary>
        public async Task<List<CloudDocument>> ListCloudDocumentsAsync(CloudProvider provider)
        {
            try
            {
                if (!_settings.CloudFolders.ContainsKey(provider))
                {
                    return new List<CloudDocument>();
                }

                var cloudFolder = _settings.CloudFolders[provider];
                var jotFolder = Path.Combine(cloudFolder, "Jot Documents");

                if (!Directory.Exists(jotFolder))
                {
                    return new List<CloudDocument>();
                }

                var files = Directory.GetFiles(jotFolder, "*.md");
                var documents = new List<CloudDocument>();

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    documents.Add(new CloudDocument
                    {
                        Id = Path.GetFileNameWithoutExtension(file),
                        Name = fileInfo.Name,
                        ModifiedAt = fileInfo.LastWriteTime,
                        SizeInBytes = fileInfo.Length
                    });
                }

                return documents;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error listing cloud documents: {ex.Message}");
                return new List<CloudDocument>();
            }
        }

        /// <summary>
        /// Elimina un documento de la carpeta de nube
        /// </summary>
        public async Task<bool> DeleteCloudDocumentAsync(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting cloud document: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Habilita sincronización automática para un documento
        /// </summary>
        public void EnableAutoSync(Document document, CloudProvider provider)
        {
            document.IsSyncEnabled = true;
            document.CloudProvider = provider;

            if (string.IsNullOrEmpty(document.CloudSyncId))
            {
                document.CloudSyncId = "";
            }
        }

        /// <summary>
        /// Desactiva sincronización automática
        /// </summary>
        public void DisableAutoSync(Document document)
        {
            document.IsSyncEnabled = false;
        }

        /// <summary>
        /// Verifica si hay una carpeta configurada para un proveedor
        /// </summary>
        public bool IsProviderConfigured(CloudProvider provider)
        {
            return _settings.CloudFolders.ContainsKey(provider) && 
                   Directory.Exists(_settings.CloudFolders[provider]);
        }

        /// <summary>
        /// Obtiene la carpeta configurada para un proveedor
        /// </summary>
        public string GetProviderFolder(CloudProvider provider)
        {
            return _settings.CloudFolders.ContainsKey(provider) 
                ? _settings.CloudFolders[provider] 
                : "";
        }

        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "document";

            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(fileName
                .Where(c => !invalidChars.Contains(c))
                .ToArray());

            if (sanitized.Length > 100)
                sanitized = sanitized.Substring(0, 100);

            return string.IsNullOrWhiteSpace(sanitized) ? "document" : sanitized;
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFile))
                {
                    var json = File.ReadAllText(_settingsFile);
                    _settings = JsonConvert.DeserializeObject<CloudSyncSettings>(json) ?? new CloudSyncSettings();
                }
                else
                {
                    _settings = new CloudSyncSettings();
                }
            }
            catch
            {
                _settings = new CloudSyncSettings();
            }
        }

        private void SaveSettings()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
                File.WriteAllText(_settingsFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving cloud sync settings: {ex.Message}");
            }
        }
    }

    public class CloudSyncSettings
    {
        public Dictionary<CloudProvider, string> CloudFolders { get; set; } = new();
    }

    public class CloudDocument
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DateTime ModifiedAt { get; set; }
        public long SizeInBytes { get; set; }
    }
}
