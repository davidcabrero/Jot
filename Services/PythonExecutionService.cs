using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using System.Linq;
using Microsoft.Win32;
using System.Text.RegularExpressions;

namespace Jot.Services
{
    public class PythonExecutionService
    {
        private List<PythonInterpreter> _availableInterpreters = new();
   private PythonInterpreter? _selectedInterpreter;

        public event EventHandler<List<PythonInterpreter>>? InterpretersUpdated;
        public event EventHandler<PythonExecutionResult>? CodeExecuted;

        public List<PythonInterpreter> AvailableInterpreters => _availableInterpreters;
        public PythonInterpreter? SelectedInterpreter => _selectedInterpreter;

        public async Task<List<PythonInterpreter>> DiscoverPythonInterpretersAsync()
      {
            _availableInterpreters.Clear();

     try
    {
  System.Diagnostics.Debug.WriteLine("🔍 Starting Python interpreter discovery...");

          // Check common Python installation paths
         var searchPaths = new List<string>
       {
  // Standard installation paths
     @"C:\Python*",
    @"C:\Program Files\Python*",
       @"C:\Program Files (x86)\Python*",
         
         // User-specific installations
     Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Programs\Python\Python*",
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Python\Python*",
      Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\AppData\Local\Programs\Python\Python*",
         
   // Microsoft Store Python
   Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Microsoft\WindowsApps\python*",
    
    // Chocolatey installations
       @"C:\ProgramData\chocolatey\lib\python*\tools",
  @"C:\tools\python*",
         
         // Developer tools
      @"C:\dev\python*",
       @"C:\Python\Python*"
      };

     System.Diagnostics.Debug.WriteLine("🔍 Checking PATH environment...");
    await CheckPathForPython();

     System.Diagnostics.Debug.WriteLine("🔍 Checking Windows Registry...");
   await CheckRegistryForPython();

       System.Diagnostics.Debug.WriteLine("🔍 Checking common directories...");
        foreach (var searchPath in searchPaths)
   {
 await CheckDirectoryPattern(searchPath);
          }

    System.Diagnostics.Debug.WriteLine("🔍 Checking Anaconda/Miniconda...");
       await CheckAnacondaInstallations();

        // Add some fallback simple commands if nothing found
   if (_availableInterpreters.Count == 0)
 {
      System.Diagnostics.Debug.WriteLine("🔍 No interpreters found, adding fallback commands...");
      _availableInterpreters.Add(new PythonInterpreter
       {
      Name = "Python (try 'python' command)",
      Path = "python",
 Version = "Unknown",
       Command = "python"
    });
    _availableInterpreters.Add(new PythonInterpreter
   {
   Name = "Python3 (try 'python3' command)",
       Path = "python3",
      Version = "Unknown",
      Command = "python3"
      });
       _availableInterpreters.Add(new PythonInterpreter
  {
      Name = "Python Launcher (try 'py' command)",
     Path = "py",
      Version = "Unknown",
    Command = "py"
   });
     }

        // Remove duplicates and sort by version
    _availableInterpreters = _availableInterpreters
  .GroupBy(p => p.Path.ToLowerInvariant())
    .Select(g => g.First())
       .OrderByDescending(p => p.Version != "Unknown" ? p.Version : "")
  .ThenBy(p => p.Name)
.ToList();

       // Set default interpreter (highest version that's not "Unknown")
  var bestInterpreter = _availableInterpreters.FirstOrDefault(p => p.Version != "Unknown") 
 ?? _availableInterpreters.FirstOrDefault();
            
 if (bestInterpreter != null && _selectedInterpreter == null)
 {
         _selectedInterpreter = bestInterpreter;
  System.Diagnostics.Debug.WriteLine($"🎯 Default interpreter: {bestInterpreter.Name}");
       }

     InterpretersUpdated?.Invoke(this, _availableInterpreters);
           System.Diagnostics.Debug.WriteLine($"✅ Discovery complete: Found {_availableInterpreters.Count} Python interpreters");
           
     foreach (var interpreter in _availableInterpreters)
     {
        System.Diagnostics.Debug.WriteLine($"  - {interpreter.Name} ({interpreter.Path})");
   }

    return _availableInterpreters;
 }
  catch (Exception ex)
     {
         System.Diagnostics.Debug.WriteLine($"❌ Error discovering Python interpreters: {ex.Message}");
     return _availableInterpreters;
 }
   }

