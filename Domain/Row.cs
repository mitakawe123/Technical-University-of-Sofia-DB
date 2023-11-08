namespace Domain
{
    [Serializable]
    public class Row
    {
        public Dictionary<string, object> Data { get; set; } = new();
    }
}
