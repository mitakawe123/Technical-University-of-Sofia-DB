using DMS.DataPages;

namespace UI
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            DataPageManager.InitDataPageManager();

            LoadTableNamesIntoListView();
        }

        private void LoadTableNamesIntoListView()
        {
            foreach (var item in DataPageManager.TableOffsets)
            {
                var listViewItem = new ListViewItem(new string(item.Key));
                listViewItem.SubItems.Add(item.Value.ToString());
                listView1.Items.Add(listViewItem);
            }
        }
    }
}