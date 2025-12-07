using System.Text.Json;
using System.Text.Json.Serialization;

namespace MooSharp.Actors.Objects;

/// <summary>
/// Stores dynamic properties for an object that can be set/get by players.
/// Serialized to/from JSON for persistence.
/// </summary>
public class DynamicPropertyBag
{
    private readonly Dictionary<string, JsonElement> _properties = new(StringComparer.OrdinalIgnoreCase);

    public object? this[string key]
    {
        get => TryGetValue(key, out var value) ? value : null;
        set
        {
            if (value is null)
            {
                _properties.Remove(key);
            }
            else
            {
                // Convert to JsonElement for consistent storage
                var json = JsonSerializer.Serialize(value);
                _properties[key] = JsonDocument.Parse(json).RootElement.Clone();
            }
        }
    }

    public bool TryGetValue(string key, out object? value)
    {
        if (_properties.TryGetValue(key, out var element))
        {
            value = ConvertFromJsonElement(element);
            return true;
        }

        value = null;
        return false;
    }

    public bool TryGetValue<T>(string key, out T? value)
    {
        if (_properties.TryGetValue(key, out var element))
        {
            try
            {
                value = element.Deserialize<T>();
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }

        value = default;
        return false;
    }

    public bool ContainsKey(string key) => _properties.ContainsKey(key);

    public IEnumerable<string> Keys => _properties.Keys;

    public int Count => _properties.Count;

    public void Remove(string key) => _properties.Remove(key);

    public void Clear() => _properties.Clear();

    public string ToJson()
    {
        return JsonSerializer.Serialize(_properties);
    }

    public static DynamicPropertyBag FromJson(string? json)
    {
        var bag = new DynamicPropertyBag();

        if (string.IsNullOrWhiteSpace(json))
        {
            return bag;
        }

        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            if (dict is not null)
            {
                foreach (var (key, value) in dict)
                {
                    bag._properties[key] = value.Clone();
                }
            }
        }
        catch
        {
            // Return empty bag if JSON is invalid
        }

        return bag;
    }

    private static object? ConvertFromJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var l) => l,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertFromJsonElement).ToArray(),
            _ => element.GetRawText()
        };
    }
}
