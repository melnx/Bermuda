using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Bermuda.Interface.Connection.External;

namespace Bermuda.ODBC.Driver
{
    public partial class BConnectionDlg : Form
    {
        /// <summary>
        /// The connection settings to populate.
        /// </summary>
        private Dictionary<string, object> m_ConnectionSettings;

        /// <summary>
        /// the server that was used for the last catalog lookup
        /// </summary>
        string m_CatalogServer { get; set; }

        /// <summary>
        /// the server connection error
        /// </summary>
        string m_ServerConnectionError = "--Error Connection To Server--";

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="connectionSettings">Settings map to initialize GUI fields with and to
        /// populate with GUI values when the user presses OK.</param>
        /// <param name="disableOptional">Indicates that optional fields should be disabled</param>
        public BConnectionDlg(
            Dictionary<string, object> connectionSettings,
            bool disableOptional)
        {
            InitializeComponent();

            comboBoxRows.Items.Add(50);
            comboBoxRows.Items.Add(100);
            comboBoxRows.Items.Add(200);
            comboBoxRows.Items.Add(500);
            comboBoxRows.Items.Add(1000);
            comboBoxRows.Items.Add(2000);
            comboBoxRows.Items.Add(5000);
            comboBoxRows.Items.Add(10000);
            comboBoxRows.Items.Add(20000);
            comboBoxRows.Items.Add(50000);
            comboBoxRows.Items.Add(100000);

            m_ConnectionSettings = connectionSettings;
            
            if (connectionSettings.ContainsKey(Driver.B_SERVER_KEY))
            {
                object server = connectionSettings[Driver.B_SERVER_KEY];
                if (server != null)
                {
                    textBoxServer.Text = server.ToString();
                }
            }
            if (string.IsNullOrWhiteSpace(textBoxServer.Text))
                textBoxServer.Text = "net.tcp://127.255.0.0:13866/ExternalService.svc";

            if (connectionSettings.ContainsKey(Driver.B_UID_KEY))
            {
                object uid = connectionSettings[Driver.B_UID_KEY];
                if (uid != null)
                {
                    textBoxUserName.Text = uid.ToString();
                }
            }

            if (connectionSettings.ContainsKey(Driver.B_PWD_KEY))
            {
                object pwd = connectionSettings[Driver.B_PWD_KEY];
                if (pwd != null)
                {
                    textBoxPassword.Text = pwd.ToString();
                }
            }

            if (connectionSettings.ContainsKey(Driver.B_ROWS_TO_FETCH_KEY))
            {
                object rows = connectionSettings[Driver.B_ROWS_TO_FETCH_KEY];
                if (rows != null)
                {
                    int nrows = 1000;
                    int.TryParse(rows.ToString(), out nrows);
                    comboBoxRows.SelectedItem = nrows;
                }
            }

            RefreshCatalogList();

            if (disableOptional)
            {
                // There are no optional settings or GUI elements in DotNetUltraLight,
                // but if there were, the controls could be disabled here.
            }
        }

        private void BConnectionDlg_Load(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Event handler for the OK button. Updates the connection settings and
        /// closes the window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonOK_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBoxServer.Text))
            {
                MessageBox.Show("The server field is required.");
                DialogResult = System.Windows.Forms.DialogResult.None;
                return;
            }
            if (string.IsNullOrWhiteSpace(textBoxUserName.Text))
            {
                MessageBox.Show("The user name field is required.");
                DialogResult = System.Windows.Forms.DialogResult.None;
                return;
            }
            if (string.IsNullOrWhiteSpace(textBoxPassword.Text))
            {
                MessageBox.Show("The password field is required.");
                DialogResult = System.Windows.Forms.DialogResult.None;
                return;
            }
            if (comboBoxCatalog.SelectedItem == null ||
                string.IsNullOrWhiteSpace(comboBoxCatalog.SelectedItem.ToString()) ||
                comboBoxCatalog.SelectedItem.ToString() == m_ServerConnectionError)
            {
                MessageBox.Show("The catalog field is required.");
                DialogResult = System.Windows.Forms.DialogResult.None;
                return;
            }
            if (comboBoxRows.SelectedItem == null)
            {
                MessageBox.Show("The rows to fetch field is required.");
                DialogResult = System.Windows.Forms.DialogResult.None;
                return;
            }
            if ((int)comboBoxRows.SelectedItem < 50 || (int)comboBoxRows.SelectedItem > 100000)
            {
                MessageBox.Show("The rows to fetch field must be between 50 and 100,000.");
                DialogResult = System.Windows.Forms.DialogResult.None;
                return;
            }

            m_ConnectionSettings[Driver.B_SERVER_KEY] = textBoxServer.Text;
            m_ConnectionSettings[Driver.B_UID_KEY] = textBoxUserName.Text;
            m_ConnectionSettings[Driver.B_PWD_KEY] = textBoxPassword.Text;
            m_ConnectionSettings[Driver.B_CATALOG_KEY] = comboBoxCatalog.SelectedItem;
            m_ConnectionSettings[Driver.B_ROWS_TO_FETCH_KEY] = comboBoxRows.SelectedItem.ToString();
            //this.Dispose();
        }

        /// <summary>
        /// Event handler for the Cancel button. Closes the window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonCancel_Click(object sender, EventArgs e)
        {
            //this.Dispose();
        }

        /// <summary>
        /// handle refreshing the catalogs 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboBoxCatalog_DropDown(object sender, EventArgs e)
        {
            RefreshCatalogList();
        }

        /// <summary>
        /// refresh the list of catalogs
        /// </summary>
        private void RefreshCatalogList()
        {
            if (string.IsNullOrWhiteSpace(m_CatalogServer) || m_CatalogServer.ToLower() != textBoxServer.Text.ToLower())
            {
                try
                {
                    this.Cursor = Cursors.WaitCursor;

                    comboBoxCatalog.Items.Clear();

                    using (var client = ExternalServiceClient.GetClient(textBoxServer.Text))
                    {
                        //get the catalogs
                        string[] catalogs = client.GetMetadataCatalogs();

                        //copy results
                        foreach (var catalog in catalogs)
                        {
                            comboBoxCatalog.Items.Add(catalog);
                        }
                        if (m_ConnectionSettings.ContainsKey(Driver.B_CATALOG_KEY))
                        {
                            object cat = m_ConnectionSettings[Driver.B_CATALOG_KEY];
                            if (cat != null)
                            {
                                comboBoxCatalog.SelectedItem = cat.ToString();
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    comboBoxCatalog.Items.Clear();
                    comboBoxCatalog.Items.Add(m_ServerConnectionError);
                    comboBoxCatalog.SelectedItem = m_ServerConnectionError;
                }
                finally
                {
                    this.Cursor = Cursors.Default;
                }
                m_CatalogServer = textBoxServer.Text;
            }
        }
    }
}
