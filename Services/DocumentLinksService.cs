using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jot.Models;

namespace Jot.Services
{
    /// <summary>
    /// Servicio para gestionar enlaces entre documentos (Wiki-style linking)
    /// </summary>
    public class DocumentLinksService
    {
        // Patrón para detectar enlaces: [[Nombre del Documento]]
        private static readonly Regex LinkPattern = new Regex(@"\[\[([^\]]+)\]\]", RegexOptions.Compiled);

        /// <summary>
        /// Extrae todos los enlaces del contenido de un documento
        /// </summary>
        public List<string> ExtractLinks(string content)
        {
            try
            {
                if (string.IsNullOrEmpty(content))
                {
                    return new List<string>();
                }

                var matches = LinkPattern.Matches(content);
                return matches.Select(m => m.Groups[1].Value.Trim()).Distinct().ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extracting links: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Actualiza los enlaces de un documento basándose en su contenido
        /// </summary>
        public void UpdateDocumentLinks(Document document, List<Document> allDocuments)
        {
            try
            {
                // Extraer nombres de documentos enlazados
                var linkedTitles = ExtractLinks(document.Content);
                
                // Convertir títulos a IDs
                document.LinkedDocumentIds.Clear();
                foreach (var title in linkedTitles)
                {
                    var linkedDoc = allDocuments.FirstOrDefault(d => 
                        d.Title.Equals(title, StringComparison.OrdinalIgnoreCase));
                    
                    if (linkedDoc != null && linkedDoc.Id != document.Id)
                    {
                        document.LinkedDocumentIds.Add(linkedDoc.Id.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating document links: {ex.Message}");
            }
        }

        /// <summary>
        /// Actualiza los backlinks de todos los documentos
        /// </summary>
        public void UpdateAllBacklinks(List<Document> allDocuments)
        {
            try
            {
                // Limpiar todos los backlinks
                foreach (var doc in allDocuments)
                {
                    doc.BackLinks.Clear();
                }

                // Reconstruir backlinks
                foreach (var doc in allDocuments)
                {
                    UpdateDocumentLinks(doc, allDocuments);
                    
                    foreach (var linkedIdStr in doc.LinkedDocumentIds)
                    {
                        if (Guid.TryParse(linkedIdStr, out var linkedId))
                        {
                            var linkedDoc = allDocuments.FirstOrDefault(d => d.Id == linkedId);
                            if (linkedDoc != null && !linkedDoc.BackLinks.Contains(doc.Id.ToString()))
                            {
                                linkedDoc.BackLinks.Add(doc.Id.ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating all backlinks: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene todos los documentos que enlazan a un documento específico
        /// </summary>
        public List<Document> GetBacklinks(Document document, List<Document> allDocuments)
        {
            try
            {
                var backlinks = new List<Document>();
                
                foreach (var backlinkIdStr in document.BackLinks)
                {
                    if (Guid.TryParse(backlinkIdStr, out var backlinkId))
                    {
                        var backlinkDoc = allDocuments.FirstOrDefault(d => d.Id == backlinkId);
                        if (backlinkDoc != null)
                        {
                            backlinks.Add(backlinkDoc);
                        }
                    }
                }

                return backlinks;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting backlinks: {ex.Message}");
                return new List<Document>();
            }
        }

        /// <summary>
        /// Obtiene todos los documentos enlazados desde un documento
        /// </summary>
        public List<Document> GetLinkedDocuments(Document document, List<Document> allDocuments)
        {
            try
            {
                var linkedDocs = new List<Document>();
                
                foreach (var linkedIdStr in document.LinkedDocumentIds)
                {
                    if (Guid.TryParse(linkedIdStr, out var linkedId))
                    {
                        var linkedDoc = allDocuments.FirstOrDefault(d => d.Id == linkedId);
                        if (linkedDoc != null)
                        {
                            linkedDocs.Add(linkedDoc);
                        }
                    }
                }

                return linkedDocs;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting linked documents: {ex.Message}");
                return new List<Document>();
            }
        }

        /// <summary>
        /// Convierte enlaces en formato Markdown a HTML con navegación
        /// </summary>
        public string ConvertLinksToHtml(string content, List<Document> allDocuments)
        {
            try
            {
                return LinkPattern.Replace(content, match =>
                {
                    var title = match.Groups[1].Value.Trim();
                    var linkedDoc = allDocuments.FirstOrDefault(d => 
                        d.Title.Equals(title, StringComparison.OrdinalIgnoreCase));

                    if (linkedDoc != null)
                    {
                        return $"<a href='#doc-{linkedDoc.Id}' class='doc-link' data-doc-id='{linkedDoc.Id}'>{title}</a>";
                    }
                    else
                    {
                        return $"<span class='broken-link' title='Document not found'>{title}</span>";
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error converting links to HTML: {ex.Message}");
                return content;
            }
        }

        /// <summary>
        /// Crea un enlace a un documento en formato Wiki
        /// </summary>
        public string CreateLink(Document document)
        {
            return $"[[{document.Title}]]";
        }

        /// <summary>
        /// Verifica si un documento tiene enlaces rotos
        /// </summary>
        public List<string> GetBrokenLinks(Document document, List<Document> allDocuments)
        {
            try
            {
                var linkedTitles = ExtractLinks(document.Content);
                var brokenLinks = new List<string>();

                foreach (var title in linkedTitles)
                {
                    var exists = allDocuments.Any(d => 
                        d.Title.Equals(title, StringComparison.OrdinalIgnoreCase));
                    
                    if (!exists)
                    {
                        brokenLinks.Add(title);
                    }
                }

                return brokenLinks;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting broken links: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Genera un grafo de relaciones entre documentos
        /// </summary>
        public Dictionary<Guid, List<Guid>> GenerateDocumentGraph(List<Document> allDocuments)
        {
            try
            {
                var graph = new Dictionary<Guid, List<Guid>>();

                foreach (var doc in allDocuments)
                {
                    var connections = new List<Guid>();
                    
                    foreach (var linkedIdStr in doc.LinkedDocumentIds)
                    {
                        if (Guid.TryParse(linkedIdStr, out var linkedId))
                        {
                            connections.Add(linkedId);
                        }
                    }

                    graph[doc.Id] = connections;
                }

                return graph;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating document graph: {ex.Message}");
                return new Dictionary<Guid, List<Guid>>();
            }
        }

        /// <summary>
        /// Encuentra documentos relacionados (que comparten enlaces)
        /// </summary>
        public List<Document> FindRelatedDocuments(Document document, List<Document> allDocuments, int maxResults = 5)
        {
            try
            {
                var relatedScores = new Dictionary<Guid, int>();

                // Documentos que este documento enlaza
                var linkedDocs = GetLinkedDocuments(document, allDocuments);
                
                // Documentos que enlazan a este
                var backlinks = GetBacklinks(document, allDocuments);

                // Asignar puntos por conexiones
                foreach (var linkedDoc in linkedDocs)
                {
                    relatedScores[linkedDoc.Id] = relatedScores.GetValueOrDefault(linkedDoc.Id, 0) + 2;
                }

                foreach (var backlinkDoc in backlinks)
                {
                    relatedScores[backlinkDoc.Id] = relatedScores.GetValueOrDefault(backlinkDoc.Id, 0) + 2;
                }

                // Documentos que comparten enlaces comunes
                foreach (var otherDoc in allDocuments)
                {
                    if (otherDoc.Id == document.Id) continue;

                    var commonLinks = document.LinkedDocumentIds.Intersect(otherDoc.LinkedDocumentIds).Count();
                    if (commonLinks > 0)
                    {
                        relatedScores[otherDoc.Id] = relatedScores.GetValueOrDefault(otherDoc.Id, 0) + commonLinks;
                    }
                }

                // Ordenar por puntuación y devolver los más relacionados
                return relatedScores
                    .OrderByDescending(kv => kv.Value)
                    .Take(maxResults)
                    .Select(kv => allDocuments.First(d => d.Id == kv.Key))
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error finding related documents: {ex.Message}");
                return new List<Document>();
            }
        }
    }
}
