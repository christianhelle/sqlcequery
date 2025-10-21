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
            if (e.Value == DBNull.Value)
            {
                e.CellStyle.Font = italicFont;
                e.CellStyle.ForeColor = SystemColors.GrayText;
            }
            else
            {
                e.CellStyle.Font = normalFont;
                e.CellStyle.ForeColor = SystemColors.WindowText;
                
                // Format DateTime values to include seconds
                if (e.Value is DateTime dateTimeValue)
                {
                    e.Value = dateTimeValue.ToString("yyyy-MM-dd HH:mm:ss");
                    e.FormattingApplied = true;
                }
            }
            
            base.OnCellFormatting(e);
        }

        protected override void OnDataBindingComplete(DataGridViewBindingCompleteEventArgs e)
        {
            base.OnDataBindingComplete(e);
            AutoResizeColumnHeadersHeight();
        }
    }
}