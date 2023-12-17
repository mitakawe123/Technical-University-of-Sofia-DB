using DMS.DataPages;

namespace UI
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            DataPageManager.InitDataPageManager();

            LoadTableNamesIntoListView();

            ContextMenuStrip menuStrip = new();
            menuStrip.Items.Add("Show all records");
            menuStrip.Items.Add("Drop table");

            tableNames.ContextMenuStrip = menuStrip;
        }

        private void LoadTableNamesIntoListView()
        {
            foreach (var item in DataPageManager.TableOffsets)
            {
                ListViewItem listViewItem = new(new string(item.Key));
                listViewItem.SubItems.Add(item.Value.ToString());
                tableNames.Items.Add(listViewItem);
            }
        }

        private void ShowTableRecords(object sender, EventArgs e)
        {
            if (tableNames.SelectedItems.Count <= 0) 
                return;

            string selectedItemsNames = "";

            foreach (ListViewItem selectedItem in tableNames.SelectedItems)
                selectedItemsNames += selectedItem.Text + "\n";

            MessageBox.Show(selectedItemsNames);
        }
    }
}