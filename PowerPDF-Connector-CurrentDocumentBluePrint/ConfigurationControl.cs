using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace CurrentDocumentBluePrint
{
    /// <summary>
    /// Configuration control user control for connector settings.
    /// </summary>
    public partial class ConfigurationControl : UserControl
    {
        /// <summary>
        /// Validates the data on the form.
        /// Throws an exception if the data is invalid.
        /// </summary>
        public void CheckData()
        {
            if (!System.IO.Directory.Exists(this.textBoxDirectory.Text))
            {
                MessageBox.Show("You must enter a valid directory name!");
                throw new InvalidOperationException("Invalid data");
            }
        }

        /// <summary>
        /// Saves the data from the form to the connector.
        /// </summary>
        public void UpdateData()
        {
            CheckData();
            m_conn.BaseDirectory = this.textBoxDirectory.Text;
        }

        /// <summary>
        /// Shows help for the configuration control. (No operation)
        /// </summary>
        public void ShowHelp()
        {
        }

        private Connector m_conn;
        private const string API_URL = "API_URL";
        private const string SECRET = "SECRET";

        /// <summary>
        /// Initializes a new instance of the ConfigurationControl class.
        /// </summary>
        /// <param name="conn">Parent connector.</param>
        public ConfigurationControl(Connector conn)
        {
            InitializeComponent();
            StoreSheet();
            m_conn = conn;
            this.textBoxDirectory.Text = m_conn.BaseDirectory;
        }

        [Flags]
        public enum WS_STYLE : int
        {
            WS_CHILD = 0x40000000,
        }

        [Flags]
        public enum DS_STYLE : int
        {
            DS_CONTROL = 0x0400,
        }

        [Flags]
        public enum WS_EX_STYLE : int
        {
            WS_EX_CONTROLPARENT = 0x00010000,
        }

        /// <summary>
        /// Gets the CreateParams for the control to appear inside the property dialog.
        /// </summary>
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams p = base.CreateParams;
                p.Style = (int)(WS_STYLE.WS_CHILD) | (int)(DS_STYLE.DS_CONTROL);
                p.ExStyle = (int)(WS_EX_STYLE.WS_EX_CONTROLPARENT);
                return p;
            }
        }

        /// <summary>
        /// Called when the window handle is destroyed.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected override void OnHandleDestroyed(EventArgs e)
        {
            ClearSheet();
            base.OnHandleDestroyed(e);
        }

        protected static Dictionary<IntPtr, ConfigurationControl> sheetList = new Dictionary<IntPtr, ConfigurationControl>();

        /// <summary>
        /// Stores the form in the sheet list.
        /// </summary>
        protected void StoreSheet()
        {
            if (!sheetList.ContainsKey(this.Handle))
                sheetList.Add(this.Handle, this);
        }

        /// <summary>
        /// Removes the form from the sheet list.
        /// </summary>
        protected void ClearSheet()
        {
            if (sheetList.ContainsKey(this.Handle))
                sheetList.Remove(this.Handle);
        }

        /// <summary>
        /// Gets the ConfigurationControl instance by window handle.
        /// </summary>
        /// <param name="hwnd">Window handle.</param>
        /// <returns>ConfigurationControl instance.</returns>
        public static ConfigurationControl GetSheet(IntPtr hwnd)
        {
            ConfigurationControl retval = null;
            sheetList.TryGetValue(hwnd, out retval);
            return retval;
        }

        /// <summary>
        /// Gets the ConfigurationControl instance by window handle (int).
        /// </summary>
        /// <param name="hWnd">Window handle as int.</param>
        /// <returns>ConfigurationControl instance.</returns>
        public static ConfigurationControl GetSheet(int hWnd)
        {
            return GetSheet((IntPtr)hWnd);
        }

        /// <summary>
        /// Handles the Save button click event, saving encrypted settings.
        /// </summary>
        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveEncryptedSetting(API_URL, txtAPIURL.Text.Trim());
            SaveEncryptedSetting(SECRET, txtSecret.Text.Trim());
            MessageBox.Show("Settings saved successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Saves an encrypted setting to the registry.
        /// </summary>
        /// <param name="key">Registry key name.</param>
        /// <param name="value">Value to encrypt and save.</param>
        private void SaveEncryptedSetting(string key, string value)
        {
            byte[] encrypted = ProtectedData.Protect(Encoding.UTF8.GetBytes(value), null, DataProtectionScope.CurrentUser);
            string base64 = Convert.ToBase64String(encrypted);

            using (RegistryKey rk = Registry.CurrentUser.CreateSubKey(@"Software\ScanSoft\Connectors\CurrentDocumentBlueprint"))
            {
                rk.SetValue(key, base64, RegistryValueKind.String);
            }
        }

        /// <summary>
        /// Loads an encrypted setting from the registry.
        /// </summary>
        /// <param name="key">Registry key name.</param>
        /// <returns>Decrypted setting value.</returns>
        private string LoadEncryptedSetting(string key)
        {
            using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"Software\ScanSoft\Connectors\CurrentDocumentBlueprint"))
            {
                if (rk == null)
                    return "";

                string base64 = rk.GetValue(key, "") as string;

                if (string.IsNullOrEmpty(base64))
                    return "";

                try
                {
                    byte[] encrypted = Convert.FromBase64String(base64);
                    byte[] decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                    return Encoding.UTF8.GetString(decrypted);
                }
                catch
                {
                    return "";
                }
            }
        }

        /// <summary>
        /// Handles the Load event for the configuration control, loading encrypted fields.
        /// </summary>
        private void ConfigurationControl_Load(object sender, EventArgs e)
        {
            try
            {
                txtAPIURL.Text = LoadEncryptedSetting(API_URL);
                txtSecret.Text = LoadEncryptedSetting(SECRET);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load Fraud Connector " + ex.Message);
            }
        }
    }
}