        private async Task CheckPathForPython()
  {
      try
    {
       // Check python command
     var result = await RunCommand("python", "--version", timeoutMs: 5000);
  if (result.Success && result.Output.Contains("Python"))
    {
        var version = ExtractVersionFromOutput(result.Output);
        var pythonPath = await GetPythonPath("python");
            
     if (!string.IsNullOrEmpty(pythonPath))
      {
        _availableInterpreters.Add(new PythonInterpreter
         {
          Name = $"Python {version} (System PATH)",
      Path = pythonPath,
      Version = version,
        Command = "python"
         });
  System.Diagnostics.Debug.WriteLine($"✅ Found Python in PATH: {pythonPath}");
   }
  }

    // Check python3 command
        var result3 = await RunCommand("python3", "--version", timeoutMs: 5000);
          if (result3.Success && result3.Output.Contains("Python"))
   {
    var version = ExtractVersionFromOutput(result3.Output);
   var pythonPath = await GetPythonPath("python3");
           
             if (!string.IsNullOrEmpty(pythonPath) && !_availableInterpreters.Any(p => p.Path.Equals(pythonPath, StringComparison.OrdinalIgnoreCase)))
         {
_availableInterpreters.Add(new PythonInterpreter
         {
          Name = $"Python {version} (python3)",
 Path = pythonPath,
   Version = version,
        Command = "python3"
      });
      System.Diagnostics.Debug.WriteLine($"✅ Found Python3 in PATH: {pythonPath}");
         }
     }

        // Check py launcher (Windows)
  var pyResult = await RunCommand("py", "--version", timeoutMs: 5000);
 if (pyResult.Success && pyResult.Output.Contains("Python"))
     {
    var version = ExtractVersionFromOutput(pyResult.Output);
  _availableInterpreters.Add(new PythonInterpreter
  {
   Name = $"Python {version} (py launcher)",
     Path = "py",
        Version = version,
           Command = "py"
     });
             System.Diagnostics.Debug.WriteLine($"✅ Found py launcher");
        }
      }
      catch (Exception ex)
       {
       System.Diagnostics.Debug.WriteLine($"⚠️ Error checking PATH for Python: {ex.Message}");
   }
 }

        private async Task<string?> GetPythonPath(string command)
        {
 try
    {
       var result = await RunCommand("where", command, timeoutMs: 3000);
     if (result.Success && !string.IsNullOrEmpty(result.Output))
         {
  return result.Output.Split('\n')[0].Trim();
           }
            }
        catch { }
            
            return null;
    }

private async Task CheckRegistryForPython()
        {
            try
            {
          // Check HKEY_LOCAL_MACHINE\SOFTWARE\Python
       await CheckRegistryKey(Registry.LocalMachine, @"SOFTWARE\Python\PythonCore");
     await CheckRegistryKey(Registry.CurrentUser, @"SOFTWARE\Python\PythonCore");
     }
       catch (Exception ex)
            {
          System.Diagnostics.Debug.WriteLine($"⚠️ Error checking registry: {ex.Message}");
      }
        }

