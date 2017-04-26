using System.Windows.Forms;

namespace ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer.Controls
{
    public sealed class ResultsContainer : TableLayoutPanel
    {
        public void Clear()
        {
            foreach (Control control in Controls)
                control.Dispose();
            Controls.Clear();
        }

        public int Count { get { return Controls.Count; } }

        public void Add(DataGridViewEx dataGrid)
        {
            dataGrid.DataError += (sender, e) => e.ThrowException = false;
            dataGrid.ReadOnly = true;
            dataGrid.Dock = DockStyle.Fill;
            Controls.Add(dataGrid);
        }
    }
}
