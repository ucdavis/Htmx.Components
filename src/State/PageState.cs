using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace Htmx.Components.State;

/// <summary>
/// Provides a contract for managing page state with encrypted storage capabilities.
/// The page state is organized into partitions and key-value pairs, allowing for structured data management.
/// </summary>
public interface IPageState
{
    /// <summary>
    /// Loads the page state from an encrypted string representation.
    /// If the encrypted string is null or empty, initializes with default state.
    /// </summary>
    /// <param name="encrypted">The encrypted string containing the serialized state data, or null for initial state.</param>
    void Load(string? encrypted);
    
    /// <summary>
    /// Gets the current state as an encrypted string representation.
    /// This encrypted string can be stored and later loaded using the Load method.
    /// </summary>
    string Encrypted { get; }
    
    /// <summary>
    /// Retrieves a value from the specified partition and key.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the value to.</typeparam>
    /// <param name="partition">The partition name to retrieve the value from.</param>
    /// <param name="key">The key within the partition.</param>
    /// <returns>The deserialized value if found, otherwise the default value for type T.</returns>
    T? Get<T>(string partition, string key);
    
    /// <summary>
    /// Retrieves a value from the specified partition and key, or creates it using the factory function if not found.
    /// </summary>
    /// <typeparam name="T">The type to deserialize/create.</typeparam>
    /// <param name="partition">The partition name to retrieve or store the value in.</param>
    /// <param name="key">The key within the partition.</param>
    /// <param name="factory">A function to create the value if it doesn't exist.</param>
    /// <returns>The existing value if found, otherwise the newly created value from the factory.</returns>
    T GetOrCreate<T>(string partition, string key, Func<T> factory);
    
    /// <summary>
    /// Sets a value in the specified partition and key.
    /// This operation marks the state as dirty and increments the version.
    /// </summary>
    /// <typeparam name="T">The type of the value to store.</typeparam>
    /// <param name="partition">The partition name to store the value in.</param>
    /// <param name="key">The key within the partition.</param>
    /// <param name="value">The value to store.</param>
    void Set<T>(string partition, string key, T value);
    
    /// <summary>
    /// Removes a specific key from the specified partition.
    /// This operation marks the state as dirty and increments the version if the key existed.
    /// </summary>
    /// <param name="partition">The partition name to remove the key from.</param>
    /// <param name="key">The key to remove from the partition.</param>
    void ClearKey(string partition, string key);
    
    /// <summary>
    /// Removes an entire partition and all its keys.
    /// This operation marks the state as dirty and increments the version if the partition existed.
    /// </summary>
    /// <param name="partition">The partition name to remove.</param>
    void ClearPartition(string partition);
    
    /// <summary>
    /// Gets a value indicating whether the state has been modified since it was last loaded.
    /// This is determined by comparing the current version with the version when the state was loaded.
    /// </summary>
    bool IsDirty { get; }
    
    /// <summary>
    /// Gets the current version number of the page state.
    /// This version is incremented whenever the state is modified.
    /// </summary>
    int Version { get; }
}


/// <summary>
/// Implements page state management with encrypted storage capabilities.
/// The state is organized into partitions containing key-value pairs, with automatic versioning for change tracking.
/// All data is serialized as JSON and encrypted using ASP.NET Core Data Protection.
/// </summary>
public class PageState : IPageState
{
    private readonly IDataProtector _protector;
    private const string MetaPartition = "__meta";
    private const string VersionKey = "__version";
    private int _version = 0;

    /// <summary>
    /// Gets the current state data organized as a dictionary of partitions, where each partition contains key-value pairs.
    /// The state includes a special metadata partition for internal tracking.
    /// </summary>
    public Dictionary<string, Dictionary<string, string>> State { get; private set; }

    /// <summary>
    /// Initializes a new instance of the PageState class with the specified data protection provider.
    /// Creates a data protector specifically for page state encryption and initializes with default state.
    /// </summary>
    /// <param name="dataProtectionProvider">The data protection provider used to create encryption keys for state data.</param>
    public PageState(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("PageState");
        State = InitialState;
    }

    /// <summary>
    /// Gets the initial state structure with metadata partition containing version information.
    /// This is used when no existing state is available or when resetting the state.
    /// </summary>
    private static Dictionary<string, Dictionary<string, string>> InitialState => new()
    {
        [MetaPartition] = new Dictionary<string, string>
        {
            [VersionKey] = "0"
        }
    };

