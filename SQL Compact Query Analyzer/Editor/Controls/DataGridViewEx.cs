using System;
using System.Drawing;
using System.Windows.Forms;

namespace ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer.Controls
{
    public sealed class DataGridViewEx : DataGridView
    {
        private Font italicFont;
        private Font normalFont;

        public DataGridViewEx()
        {
            DoubleBuffered = true;
            italicFont = new Font(Font.FontFamily, Font.Size, FontStyle.Italic);
            normalFont = Font;

            DefaultCellStyle.NullValue = "NULL";
        }

        public override Font Font
        {
            get { return base.Font; }
            set
            {
                base.Font = value;
                italicFont = new Font(Font.FontFamily, Font.Size, FontStyle.Italic);
                normalFont = Font;
            }
        }

        protected override void OnCellFormatting(DataGridViewCellFormattingEventArgs e)
        {
            e.CellStyle.Font = e.Value == DBNull.Value ? italicFont : normalFont;
            e.CellStyle.ForeColor = e.Value == DBNull.Value ? SystemColors.GrayText : SystemColors.WindowText;
            base.OnCellFormatting(e);
        }
    }
}