using System.Collections.Generic;

/// <summary>
/// Minimal test double for the KSP ConfigNode API used by persistence state classes.
/// </summary>
public sealed class ConfigNode
{
    private readonly Dictionary<string, string> _values = new Dictionary<string, string>();
    private readonly Dictionary<string, List<ConfigNode>> _nodes =
        new Dictionary<string, List<ConfigNode>>();

    public void AddValue(string name, object value)
    {
        _values[name] = value == null ? null : value.ToString();
    }

    public string GetValue(string name)
    {
        string value;
        return _values.TryGetValue(name, out value) ? value : null;
    }

    public ConfigNode AddNode(string name)
    {
        List<ConfigNode> nodes;
        if (!_nodes.TryGetValue(name, out nodes))
        {
            nodes = new List<ConfigNode>();
            _nodes[name] = nodes;
        }

        var node = new ConfigNode();
        nodes.Add(node);
        return node;
    }

    public ConfigNode GetNode(string name)
    {
        List<ConfigNode> nodes;
        return _nodes.TryGetValue(name, out nodes) && nodes.Count > 0 ? nodes[0] : null;
    }

    public ConfigNode[] GetNodes(string name)
    {
        List<ConfigNode> nodes;
        return _nodes.TryGetValue(name, out nodes) ? nodes.ToArray() : new ConfigNode[0];
    }
}
