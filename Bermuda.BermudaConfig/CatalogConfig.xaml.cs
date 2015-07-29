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
    /// Interaction logic for CatalogConfig.xaml
    /// </summary>
    public partial class CatalogConfig : Window, INotifyPropertyChanged
    {
        #region Variables and Properties

        /// <summary>
        /// the catalog
        /// </summary>
        public ICatalog Catalog { get; set; }
        
        /// <summary>
        /// the original catalog name
        /// </summary>
        public string CatalogOriginal { get; set; }

        /// <summary>
        /// table metadata
        /// </summary>
        public ObservableCollection<ITableMetadata> Tables { get; set; }

        /// <summary>
        /// relationship metadata
        /// </summary>
        public ObservableCollection<IRelationshipMetadata> Relationships { get; set; }

        /// <summary>
        /// the connection types for catalog
        /// </summary>
        public ObservableCollection<string> ConnectionTypes { get; set; }

        /// <summary>
        /// the selected table
        /// </summary>
        private ITableMetadata _SelectedTable;
        public ITableMetadata SelectedTable
        {
            get
            {
                return _SelectedTable;
            }
            set
            {
                _SelectedTable = value;
                NotifyPropertyChanged("SelectedTable");
                NotifyPropertyChanged("ValidSelectedTable");
            }
        }

        /// <summary>
        /// the selected relationship
        /// </summary>
        private IRelationshipMetadata _SelectedRel;
        public IRelationshipMetadata SelectedRel
        {
            get
            {
                return _SelectedRel;
            }
            set
            {
                _SelectedRel = value;
                NotifyPropertyChanged("SelectedRel");
                NotifyPropertyChanged("ValidSelectedRel");
            }
        }

        /// <summary>
        /// the table selection is valid
        /// </summary>
        public bool ValidSelectedTable
        {
            get
            {
                return SelectedTable != null;
            }
        }

        /// <summary>
        /// the relationship selection is valid
        /// </summary>
        public bool ValidSelectedRel
        {
            get
            {
                return SelectedRel != null;
            }
        }

        /// <summary>
        /// handle the connection type
        /// </summary>
        public string ConnectionType
        {
            get
            {
                switch (Catalog.ConnectionType)
                {
                    case Constants.ConnectionTypes.SQLServer:
                        return "SQL Server";
                    case Constants.ConnectionTypes.Oracle:
                        return "Oracle";
                    case Constants.ConnectionTypes.ODBC:
                        return "ODBC";
                    case Constants.ConnectionTypes.FileSystem:
                        return "File System";
                    case Constants.ConnectionTypes.S3:
                        return "Amazon S3";
                    default:
                        return "Unknown Connection Type";
                }
            }
            set
            {
                switch (value)
                {
                    case "SQL Server":
                        Catalog.ConnectionType = Constants.ConnectionTypes.SQLServer;
                        break;
                    case "Oracle":
                        Catalog.ConnectionType = Constants.ConnectionTypes.Oracle;
                        break;
                    case "ODBC":
                        Catalog.ConnectionType = Constants.ConnectionTypes.ODBC;
                        break;
                    case "File System":
                        Catalog.ConnectionType = Constants.ConnectionTypes.FileSystem;
                        break;
                    case "Amazon S3":
                        Catalog.ConnectionType = Constants.ConnectionTypes.S3;
                        break;
                    default:
                        Catalog.ConnectionType = Constants.ConnectionTypes.SQLServer;
                        break;
                }
            }
        }
        #endregion

        #region Constructor

        /// <summary>
        /// constructor with catalog
        /// </summary>
        /// <param name="catalog"></param>
        public CatalogConfig(ICatalog copy, string original)
        {
            //inti properties
            Tables = new ObservableCollection<ITableMetadata>();
            Relationships = new ObservableCollection<IRelationshipMetadata>();

            //get the catalog
            Catalog = copy;
            CatalogOriginal = original;

            //get the lists
            Catalog.CatalogMetadata.Tables.Values.ToList().ForEach(t => Tables.Add(t));
            Catalog.CatalogMetadata.Relationships.Values.ToList().ForEach(r => Relationships.Add(r));
            ConnectionTypes = BermudaConfigUtil.GetConnectionTypes();

            //inti gui
            InitializeComponent();
        }

        #endregion

        #region Event handlers

        /// <summary>
        /// double click on table for edit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbTables_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = sender as ListBoxItem;
            if (item == null || !item.IsSelected) return;
            ITableMetadata sel = item.Content as ITableMetadata;
            ITableMetadata table = new TableMetadata(Catalog.CatalogMetadata);
            BermudaConfigUtil.CopyTable(sel, table);
            TableConfig window = new TableConfig(table, table.TableName);
            var ret = window.ShowDialog();
            if (!ret.HasValue || ret.Value == false)
                return;
            BermudaConfigUtil.CopyTable(table, sel);
            lbTables.Items.Refresh();
        }

        /// <summary>
        /// double click on relationship for edit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbRealtions_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = sender as ListBoxItem;
            if (item == null || !item.IsSelected) return;
            IRelationshipMetadata sel = item.Content as IRelationshipMetadata;
            IRelationshipMetadata relationship = new RelationshipMetadata(Catalog.CatalogMetadata);
            BermudaConfigUtil.CopyRelationship(sel, relationship);
            RelationshipConfig window = new RelationshipConfig(relationship, relationship.RelationshipName);
            var ret = window.ShowDialog();
            if (!ret.HasValue || ret.Value == false)
                return;
            BermudaConfigUtil.CopyRelationship(relationship, sel);
            lbRelations.Items.Refresh();
        }

        /// <summary>
        /// handle verification and close
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            //check empty name
            if (string.IsNullOrWhiteSpace(Catalog.CatalogName))
            {
                MessageBox.Show("You must enter a name.");
                return;
            }
            //check unique name
            if (Catalog.ComputeNode.Catalogs.Values.Cast<ICatalog>().Any(c => c.CatalogName != CatalogOriginal && c.CatalogName == Catalog.CatalogName))
            {
                MessageBox.Show("You must have a unique name");
                return;
            }
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// handle cancel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// new table
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btNewTable_Click(object sender, RoutedEventArgs e)
        {
            //create the new item
            ITableMetadata table = new TableMetadata(Catalog.CatalogMetadata);
            
            //open the window
            TableConfig window = new TableConfig(table, "");
            var ret = window.ShowDialog();
            if (!ret.HasValue || ret == false)
                return;

            //add to list
            Tables.Add(table);
            Catalog.CatalogMetadata.Tables.Add(table.TableName, table);
        }

        /// <summary>
        /// copy table
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btCopyTable_Click(object sender, RoutedEventArgs e)
        {
            //get the selected item
            if (lbTables.SelectedItem == null) return;
            ITableMetadata copy = lbTables.SelectedItem as ITableMetadata;

            //create the new item
            ITableMetadata table = new TableMetadata(Catalog.CatalogMetadata);
            BermudaConfigUtil.CopyTable(copy, table);

            //open the window
            TableConfig window = new TableConfig(table, "");
            var ret = window.ShowDialog();
            if (!ret.HasValue || ret == false)
                return;
            
            //add to list
            Tables.Add(table);
            Catalog.CatalogMetadata.Tables.Add(table.TableName, table);
        }

        /// <summary>
        /// delete table
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btDeleteTable_Click(object sender, RoutedEventArgs e)
        {
            //get the selected item
            if (lbTables.SelectedItem == null) return;
            ITableMetadata table = lbTables.SelectedItem as ITableMetadata;

            //prompt to delete
            if (MessageBox.Show("Are you sure you wish to delete the selected item.", "Delete Confirmation", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            //remove the item
            Catalog.CatalogMetadata.Tables.Remove(table.TableName);
            Tables.Remove(table);
        }

        /// <summary>
        /// new realtionship
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btNewRel_Click(object sender, RoutedEventArgs e)
        {
            //create the new item
            IRelationshipMetadata rel = new RelationshipMetadata(Catalog.CatalogMetadata);

            //open the window
            RelationshipConfig window = new RelationshipConfig(rel, "");
            var ret = window.ShowDialog();
            if (!ret.HasValue || ret == false)
                return;

            //add to list
            Relationships.Add(rel);
            Catalog.CatalogMetadata.Relationships.Add(rel.RelationshipName, rel);
        }

        /// <summary>
        /// copy a relationship
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btCopyRel_Click(object sender, RoutedEventArgs e)
        {
            //get the selected item
            if (lbRelations.SelectedItem == null) return;
            IRelationshipMetadata copy = lbRelations.SelectedItem as IRelationshipMetadata;

            //create the new item
            IRelationshipMetadata rel = new RelationshipMetadata(Catalog.CatalogMetadata);
            BermudaConfigUtil.CopyRelationship(copy, rel);

            //open the window
            RelationshipConfig window = new RelationshipConfig(rel, "");
            var ret = window.ShowDialog();
            if (!ret.HasValue || ret == false)
                return;

            //add to list
            Relationships.Add(rel);
            Catalog.CatalogMetadata.Relationships.Add(rel.RelationshipName, rel);
        }

        /// <summary>
        /// delete a relationship
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btDeleteRel_Click(object sender, RoutedEventArgs e)
        {
            //get the selected item
            if (lbRelations.SelectedItem == null) return;
            IRelationshipMetadata rel = lbRelations.SelectedItem as IRelationshipMetadata;

            //prompt to delete
            if (MessageBox.Show("Are you sure you wish to delete the selected item.", "Delete Confirmation", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            //remove the item
            Catalog.CatalogMetadata.Relationships.Remove(rel.RelationshipName);
            Relationships.Remove(rel);
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
