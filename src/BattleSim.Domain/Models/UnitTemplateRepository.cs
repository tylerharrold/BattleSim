using System.Text.Json;

namespace BattleSim.Domain.Models;

public sealed class UnitTemplateRepository
{
    private readonly string templateDirectory;
    private readonly IReadOnlyDictionary<string, TroopClassDefinition> classDefinitions;
    private readonly JsonSerializerOptions serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public UnitTemplateRepository(
        string templateDirectory,
        IReadOnlyDictionary<string, TroopClassDefinition> classDefinitions)
    {
        if (string.IsNullOrWhiteSpace(templateDirectory))
        {
            throw new ArgumentException("Template directory is required.", nameof(templateDirectory));
        }

        this.templateDirectory = templateDirectory;
        this.classDefinitions = classDefinitions;
    }

    public IReadOnlyList<UnitTemplate> LoadAll()
    {
        if (!Directory.Exists(templateDirectory))
        {
            return Array.Empty<UnitTemplate>();
        }

        return Directory
            .EnumerateFiles(templateDirectory, "*.json")
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(Load)
            .ToArray();
    }

    public UnitTemplate Load(string path)
    {
        using var stream = File.OpenRead(path);
        var template = JsonSerializer.Deserialize<UnitTemplate>(stream, serializerOptions)
            ?? throw new UnitTemplateValidationException($"Unit template file '{path}' is empty or invalid.");

        UnitTemplateValidator.ValidateAndThrow(template, classDefinitions);
        return template;
    }

    public string SaveNew(UnitTemplate template)
    {
        UnitTemplateValidator.ValidateAndThrow(template, classDefinitions);
        Directory.CreateDirectory(templateDirectory);

        var fileName = $"{template.Id}.json";
        var path = Path.Combine(templateDirectory, fileName);

        if (File.Exists(path))
        {
            throw new IOException($"Unit template '{template.Id}' already exists.");
        }

        using var stream = File.Create(path);
        JsonSerializer.Serialize(stream, template, serializerOptions);
        return path;
    }
}
