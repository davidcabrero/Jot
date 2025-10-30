using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Jot.Models;
using System.Linq;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using System.Text.Json.Serialization;

namespace Jot.Services
{
    public class GitHubUploadService
    {
        private readonly HttpClient _httpClient;
        private string? _personalAccessToken;
    private GitHubUserInfo? _userInfo;

        public event EventHandler<GitHubUploadResult>? UploadCompleted;
        public event EventHandler<string>? UploadProgress;

        public bool IsAuthenticated => !string.IsNullOrEmpty(_personalAccessToken) && _userInfo != null;
        public GitHubUserInfo? UserInfo => _userInfo;

        public GitHubUploadService()
  {
            _httpClient = new HttpClient();
  _httpClient.DefaultRequestHeaders.Add("User-Agent", "Jot-Note-App/1.0");
   }

        public async Task<bool> AuthenticateAsync(string personalAccessToken)
  {
        try
   {
      _personalAccessToken = personalAccessToken;
       _httpClient.DefaultRequestHeaders.Authorization = 
       new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _personalAccessToken);

     // Verify token by getting user info
  var response = await _httpClient.GetAsync("https://api.github.com/user");
    
                if (response.IsSuccessStatusCode)
              {
            var jsonContent = await response.Content.ReadAsStringAsync();
           _userInfo = JsonSerializer.Deserialize<GitHubUserInfo>(jsonContent);
       
  System.Diagnostics.Debug.WriteLine($"✅ GitHub authentication successful for user: {_userInfo?.Login}");
           return true;
   }
        else
    {
         System.Diagnostics.Debug.WriteLine($"❌ GitHub authentication failed: {response.StatusCode}");
           _personalAccessToken = null;
                 _userInfo = null;
 return false;
      }
          }
            catch (Exception ex)
            {
     System.Diagnostics.Debug.WriteLine($"❌ GitHub authentication error: {ex.Message}");
                _personalAccessToken = null;
             _userInfo = null;
      return false;
     }
        }

        public async Task<List<GitHubRepository>> GetRepositoriesAsync()
   {
      if (!IsAuthenticated)
          return new List<GitHubRepository>();

      try
    {
            var repositories = new List<GitHubRepository>();
                
   // Get user repositories
    var userReposResponse = await _httpClient.GetAsync("https://api.github.com/user/repos?sort=updated&per_page=100");
        if (userReposResponse.IsSuccessStatusCode)
        {
        var userReposJson = await userReposResponse.Content.ReadAsStringAsync();
   var userRepos = JsonSerializer.Deserialize<GitHubRepository[]>(userReposJson) ?? Array.Empty<GitHubRepository>();
      repositories.AddRange(userRepos);
        }

     return repositories.OrderByDescending(r => r.UpdatedAt).ToList();
         }
  catch (Exception ex)
            {
     System.Diagnostics.Debug.WriteLine($"❌ Error getting repositories: {ex.Message}");
      return new List<GitHubRepository>();
            }
        }

