namespace Domain
{
    public class Column
    {
        public string Name { get; set; } = string.Empty;
        public required Type DataType { get; set; }
    }
}
