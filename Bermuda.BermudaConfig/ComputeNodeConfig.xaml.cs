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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Bermuda.Interface;
using System.IO;
using Bermuda.Catalog;
using System.Collections.ObjectModel;
using Bermuda.BermudaConfig.Storage;
using System.ComponentModel;

namespace Bermuda.BermudaConfig
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ComputeNodeConfig : Window, INotifyPropertyChanged
    {
        #region Variables and Properties

        /// <summary>
        /// the compute node
        /// </summary>
        public IComputeNode ComputeNode { get; set; }

        /// <summary>
        /// the catalog collection
        /// </summary>
        public ObservableCollection<ICatalog> Catalogs { get; set; }

        /// <summary>
        /// the selected catalog
        /// </summary>
        private ICatalog _SelectedCatalog;
        public ICatalog SelectedCatalog
        {
            get
            {
                return _SelectedCatalog;
            }
            set
            {
                _SelectedCatalog = value;
                NotifyPropertyChanged("SelectedCatalog");
                NotifyPropertyChanged("ValidSelectedCatalog");
            }
        }

        /// <summary>
        /// the selection is valid enable disable buttons
        /// </summary>
        public bool ValidSelectedCatalog
        {
            get
            {
                return _SelectedCatalog != null;
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// default constructor
        /// </summary>
        public ComputeNodeConfig(IComputeNode compute_node)
        {
            //init properties
            Catalogs = new ObservableCollection<ICatalog>();

            //read the config file
            //string json = File.ReadAllText("c:\\temp\\Bermuda.Config");
            //ComputeNode = new ComputeNode().DeserializeComputeNode(json);
            ComputeNode = compute_node;

            //set the catalogs
            ComputeNode.Catalogs.Values.ToList().ForEach(c => Catalogs.Add((ICatalog)c));

            //FixCollections();

            //init gui
            InitializeComponent();
        }

        private void FixCollections()
        {
            ComputeNode.Catalogs.Values.Cast<ICatalog>().ToList().ForEach(catalog =>
                {
                    catalog.CatalogMetadata.Tables.Values.ToList().ForEach(table =>
                        {
                            table.ColumnsMetadata.Values.ToList().ForEach(col =>
                                {
                                    if (col.ColumnType == typeof(List<Tuple<long, long>>))
                                    {
                                        col.ColumnType = typeof(List<Tuple<List<long>, long>>);
                                    }
                                });
                        });
                });
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// handle the double click in the list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = sender as ListBoxItem;
            if (item == null || !item.IsSelected) return;
            ICatalog sel = item.Content as ICatalog;
            ICatalog catalog = new Catalog.Catalog(ComputeNode);
            BermudaConfigUtil.CopyCatalog(sel, catalog);
            CatalogConfig window = new CatalogConfig(catalog, catalog.CatalogName);
            var ret = window.ShowDialog();
            if (!ret.HasValue || ret == false)
                return;
            BermudaConfigUtil.CopyCatalog(catalog, sel);
            lbCatalogs.Items.Refresh();
        }

        /// <summary>
        /// handle ok click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            OpenConfig dlg = new OpenConfig(ComputeNode);
            dlg.ShowDialog();
            //Close();
        }

        /// <summary>
        /// handle cancel click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            //DialogResult = false;
            Close();
        }

        /// <summary>
        /// delete a catalog
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btDelete_Click(object sender, RoutedEventArgs e)
        {
            //get the selected item
            if (lbCatalogs.SelectedItem == null) return;
            ICatalog cat = lbCatalogs.SelectedItem as ICatalog;

            //prompt to delete
            if (MessageBox.Show("Are you sure you wish to delete the selected item.", "Delete Confirmation", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            //remove the column
            ComputeNode.Catalogs.Remove(cat.CatalogName);
            Catalogs.Remove(cat);
        }

        /// <summary>
        /// make a copy of catalog
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btCopy_Click(object sender, RoutedEventArgs e)
        {
            //get the selected item
            if (lbCatalogs.SelectedItem == null) return;
            ICatalog copy = lbCatalogs.SelectedItem as ICatalog;

            //copy fields
            var cat = new Catalog.Catalog(ComputeNode);
            BermudaConfigUtil.CopyCatalog(copy, cat);

            //open the window
            CatalogConfig window = new CatalogConfig(cat, "");
            var ret = window.ShowDialog();
            if (!ret.HasValue || ret == false)
                return;

            //add to list
            Catalogs.Add(cat);
            ComputeNode.Catalogs.Add(cat.CatalogName, cat);
        }

        /// <summary>
        /// create a new catalog
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btNew_Click(object sender, RoutedEventArgs e)
        {
            //new item
            var cat = new Catalog.Catalog(ComputeNode);
            cat.CatalogMetadata = new CatalogMetadata(cat);

            //open the window
            CatalogConfig window = new CatalogConfig(cat, "");
            var ret = window.ShowDialog();
            if (!ret.HasValue || ret == false)
                return;

            //add to list
            Catalogs.Add(cat);
            ComputeNode.Catalogs.Add(cat.CatalogName, cat);
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
