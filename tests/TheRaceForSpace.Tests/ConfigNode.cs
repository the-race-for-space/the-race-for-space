using System.Collections.Generic;

/// <summary>
/// Minimal test double for the KSP ConfigNode API used by persistence state classes.
/// </summary>
public sealed class ConfigNode
{
    private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

    public void AddValue(string name, object value)
    {
        _values[name] = value == null ? null : value.ToString();
    }

    public string GetValue(string name)
    {
        string value;
        return _values.TryGetValue(name, out value) ? value : null;
    }
}
