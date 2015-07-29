using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Bermuda.Interface;
using System.Collections.ObjectModel;

namespace Bermuda.BermudaConfig
{
    /// <summary>
    /// Interaction logic for ColumnConfig.xaml
    /// </summary>
    public partial class ColumnConfig : Window
    {
        #region Variables and Properties

        /// <summary>
        /// the column
        /// </summary>
        public IColumnMetadata Column { get; set; }
        
        /// <summary>
        /// the original column name
        /// </summary>
        public string ColumnOriginal { get; set; }

        /// <summary>
        /// the system types
        /// </summary>
        public ObservableCollection<string> SystemTypes { get; set; }

        /// <summary>
        /// the bool Values
        /// </summary>
        public ObservableCollection<string> BoolValues { get; set; }

        /// <summary>
        /// handle the column type
        /// </summary>
        public string ColumnSystemType
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Column.ColumnTypeSerializer))
                    return null;
                else if (Column.ColumnTypeSerializer.StartsWith("System.Collections.Generic.List"))
                    return "IdCollection";
                else if (Column.ColumnTypeSerializer.IndexOf(",") != -1)
                    return Column.ColumnTypeSerializer.Substring(0, Column.ColumnTypeSerializer.IndexOf(","));
                else
                    return Column.ColumnTypeSerializer;
            }
            set
            {
                if (value == "IdCollection")
                    Column.ColumnType = typeof(List<Tuple<List<long>, long>>);
                else
                    Column.ColumnTypeSerializer = value == null ? null : value;
            }
        }

        /// <summary>
        /// the nullable field
        /// </summary>
        public string Nullable
        {
            get
            {
                if (Column.Nullable)
                    return "True";
                else
                    return "False";
            }
            set
            {
                if (value == "True")
                    Column.Nullable = true;
                else
                    Column.Nullable = false;
            }
        }

        /// <summary>
        /// the visible field
        /// </summary>
        public string ColumnVisible
        {
            get
            {
                if (Column.Visible)
                    return "True";
                else
                    return "False";
            }
            set
            {
                if (value == "True")
                    Column.Visible = true;
                else
                    Column.Visible = false;
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// constructor with column data
        /// </summary>
        /// <param name="column"></param>
        public ColumnConfig(IColumnMetadata copy, string original)
        {
            //init properties
            SystemTypes = BermudaConfigUtil.GetSystemTypes();
            BoolValues = BermudaConfigUtil.GetBoolValues();

            //init column
            Column = copy;
            ColumnOriginal = original;

            //init gui
            InitializeComponent();
        }

        #endregion

        #region Event handlers

        /// <summary>
        /// cancel the edit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// verification and close
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            //check empty name
            if (string.IsNullOrWhiteSpace(Column.ColumnName))
            {
                MessageBox.Show("You must enter a name.");
                return;
            }
            //check unique name
            if (Column.TableMetadata.ColumnsMetadata.Values.Any(c => c.ColumnName != ColumnOriginal && c.ColumnName == Column.ColumnName))
            {
                MessageBox.Show("You must have a unique name");
                return;
            }
            DialogResult = true;
            Close();
        }

        #endregion
    }
}
