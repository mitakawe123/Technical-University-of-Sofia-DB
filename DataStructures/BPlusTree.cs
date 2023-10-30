namespace DataStructures
{
    public class BPlusTree<T>
    {
        public Node<T> Root { get; private set; } = new();
        private readonly int order;

        public BPlusTree(int order)
        {
            if(order < 3)
                throw new ArgumentException("Order must be at least 3", nameof(order));
            
            this.order = order;
        }

        // Inside BPlusTree class

        public void Insert(int key)
        {
            var root = Root;
            if (root.Keys.Count == order - 1)
            {
                var newRoot = new Node<T>();
                newRoot.Children.Add(root);
                SplitChild(newRoot, 0);
                Root = newRoot;
            }
            //InsertNonFull(Root, key);
        }

        private void SplitChild(Node<T> parent, int index)
        {
            var child = parent.Children[index];
            var newChild = new Node<T>();
            var mid = order / 2;

            // Moving half the keys and children (if not a leaf) to new child
            newChild.Keys.AddRange(child.Keys.GetRange(mid + 1, child.Keys.Count - (mid + 1)));
            if (!child.IsLeaf)
            {
                newChild.Children.AddRange(child.Children.GetRange(mid + 1, child.Children.Count - (mid + 1)));
            }

            // Adjusting the current child
            child.Keys.RemoveRange(mid, child.Keys.Count - mid);
            if (!child.IsLeaf)
            {
                child.Children.RemoveRange(mid + 1, child.Children.Count - (mid + 1));
            }

            parent.Keys.Insert(index, newChild.Keys[0]);
            parent.Children.Insert(index + 1, newChild);
        }

      /*  private void InsertNonFull(Node<T> node, int key)
        {
            int i = node.Keys.Count - 1;
            while (i >= 0 && key < node.Keys[i])
            {
                i--;
            }
            i++;
            if (node.IsLeaf)
            {
                node.Keys.Insert(i, key);
            }
            else
            {
                if (node.Children[i].Keys.Count == order - 1)
                {
                    SplitChild(node, i);
                    if (key > node.Keys[i])
                    {
                        i++;
                    }
                }
                InsertNonFull(node.Children[i], key);
            }
        }*/

        //public bool Search(int key) => SearchInternal(Root, key);

        /*private bool SearchInternal(Node<T> node, int key)
        {
            int i = 0;
            while (i < node.Keys.Count && key > node.Keys[i])
            {
                i++;
            }
            if (i < node.Keys.Count && key == node.Keys[i])
            {
                return true;
            }
            if (node.IsLeaf)
            {
                return false;
            }
            return SearchInternal(node.Children[i], key);
        }*/

        public class Node<T>
        {
            public DKList<T> Keys { get; } = new();
            public DKList<Node<T>> Children { get; } = new();
            public bool IsLeaf => Children.Count == 0;
            public Node<T> NextLeaf { get; set; }
        }
    }
}