        private async Task CheckRegistryKey(RegistryKey rootKey, string subKeyPath)
        {
try
            {
using var key = rootKey.OpenSubKey(subKeyPath);
         if (key == null) return;

    foreach (string versionName in key.GetSubKeyNames())
       {
 using var versionKey = key.OpenSubKey($"{versionName}\\InstallPath");
    if (versionKey?.GetValue("") is string installPath)
       {
             var pythonExe = Path.Combine(installPath, "python.exe");
              if (File.Exists(pythonExe))
        {
    var version = await GetPythonVersionFromExecutable(pythonExe);
  if (!string.IsNullOrEmpty(version))
        {
       _availableInterpreters.Add(new PythonInterpreter
      {
      Name = $"Python {version} (Registry)",
         Path = pythonExe,
  Version = version,
            Command = pythonExe
  });
         }
     }
          }
    }
    }
      catch (Exception ex)
{
           System.Diagnostics.Debug.WriteLine($"⚠️ Error checking registry key {subKeyPath}: {ex.Message}");
            }
   }

        private async Task CheckDirectoryPattern(string pattern)
        {
  try
            {
     var basePath = Path.GetDirectoryName(pattern);
        var searchPattern = Path.GetFileName(pattern);

        if (string.IsNullOrEmpty(basePath))
    return;

   // Handle wildcards in base path
  if (basePath.Contains("*"))
    {
    var parentPath = Path.GetDirectoryName(basePath);
        var dirPattern = Path.GetFileName(basePath);
        
        if (string.IsNullOrEmpty(parentPath) || !Directory.Exists(parentPath))
      return;

     try
     {
    var matchingDirs = Directory.GetDirectories(parentPath, dirPattern);
      foreach (var dir in matchingDirs)
        {
     await CheckSingleDirectory(dir);
  }
       }
       catch (Exception ex)
    {
   System.Diagnostics.Debug.WriteLine($"⚠️ Error checking wildcard directory {basePath}: {ex.Message}");
      }
         }
  else if (Directory.Exists(basePath))
     {
      await CheckSingleDirectory(basePath);
   }
     }
   catch (Exception ex)
   {
              System.Diagnostics.Debug.WriteLine($"⚠️ Error checking directory pattern {pattern}: {ex.Message}");
        }
  }

  private async Task CheckSingleDirectory(string directory)
     {
          try
    {
     var pythonExe = Path.Combine(directory, "python.exe");
     if (File.Exists(pythonExe))
  {
   var version = await GetPythonVersionFromExecutable(pythonExe);
        if (!string.IsNullOrEmpty(version))
         {
 var name = $"Python {version} ({Path.GetFileName(directory)})";
    
     // Avoid duplicates
  if (!_availableInterpreters.Any(p => p.Path.Equals(pythonExe, StringComparison.OrdinalIgnoreCase)))
     {
 _availableInterpreters.Add(new PythonInterpreter
      {
       Name = name,
    Path = pythonExe,
      Version = version,
     Command = pythonExe
       });
        System.Diagnostics.Debug.WriteLine($"✅ Found Python: {name} at {pythonExe}");
      }
 }
       }
 }
    catch (Exception ex)
      {
         System.Diagnostics.Debug.WriteLine($"⚠️ Error checking single directory {directory}: {ex.Message}");
     }
   }

        private async Task CheckAnacondaInstallations()
        {
  try
            {
var anacondaPaths = new[]
   {
     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "anaconda3"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "miniconda3"),
       @"C:\Anaconda3",
    @"C:\Miniconda3",
      @"C:\ProgramData\Anaconda3",
    @"C:\ProgramData\Miniconda3"
      };

  foreach (var anacondaPath in anacondaPaths)
    {
       if (Directory.Exists(anacondaPath))
        {
  // Check base environment
   var pythonExe = Path.Combine(anacondaPath, "python.exe");
        if (File.Exists(pythonExe))
            {
                 var version = await GetPythonVersionFromExecutable(pythonExe);
          if (!string.IsNullOrEmpty(version))
  {
        _availableInterpreters.Add(new PythonInterpreter
   {
        Name = $"Python {version} (Anaconda Base)",
     Path = pythonExe,
          Version = version,
              Command = pythonExe
      });
            }
        }

 // Check environments
 var envsPath = Path.Combine(anacondaPath, "envs");
      if (Directory.Exists(envsPath))
          {
       foreach (var envDir in Directory.GetDirectories(envsPath))
    {
  var envPython = Path.Combine(envDir, "python.exe");
  if (File.Exists(envPython))
             {
             var version = await GetPythonVersionFromExecutable(envPython);
      if (!string.IsNullOrEmpty(version))
  {
              _availableInterpreters.Add(new PythonInterpreter
  {
   Name = $"Python {version} ({Path.GetFileName(envDir)})",
     Path = envPython,
           Version = version,
      Command = envPython
               });
         }
  }
   }
   }
    }
             }
          }
   catch (Exception ex)
       {
         System.Diagnostics.Debug.WriteLine($"⚠️ Error checking Anaconda installations: {ex.Message}");
            }
        }

