using DMSConnector;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace CurrentDocumentBluePrint
{
    /// <summary>
    /// Contains error code constants for the application.
    /// </summary>
    public abstract class ERRORS
    {
        /// <summary>
        /// Error code for cancelled operations.
        /// </summary>
        public const int E_CANCELLED = -2147221492;
    }

    /// <summary>
    /// Implements the DMSConnector and property page handler interfaces for document management.
    /// </summary>
    [ClassInterface(ClassInterfaceType.None)]
    [ComVisible(true)]
    [GuidAttribute("219588E2-A4B6-4A7A-9044-BDCBDE05566E")]
    [ProgId("CurrentDocumentBluePrint.Connector")]
    public class Connector : IDMSConnector, IPropertyPageHandler
    {
        private static string strProgId = ((ProgIdAttribute)(typeof(Connector).GetCustomAttributes(typeof(ProgIdAttribute), true).First())).Value;

        /// <summary>
        /// Registers this connector under HKLM\Software\Scansoft\Connectors.
        /// </summary>
        /// <param name="t">Type to register.</param>
        [ComRegisterFunctionAttribute]
        public static void RegisterAsConnector(Type t)
        {
            RegistryKey rkSoftware = OpenOrCreateSubkey(Registry.LocalMachine, "Software");
            RegistryKey rkScansoft = OpenOrCreateSubkey(rkSoftware, "ScanSoft");
            RegistryKey rkConnectors = OpenOrCreateSubkey(rkScansoft, "Connectors");
            RegistryKey rkThisOne = OpenOrCreateSubkey(rkConnectors, GetProgId(t));
        }

        /// <summary>
        /// Unregisters this connector from HKLM\Software\Scansoft\Connectors.
        /// </summary>
        /// <param name="t">Type to unregister.</param>
        [ComUnregisterFunctionAttribute]
        public static void UnregisterAsConnector(Type t)
        {
            RegistryKey rk = Registry.LocalMachine;
            rk.DeleteSubKey(@"Software\Scansoft\Connectors\" + GetProgId(t));
        }

        /// <summary>
        /// Opens or creates a registry subkey.
        /// </summary>
        /// <param name="parent">Parent registry key.</param>
        /// <param name="name">Subkey name.</param>
        /// <returns>The opened or created RegistryKey.</returns>
        private static RegistryKey OpenOrCreateSubkey(RegistryKey parent, string name)
        {
            RegistryKey rk = parent.OpenSubKey(name, true);

            if (rk == null)
            {
                rk = parent.CreateSubKey(name);
                if (rk == null)
                {
                    throw new COMException("Can't register connector!");
                }
            }
            return rk;
        }

        /// <summary>
        /// Gets the ProgId attribute value for a given type.
        /// </summary>
        /// <param name="t">Type to get ProgId for.</param>
        /// <returns>ProgId string.</returns>
        private static string GetProgId(Type t)
        {
            return ((ProgIdAttribute)t.GetCustomAttributes(typeof(ProgIdAttribute), true).First()).Value;
        }

        protected string _connectorName = "CurrentDocumentBluePrint";
        protected string _languageCode;
        protected bool _initialized = false;
        protected IntPtr _parentWindow = (IntPtr)0;
        protected MenuItemList _menuItems;
        protected string _baseDirectory;
        protected Dictionary<string, Document> _documents = new Dictionary<string, Document>();
        private const string API_URL = "API_URL";
        private const string SECRET = "SECRET";
        protected static string _defaultBaseDirectory = @"c:\";

        /// <summary>
        /// Initializes a new instance of the Connector class.
        /// </summary>
        public Connector()
        {
        }

        /// <summary>
        /// Returns the number of menu items and the menu title.
        /// </summary>
        void IDMSConnector.MenuGetNumberOfItems(out int num, out string title)
        {
            num = _menuItems.Count;
            title = _connectorName;
        }

        /// <summary>
        /// Returns the definition of a menu item.
        /// </summary>
        void IDMSConnector.MenuGetMenuItem(int num, out int menuItemId, out string text,
            out string tooltip, out bool isPartOfToolbar, out CallbackType cbType,
            out int hIconBig, out int hIconSmall, out bool enabledWithoutDoc)
        {
            if (num < 0 || num >= _menuItems.Count)
            {
                throw new ArgumentOutOfRangeException("num");
            }

            MenuItem item = _menuItems[num];

            menuItemId = item.menuItemId;
            text = item.text;
            tooltip = item.tooltip;
            isPartOfToolbar = item.isPartOfToolbar;
            cbType = item.cbType;
            hIconBig = item.hIconBig.ToInt32();
            hIconSmall = item.hIconSmall.ToInt32();
            enabledWithoutDoc = item.enabledWithoutDoc;
        }

        /// <summary>
        /// Returns whether a menu item is enabled for a given document.
        /// </summary>
        bool IDMSConnector.MenuGetItemState(int menuItemId, string docId)
        {
            return true;
        }

        /// <summary>
        /// Executes the action for a menu item.
        /// </summary>
        void IDMSConnector.MenuAction(int menuItemId, string docId)
        {
            if (menuItemId == (int)ItemId.TryMe)
            {
                string apiURL = LoadEncryptedSetting(API_URL);
                string secret = LoadEncryptedSetting(SECRET);
                if (string.IsNullOrEmpty(apiURL) || string.IsNullOrEmpty(secret))
                {
                    MessageBox.Show("Please configure API details in the settings", "Settings Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show("API URL: " + apiURL + "\nSecret: " + secret, "It Works", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
                throw new NotImplementedException();
        }

        /// <summary>
        /// Loads an encrypted setting from the registry.
        /// </summary>
        /// <param name="key">Registry key name.</param>
        /// <returns>Decrypted setting value.</returns>
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

        /// <summary>
        /// Gets the base directory from the registry.
        /// </summary>
        /// <returns>Base directory path.</returns>
        public string GetBaseDirFromRegistry()
        {
            try
            {
                RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"Software\ScanSoft\Connectors\" +
                                            GetProgId(this.GetType()));
                if (rk != null)
                {
                    Object value = rk.GetValue("BaseDirectory", _defaultBaseDirectory);
                    return value.ToString();
                }
            }
            catch (Exception)
            {
            }
            return _defaultBaseDirectory;
        }

        /// <summary>
        /// Saves the base directory to the registry.
        /// </summary>
        protected void SaveBaseDirToRegistry()
        {
            RegistryKey rkSoftware = OpenOrCreateSubkey(Registry.CurrentUser, "Software");
            RegistryKey rkScansoft = OpenOrCreateSubkey(rkSoftware, "ScanSoft");
            RegistryKey rkConnectors = OpenOrCreateSubkey(rkScansoft, "Connectors");
            RegistryKey rkThisOne = OpenOrCreateSubkey(rkConnectors, GetProgId(this.GetType()));
            rkThisOne.SetValue("BaseDirectory", (object)_baseDirectory, RegistryValueKind.String);
        }

        /// <summary>
        /// Gets or sets the base directory for the connector.
        /// </summary>
        public string BaseDirectory
        {
            get { return _baseDirectory; }
            set { _baseDirectory = value; SaveBaseDirToRegistry(); }
        }

        /// <summary>
        /// Initializes the connector with the application and language code.
        /// </summary>
        void IDMSConnector.Init(object application, string LangCode)
        {
            if (!_initialized)
            {
                _languageCode = LangCode;

                CultureInfo cultureInfo = Langs.Iso639_3ToCulture(_languageCode);
                if (!cultureInfo.IsNeutralCulture)
                    Thread.CurrentThread.CurrentCulture = cultureInfo;

                Thread.CurrentThread.CurrentUICulture = cultureInfo;

                _menuItems = MenuItemList.Create();

                _baseDirectory = GetBaseDirFromRegistry();

                _initialized = true;
            }
        }

        /// <summary>
        /// Shuts down the connector and releases resources.
        /// </summary>
        void IDMSConnector.Shutdown()
        {
            if (_initialized)
            {
                foreach (Document doc in _documents.Values)
                {
                    doc.Close();
                }
                _documents.Clear();

                _initialized = false;
            }
        }

        /// <summary>
        /// Gets the connector name.
        /// </summary>
        string IDMSConnector.ConnectorName
        {
            get { return _connectorName; }
        }

        /// <summary>
        /// Sets the parent window handle.
        /// </summary>
        int IDMSConnector.ParentWindow
        {
            set { _parentWindow = (IntPtr)value; }
        }

        /// <summary>
        /// Adds a new document to the connector.
        /// </summary>
        string IDMSConnector.DocAddNew(string sourceFile, string title, string[] docProperties)
        {
            Document doc = Document.CreateNewDocument(_parentWindow, BaseDirectory, sourceFile, title);
            if (doc != null)
            {
                _documents.Add(doc.UniqueId, doc);
                return doc.UniqueId;
            }
            return null;
        }

        /// <summary>
        /// Gets the local file name for a document.
        /// </summary>
        string IDMSConnector.DocGetLocalFile(string docId)
        {
            Document doc = _documents[docId];
            return doc.LocalFileName;
        }

        /// <summary>
        /// Gets the open mode for a document.
        /// </summary>
        OpenMode IDMSConnector.DocGetOpenMode(string docId)
        {
            Document doc = _documents[docId];
            return doc.OpenMode;
        }

        /// <summary>
        /// Gets the title for a document.
        /// </summary>
        string IDMSConnector.DocGetTitle(string docId)
        {
            Document doc = _documents[docId];
            return doc.Title;
        }

        /// <summary>
        /// Called when a document is modified.
        /// </summary>
        void IDMSConnector.DocModified(string docId)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Closes a document and removes it from the connector.
        /// </summary>
        void IDMSConnector.DocClose(string docId, DMSConnector.CloseReason reason)
        {
            Document doc = _documents[docId];
            doc.Close();
            _documents.Remove(docId);
        }

        /// <summary>
        /// Opens a document with the specified mode.
        /// </summary>
        void IDMSConnector.DocOpen(string docId, OpenMode mode)
        {
            Document doc = _documents[docId];
            doc.Open(mode);
        }

        /// <summary>
        /// Prepares a document for saving (Save As operation).
        /// </summary>
        void IDMSConnector.DocPrepareSave(string docId, int menuItemId, string[] docProperties, out string targetFileName)
        {
            Document doc = _documents[docId];
            doc.PrepareSave(_parentWindow, BaseDirectory);
            targetFileName = Path.GetTempFileName();
        }

        /// <summary>
        /// Saves a document to the specified file name.
        /// </summary>
        void IDMSConnector.DocSave(string docId, string targetFileName, out string newDocId)
        {
            Document doc = _documents[docId];
            Document newDoc = doc.Save(targetFileName);
            if (newDoc != null)
            {
                _documents.Add(newDoc.UniqueId, newDoc);
                newDocId = newDoc.UniqueId;
            }
            else
            {
                newDocId = null;
            }
        }

        /// <summary>
        /// Prompts the user to select files and adds them as documents.
        /// </summary>
        void IDMSConnector.DocSelectFiles(DMSConnector.SelectType type, int MenuIndex, out string[] docIds)
        {
            bool bMultiSelect = (type & SelectType.SELECT_SINGLE_FILE) == 0;
            Document[] docs = Document.SelectFiles(_parentWindow, BaseDirectory, bMultiSelect);

            List<string> uniqueIds = new List<string>();
            foreach (Document doc in docs)
            {
                _documents.Add(doc.UniqueId, doc);
                uniqueIds.Add(doc.UniqueId);
            }

            docIds = uniqueIds.ToArray();
        }

        /// <summary>
        /// Determines if a file is from DMS. Not implemented.
        /// </summary>
        string IDMSConnector.FileIsFromDms(string localFile)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the property page handler for the connector.
        /// </summary>
        IPropertyPageHandler IDMSConnector.PropertyPageHandler
        {
            get { return this; }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        /// <summary>
        /// Creates the property sheet and sets its parent window.
        /// </summary>
        int IPropertyPageHandler.Create(int parenthWnd)
        {
            PropertySheet2 p = new PropertySheet2(this);
            SetParent(p.Handle, (IntPtr)parenthWnd);
            return (int)p.Handle.ToInt32();
        }

        /// <summary>
        /// Shows help for the property sheet.
        /// </summary>
        void IPropertyPageHandler.ShowHelp(int hWnd)
        {
            PropertySheet2 p = PropertySheet2.GetSheet(hWnd);
            if (p != null)
                p.ShowHelp();
        }

        /// <summary>
        /// Checks the data in the property sheet.
        /// </summary>
        void IPropertyPageHandler.CheckData(int hWnd)
        {
            PropertySheet2 p = PropertySheet2.GetSheet(hWnd);
            if (p != null)
                p.CheckData();
        }

        /// <summary>
        /// Updates the data in the property sheet.
        /// </summary>
        void IPropertyPageHandler.UpdateData(int hWnd)
        {
            PropertySheet2 p = PropertySheet2.GetSheet(hWnd);
            if (p != null)
                p.UpdateData();
        }
    }
}