using System.Text.Json;

namespace MooSharp.Actors.Objects;

/// <summary>
/// Collection of verb scripts for an object.
/// Serialized to/from JSON for persistence.
/// </summary>
public class VerbCollection
{
    private readonly Dictionary<string, VerbScript> _verbs = new(StringComparer.OrdinalIgnoreCase);

    public VerbScript? this[string verbName]
    {
        get => _verbs.TryGetValue(verbName, out var script) ? script : null;
        set
        {
            if (value is null)
            {
                _verbs.Remove(verbName);
            }
            else
            {
                _verbs[verbName] = value;
            }
        }
    }

    public bool TryGetVerb(string verbName, out VerbScript? script)
    {
        return _verbs.TryGetValue(verbName, out script);
    }

    public bool HasVerb(string verbName) => _verbs.ContainsKey(verbName);

    public IEnumerable<string> VerbNames => _verbs.Keys;

    public IEnumerable<VerbScript> Verbs => _verbs.Values;

    public int Count => _verbs.Count;

    public void Remove(string verbName) => _verbs.Remove(verbName);

    public void Clear() => _verbs.Clear();

    public string ToJson()
    {
        return JsonSerializer.Serialize(_verbs.Values.ToList());
    }

    public static VerbCollection FromJson(string? json)
    {
        var collection = new VerbCollection();

        if (string.IsNullOrWhiteSpace(json))
        {
            return collection;
        }

        try
        {
            var verbs = JsonSerializer.Deserialize<List<VerbScript>>(json);
            if (verbs is not null)
            {
                foreach (var verb in verbs)
                {
                    collection._verbs[verb.VerbName] = verb;
                }
            }
        }
        catch
        {
            // Return empty collection if JSON is invalid
        }

        return collection;
    }
}