        private async Task<string?> GetPythonVersionFromExecutable(string pythonPath)
        {
        try
          {
      var result = await RunCommand(pythonPath, "--version", timeoutMs: 5000);
        if (result.Success)
     {
          return ExtractVersionFromOutput(result.Output);
      }
  }
      catch { }
   
            return null;
        }

        private string ExtractVersionFromOutput(string output)
        {
        var match = Regex.Match(output, @"Python (\d+\.\d+\.\d+)");
    return match.Success ? match.Groups[1].Value : "Unknown";
   }

    public void SetSelectedInterpreter(PythonInterpreter interpreter)
        {
         _selectedInterpreter = interpreter;
  System.Diagnostics.Debug.WriteLine($"🐍 Selected Python interpreter: {interpreter.Name}");
        }

        public async Task<PythonExecutionResult> ExecuteCodeAsync(string code, string? workingDirectory = null)
  {
  if (_selectedInterpreter == null)
      {
           return new PythonExecutionResult
        {
    Success = false,
         Error = "No Python interpreter selected. Please select an interpreter first."
  };
       }

   if (string.IsNullOrWhiteSpace(code))
    {
        return new PythonExecutionResult
        {
        Success = false,
         Error = "No code provided to execute."
   };
    }

     try
 {
 System.Diagnostics.Debug.WriteLine($"🐍 Executing Python code with {_selectedInterpreter.Name}...");
  System.Diagnostics.Debug.WriteLine($"Code to execute:\n{code}");

       // Create temporary file for the code
  var tempFile = Path.GetTempFileName();
    var pythonFile = Path.ChangeExtension(tempFile, ".py");
     var outputDir = Path.GetDirectoryName(pythonFile);
    
        try
       {
           // Enhance code to capture matplotlib plots
     var enhancedCode = EnhanceCodeForPlotCapture(code, outputDir);
         
   // Write code with UTF-8 encoding to handle special characters
    await File.WriteAllTextAsync(pythonFile, enhancedCode, Encoding.UTF8);
    System.Diagnostics.Debug.WriteLine($"📝 Enhanced code written to: {pythonFile}");

    // Determine the command to use
   string command;
     string arguments;
         
     if (_selectedInterpreter.Command == "py")
     {
      command = "py";
   arguments = $"\"{pythonFile}\"";
  }
 else if (File.Exists(_selectedInterpreter.Path))
  {
      command = _selectedInterpreter.Path;
        arguments = $"\"{pythonFile}\"";
     }
    else
      {
command = _selectedInterpreter.Command;
     arguments = $"\"{pythonFile}\"";
 }

   System.Diagnostics.Debug.WriteLine($"🚀 Executing: {command} {arguments}");

    var result = await RunCommand(
     command, 
     arguments, 
       workingDirectory: workingDirectory,
        timeoutMs: 30000
   );

  System.Diagnostics.Debug.WriteLine($"📊 Exit code: {result.ExitCode}");
   System.Diagnostics.Debug.WriteLine($"📤 Output: {result.Output}");
           if (!string.IsNullOrEmpty(result.Error))
     {
   System.Diagnostics.Debug.WriteLine($"⚠️ Stderr: {result.Error}");
       }

      // Check for generated plot files
         var plotFiles = Directory.GetFiles(outputDir, "jot_plot_*.png").ToList();
    System.Diagnostics.Debug.WriteLine($"🎨 Found {plotFiles.Count} plot files");

 var executionResult = new PythonExecutionResult
   {
 Success = result.Success,
  Output = result.Output,
  Error = result.Error,
    ExecutionTime = result.ExecutionTime,
      Interpreter = _selectedInterpreter,
   PlotFiles = plotFiles
  };

     CodeExecuted?.Invoke(this, executionResult);
      return executionResult;
      }
 finally
 {
  // Cleanup temporary files (but keep plot files for now)
   try
  {
      if (File.Exists(tempFile)) 
   {
    File.Delete(tempFile);
        System.Diagnostics.Debug.WriteLine($"🗑️ Deleted temp file: {tempFile}");
      }
 if (File.Exists(pythonFile)) 
{
     File.Delete(pythonFile);
  System.Diagnostics.Debug.WriteLine($"🗑️ Deleted Python file: {pythonFile}");
   }
      }
      catch (Exception cleanupEx)
     {
         System.Diagnostics.Debug.WriteLine($"⚠️ Cleanup error: {cleanupEx.Message}");
    }
      }
 }
    catch (Exception ex)
     {
        System.Diagnostics.Debug.WriteLine($"❌ Python execution error: {ex.Message}");
    var errorResult = new PythonExecutionResult
       {
   Success = false,
   Error = $"Execution error: {ex.Message}",
        Interpreter = _selectedInterpreter
         };

   CodeExecuted?.Invoke(this, errorResult);
    return errorResult;
  }
        }

