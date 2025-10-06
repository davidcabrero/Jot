using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Jot.Models;
using System.Text.RegularExpressions;
using System.IO;
using Windows.Storage;

namespace Jot.Services
{
    public class ChatbotService
    {
        private readonly DocumentService _documentService;
        private readonly List<DocumentChunk> _indexedChunks;
        private readonly Dictionary<string, float[]> _embeddings;
        private readonly string _indexFilePath;

        public ChatbotService(DocumentService documentService)
        {
            _documentService = documentService;
            _indexedChunks = new List<DocumentChunk>();
            _embeddings = new Dictionary<string, float[]>();
            
            var localFolder = ApplicationData.Current.LocalFolder.Path;
            _indexFilePath = Path.Combine(localFolder, "chatbot_index.json");
        }

        public async Task<ChatResponse> AskQuestionAsync(string question, List<Document> documents)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"ChatbotService: Processing question '{question}' with {documents.Count} documents");

                // Check if we have documents
                if (!documents.Any())
                {
                    return new ChatResponse
                    {
                        Answer = "No tengo documentos disponibles para analizar. Por favor, crea algunos documentos primero y luego pregúntame sobre su contenido.",
                        SourceDocuments = new List<string>(),
                        Confidence = 0.0,
                        Timestamp = DateTime.Now
                    };
                }

                // Ensure documents are indexed
                await IndexDocumentsAsync(documents);

                System.Diagnostics.Debug.WriteLine($"ChatbotService: Indexed {_indexedChunks.Count} chunks");

                // Find relevant chunks using simple text matching and TF-IDF
                var relevantChunks = await FindRelevantChunksAsync(question, 5);

                System.Diagnostics.Debug.WriteLine($"ChatbotService: Found {relevantChunks.Count} relevant chunks");

                // Generate response based on relevant chunks
                var response = await GenerateResponseAsync(question, relevantChunks);

                System.Diagnostics.Debug.WriteLine($"ChatbotService: Generated response: '{response}'");

                var chatResponse = new ChatResponse
                {
                    Answer = response,
                    SourceDocuments = relevantChunks.Select(c => c.DocumentTitle).Distinct().ToList(),
                    Confidence = CalculateConfidence(question, relevantChunks),
                    Timestamp = DateTime.Now
                };

                System.Diagnostics.Debug.WriteLine($"ChatbotService: Response confidence: {chatResponse.Confidence:P2}");
                System.Diagnostics.Debug.WriteLine($"ChatbotService: Source documents: {string.Join(", ", chatResponse.SourceDocuments)}");

