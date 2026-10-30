using System.IO;
using ClaudeSessions.Adapters;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Tests.Fakes;
using Xunit;

namespace ClaudeSessions.Tests.Adapters;

/// <summary>Saving and loading the preferences file.</summary>
public sealed class JsonPreferencesStoreTests
{
    /// <summary>Saved preferences load back.</summary>
    [Fact]
    public void Loads_back_the_saved_preferences()
    {
        using TempFolder folder = new();
        JsonPreferencesStore store = new(Path.Combine(folder.Path, "nested", "preferences.json"));
        store.Save(new ViewPreferences(14, 5));

        ViewPreferences loaded = store.Load();

        Assert.Equal(new ViewPreferences(14, 5), loaded);
    }

    /// <summary>A preference left unset loads back unset.</summary>
    [Fact]
    public void Loads_back_a_partial_save()
    {
        using TempFolder folder = new();
        JsonPreferencesStore store = new(folder.File("preferences.json"));
        store.Save(new ViewPreferences(null, 10));

        Assert.Equal(new ViewPreferences(null, 10), store.Load());
    }

    /// <summary>No file means no preference.</summary>
    [Fact]
    public void Loads_nothing_when_no_file_exists()
    {
        using TempFolder folder = new();
        JsonPreferencesStore store = new(folder.File("preferences.json"));

        Assert.Equal(ViewPreferences.None, store.Load());
    }

    /// <summary>Saving no preference at all deletes the file.</summary>
    [Fact]
    public void Saving_no_preference_deletes_the_file()
    {
        using TempFolder folder = new();
        string path = folder.File("preferences.json");
        JsonPreferencesStore store = new(path);
        store.Save(new ViewPreferences(14, null));

        store.Save(ViewPreferences.None);

        Assert.False(File.Exists(path));
    }

    /// <summary>Saving no preference without a file does not fail.</summary>
    [Fact]
    public void Saving_no_preference_without_a_file_does_nothing()
    {
        using TempFolder folder = new();
        JsonPreferencesStore store = new(folder.File("preferences.json"));

        store.Save(ViewPreferences.None);

        Assert.Equal(ViewPreferences.None, store.Load());
    }

    /// <summary>A file that is not JSON counts as no preference.</summary>
    [Fact]
    public void Reads_an_unreadable_file_as_no_preference()
    {
        using TempFolder folder = new();
        string path = folder.File("preferences.json");
        File.WriteAllText(path, "{ not json");
        JsonPreferencesStore store = new(path);

        Assert.Equal(ViewPreferences.None, store.Load());
    }
}