 private string EnhanceCodeForPlotCapture(string originalCode, string outputDir)
    {
      var enhancedCode = new StringBuilder();
 
  // Add necessary imports and setup for plot capture
          enhancedCode.AppendLine("import sys");
         enhancedCode.AppendLine("import os");
       enhancedCode.AppendLine("import matplotlib");
   enhancedCode.AppendLine("matplotlib.use('Agg')  # Use non-interactive backend");
            enhancedCode.AppendLine("import matplotlib.pyplot as plt");
   enhancedCode.AppendLine("import uuid");
 enhancedCode.AppendLine();
            
      // Setup plot counter
    enhancedCode.AppendLine("_jot_plot_counter = 0");
       enhancedCode.AppendLine();
       
      // Add function to save plots automatically
       enhancedCode.AppendLine("def _jot_save_current_plot():");
    enhancedCode.AppendLine(" global _jot_plot_counter");
       enhancedCode.AppendLine("    if plt.get_fignums():");  // Check if there are any figures
    enhancedCode.AppendLine("        _jot_plot_counter += 1");
   enhancedCode.AppendLine($"      plot_path = os.path.join(r'{outputDir}', f'jot_plot_{{_jot_plot_counter:03d}}.png')");
 enhancedCode.AppendLine("      plt.savefig(plot_path, dpi=150, bbox_inches='tight')");
   enhancedCode.AppendLine("        print(f'🎨 Plot saved: {plot_path}')");
      enhancedCode.AppendLine("        plt.close('all')  # Close all figures to free memory");
            enhancedCode.AppendLine();

    // Wrap the original code
            enhancedCode.AppendLine("# Original user code starts here");
     enhancedCode.AppendLine("try:");
       
  // Indent original code
 var lines = originalCode.Split('\n');
            foreach (var line in lines)
{
     enhancedCode.AppendLine("    " + line);
      }
        
 enhancedCode.AppendLine();
     enhancedCode.AppendLine("    # Auto-save any remaining plots");
     enhancedCode.AppendLine("    _jot_save_current_plot()");
    enhancedCode.AppendLine();
  enhancedCode.AppendLine("except Exception as e:");
  enhancedCode.AppendLine("    print(f'Error in user code: {e}')");
 enhancedCode.AppendLine("    import traceback");
   enhancedCode.AppendLine("    traceback.print_exc()");
        enhancedCode.AppendLine("    # Still try to save any plots that were created");
 enhancedCode.AppendLine("    _jot_save_current_plot()");

      return enhancedCode.ToString();
        }