                return chatResponse;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ChatbotService Error: {ex.Message}");
                return new ChatResponse
                {
                    Answer = $"Lo siento, ocurrió un error al procesar tu pregunta: {ex.Message}",
                    SourceDocuments = new List<string>(),
                    Confidence = 0.0,
                    Timestamp = DateTime.Now
                };
            }
        }

        private async Task IndexDocumentsAsync(List<Document> documents)
        {
            // Clear existing index
            _indexedChunks.Clear();

            System.Diagnostics.Debug.WriteLine($"ChatbotService: Starting indexing of {documents.Count} documents");

            foreach (var document in documents)
            {
                System.Diagnostics.Debug.WriteLine($"ChatbotService: Processing document '{document.Title}' with content length: {document.Content?.Length ?? 0}");
                
                if (string.IsNullOrWhiteSpace(document.Content))
                {
                    System.Diagnostics.Debug.WriteLine($"ChatbotService: Skipping document '{document.Title}' - empty content");
                    continue;
                }

                var chunks = SplitDocumentIntoChunks(document);
                System.Diagnostics.Debug.WriteLine($"ChatbotService: Generated {chunks.Count} chunks for document '{document.Title}'");
                
                foreach (var chunk in chunks)
                {
                    System.Diagnostics.Debug.WriteLine($"ChatbotService: Chunk content: '{chunk.Content}' (IsHeader: {chunk.IsHeader})");
                }
                
                _indexedChunks.AddRange(chunks);
            }

            System.Diagnostics.Debug.WriteLine($"ChatbotService: Total indexed chunks: {_indexedChunks.Count}");

            // Save index to disk
            await SaveIndexAsync();
        }

        private List<DocumentChunk> SplitDocumentIntoChunks(Document document)
        {
            var chunks = new List<DocumentChunk>();
            
            if (string.IsNullOrWhiteSpace(document.Content))
            {
                System.Diagnostics.Debug.WriteLine($"ChatbotService: Document '{document.Title}' has no content");
                return chunks;
            }

            // First, add the title as a chunk if it's meaningful
            if (!string.IsNullOrWhiteSpace(document.Title))
            {
                chunks.Add(new DocumentChunk
                {
                    Id = Guid.NewGuid().ToString(),
                    DocumentId = document.Id,
                    DocumentTitle = document.Title,
                    Content = $"Título del documento: {document.Title}",
                    Keywords = ExtractKeywords(document.Title),
                    IsHeader = true,
                    CreatedAt = DateTime.Now
                });
            }

            var content = CleanMarkdownContent(document.Content);
            System.Diagnostics.Debug.WriteLine($"ChatbotService: Cleaned content length: {content.Length}");
            
            // Split by paragraphs first
            var paragraphs = content.Split(new[] { "\n\n", "\r\n\r\n", "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            System.Diagnostics.Debug.WriteLine($"ChatbotService: Found {paragraphs.Length} paragraphs");
            
            foreach (var paragraph in paragraphs)
            {
                var trimmed = paragraph.Trim();
                if (trimmed.Length > 10) // More permissive - was 50
                {
                    chunks.Add(new DocumentChunk
                    {
                        Id = Guid.NewGuid().ToString(),
                        DocumentId = document.Id,
                        DocumentTitle = document.Title,
                        Content = trimmed,
                        Keywords = ExtractKeywords(trimmed),
                        CreatedAt = DateTime.Now
                    });
                    System.Diagnostics.Debug.WriteLine($"ChatbotService: Added paragraph chunk: '{trimmed.Substring(0, Math.Min(50, trimmed.Length))}...'");
                }
            }

            // Also create chunks for headers as they're important for context
            var headers = ExtractHeaders(document.Content);
            System.Diagnostics.Debug.WriteLine($"ChatbotService: Found {headers.Count} headers");
            
            foreach (var header in headers)
            {
                chunks.Add(new DocumentChunk
                {
                    Id = Guid.NewGuid().ToString(),
                    DocumentId = document.Id,
                    DocumentTitle = document.Title,
                    Content = header,
                    Keywords = ExtractKeywords(header),
                    IsHeader = true,
                    CreatedAt = DateTime.Now
                });
                System.Diagnostics.Debug.WriteLine($"ChatbotService: Added header chunk: '{header}'");
            }

            // If we still don't have enough chunks, split the entire content more aggressively
            if (chunks.Count < 2)
            {
                var sentences = content.Split(new[] { ".", "!", "?" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var sentence in sentences)
                {
                    var trimmed = sentence.Trim();
                    if (trimmed.Length > 5)
                    {
                        chunks.Add(new DocumentChunk
                        {
                            Id = Guid.NewGuid().ToString(),
                            DocumentId = document.Id,
                            DocumentTitle = document.Title,
                            Content = trimmed,
                            Keywords = ExtractKeywords(trimmed),
                            CreatedAt = DateTime.Now
                        });
                        System.Diagnostics.Debug.WriteLine($"ChatbotService: Added sentence chunk: '{trimmed.Substring(0, Math.Min(30, trimmed.Length))}...'");
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"ChatbotService: Total chunks created for '{document.Title}': {chunks.Count}");
            return chunks;
        }

        private string CleanMarkdownContent(string content)
        {
            // Remove markdown formatting for better text processing
            content = Regex.Replace(content, @"```[\s\S]*?```", ""); // Remove code blocks
            content = Regex.Replace(content, @"`([^`]+)`", "$1"); // Remove inline code
            content = Regex.Replace(content, @"\*\*([^*]+)\*\*", "$1"); // Remove bold
            content = Regex.Replace(content, @"\*([^*]+)\*", "$1"); // Remove italic
            content = Regex.Replace(content, @"#{1,6}\s+", ""); // Remove headers
            content = Regex.Replace(content, @"\[([^\]]+)\]\([^)]+\)", "$1"); // Remove links
            content = Regex.Replace(content, @"!\[([^\]]*)\]\([^)]+\)", "$1"); // Remove images
            content = Regex.Replace(content, @">\s+", ""); // Remove quotes
            content = Regex.Replace(content, @"[-*+]\s+", ""); // Remove list markers
            content = Regex.Replace(content, @"\d+\.\s+", ""); // Remove numbered lists
            
            return content.Trim();
        }

        private List<string> ExtractHeaders(string content)
        {
            var headers = new List<string>();
            var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var match = Regex.Match(line.Trim(), @"^#{1,6}\s+(.+)$");
                if (match.Success)
                {
                    headers.Add(match.Groups[1].Value);
                }
            }
            
            return headers;
        }

        private List<string> ExtractKeywords(string text)
        {
            // Simple keyword extraction using word frequency
            var words = Regex.Matches(text.ToLowerInvariant(), @"\b\w{3,}\b")
                .Cast<Match>()
                .Select(m => m.Value)
                .Where(word => !IsStopWord(word))
                .GroupBy(word => word)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => g.Key)
                .ToList();
            
            return words;
        }

        private bool IsStopWord(string word)
        {
            var stopWords = new HashSet<string>
            {
                "el", "la", "de", "que", "y", "a", "en", "un", "es", "se", "no", "te", "lo", "le", "da", "su", "por", "son", "con", "para", "al", "del", "los", "las", "una", "como", "pero", "sus", "han", "me", "si", "sin", "sobre", "este", "ya", "todo", "esta", "cuando", "muy", "sin", "puede", "están", "tiene", "más", "fue", "ser", "hacer", "general", "gobierno", "cada", "hasta", "desde", "va", "mi", "porque", "qué", "sólo", "han", "yo", "hay", "vez", "puede", "todos", "así", "nos", "ni", "parte", "tiene", "él", "uno", "donde", "bien", "tiempo", "mismo", "ese", "ahora", "cada", "e", "vida", "otro", "después", "te", "otros", "aunque", "esa", "eso", "hace", "otra", "gobierno", "tan", "durante", "siempre", "día", "tanto", "ella", "tres", "sí", "dijo", "sido", "gran"
            };
            
            return stopWords.Contains(word);
        }

        private async Task<List<DocumentChunk>> FindRelevantChunksAsync(string question, int maxResults)
        {
            System.Diagnostics.Debug.WriteLine($"ChatbotService: Searching for relevant chunks for question: '{question}'");
            
            var questionKeywords = ExtractKeywords(question.ToLowerInvariant());
            System.Diagnostics.Debug.WriteLine($"ChatbotService: Question keywords: {string.Join(", ", questionKeywords)}");
            
            var scores = new List<(DocumentChunk chunk, double score)>();

            foreach (var chunk in _indexedChunks)
            {
                var score = CalculateRelevanceScore(question, questionKeywords, chunk);
                System.Diagnostics.Debug.WriteLine($"ChatbotService: Chunk '{chunk.Content.Substring(0, Math.Min(50, chunk.Content.Length))}...' scored: {score:F2}");
                
                if (score > 0.05) // Lower threshold - was 0.1
                {
                    scores.Add((chunk, score));
                }
            }

            var result = scores
                .OrderByDescending(s => s.score)
                .Take(maxResults)
                .Select(s => s.chunk)
                .ToList();

            System.Diagnostics.Debug.WriteLine($"ChatbotService: Selected {result.Count} relevant chunks with scores above threshold");
            
            return result;
        }

        private double CalculateRelevanceScore(string question, List<string> questionKeywords, DocumentChunk chunk)
        {
            var score = 0.0;
            var questionLower = question.ToLowerInvariant();
            var contentLower = chunk.Content.ToLowerInvariant();

            System.Diagnostics.Debug.WriteLine($"ChatbotService: Scoring chunk: '{chunk.Content.Substring(0, Math.Min(30, chunk.Content.Length))}...'");

            // Exact phrase match (highest weight)
            if (contentLower.Contains(questionLower))
            {
                score += 10.0;
                System.Diagnostics.Debug.WriteLine($"ChatbotService: Exact phrase match found, score += 10.0");
            }

            // Check for partial matches of question words
            var questionWordsSimple = questionLower.Split(new[] { ' ', '?', '¿', '¡', '!' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2 && !IsStopWord(w))
                .ToList();

            foreach (var word in questionWordsSimple)
            {
                if (contentLower.Contains(word))
                {
                    score += 2.0;
                    System.Diagnostics.Debug.WriteLine($"ChatbotService: Word '{word}' found in content, score += 2.0");
                }
            }

            // Keyword matches
            var keywordMatches = questionKeywords.Count(keyword => 
                chunk.Keywords.Contains(keyword) || contentLower.Contains(keyword));
            
            if (questionKeywords.Count > 0)
            {
                var keywordScore = (keywordMatches / (double)questionKeywords.Count) * 5.0;
                score += keywordScore;
                System.Diagnostics.Debug.WriteLine($"ChatbotService: Keyword matches: {keywordMatches}/{questionKeywords.Count}, score += {keywordScore:F2}");
            }

            // Content similarity (simple word overlap)
            var questionWords = Regex.Matches(questionLower, @"\b\w{3,}\b")
                .Cast<Match>()
                .Select(m => m.Value)
                .Where(word => !IsStopWord(word))
                .ToHashSet();

            var contentWords = Regex.Matches(contentLower, @"\b\w{3,}\b")
                .Cast<Match>()
                .Select(m => m.Value)
                .Where(word => !IsStopWord(word))
                .ToHashSet();

            var overlap = questionWords.Intersect(contentWords).Count();
            var union = questionWords.Union(contentWords).Count();
            
            if (union > 0)
            {
                var similarityScore = (overlap / (double)union) * 3.0;
                score += similarityScore;
                System.Diagnostics.Debug.WriteLine($"ChatbotService: Word overlap: {overlap}/{union}, score += {similarityScore:F2}");
            }

            // Boost for headers as they're often more relevant
            if (chunk.IsHeader)
            {
                score *= 1.5;
                System.Diagnostics.Debug.WriteLine($"ChatbotService: Header boost applied, score *= 1.5");
            }

            // Lower the threshold for better recall
            if (score > 0.0)
            {
                score += 0.5; // Base relevance score
            }

            System.Diagnostics.Debug.WriteLine($"ChatbotService: Final score for chunk: {score:F2}");
            return score;
        }

        private async Task<string> GenerateResponseAsync(string question, List<DocumentChunk> relevantChunks)
        {
            if (!relevantChunks.Any())
            {
                return GenerateNoResultsResponse(question);
            }

            var context = string.Join("\n\n", relevantChunks.Select(c => 
                $"[De '{c.DocumentTitle}']: {c.Content}"));

            // Simple rule-based response
            var response = GenerateRuleBasedResponse(question, relevantChunks, context);
            
            return response;
        }

        private string GenerateRuleBasedResponse(string question, List<DocumentChunk> chunks, string context)
        {
            var questionLower = question.ToLowerInvariant();
            var response = new StringBuilder();

            System.Diagnostics.Debug.WriteLine($"ChatbotService: Generating response for {chunks.Count} chunks");

            if (!chunks.Any())
            {
                return GenerateNoResultsResponse(question);
            }

            // Start with a simple introduction
            response.AppendLine("Basándome en tus documentos:");
            response.AppendLine();

            // Add relevant content - simplified approach
            var addedContent = false;
            var addedDocuments = new HashSet<string>();
            
            foreach (var chunk in chunks.Take(5)) // Take more chunks
            {
                if (!string.IsNullOrWhiteSpace(chunk.Content))
                {
                    // Add document header if not added yet
                    if (!addedDocuments.Contains(chunk.DocumentTitle))
                    {
                        response.AppendLine($"**Documento: {chunk.DocumentTitle}**");
                        addedDocuments.Add(chunk.DocumentTitle);
                    }
                    
                    // Add the content
                    response.AppendLine($"• {chunk.Content}");
                    response.AppendLine();
                    addedContent = true;
                    
                    System.Diagnostics.Debug.WriteLine($"ChatbotService: Added content from chunk: '{chunk.Content.Substring(0, Math.Min(50, chunk.Content.Length))}...'");
                }
            }

            if (!addedContent)
            {
                // Fallback response
                response.Clear();
                response.AppendLine("Encontré información en tus documentos, pero no pude extraer contenido específico.");
                response.AppendLine();
                response.AppendLine("Documentos disponibles:");
                foreach (var docTitle in chunks.Select(c => c.DocumentTitle).Distinct().Take(3))
                {
                    response.AppendLine($"• {docTitle}");
                }
                response.AppendLine();
                response.AppendLine("Intenta hacer una pregunta más específica.");
            }

            var finalResponse = response.ToString().Trim();
            
            System.Diagnostics.Debug.WriteLine($"ChatbotService: Final response length: {finalResponse.Length}");
            System.Diagnostics.Debug.WriteLine($"ChatbotService: Final response: '{finalResponse}'");

            // Ensure we never return empty response
            if (string.IsNullOrWhiteSpace(finalResponse))
            {
                return "No pude generar una respuesta adecuada. Por favor, reformula tu pregunta.";
            }

            return finalResponse;
        }

        private bool IsWhatQuestion(string question) => 
            question.StartsWith("qué") || question.StartsWith("que") || question.Contains("qué es") || question.Contains("que es");

        private bool IsHowQuestion(string question) => 
            question.StartsWith("cómo") || question.StartsWith("como") || question.Contains("cómo se") || question.Contains("como se");

        private bool IsWhereQuestion(string question) => 
            question.StartsWith("dónde") || question.StartsWith("donde") || question.Contains("dónde está") || question.Contains("donde esta");

        private bool IsWhenQuestion(string question) => 
            question.StartsWith("cuándo") || question.StartsWith("cuando") || question.Contains("cuándo se") || question.Contains("cuando se");

        private string GenerateNoResultsResponse(string question)
        {
            return $"No encontré información específica para: \"{question}\"\n\n" +
                   $"Actualmente tengo {_indexedChunks.Count} fragmentos de información indexados.\n\n" +
                   "Sugerencias:\n" +
                   "• Intenta reformular tu pregunta\n" +
                   "• Usa palabras clave más específicas\n" +
                   "• Asegúrate de que la información esté en tus documentos";
        }

        private double CalculateConfidence(string question, List<DocumentChunk> chunks)
        {
            if (!chunks.Any()) return 0.0;

            var questionKeywords = ExtractKeywords(question.ToLowerInvariant());
            if (!questionKeywords.Any()) return 0.5;

            var averageScore = chunks.Average(chunk => 
                CalculateRelevanceScore(question, questionKeywords, chunk));

            // Normalize score to 0-1 range
            return Math.Min(averageScore / 10.0, 1.0);
        }

        private async Task SaveIndexAsync()
        {
            try
            {
                var indexData = new
                {
                    Chunks = _indexedChunks,
                    LastUpdated = DateTime.Now
                };

                var json = JsonSerializer.Serialize(indexData, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });

                await File.WriteAllTextAsync(_indexFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving chatbot index: {ex.Message}");
            }
        }

        public async Task LoadIndexAsync()
        {
            try
            {
                if (File.Exists(_indexFilePath))
                {
                    var json = await File.ReadAllTextAsync(_indexFilePath);
                    var indexData = JsonSerializer.Deserialize<dynamic>(json);
                    // Implement loading logic as needed
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading chatbot index: {ex.Message}");
            }
        }

        public async Task<List<string>> GetSuggestedQuestionsAsync(List<Document> documents)
        {
            var suggestions = new List<string>();
            
            if (!documents.Any()) return suggestions;

            // Generate suggestions based on document content
            suggestions.Add("¿Cuáles son los temas principales en mis documentos?");
            suggestions.Add("¿Qué información hay sobre [tema específico]?");
            suggestions.Add("Resume el contenido de mis documentos");
            
            // Add document-specific suggestions
            foreach (var doc in documents.Take(3))
            {
                suggestions.Add($"¿Qué información contiene '{doc.Title}'?");
            }

            return suggestions;
        }
    }

    public class DocumentChunk
    {
        public string Id { get; set; } = "";
        public Guid DocumentId { get; set; }
        public string DocumentTitle { get; set; } = "";
        public string Content { get; set; } = "";
        public List<string> Keywords { get; set; } = new();
        public bool IsHeader { get; set; } = false;
        public DateTime CreatedAt { get; set; }
    }

    public class ChatResponse
    {
        public string Answer { get; set; } = "";
        public List<string> SourceDocuments { get; set; } = new();
        public double Confidence { get; set; }
        public DateTime Timestamp { get; set; }
    }
}