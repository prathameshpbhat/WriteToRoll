using System.IO;
using System.Text.Json;
using App.Core.Models;

namespace App.Persistence.Services;

public interface IDocumentService
{
    Task<Script> LoadScriptAsync(string path);
    Task SaveScriptAsync(Script script, string path);
    Task<Script> CreateNewScriptAsync();
    Task<Script> ImportFountainAsync(string path);
    Task ExportFountainAsync(Script script, string path);
    Task<string> ExportPdfAsync(Script script, string path);
    Task AutoSaveAsync(Script script);
    IEnumerable<string> GetRecentFiles();
    void AddRecentFile(string path);
    Task<Script?> GetLatestVersionAsync(string scriptId);
    Task SaveVersionAsync(Script script);
}

public class DocumentService : IDocumentService
{
    private readonly IFountainParser _fountainParser;
    private readonly string _autoSaveDirectory;
    private readonly string _versionsDirectory;
    private readonly string _recentFilesPath;
    private readonly List<string> _recentFiles;
    private const int MaxRecentFiles = 10;

    public DocumentService(IFountainParser fountainParser)
    {
        _fountainParser = fountainParser;
        
        // Set up directories
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appData, "ScriptWriter");
        _autoSaveDirectory = Path.Combine(appFolder, "AutoSave");
        _versionsDirectory = Path.Combine(appFolder, "Versions");
        _recentFilesPath = Path.Combine(appFolder, "recent.json");

        // Create directories if they don't exist
        Directory.CreateDirectory(_autoSaveDirectory);
        Directory.CreateDirectory(_versionsDirectory);

        // Load recent files
        _recentFiles = LoadRecentFiles();
    }

    public async Task<Script> LoadScriptAsync(string path)
    {
        using var stream = File.OpenRead(path);
        var script = await JsonSerializer.DeserializeAsync<Script>(stream);
        if (script == null)
            throw new InvalidOperationException("Failed to load script.");

        AddRecentFile(path);
        return script;
    }

    public async Task SaveScriptAsync(Script script, string path)
    {
        using var stream = File.Create(path);
        var options = new JsonSerializerOptions { WriteIndented = true };
        await JsonSerializer.SerializeAsync(stream, script, options);
        
        AddRecentFile(path);
        await SaveVersionAsync(script);
    }

    public Task<Script> CreateNewScriptAsync()
    {
        var script = new Script
        {
            Title = "Untitled",
            Author = Environment.UserName,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        return Task.FromResult(script);
    }

    public async Task<Script> ImportFountainAsync(string path)
    {
        var text = await File.ReadAllTextAsync(path);
        var script = _fountainParser.ParseFountain(text);
        AddRecentFile(path);
        return script;
    }

    public async Task ExportFountainAsync(Script script, string path)
    {
        var text = _fountainParser.ConvertToFountain(script);
        await File.WriteAllTextAsync(path, text);
    }

    public Task<string> ExportPdfAsync(Script script, string path)
    {
        // TODO: Implement PDF export with proper formatting
        throw new NotImplementedException();
    }

    public async Task AutoSaveAsync(Script script)
    {
        var fileName = $"{script.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.autosave";
        var path = Path.Combine(_autoSaveDirectory, fileName);
        
        using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, script);

        // Clean up old autosave files
        var files = Directory.GetFiles(_autoSaveDirectory, $"{script.Id}_*.autosave")
            .OrderByDescending(f => f)
            .Skip(5); // Keep last 5 autosaves

        foreach (var file in files)
        {
            try { File.Delete(file); }
            catch { /* Ignore deletion errors */ }
        }
    }

    public IEnumerable<string> GetRecentFiles()
    {
        return _recentFiles;
    }

    public void AddRecentFile(string path)
    {
        _recentFiles.Remove(path); // Remove if exists
        _recentFiles.Insert(0, path); // Add to start
        
        while (_recentFiles.Count > MaxRecentFiles)
            _recentFiles.RemoveAt(_recentFiles.Count - 1);

        SaveRecentFiles();
    }

    public async Task<Script?> GetLatestVersionAsync(string scriptId)
    {
        var versionPath = Path.Combine(_versionsDirectory, scriptId);
        if (!Directory.Exists(versionPath))
            return null;

        var latest = Directory.GetFiles(versionPath, "*.version")
            .OrderByDescending(f => f)
            .FirstOrDefault();

        if (latest == null)
            return null;

        using var stream = File.OpenRead(latest);
        return await JsonSerializer.DeserializeAsync<Script>(stream);
    }

    public async Task SaveVersionAsync(Script script)
    {
        var versionPath = Path.Combine(_versionsDirectory, script.Id.ToString());
        Directory.CreateDirectory(versionPath);

        var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}.version";
        var path = Path.Combine(versionPath, fileName);
        
        using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, script);

        // Keep last 10 versions
        var files = Directory.GetFiles(versionPath, "*.version")
            .OrderByDescending(f => f)
            .Skip(10);

        foreach (var file in files)
        {
            try { File.Delete(file); }
            catch { /* Ignore deletion errors */ }
        }
    }

    private List<string> LoadRecentFiles()
    {
        if (File.Exists(_recentFilesPath))
        {
            try
            {
                var json = File.ReadAllText(_recentFilesPath);
                var files = JsonSerializer.Deserialize<List<string>>(json);
                if (files != null)
                    return files.Where(File.Exists).ToList();
            }
            catch { /* Use empty list if loading fails */ }
        }
        return new List<string>();
    }

    private void SaveRecentFiles()
    {
        try
        {
            var json = JsonSerializer.Serialize(_recentFiles);
            File.WriteAllText(_recentFilesPath, json);
        }
        catch { /* Ignore save errors */ }
    }
}