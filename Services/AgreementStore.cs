using System.Text.Json;
using System.Text.Json.Serialization;
using Discretion.Models;

namespace Discretion.Services;

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(Agreement))]
[JsonSerializable(typeof(PartyInfo))]
internal partial class DiscretionJsonContext : JsonSerializerContext;

/// <summary>Persists agreements as JSON files in the app's private data directory.</summary>
public sealed class AgreementStore
{
    private readonly string _root;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public AgreementStore() : this(Path.Combine(FileSystem.AppDataDirectory, "agreements")) { }

    public AgreementStore(string root)
    {
        _root = root;
        Directory.CreateDirectory(_root);
    }

    public async Task<IReadOnlyList<Agreement>> GetAllAsync()
    {
        await _gate.WaitAsync();
        try
        {
            var list = new List<Agreement>();
            foreach (var file in Directory.EnumerateFiles(_root, "*.json"))
            {
                try
                {
                    await using var stream = File.OpenRead(file);
                    if (await JsonSerializer.DeserializeAsync(stream, DiscretionJsonContext.Default.Agreement) is { } a)
                        list.Add(a);
                }
                catch (JsonException)
                {
                    // Skip corrupt records rather than failing the whole list.
                }
            }
            return list.OrderByDescending(a => a.CreatedAt).ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<Agreement?> GetAsync(Guid id)
    {
        var path = PathFor(id);
        if (!File.Exists(path)) return null;
        await _gate.WaitAsync();
        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync(stream, DiscretionJsonContext.Default.Agreement);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(Agreement agreement)
    {
        await _gate.WaitAsync();
        try
        {
            var path = PathFor(agreement.Id);
            var tmp = path + ".tmp";
            await using (var stream = File.Create(tmp))
                await JsonSerializer.SerializeAsync(stream, agreement, DiscretionJsonContext.Default.Agreement);
            File.Move(tmp, path, overwrite: true);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        await _gate.WaitAsync();
        try
        {
            File.Delete(PathFor(id));
        }
        finally
        {
            _gate.Release();
        }
    }

    private string PathFor(Guid id) => Path.Combine(_root, $"{id:N}.json");
}
