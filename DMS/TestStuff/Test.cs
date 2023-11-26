
namespace DMS.TestStuff
{
    class BinaryTreeWriter
    {
        private FileStream fileStream;
        private BinaryWriter binaryWriter;

        public BinaryTreeWriter(string filePath)
        {
            fileStream = new FileStream(filePath, FileMode.Create);
            binaryWriter = new BinaryWriter(fileStream);
        }

        public void WriteNode(char[] key, List<long> value)
        {
            binaryWriter.Write(key.Length);
            foreach (char c in key)
                binaryWriter.Write(c);

            binaryWriter.Write(value.Count);
            foreach (long l in value)
                binaryWriter.Write(l);
        }

        public void Close()
        {
            binaryWriter.Close();
            fileStream.Close();
        }
    }

    class BinaryTreeReader
    {
        private FileStream fileStream;
        private BinaryReader binaryReader;

        public BinaryTreeReader(string filePath)
        {
            fileStream = new FileStream(filePath, FileMode.Open);
            binaryReader = new BinaryReader(fileStream);
        }

        public IEnumerable<(char[] key, List<long> value)> ReadAllNodes()
        {
            while (fileStream.Position < fileStream.Length)
            {
                int keyLength = binaryReader.ReadInt32();
                char[] key = binaryReader.ReadChars(keyLength);

                int valueCount = binaryReader.ReadInt32();
                List<long> value = new();
                for (int i = 0; i < valueCount; i++)
                    value.Add(binaryReader.ReadInt64());

                yield return (key, value);
            }
        }

        public void Close()
        {
            binaryReader.Close();
            fileStream.Close();
        }
    }

    class BinaryTreeFileHandler
    {
        private string filePath;

        public BinaryTreeFileHandler(string filePath)
        {
            this.filePath = filePath;
        }

        public void InsertOrUpdateNode(char[] key, List<long> newValue)
        {
            var nodes = ReadAllNodes();

            var existingNode = nodes.FirstOrDefault(n => n.key.SequenceEqual(key));
            if (existingNode.key != null)
                existingNode.value.AddRange(newValue);
            else
                nodes.Add((key, newValue));

            RewriteFile(nodes);
        }

        public void DeleteNode(char[] key)
        {
            var nodes = ReadAllNodes();

            nodes.RemoveAll(n => n.key.SequenceEqual(key));

            RewriteFile(nodes);
        }

        private List<(char[] key, List<long> value)> ReadAllNodes()
        {
            List<(char[] key, List<long> value)> nodes = new();

            using FileStream fileStream = new(filePath, FileMode.Open);
            using BinaryReader binaryReader = new(fileStream);
            //here I will seek to the block of memory that the tree will start
            while (fileStream.Position < fileStream.Length)
            {
                int keyLength = binaryReader.ReadInt32();
                char[] key = binaryReader.ReadChars(keyLength);

                int valueCount = binaryReader.ReadInt32();
                List<long> value = new();
                for (int i = 0; i < valueCount; i++)
                    value.Add(binaryReader.ReadInt64());

                nodes.Add((key, value));
            }

            return nodes;
        }

        private void RewriteFile(List<(char[] key, List<long> value)> nodes)
        {
            using FileStream fileStream = new(filePath, FileMode.Open);
            using BinaryWriter binaryWriter = new(fileStream);
            //here I will seek to the block of memory that the tree will start
            //catch the case when there will be a data
            foreach (var (key, value) in nodes)
            {
                binaryWriter.Write(key.Length);
                foreach (char c in key)
                    binaryWriter.Write(c);

                binaryWriter.Write(value.Count);
                foreach (long l in value)
                    binaryWriter.Write(l);
            }
        }
    }

    class Program1
    {
        static void Main1(string[] args)
        {
            Console.WriteLine("--------------------------------------------------------------");
            Console.WriteLine("Original values");
            BinaryTreeWriter writer = new("tree.bin");

            // Example of writing nodes
            writer.WriteNode(new[] { 'A', 'B', 'C' }, new List<long> { 123, 456, 789 });
            writer.WriteNode(new[] { 'D', 'E', 'F' }, new List<long> { 234, 567, 890 });

            writer.Close();

            BinaryTreeReader reader = new("tree.bin");

            foreach (var (key, value) in reader.ReadAllNodes())
                Console.WriteLine($"Key: {new string(key)}, Value: [{string.Join(", ", value)}]");

            reader.Close();

            BinaryTreeFileHandler handler = new("tree.bin");
            Console.WriteLine("--------------------------------------------------------------------------------------");
            Console.WriteLine("Insert value");
            handler.InsertOrUpdateNode(new char[] { 'A', 'B', 'C' }, new List<long> { 111, 222, 333 });

            BinaryTreeReader reader1 = new("tree.bin");

            foreach (var (key, value) in reader1.ReadAllNodes())
                Console.WriteLine($"Key: {new string(key)}, Value: [{string.Join(", ", value)}]");

            reader1.Close();
            Console.WriteLine("---------------------------------");
            Console.WriteLine("Delete value");
            BinaryTreeFileHandler handler12 = new("tree.bin");

            handler12.DeleteNode(new char[] { 'A', 'B', 'C' });

            BinaryTreeReader reader13 = new("tree.bin");

            foreach (var (key, value) in reader13.ReadAllNodes())
                Console.WriteLine($"Key: {new string(key)}, Value: [{string.Join(", ", value)}]");

            reader13.Close();
        }
    }
}
