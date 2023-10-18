namespace DataStructures
{
    public class BTree<T> where T : IComparable<T>
    {
        private int degree;
        private BTreeNode<T> root;

        public BTree(int degree)
        {
            this.degree = degree;
            root = new BTreeNode<T>();
        }

        public void Insert(T key)
        {
            if (root.Keys.Count == (2 * degree) - 1)
            {
                BTreeNode<T> newRoot = new();
                newRoot.Children.Add(root);
                SplitChild(newRoot, 0);
                root = newRoot;
            }

            InsertNonFull(root, key);
        }

        private void InsertNonFull(BTreeNode<T> node, T key)
        {
            int index = node.Keys.Count - 1;

            if (node.IsLeaf)
            {
                // Insert key into the leaf node
                while (index >= 0 && key.CompareTo(node.Keys[index]) < 0)
                {
                    index--;
                }
                node.Keys.Insert(index + 1, key);
            }
            else
            {
                // Insert into a non-leaf node
                while (index >= 0 && key.CompareTo(node.Keys[index]) < 0)
                {
                    index--;
                }
                index++;

                if (node.Children[index].Keys.Count == (2 * degree) - 1)
                {
                    // If the child is full, split it
                    SplitChild(node, index);

                    if (key.CompareTo(node.Keys[index]) > 0)
                    {
                        index++;
                    }
                }
                InsertNonFull(node.Children[index], key);
            }
        }

        private void SplitChild(BTreeNode<T> parentNode, int childIndex)
        {
            BTreeNode<T> child = parentNode.Children[childIndex];
            BTreeNode<T> newChild = new();

            parentNode.Keys.Insert(childIndex, child.Keys[degree - 1]);
            parentNode.Children.Insert(childIndex + 1, newChild);

            newChild.Keys.AddRange(child.Keys.GetRange(degree, degree - 1));
            child.Keys.RemoveRange(degree - 1, degree);

            if (child.IsLeaf) return;
            
            newChild.Children.AddRange(child.Children.GetRange(degree, degree));
            child.Children.RemoveRange(degree, degree);
        }

        public class BTreeNode<T> where T : IComparable<T>
        {
            public List<T> Keys { get; private set; } = new();
            public List<BTreeNode<T>> Children { get; private set; } = new();
            public bool IsLeaf => Children.Count == 0;
        }
    }
}