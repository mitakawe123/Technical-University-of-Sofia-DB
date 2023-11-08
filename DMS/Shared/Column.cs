using DMS.Constants;

namespace DMS.Shared
{
    public readonly struct Column
    {
        public string Name { get; }
        public EDataTypes Type { get; }
      
        public Column(string name, EDataTypes type)
        {
            Name = name;
            Type = type;
        }
    }
}
