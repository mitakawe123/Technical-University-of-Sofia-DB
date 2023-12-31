namespace DMS.Shared;

public readonly struct Column
{
    public string Name { get; }
    public string Type { get; }
    public string DefaultValue { get; }

    public Column(string name, string type, string defaultValue)
    {
        Name = name;
        Type = type;
        DefaultValue = defaultValue;
    }
}