        public async Task<GitHubUploadResult> UploadDocumentAsync(Document document, GitHubRepository repository, string? path = null, string? commitMessage = null)
  {
        if (!IsAuthenticated)
            {
   return new GitHubUploadResult
        {
        Success = false,
        Error = "Not authenticated with GitHub"
       };
   }

            try
            {
           UploadProgress?.Invoke(this, "Preparing document for upload...");

     // Convert document to markdown content
 var content = ConvertDocumentToMarkdown(document);
   var fileName = SanitizeFileName(document.Title) + ".md";
       var filePath = string.IsNullOrEmpty(path) ? fileName : $"{path.TrimEnd('/')}/{fileName}";

    UploadProgress?.Invoke(this, "Checking if file exists...");

 // Check if file already exists
                var existingFile = await GetFileInfoAsync(repository.FullName, filePath);
     
     UploadProgress?.Invoke(this, "Uploading to GitHub...");

       // Prepare the request
                var uploadRequest = new GitHubFileUploadRequest
        {
 Message = commitMessage ?? $"Add/Update {document.Title} from Jot",
            Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(content)),
      Sha = existingFile?.Sha // Include SHA if file exists (for updates)
};

           var requestJson = JsonSerializer.Serialize(uploadRequest, new JsonSerializerOptions 
    { 
  PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
 });

             var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

   // Upload file
                var uploadUrl = $"https://api.github.com/repos/{repository.FullName}/contents/{filePath}";
              var response = await _httpClient.PutAsync(uploadUrl, requestContent);

      if (response.IsSuccessStatusCode)
     {
         var responseJson = await response.Content.ReadAsStringAsync();
      var uploadResponse = JsonSerializer.Deserialize<GitHubFileUploadResponse>(responseJson);

 var result = new GitHubUploadResult
        {
  Success = true,
     FileUrl = uploadResponse?.Content?.HtmlUrl ?? "",
    FileName = fileName,
          FilePath = filePath,
            Repository = repository.FullName,
        CommitSha = uploadResponse?.Commit?.Sha ?? ""
      };

 UploadProgress?.Invoke(this, "Upload completed successfully!");
     UploadCompleted?.Invoke(this, result);
           
        System.Diagnostics.Debug.WriteLine($"✅ Document uploaded to GitHub: {result.FileUrl}");
            return result;
            }
        else
      {
         var errorContent = await response.Content.ReadAsStringAsync();
         var result = new GitHubUploadResult
    {
      Success = false,
            Error = $"GitHub API error ({response.StatusCode}): {errorContent}"
       };

         UploadCompleted?.Invoke(this, result);
 return result;
   }
            }
catch (Exception ex)
         {
   var result = new GitHubUploadResult
      {
   Success = false,
          Error = $"Upload error: {ex.Message}"
          };

       UploadCompleted?.Invoke(this, result);
     return result;
        }
        }

      public async Task<GitHubUploadResult> CreateRepositoryAsync(string name, string description, bool isPrivate = false)
        {
 if (!IsAuthenticated)
            {
return new GitHubUploadResult
      {
            Success = false,
         Error = "Not authenticated with GitHub"
            };
        }

            try
   {
    var createRepoRequest = new
          {
               name = name,
         description = description,
 @private = isPrivate,
      auto_init = true
             };

     var requestJson = JsonSerializer.Serialize(createRepoRequest);
         var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

   var response = await _httpClient.PostAsync("https://api.github.com/user/repos", requestContent);

                if (response.IsSuccessStatusCode)
   {
          var responseJson = await response.Content.ReadAsStringAsync();
      var repository = JsonSerializer.Deserialize<GitHubRepository>(responseJson);

   return new GitHubUploadResult
     {
         Success = true,
  Repository = repository?.FullName ?? "",
       FileUrl = repository?.HtmlUrl ?? ""
     };
   }
          else
      {
       var errorContent = await response.Content.ReadAsStringAsync();
            return new GitHubUploadResult
 {
          Success = false,
     Error = $"Failed to create repository: {errorContent}"
        };
           }
    }
       catch (Exception ex)
{
      return new GitHubUploadResult
         {
       Success = false,
        Error = $"Error creating repository: {ex.Message}"
     };
            }
        }

      private async Task<GitHubFileInfo?> GetFileInfoAsync(string repositoryFullName, string filePath)
        {
    try
          {
   var url = $"https://api.github.com/repos/{repositoryFullName}/contents/{filePath}";
     var response = await _httpClient.GetAsync(url);

     if (response.IsSuccessStatusCode)
           {
    var jsonContent = await response.Content.ReadAsStringAsync();
              return JsonSerializer.Deserialize<GitHubFileInfo>(jsonContent);
           }

     return null;
        }
            catch
            {
    return null;
 }
 }

        private string ConvertDocumentToMarkdown(Document document)
        {
    var markdown = new StringBuilder();

   // Add frontmatter
            markdown.AppendLine("---");
 markdown.AppendLine($"title: \"{document.Title}\"");
         markdown.AppendLine($"created: {document.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            markdown.AppendLine($"modified: {document.ModifiedAt:yyyy-MM-dd HH:mm:ss}");
  markdown.AppendLine($"exported_from: Jot Note App");
            markdown.AppendLine($"export_date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
         markdown.AppendLine("---");
       markdown.AppendLine();

  // Add title as H1
    markdown.AppendLine($"# {document.Title}");
            markdown.AppendLine();

       // Add metadata
 markdown.AppendLine("**Document Information:**");
    markdown.AppendLine($"- Created: {document.CreatedAt:yyyy-MM-dd HH:mm}");
            markdown.AppendLine($"- Last Modified: {document.ModifiedAt:yyyy-MM-dd HH:mm}");
  markdown.AppendLine($"- Exported from: Jot Note App");
    markdown.AppendLine();

      markdown.AppendLine("---");
  markdown.AppendLine();

   // Add document content
   markdown.AppendLine(document.Content);

       // Add footer
            markdown.AppendLine();
   markdown.AppendLine("---");
            markdown.AppendLine($"*Document exported from Jot on {DateTime.Now:yyyy-MM-dd HH:mm:ss}*");

         return markdown.ToString();
        }

        private string SanitizeFileName(string fileName)
   {
            var invalidChars = Path.GetInvalidFileNameChars();
     var sanitized = new StringBuilder();

       foreach (char c in fileName)
      {
     if (!invalidChars.Contains(c) && c != ' ')
      {
  sanitized.Append(c);
    }
       else if (c == ' ')
                {
        sanitized.Append('-');
    }
     }

      var result = sanitized.ToString().Trim('-');
            return string.IsNullOrEmpty(result) ? "untitled-document" : result;
        }

        public void Logout()
        {
            _personalAccessToken = null;
     _userInfo = null;
       _httpClient.DefaultRequestHeaders.Authorization = null;
        System.Diagnostics.Debug.WriteLine("🔓 Logged out from GitHub");
        }

 public void Dispose()
        {
      _httpClient?.Dispose();
        }
    }

    public class GitHubUserInfo
    {
        [JsonPropertyName("login")]
        public string Login { get; set; } = "";

        [JsonPropertyName("id")]
        public long Id { get; set; }

     [JsonPropertyName("avatar_url")]
        public string AvatarUrl { get; set; } = "";

     [JsonPropertyName("name")]
   public string? Name { get; set; }

        [JsonPropertyName("email")]
     public string? Email { get; set; }

        [JsonPropertyName("public_repos")]
        public int PublicRepos { get; set; }

        [JsonPropertyName("private_repos")]
        public int PrivateRepos { get; set; }
    }

    public class GitHubRepository
    {
      [JsonPropertyName("id")]
        public long Id { get; set; }

   [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = "";

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("html_url")]
 public string HtmlUrl { get; set; } = "";

        [JsonPropertyName("private")]
        public bool Private { get; set; }

        [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("default_branch")]
        public string DefaultBranch { get; set; } = "main";

        public override string ToString() => $"{Name} - {Description}";
    }

  public class GitHubUploadResult
    {
        public bool Success { get; set; }
        public string Error { get; set; } = "";
  public string FileUrl { get; set; } = "";
        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string Repository { get; set; } = "";
        public string CommitSha { get; set; } = "";
    }

    public class GitHubFileUploadRequest
    {
    public string Message { get; set; } = "";
        public string Content { get; set; } = "";
        public string? Sha { get; set; }
    }

    public class GitHubFileUploadResponse
    {
    [JsonPropertyName("content")]
        public GitHubFileContent? Content { get; set; }

        [JsonPropertyName("commit")]
        public GitHubCommitInfo? Commit { get; set; }
    }

    public class GitHubFileContent
    {
        [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = "";

        [JsonPropertyName("download_url")]
        public string DownloadUrl { get; set; } = "";
    }

    public class GitHubCommitInfo
    {
        [JsonPropertyName("sha")]
    public string Sha { get; set; } = "";
  }

    public class GitHubFileInfo
 {
        [JsonPropertyName("sha")]
        public string Sha { get; set; } = "";

        [JsonPropertyName("content")]
        public string Content { get; set; } = "";
  }
}