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
using System.Collections.ObjectModel;
using Amazon.S3.Model;
using Amazon.S3;
using System.ComponentModel;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace Bermuda.BermudaConfig.Storage
{
    /// <summary>
    /// Interaction logic for CloudStorageBrowser.xaml
    /// </summary>
    public partial class CloudStorageBrowser : Window, INotifyPropertyChanged
    {
        #region Variables and Properties

        /// <summary>
        /// the interface for browsing cloud storage
        /// </summary>
        private ICloudStorageBrowser CloudStorage { get; set; }

        /// <summary>
        /// the buckets or containers
        /// </summary>
        public ObservableCollection<string> Buckets { get; set; }

        /// <summary>
        /// the files for a bucket
        /// </summary>
        public ObservableCollection<string> Files { get; set; }

        /// <summary>
        /// the open mode indicates if this is an open or save
        /// </summary>
        public bool OpenMode {get;set;}

        /// <summary>
        /// Selected bucket for binding
        /// </summary>
        private string _SelectedBucket { get; set; }
        public string SelectedBucket
        {
            get
            {
                return _SelectedBucket;
            }
            set
            {
                _SelectedBucket = value;
                NotifyPropertyChanged("SelectedBucket");
                NotifyPropertyChanged("ValidBucketSelection");
            }
        }

        /// <summary>
        /// Selected file for binding
        /// </summary>
        private string _SelectedFile { get; set; }
        public string SelectedFile
        {
            get
            {
                return _SelectedFile;
            }
            set
            {
                _SelectedFile = value;
                NotifyPropertyChanged("SelectedFile");
                NotifyPropertyChanged("ValidFileSelection");
            }
        }

        /// <summary>
        /// the label of the open button depending on open mode or save mode
        /// </summary>
        public string OpenButtonLabel
        {
            get
            {
                return OpenMode ? "Open" : "Save";
            }
        }

        /// <summary>
        /// the bucket selection is valid
        /// </summary>
        public bool ValidBucketSelection
        {
            get
            {
                return !string.IsNullOrWhiteSpace(SelectedBucket);
            }
        }

        /// <summary>
        /// the file selection is valid
        /// </summary>
        public bool ValidFileSelection
        {
            get
            {
                return !string.IsNullOrWhiteSpace(SelectedFile);
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// constructor with storage interface and open mode
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="cloud_storage"></param>
        public CloudStorageBrowser(bool mode, ICloudStorageBrowser cloud_storage)
        {
            OpenMode = mode;
            CloudStorage = cloud_storage;

            Files = new ObservableCollection<string>();
            Buckets = new ObservableCollection<string>();

            RefreshBuckets();

            Title = CloudStorage.BrowserTitle;

            InitializeComponent();
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

        #region Methods

        /// <summary>
        /// refresh the bucket list
        /// </summary>
        void RefreshBuckets()
        {
            //init buckets and files
            SelectedBucket = "";
            SelectedFile = "";
            Buckets.Clear();
            Files.Clear();

            //get the buckets
            IEnumerable<string> buckets;
            if (!CloudStorage.GetBuckets(out buckets))
            {
                MessageBox.Show("There was an error getting buckets.");
                return;
            }
            //copy into our list
            buckets.ToList().ForEach(b => Buckets.Add(b));
        }

        /// <summary>
        /// refresh the list of files
        /// </summary>
        void RefreshFiles()
        {
            //init the files
            SelectedFile = "";
            Files.Clear();

            //get the selected bucket
            if (lbBuckets.SelectedItem == null) return;
            string bucket = lbBuckets.SelectedItem.ToString();

            //get the files
            IEnumerable<string> files;
            if (!CloudStorage.GetFiles(bucket, out files))
            {
                MessageBox.Show("There was a error getting files for bucket: " + bucket);
                return;
            }
            //get the files
            files.ToList().ForEach(f => Files.Add(f));
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
            ButtonAutomationPeer peer = new ButtonAutomationPeer(btOpen);

            IInvokeProvider invokeProv =
              peer.GetPattern(PatternInterface.Invoke)
              as IInvokeProvider;

            invokeProv.Invoke();
        }

        /// <summary>
        /// on selection change of buckets refresh the file list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbBuckets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //refresh the files
            RefreshFiles();
        }

        /// <summary>
        /// the open or save button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btOpen_Click(object sender, RoutedEventArgs e)
        {
            //make sure we have valid bucket selection
            if (string.IsNullOrWhiteSpace(SelectedBucket))
            {
                MessageBox.Show("You must select a bucket.");
                return;
            }
            //check our mode
            if (OpenMode)
            {
                //make sure we have valid file selection
                if (string.IsNullOrWhiteSpace(SelectedBucket))
                {
                    MessageBox.Show("You must select a file to open.");
                    return;
                }
            }
            else
            {
                //check if we have a file specified
                if (string.IsNullOrWhiteSpace(SelectedFile))
                {
                    MessageBox.Show("You must select a file to save.");
                    return;
                }
                //check if file already exists
                if (Files.Any(f => f.Equals(SelectedFile, StringComparison.InvariantCultureIgnoreCase)))
                {
                    //prompt to overwrite
                    if (MessageBox.Show("The selected file will be overwritten. Are you sure?", "Save File", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }
            }
            //close dlg
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// update the selection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (e.AddedItems.Count == 0) return;
            //var file = e.AddedItems[0] as string;
            //SelectedFile = file;
        }

        /// <summary>
        /// create a new bucket in cloud
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btNewBucket_Click(object sender, RoutedEventArgs e)
        {
            //create the bucket
            InputWindow dlg = new InputWindow("New Bucket", "Enter the new bucket name.");
            var ret = dlg.ShowDialog();
            if (!ret.HasValue || ret.Value == false)
                return;

            //check the return
            string name = dlg.Questions[0].Answer;
            if (name.IndexOf(' ') != -1)
            {
                MessageBox.Show("The new bucket name can not contain spaces.");
                return;
            }
            else if (Buckets.Any(b => b.Equals(name, StringComparison.InvariantCultureIgnoreCase)))
            {
                MessageBox.Show("There is already a bucket with the name: " + name);
                return;
            }

            //create new directory
            if (!CloudStorage.NewDirectory(name))
            {
                MessageBox.Show("There was an error creating the bucket.");
                return;
            }
            //refresh the bucket list
            RefreshBuckets();
        }

        /// <summary>
        /// delete a bucket
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btDeleteBucket_Click(object sender, RoutedEventArgs e)
        {
            //make sure bucket is selected
            if (!string.IsNullOrWhiteSpace(SelectedBucket))
            {
                //prompt to delete
                if (MessageBox.Show(
                    string.Format("Are you sure you wish to delete the bucket: {0}?", SelectedBucket),
                    "Delete Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;

                //delete the bucket
                if (!CloudStorage.DeleteDirectory(SelectedBucket))
                {
                    MessageBox.Show("There was an error deleting the bucket: " + SelectedBucket);
                    return;
                }
                //refresh the bucket list
                RefreshBuckets();
            }
        }

        /// <summary>
        /// handle the cancel click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// refresh the buckets
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btRefreshBuckets_Click(object sender, RoutedEventArgs e)
        {
            RefreshBuckets();
        }

        /// <summary>
        /// refresh the files
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btRefreshFiles_Click(object sender, RoutedEventArgs e)
        {
            RefreshFiles();
        }

        /// <summary>
        /// delete the file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btDeleteFile_Click(object sender, RoutedEventArgs e)
        {
            //make sure file is selected
            if (!string.IsNullOrWhiteSpace(SelectedFile) && !string.IsNullOrWhiteSpace(SelectedBucket))
            {
                //prompt to delete
                if (MessageBox.Show(
                    string.Format("Are you sure you wish to delete the file: {0}?", SelectedFile),
                    "Delete Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;

                //delete the file
                if (!CloudStorage.DeleteFile(SelectedBucket, SelectedFile))
                {
                    MessageBox.Show("There was an error deleting the file: " + SelectedFile);
                    return;
                }
                //refresh the bucket list
                RefreshFiles();
            }
        }

        #endregion

        

        

        

        

        

        

        
        

        

        

        

        

        
    }
}
