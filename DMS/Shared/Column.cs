namespace DMS.Shared
{
    public readonly struct Column
    {
        public string Name { get; }
        public string Type { get; }
      
        public Column(string name, string type)
        {
            Name = name;
            Type = type;
        }
    }
}
