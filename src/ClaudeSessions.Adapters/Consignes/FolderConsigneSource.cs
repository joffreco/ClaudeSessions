using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using ClaudeSessions.Adapters.Consignes.Dto;
using ClaudeSessions.Core.Consignes.Domain;
using ClaudeSessions.Core.Consignes.Interfaces;

namespace ClaudeSessions.Adapters.Consignes;

/// <summary>
/// Reads the consignes from the folder the consigne script writes: one folder per tab id, and
/// <c>_sans-onglet/&lt;folder&gt;</c> for sessions outside a tab. A file is read again only when its
/// write time or length changed.
/// </summary>
public sealed class FolderConsigneSource : IConsigneSource
{
    /// <summary>The folder that holds the sessions outside a ClaudeSessions tab.</summary>
    public const string OutsideTabFolder = "_sans-onglet";

    /// <summary>The folder, inside a session folder, that holds its archived consignes.</summary>
    public const string ArchiveFolder = "archive";

    /// <summary>The suffix of the file, next to a consigne, where the marked steps the user ticks are added, one JSON line each.</summary>
    public const string EventsSuffix = ".events.jsonl";

    /// <summary>The suffix of the file where <c>consigne wait</c> keeps how many event lines it already gave.</summary>
    public const string SeenSuffix = ".events.seen";

    private const string Extension = ".md";
    private static readonly JsonSerializerOptions LineOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    private readonly string _root;
    private readonly Dictionary<string, CachedConsigne> _cache = new(StringComparer.Ordinal);

    /// <summary>Creates the source.</summary>
    /// <param name="root">The consignes folder.</param>
    public FolderConsigneSource(string root)
    {
        _root = root;
    }

    /// <inheritdoc/>
    public IReadOnlyList<Consigne> ReadAll()
    {
        HashSet<string> present = new(StringComparer.Ordinal);
        List<Consigne> consignes = [];
        foreach ((string folder, string directory) in SessionFolders())
        {
            foreach (string file in FilesOf(directory))
            {
                present.Add(file);
                if (Read(file, new ConsigneKey(folder, Path.GetFileNameWithoutExtension(file))) is Consigne consigne)
                {
                    consignes.Add(consigne);
                }
            }
        }
        foreach (string gone in _cache.Keys.Where(file => !present.Contains(file)).ToList())
        {
            _cache.Remove(gone);
        }
        return consignes;
    }

    /// <inheritdoc/>
    public void Archive(ConsigneKey key)
    {
        string directory = PathOf(key.Folder);
        string archive = Path.Combine(directory, ArchiveFolder);
        Directory.CreateDirectory(archive);
        File.Move(Path.Combine(directory, key.Id + Extension), Path.Combine(archive, key.Id + Extension), overwrite: true);
        foreach (string suffix in new[] { EventsSuffix, SeenSuffix })
        {
            string file = Path.Combine(directory, key.Id + suffix);
            if (File.Exists(file))
            {
                File.Move(file, Path.Combine(archive, key.Id + suffix), overwrite: true);
            }
        }
    }

    /// <inheritdoc/>
    public void SendTick(ConsigneKey key, StepTick tick)
    {
        string line = JsonSerializer.Serialize(new StepTickLine(tick.Index, tick.Text, tick.At), LineOptions) + "\n";
        using FileStream stream = new(Path.Combine(PathOf(key.Folder), key.Id + EventsSuffix), FileMode.Append, FileAccess.Write, FileShare.Read);
        stream.Write(Utf8.GetBytes(line));
    }

    /// <inheritdoc/>
    public string PathOf(string folder)
    {
        return Path.Combine([_root, .. folder.Split('/')]);
    }

    private IEnumerable<(string Folder, string Directory)> SessionFolders()
    {
        foreach (string directory in DirectoriesOf(_root))
        {
            string name = Path.GetFileName(directory);
            if (name == OutsideTabFolder)
            {
                foreach (string inner in DirectoriesOf(directory))
                {
                    yield return (OutsideTabFolder + "/" + Path.GetFileName(inner), inner);
                }
            }
            else
            {
                yield return (name, directory);
            }
        }
    }

    private CachedConsigne? CachedIfCurrent(string file, FileInfo info)
    {
        return _cache.TryGetValue(file, out CachedConsigne? cached) && cached.Written == info.LastWriteTimeUtc && cached.Length == info.Length ? cached : null;
    }

    private Consigne? Read(string file, ConsigneKey key)
    {
        FileInfo info = new(file);
        if (CachedIfCurrent(file, info) is CachedConsigne current)
        {
            return current.Consigne;
        }
        try
        {
            using FileStream stream = new(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using StreamReader reader = new(stream);
            Consigne? consigne = ConsigneFileParser.Parse(reader.ReadToEnd(), key);
            _cache[file] = new CachedConsigne(info.LastWriteTimeUtc, info.Length, consigne);
            return consigne;
        }
        catch (IOException)
        {
            return _cache.TryGetValue(file, out CachedConsigne? previous) ? previous.Consigne : null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string[] DirectoriesOf(string directory)
    {
        try
        {
            return Directory.Exists(directory) ? Directory.GetDirectories(directory) : [];
        }
        catch (IOException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static IEnumerable<string> FilesOf(string directory)
    {
        try
        {
            return Directory.GetFiles(directory, "*" + Extension).Where(file => !Path.GetFileName(file).StartsWith('.'));
        }
        catch (IOException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
    }
}
