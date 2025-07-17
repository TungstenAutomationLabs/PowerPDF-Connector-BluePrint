using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace CurrentDocumentBluePrint
{
    public partial class PropertySheet2 : UserControl
    {
        #region IPropertyPageHandler functions

        public void CheckData()
        {
            // validate the data on the form
            // throw COMException if there's invalid data
            // the property sheet will remain active and the dialog
            // will not be closed until the data is valid
            if (!System.IO.Directory.Exists(this.textBoxDirectory.Text))
            {
                MessageBox.Show("You must enter a valid directory name!");
                throw new InvalidOperationException("Invalid data");
            }
        }

        public void UpdateData()
        {
            // save the data from the form
            CheckData();
            m_conn.BaseDirectory = this.textBoxDirectory.Text;
        }

        public void ShowHelp()
        {
            // do nothing
        }

        #endregion IPropertyPageHandler functions

        private Connector m_conn;    // parent connector
        private const string API_URL = "API_URL";
        private const string SECRET = "SECRET";

        public PropertySheet2(Connector conn)
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

        // need to override CreateParams to have our form appear inside
        // the property dialog
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

        // This virtual fn is called when the window handle (HWND) is
        // destroyed.
        protected override void OnHandleDestroyed(EventArgs e)
        {
            ClearSheet();
            base.OnHandleDestroyed(e);
        }

        #region Sheet Store functions

        // dictionary for storing HWND - Form associations
        protected static Dictionary<IntPtr, PropertySheet2> sheetList =
                                                new Dictionary<IntPtr, PropertySheet2>();

        // store the form in the sheet list
        protected void StoreSheet()
        {
            if (!sheetList.ContainsKey(this.Handle))
                sheetList.Add(this.Handle, this);
        }

        // clear the form from the sheet list
        protected void ClearSheet()
        {
            if (sheetList.ContainsKey(this.Handle))
                sheetList.Remove(this.Handle);
        }

        // return the form from the sheet list by HWND
        public static PropertySheet2 GetSheet(IntPtr hwnd)
        {
            PropertySheet2 retval = null;
            sheetList.TryGetValue(hwnd, out retval);
            return retval;
        }

        public static PropertySheet2 GetSheet(int hWnd)
        {
            return GetSheet((IntPtr)hWnd);
        }

        #endregion Sheet Store functions

        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveEncryptedSetting(API_URL, txtAPIURL.Text.Trim());
            SaveEncryptedSetting(SECRET, txtSecret.Text.Trim());
            MessageBox.Show("Settings saved successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void SaveEncryptedSetting(string key, string value)
        {
            byte[] encrypted = ProtectedData.Protect(Encoding.UTF8.GetBytes(value), null, DataProtectionScope.CurrentUser);
            string base64 = Convert.ToBase64String(encrypted);

            using (RegistryKey rk = Registry.CurrentUser.CreateSubKey(@"Software\ScanSoft\Connectors\PPDFFraudConnector"))
            {
                rk.SetValue(key, base64, RegistryValueKind.String);
            }
        }

        private string LoadEncryptedSetting(string key)
        {
            using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"Software\ScanSoft\Connectors\PPDFFraudConnector"))
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

        private void PropertySheet2_Load(object sender, EventArgs e)
        {
            try
            {
                // Load encrypted fields
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