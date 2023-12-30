using DataStructures;
using DMS.Commands;
using DMS.Extensions;
using DMS.Shared;

namespace UI;

public partial class InsertForm : Form
{
    private string tableName;
    private int numberOfDataPages;
    private int columnCount;
    private DKList<string> columnTypes;
    private DKList<string> columnNames;
    private DKList<string> defaultValues;

    public InsertForm()
    {
        InitializeComponent();
    }

    public void GetTableInfo(TableInfo tableInfo)
    {
        tableName = tableInfo.TableName;
        numberOfDataPages = tableInfo.NumberOfDataPages;
        columnCount = tableInfo.ColumnCount;
        columnTypes = tableInfo.ColumnTypes;
        columnNames = tableInfo.ColumnNames;
        defaultValues = tableInfo.DefaultValues;
    }

    private void InsertForm_Load(object sender, EventArgs e)
    {
        TableNameLabel.Text = "Table name: " + tableName;

        const int spacing = 30;
        const int controlHeight = 20;
        int totalControlsHeight = (controlHeight + spacing) * columnCount;

        int startY = (this.ClientSize.Height - totalControlsHeight) / 2;
        int currentY = startY > 0 ? startY : 10;

        for (int i = 0; i < columnCount; i++)
        {
            Label nameLabel = new()
            {
                Text = columnNames[i],
                AutoSize = true,
                Location = new Point((this.ClientSize.Width / 2) - 200, currentY)
            };
            this.Controls.Add(nameLabel);

            TextBox inputBox = new()
            {
                Location = new Point((this.ClientSize.Width / 2) - 90, currentY),
                Name = $"inputBox_{i}"
            };
            this.Controls.Add(inputBox);

            Label typeLabel = new()
            {
                Text = columnTypes[i],
                AutoSize = true,
                Location = new Point((this.ClientSize.Width / 2) + 20, currentY)
            };
            this.Controls.Add(typeLabel);

            currentY += spacing;
        }
    }

    private void InsertButton_Click(object sender, EventArgs e)
    {
        DKList<DKList<char[]>> valuesToInsert = new();
        DKList<char[]> columnData = new();

        for (int i = 0; i < columnCount; i++)
        {
            if (Controls.Find($"inputBox_{i}", true).CustomFirstOrDefault() is not TextBox inputBox)
                continue;

            string inputText = inputBox.Text;
            char[] charArray = inputText.ToCharArray();
            columnData.Add(charArray);
        }

        valuesToInsert.Add(columnData);
        SqlCommands.InsertIntoTable(valuesToInsert, new DKList<string>(new string[columnCount]), tableName);
        MessageBox.Show($@"Successfully inserted into table {tableName}");

        Close();
    }
}