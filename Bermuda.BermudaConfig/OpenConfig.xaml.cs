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
using Bermuda.BermudaConfig.Storage;
using Bermuda.Interface;
using Bermuda.Catalog;
using System.IO;

namespace Bermuda.BermudaConfig
{
    /// <summary>
    /// Interaction logic for OpenConfig.xaml
    /// </summary>
    public partial class OpenConfig : Window
    {
        #region Variables and Properties

        /// <summary>
        /// the compute node to save and open
        /// </summary>
        private IComputeNode ComputeNode { get; set; }

        /// <summary>
        /// the open / save mode
        /// </summary>
        public bool OpenMode { get; set; }

        /// <summary>
        /// label for local button
        /// </summary>
        public string LocalLabel
        {
            get
            {
                if (OpenMode)
                    return "Open Local";
                else
                    return "Save Local";
            }
        }

        /// <summary>
        /// label for amazon button
        /// </summary>
        public string AmazonLabel
        {
            get
            {
                if (OpenMode)
                    return "Open Amazon";
                else
                    return "Save Amazon";
            }
        }

        /// <summary>
        /// label for azure button
        /// </summary>
        public string AzureLabel
        {
            get
            {
                if (OpenMode)
                    return "Open Azure";
                else
                    return "Save Azure";
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// the constructor for open mode
        /// </summary>
        /// <param name="open_mode"></param>
        public OpenConfig()
        {
            OpenMode = true;
            InitializeComponent();
        }

        /// <summary>
        /// the constructor for save mode
        /// </summary>
        /// <param name="compute_node"></param>
        public OpenConfig(IComputeNode compute_node)
        {
            OpenMode = false;
            ComputeNode = compute_node;
            InitializeComponent();
        }

        #endregion

        #region Methods

        /// <summary>
        /// handle the click and open/save mode
        /// </summary>
        /// <param name="storage"></param>
        private void HandleClick(IStorageAccess storage)
        {
            if (OpenMode)
                OpenStorage(storage);
            else
                SaveStorage(storage);
        }

        /// <summary>
        /// open from the storage interface
        /// </summary>
        /// <param name="storage"></param>
        private void OpenStorage(IStorageAccess storage)
        {
            //open file dialog
            string PathName;
            string FileName;
            if (!storage.OpenFileDialog(out PathName, out FileName))
                return;

            //read the file
            string data;
            if (!storage.ReadFile(PathName, FileName, out data))
            {
                MessageBox.Show("There was an error reading the file.");
                return;
            }
            //create the compute node and window
            IComputeNode compute_node = new ComputeNode().DeserializeComputeNode(data);
            ComputeNodeConfig dlg = new ComputeNodeConfig(compute_node);
            dlg.Show();
            Close();
        }

        /// <summary>
        /// save to the storage interface
        /// </summary>
        /// <param name="storage"></param>
        private void SaveStorage(IStorageAccess storage)
        {
            //save file dialog
            string PathName;
            string FileName;
            if (!storage.SaveFileDialog(out PathName, out FileName))
                return;

            //get the data to save
            string data = ComputeNode.SerializeComputeNode();

            //save the data
            if (!storage.SaveFile(data, PathName, FileName))
            {
                MessageBox.Show("There was an error saving the file.");
                return;
            }
            Close();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// open local clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOpenLocal_Click(object sender, RoutedEventArgs e)
        {
            IStorageAccess storage = StorageFactory.CreateStorageAccess(StorageFactory.StorageType.Local);
            HandleClick(storage);
        }

        /// <summary>
        /// open azure clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOpenAzure_Click(object sender, RoutedEventArgs e)
        {
            IStorageAccess storage = StorageFactory.CreateStorageAccess(StorageFactory.StorageType.Azure);
            HandleClick(storage);
        }

        /// <summary>
        /// open amazon clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOpenAmazon_Click(object sender, RoutedEventArgs e)
        {
            IStorageAccess storage = StorageFactory.CreateStorageAccess(StorageFactory.StorageType.Amazon);
            HandleClick(storage);
        }

        #endregion

    }
}
