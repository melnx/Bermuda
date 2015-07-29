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
using Bermuda.Catalog;
using System.ComponentModel;

namespace Bermuda.BermudaConfig
{
    /// <summary>
    /// Interaction logic for TableConfig.xaml
    /// </summary>
    public partial class TableConfig : Window, INotifyPropertyChanged
    {
        #region Variables and Properties

        /// <summary>
        /// the table metadata for this config
        /// </summary>
        public ITableMetadata Table { get; set; }

        /// <summary>
        /// the original table name
        /// </summary>
        public string TableOriginal { get; set; }

        /// <summary>
        /// the list of collumn metadata
        /// </summary>
        private ObservableCollection<IColumnMetadata> _Columns { get; set; }
        public CollectionViewSource Columns { get; set; }

        /// <summary>
        /// the bool Values
        /// </summary>
        public ObservableCollection<string> BoolValues { get; set; }

        /// <summary>
        /// the system types
        /// </summary>
        public ObservableCollection<string> SystemTypes { get; set; }

        /// <summary>
        /// The comparators
        /// </summary>
        public ObservableCollection<string> Comparators { get; set; }

        /// <summary>
        /// The purge operations
        /// </summary>
        public ObservableCollection<string> PurgeOperations { get; set; }

        /// <summary>
        /// handle the primary key
        /// </summary>
        public IColumnMetadata PrimaryKey
        {
            get
            {
                return _Columns.FirstOrDefault(c => c.FieldMapping == Table.PrimaryKey);
            }
            set
            {
                if (value == null)
                {
                    Table.PrimaryKey = null;
                    //Table.SaturationUpdateType = null;
                }
                else
                {
                    Table.PrimaryKey = value.ColumnName;
                    //Table.SaturationUpdateType = value.ColumnType;
                }
            }
        }

        /// <summary>
        /// handle the mod field
        /// </summary>
        public IColumnMetadata ModField
        {
            get
            {
                return _Columns.FirstOrDefault(c => c.ColumnName == Table.ModField);
            }
            set
            {
                if (value == null)
                {
                    Table.ModField = null;
                    //Table.SaturationUpdateType = null;
                }
                else
                {
                    Table.ModField = value.ColumnName;
                    //Table.SaturationUpdateType = value.ColumnType;
                }
            }
        }

        /// <summary>
        /// handle the update column
        /// </summary>
        public IColumnMetadata UpdateColumn
        {
            get
            {
                return _Columns.FirstOrDefault(c => c.ColumnName == Table.SaturationUpdateField);
            }
            set
            {
                if (value == null)
                {
                    Table.SaturationUpdateField = null;
                    Table.SaturationUpdateType = null;
                }
                else
                {
                    Table.SaturationUpdateField = value.ColumnName;
                    Table.SaturationUpdateType = value.ColumnType;
                }
            }
        }

        /// <summary>
        /// handle the delete column
        /// </summary>
        public IColumnMetadata DeleteColumn
        {
            get
            {
                return _Columns.FirstOrDefault(c => c.ColumnName == Table.SaturationDeleteField);
            }
            set
            {
                if (value == null)
                {
                    Table.SaturationDeleteField = null;
                    Table.SaturationDeleteType = null;
                }
                else
                {
                    Table.SaturationDeleteField = value.ColumnName;
                    Table.SaturationDeleteType = value.ColumnType;
                }
            }
        }

        /// <summary>
        /// handle the purge column
        /// </summary>
        public IColumnMetadata PurgeColumn
        {
            get
            {
                return _Columns.FirstOrDefault(c => c.ColumnName == Table.SaturationPurgeField);
            }
            set
            {
                if (value == null)
                {
                    Table.SaturationPurgeField = null;
                    Table.SaturationPurgeType = null;
                }
                else
                {
                    Table.SaturationPurgeField = value.ColumnName;
                    Table.SaturationPurgeType = value.ColumnType;
                }
            }
        }