    public async Task<bool> InstallPackageAsync(string packageName)
{
  if (_selectedInterpreter == null)
return false;

   try
      {
          System.Diagnostics.Debug.WriteLine($"📦 Installing package: {packageName}");

   var result = await RunCommand(_selectedInterpreter.Command, $"-m pip install {packageName}", timeoutMs: 60000);
     
          if (result.Success)
      {
     System.Diagnostics.Debug.WriteLine($"✅ Package {packageName} installed successfully");
          return true;
      }
       else
         {
         System.Diagnostics.Debug.WriteLine($"❌ Failed to install {packageName}: {result.Error}");
         return false;
          }
     }
      catch (Exception ex)
   {
        System.Diagnostics.Debug.WriteLine($"❌ Error installing package {packageName}: {ex.Message}");
      return false;
   }
    }

        private async Task<CommandResult> RunCommand(string fileName, string arguments, string? workingDirectory = null, int timeoutMs = 10000)
        {
   try
    {
 var startInfo = new ProcessStartInfo
     {
 FileName = fileName,
     Arguments = arguments,
          UseShellExecute = false,
RedirectStandardOutput = true,
       RedirectStandardError = true,
    CreateNoWindow = true,
   WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory
};

  var stopwatch = Stopwatch.StartNew();
   using var process = new Process { StartInfo = startInfo };
    
       var outputBuilder = new StringBuilder();
       var errorBuilder = new StringBuilder();

         process.OutputDataReceived += (s, e) => {
          if (e.Data != null) outputBuilder.AppendLine(e.Data);
 };
 
            process.ErrorDataReceived += (s, e) => {
           if (e.Data != null) errorBuilder.AppendLine(e.Data);
    };

        process.Start();
  process.BeginOutputReadLine();
   process.BeginErrorReadLine();

     var completed = await Task.Run(() => process.WaitForExit(timeoutMs));
  stopwatch.Stop();

    if (!completed)
      {
     try { process.Kill(); } catch { }
           return new CommandResult
       {
Success = false,
         Error = $"Command timed out after {timeoutMs}ms",
           ExecutionTime = stopwatch.Elapsed
    };
      }

   return new CommandResult
      {
      Success = process.ExitCode == 0,
 Output = outputBuilder.ToString().Trim(),
            Error = errorBuilder.ToString().Trim(),
    ExitCode = process.ExitCode,
  ExecutionTime = stopwatch.Elapsed
        };
      }
     catch (Exception ex)
    {
 return new CommandResult
    {
           Success = false,
    Error = ex.Message,
     ExecutionTime = TimeSpan.Zero
   };
        }
    }
    }

    public class PythonInterpreter
    {
    public string Name { get; set; } = "";
        public string Path { get; set; } = "";
     public string Version { get; set; } = "";
        public string Command { get; set; } = "";
        
        public override string ToString() => Name;
    }

    public class PythonExecutionResult
    {
        public bool Success { get; set; }
   public string Output { get; set; } = "";
   public string Error { get; set; } = "";
   public TimeSpan ExecutionTime { get; set; }
        public PythonInterpreter? Interpreter { get; set; }
   public List<string> PlotFiles { get; set; } = new List<string>();
 }

    public class CommandResult
  {
        public bool Success { get; set; }
        public string Output { get; set; } = "";
        public string Error { get; set; } = "";
        public int ExitCode { get; set; }
        public TimeSpan ExecutionTime { get; set; }
 }
}