using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Object = MooSharp.Actors.Objects.Object;

namespace MooSharp.Scripting.Api;

[MoonSharpUserData]
public class LuaSelfApi(Object obj)
{
    [UsedImplicitly]
    public string Name => obj.Name;

    [UsedImplicitly]
    public string Description => obj.Description;

    [UsedImplicitly]
    public string Id => obj.Id.Value.ToString();

    [UsedImplicitly]
    public bool IsOpen => obj.IsOpen;

    [UsedImplicitly]
    public bool IsLocked => obj.IsLocked;

    [UsedImplicitly]
    public bool IsContainer => obj.IsContainer;

    [UsedImplicitly]
    [MoonSharpUserDataMetamethod("__index")]
    public DynValue Index(DynValue key)
    {
        if (key.Type != DataType.String)
        {
            return DynValue.Nil;
        }

        var propertyName = key.String;

        // Check standard properties first
        return propertyName.ToLowerInvariant() switch
        {
            "name" => DynValue.NewString(Name),
            "description" => DynValue.NewString(Description),
            "id" => DynValue.NewString(Id),
            "isopen" => DynValue.NewBoolean(IsOpen),
            "islocked" => DynValue.NewBoolean(IsLocked),
            "iscontainer" => DynValue.NewBoolean(IsContainer),
            _ => GetDynamicProperty(propertyName)
        };
    }

    [UsedImplicitly]
    [MoonSharpUserDataMetamethod("__newindex")]
    public void NewIndex(DynValue key, DynValue value)
    {
        if (key.Type != DataType.String)
        {
            return;
        }

        SetDynamicProperty(key.String, value);
    }

    private DynValue GetDynamicProperty(string key)
    {
        if (!obj.Properties.TryGetValue(key, out var value) || value is null)
        {
            return DynValue.Nil;
        }

        return value switch
        {
            string s => DynValue.NewString(s),
            long l => DynValue.NewNumber(l),
            int i => DynValue.NewNumber(i),
            double d => DynValue.NewNumber(d),
            bool b => DynValue.NewBoolean(b),
            _ => DynValue.NewString(value.ToString() ?? "")
        };
    }

    private void SetDynamicProperty(string key, DynValue value)
    {
        // Don't allow overwriting standard properties
        var standardProps = new[] { "name", "description", "id", "isopen", "islocked", "iscontainer" };
        if (standardProps.Contains(key.ToLowerInvariant()))
        {
            return;
        }

        obj.Properties[key] = value.Type switch
        {
            DataType.String => value.String,
            DataType.Number => value.Number,
            DataType.Boolean => value.Boolean,
            DataType.Nil => null,
            _ => value.ToString()
        };
    }
}