        /// <summary>
        /// handle the update type
        /// </summary>
        public string UpdateSystemType
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Table.SaturationUpdateTypeSerializer))
                    return null;
                else if (Table.SaturationUpdateTypeSerializer.IndexOf(",") != -1)
                    return Table.SaturationUpdateTypeSerializer.Substring(0, Table.SaturationUpdateTypeSerializer.IndexOf(","));
                else
                    return Table.SaturationUpdateTypeSerializer;
            }
            set
            {
                Table.SaturationUpdateTypeSerializer = value == null ? null : value;
            }
        }

        /// <summary>
        /// handle the delete type
        /// </summary>
        public string DeleteSystemType
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Table.SaturationDeleteTypeSerializer))
                    return null;
                else if (Table.SaturationDeleteTypeSerializer.IndexOf(",") != -1)
                    return Table.SaturationDeleteTypeSerializer.Substring(0, Table.SaturationDeleteTypeSerializer.IndexOf(","));
                else
                    return Table.SaturationDeleteTypeSerializer;
            }
            set
            {
                Table.SaturationDeleteTypeSerializer = value == null ? null : value;
            }
        }

        /// <summary>
        /// handle the delete value
        /// </summary>
        public object DeleteValue
        {
            get
            {
                return Table.SaturationDeleteValue;
            }
            set
            {
                try
                {
                    Table.SaturationDeleteValue = Convert.ChangeType(value, Table.SaturationDeleteType);
                }
                catch (Exception) 
                {
                    Table.SaturationDeleteValue = null;
                }
            }
        }

        /// <summary>
        /// the referehce table
        /// </summary>
        public string ReferenceTable
        {
            get
            {
                if (Table.ReferenceTable)
                    return "True";
                else
                    return "False";
            }
            set
            {
                if (value == "True")
                    Table.ReferenceTable = true;
                else
                    Table.ReferenceTable = false;
            }
        }

        /// <summary>
        /// the the file table is a fixed width table
        /// </summary>
        public string IsFixedWidth
        {
            get
            {
                if (Table.IsFixedWidth)
                    return "True";
                else
                    return "False";
            }
            set
            {
                if (value == "True")
                    Table.IsFixedWidth = true;
                else
                    Table.IsFixedWidth = false;
            }
        }

        /// <summary>
        /// the column delimiters
        /// </summary>
        public string ColumnDelimiters
        {
            get
            {
                if (Table.ColumnDelimiters == null)
                    return "";
                else
                    return string.Join("", Table.ColumnDelimiters).Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
            }
            set
            {
                try
                {
                    var char_array = value.Replace("\\r", "\r").Replace("\\n", "\n").Replace("\\t", "\t").ToCharArray();
                    string[] string_array = new string[char_array.Length];
                    for (int x = 0; x < char_array.Length; x++)
                        string_array[x] = char_array[x].ToString();
                    Table.ColumnDelimiters = string_array;
                }
                catch (Exception) { }
            }
        }

        /// <summary>
        /// the line delimiters
        /// </summary>
        public string LineDelimiters
        {
            get
            {
                if (Table.LineDelimiters == null)
                    return "";
                else
                    return string.Join("", Table.LineDelimiters).Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
            }
            set
            {
                try
                {
                    var char_array = value.Replace("\\r", "\r").Replace("\\n", "\n").Replace("\\t", "\t").ToCharArray();
                    string[] string_array = new string[char_array.Length];
                    for (int x = 0; x < char_array.Length; x++)
                        string_array[x] = char_array[x].ToString();
                    Table.LineDelimiters = string_array;
                }
                catch (Exception) { }
            }
        }

        /// <summary>
        /// the selected column
        /// </summary>
        private IColumnMetadata _SelectedColumn;
        public IColumnMetadata SelectedColumn
        {
            get
            {
                return _SelectedColumn;
            }
            set
            {
                _SelectedColumn = value;
                NotifyPropertyChanged("SelectedColumn");
                NotifyPropertyChanged("ValidSelectedColumn");
            }
        }

        /// <summary>
        /// the selected column is valid
        /// </summary>
        public bool ValidSelectedColumn
        {
            get
            {
                return SelectedColumn != null;
            }
        }


        #endregion

        #region Constructor

        /// <summary>
        /// constructor with table
        /// </summary>
        public TableConfig(ITableMetadata copy, string original)
        {
            //init properties
            SystemTypes = BermudaConfigUtil.GetSystemTypes();
            Comparators = BermudaConfigUtil.GetComparators();
            PurgeOperations = BermudaConfigUtil.GetPurgeOperations();
            BoolValues = BermudaConfigUtil.GetBoolValues();
            
            //get the table
            Table = copy;
            TableOriginal = original;

            //init the column collection
            _Columns = new ObservableCollection<IColumnMetadata>();
            Table.ColumnsMetadata.Values.ToList().ForEach(c => _Columns.Add(c));
            int position = 0;
            _Columns.OrderBy(c => c.OrdinalPosition).ToList().ForEach(c => c.OrdinalPosition = position++);
            Columns = new CollectionViewSource() { Source = _Columns };
            Columns.SortDescriptions.Add(new System.ComponentModel.SortDescription("OrdinalPosition", System.ComponentModel.ListSortDirection.Ascending));

            //init gui
            InitializeComponent();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// handle double click on column for edit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = sender as ListBoxItem;
            if (item == null || !item.IsSelected) return;
            IColumnMetadata sel = item.Content as IColumnMetadata;
            IColumnMetadata column = new ColumnMetadata(Table);
            BermudaConfigUtil.CopyColumn(sel, column);
            ColumnConfig window = new ColumnConfig(column, column.ColumnName);
            var ret = window.ShowDialog();
            if (!ret.HasValue || ret.Value == false)
                return;
            BermudaConfigUtil.CopyColumn(column, sel);
            lbColumns.Items.Refresh();
        }

        /// <summary>
        /// handle verification and close
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            //check empty name
            if (string.IsNullOrWhiteSpace(Table.TableName))
            {
                MessageBox.Show("You must enter a name.");
                return;
            }
            //check unique name
            if (Table.CatalogMetadata.Tables.Values.Any(t => t.TableName != TableOriginal && t.TableName == Table.TableName))
            {
                MessageBox.Show("You must have a unique name");
                return;
            }
            DialogResult = true;
            Close();
        }

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
        /// new column
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btNew_Click(object sender, RoutedEventArgs e)
        {
            //get the column position
            int position = 0;
            if (_Columns.Count > 0)
                position = _Columns.Max(c => c.OrdinalPosition) + 1;

            //create the new item
            IColumnMetadata col = new ColumnMetadata(Table);
            col.OrdinalPosition = position;
            
            //open the window
            ColumnConfig window = new ColumnConfig(col, "");
            var ret = window.ShowDialog();
            if (!ret.HasValue || ret.Value == false)
                return;
            
            //add to list
            _Columns.Add(col);
            Table.ColumnsMetadata.Add(col.ColumnName, col);
        }

        /// <summary>
        /// copy a column
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btCopy_Click(object sender, RoutedEventArgs e)
        {
            //get the column position
            int position = 0;
            if (_Columns.Count > 0)
                position = _Columns.Max(c => c.OrdinalPosition) + 1;

            //get the selected item
            if (lbColumns.SelectedItem == null) return;
            IColumnMetadata copy = lbColumns.SelectedItem as IColumnMetadata;

            //copy fields
            var col = new ColumnMetadata(Table);
            BermudaConfigUtil.CopyColumn(copy, col);
            col.OrdinalPosition = position;

            //open the window
            ColumnConfig window = new ColumnConfig(col, "");
            var ret = window.ShowDialog();
            if (!ret.HasValue || ret.Value == false)
                return;
            
            //add to list
            _Columns.Add(col);
            Table.ColumnsMetadata.Add(col.ColumnName, col);
        }

        /// <summary>
        /// delete a column
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btDelete_Click(object sender, RoutedEventArgs e)
        {
            //get the selected item
            if (lbColumns.SelectedItem == null) return;
            IColumnMetadata col = lbColumns.SelectedItem as IColumnMetadata;

            //prompt to delete
            if (MessageBox.Show("Are you sure you wish to delete the selected item.", "Delete Confirmation", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            //remove the column
            Table.ColumnsMetadata.Remove(col.ColumnName);
            _Columns.Remove(col);
        }

        /// <summary>
        /// move the column up in order
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btUp_Click(object sender, RoutedEventArgs e)
        {
            //get the selected item
            if (lbColumns.SelectedItem == null) return;
            IColumnMetadata col = lbColumns.SelectedItem as IColumnMetadata;

            //check we are first
            if (col.OrdinalPosition == 0) return;

            //decrement position
            var swap = _Columns.FirstOrDefault(c => c.OrdinalPosition == col.OrdinalPosition - 1);
            if (swap != null)
                swap.OrdinalPosition++;
            col.OrdinalPosition--;
            Columns.View.Refresh();
        }

        /// <summary>
        /// move the column down in order
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btDown_Click(object sender, RoutedEventArgs e)
        {
            //get the selected item
            if (lbColumns.SelectedItem == null) return;
            IColumnMetadata col = lbColumns.SelectedItem as IColumnMetadata;

            //get the column position
            int position = 0;
            if (_Columns.Count > 0)
                position = _Columns.Max(c => c.OrdinalPosition);

            //check if we are max
            if (position == col.OrdinalPosition) return;

            //decrement position
            var swap = _Columns.FirstOrDefault(c => c.OrdinalPosition == col.OrdinalPosition + 1);
            if (swap != null)
                swap.OrdinalPosition--;
            col.OrdinalPosition++;
            Columns.View.Refresh();
        }

        #endregion

        #region INotifyPropertyChanged

        /// <summary>
        /// handling of property change for this window
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        
    }
}