    /// <summary>
    /// Loads the page state from an encrypted string representation.
    /// If the encrypted string is null or empty, initializes with default state.
    /// Updates the internal version tracking after loading.
    /// </summary>
    /// <param name="encrypted">The encrypted string containing the serialized state data, or null for initial state.</param>
    public void Load(string? encrypted)
    {
        if (string.IsNullOrWhiteSpace(encrypted))
        {
            State = InitialState;
        }
        else
        {
            var json = _protector.Unprotect(encrypted);
            State = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json)
                    ?? new Dictionary<string, Dictionary<string, string>>();
        }
        _version = Get<int>(MetaPartition, VersionKey);
    }

    /// <summary>
    /// Gets the current state as an encrypted string representation.
    /// The state is serialized to JSON and then encrypted using the data protector.
    /// </summary>
    public string Encrypted => _protector.Protect(JsonSerializer.Serialize(State));

    /// <summary>
    /// Gets or creates a partition dictionary for the specified partition name.
    /// If the partition doesn't exist, creates a new empty dictionary and adds it to the state.
    /// </summary>
    /// <param name="partition">The name of the partition to retrieve or create.</param>
    /// <returns>The dictionary containing the key-value pairs for the specified partition.</returns>
    private Dictionary<string, string> GetPartition(string partition)
    {
        if (!State.TryGetValue(partition, out var values))
        {
            values = new Dictionary<string, string>();
            State[partition] = values;
        }
        return values;
    }

    /// <summary>
    /// Increments the version number in the metadata partition to track state changes.
    /// This is called whenever the state is modified and is used for dirty state tracking.
    /// </summary>
    private void BumpVersion()
    {
        var meta = GetPartition(MetaPartition);
        if (!meta.TryGetValue(VersionKey, out var versionStr) || !int.TryParse(versionStr, out var version))
        {
            version = 0;
        }
        meta[VersionKey] = (version + 1).ToString();
    }

    /// <summary>
    /// Retrieves a value from the specified partition and key.
    /// The value is deserialized from JSON to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the value to.</typeparam>
    /// <param name="partition">The partition name to retrieve the value from.</param>
    /// <param name="key">The key within the partition.</param>
    /// <returns>The deserialized value if found, otherwise the default value for type T.</returns>
    public T? Get<T>(string partition, string key)
    {
        var p = GetPartition(partition);
        if (!p.TryGetValue(key, out var value)) return default;

        return JsonSerializer.Deserialize<T>(value)!;
    }

    /// <summary>
    /// Retrieves a value from the specified partition and key, or creates it using the factory function if not found.
    /// If the value is created, it is stored in the state and the version is incremented.
    /// </summary>
    /// <typeparam name="T">The type to deserialize/create.</typeparam>
    /// <param name="partition">The partition name to retrieve or store the value in.</param>
    /// <param name="key">The key within the partition.</param>
    /// <param name="factory">A function to create the value if it doesn't exist.</param>
    /// <returns>The existing value if found, otherwise the newly created value from the factory.</returns>
    public T GetOrCreate<T>(string partition, string key, Func<T> factory)
    {
        var p = GetPartition(partition);
        if (!p.TryGetValue(key, out var value))
        {
            var newValue = factory();
            p[key] = JsonSerializer.Serialize(newValue);
            BumpVersion();
            return newValue;
        }

        return JsonSerializer.Deserialize<T>(value)!;
    }

    /// <summary>
    /// Sets a value in the specified partition and key.
    /// The value is serialized to JSON before storage, and the version is incremented.
    /// </summary>
    /// <typeparam name="T">The type of the value to store.</typeparam>
    /// <param name="partition">The partition name to store the value in.</param>
    /// <param name="key">The key within the partition.</param>
    /// <param name="value">The value to store.</param>
    public void Set<T>(string partition, string key, T value)
    {
        var p = GetPartition(partition);
        p[key] = JsonSerializer.Serialize(value);
        BumpVersion();
    }

    /// <summary>
    /// Removes a specific key from the specified partition.
    /// If the key existed and was removed, the version is incremented.
    /// </summary>
    /// <param name="partition">The partition name to remove the key from.</param>
    /// <param name="key">The key to remove from the partition.</param>
    public void ClearKey(string partition, string key)
    {
        var p = GetPartition(partition);
        if (p.Remove(key))
        {
            BumpVersion();
        }
    }

    /// <summary>
    /// Removes an entire partition and all its keys from the state.
    /// If the partition existed and was removed, the version is incremented.
    /// </summary>
    /// <param name="partition">The partition name to remove.</param>
    public void ClearPartition(string partition)
    {
        if (State.Remove(partition))
        {
            BumpVersion();
        }
    }

    /// <summary>
    /// Gets a value indicating whether the state has been modified since it was last loaded.
    /// This is determined by comparing the current version with the version when the state was loaded.
    /// </summary>
    public bool IsDirty => _version != Get<int>(MetaPartition, VersionKey);
    
    /// <summary>
    /// Gets the current version number of the page state.
    /// This version is incremented whenever the state is modified.
    /// </summary>
    public int Version => Get<int>(MetaPartition, VersionKey);
}