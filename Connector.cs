using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using System.Drawing;
using Microsoft.Win32;
using System.Globalization;
using System.Threading;
using DMSConnector;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace CurrentDocumentBluePrint
{
    public abstract class ERRORS
    {
        public const int E_CANCELLED = -2147221492;
    }

    [ClassInterface(ClassInterfaceType.None)]
    [ComVisible(true)]
    [GuidAttribute("219588E2-A4B6-4A7A-9044-BDCBDE05566E")]
    [ProgId("CurrentDocumentBluePrint.Connector")]
    public class Connector :  IDMSConnector, 
                              IPropertyPageHandler  // this class implements IPropertyPageHandler itself

    {
        #region COM registration

        static string strProgId = ((ProgIdAttribute)(typeof(Connector).GetCustomAttributes(typeof(ProgIdAttribute), true).First())).Value;

        // register this connector under "HKLM\Software\Scansoft\Connectors"
        [ComRegisterFunctionAttribute]
        public static void RegisterAsConnector(Type t)
        {
            RegistryKey rkSoftware = OpenOrCreateSubkey(Registry.LocalMachine, "Software");
            RegistryKey rkScansoft = OpenOrCreateSubkey(rkSoftware, "ScanSoft");
            RegistryKey rkConnectors = OpenOrCreateSubkey(rkScansoft, "Connectors");
            RegistryKey rkThisOne = OpenOrCreateSubkey(rkConnectors, GetProgId(t));
        }

        // unregister this connector from under "HKLM\Software\Scansoft\Connectors"
        [ComUnregisterFunctionAttribute]
        public static void UnregisterAsConnector(Type t)
        {
            RegistryKey rk = Registry.LocalMachine;
            rk.DeleteSubKey(@"Software\Scansoft\Connectors\" + GetProgId(t));
        }

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

        // return the ProgId attribute of a type
        private static string GetProgId(Type t)
        {
            return ((ProgIdAttribute)t.GetCustomAttributes(typeof(ProgIdAttribute), true).First()).Value;
        }
        #endregion COM registration

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

        #region Constructor / Dispose
        public Connector()
        {
        }

        #endregion Constructor / Dispose

        #region Menu functions

        // returns the number of menu items and the title of the menu
        void IDMSConnector.MenuGetNumberOfItems(out int num, out string title)
        {
            num = _menuItems.Count;
            title = _connectorName;
        }

        // returns the definition of one menu item
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

        // called every time the menu is displayed or the state of the toolbar buttons has to be updated
        // returns true if the menu item for the given docId is enabled (if docId is null or empty, then
        // the document is not from this connector)
        bool IDMSConnector.MenuGetItemState(int menuItemId, string docId)
        {
            // by default every menu item is enabled for every document
            return true;
        }
        
        // would be called for menu items with MenuItem callback type
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
                    // Here you would implement the logic for the "Try Me" action
                    // For example, you could open a dialog or perform some operation
                    MessageBox.Show("API URL: " + apiURL + "\nSecret: " + secret, "It Works", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

            }
            else
                // we don't have any such menu item by default
                throw new NotImplementedException();
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
        #endregion Menu functions

        public string GetBaseDirFromRegistry()
        {
            try
            {
                RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"Software\ScanSoft\Connectors\" + 
                                            GetProgId(this.GetType()));
                if ( rk != null )
                {
                    Object value = rk.GetValue("BaseDirectory", _defaultBaseDirectory);
                    return value.ToString();
                }
            }
            catch(Exception)
            {
            }
            return _defaultBaseDirectory;
        }
        protected void SaveBaseDirToRegistry()
        {
            RegistryKey rkSoftware = OpenOrCreateSubkey(Registry.CurrentUser, "Software");
            RegistryKey rkScansoft = OpenOrCreateSubkey(rkSoftware, "ScanSoft");
            RegistryKey rkConnectors = OpenOrCreateSubkey(rkScansoft, "Connectors");
            RegistryKey rkThisOne = OpenOrCreateSubkey(rkConnectors, GetProgId(this.GetType()));
            rkThisOne.SetValue("BaseDirectory", (object)_baseDirectory, RegistryValueKind.String);
        }
        public string BaseDirectory
        {
            get { return _baseDirectory; }
            set { _baseDirectory = value; SaveBaseDirToRegistry(); }
        }

        #region IDMSConnector implementation

        #region Init, Shutdown, etc.

        void IDMSConnector.Init(object application, string LangCode)
        {
            if (!_initialized)
            {
                _languageCode = LangCode;

                // switch the current culture based on the language
                CultureInfo cultureInfo = Langs.Iso639_3ToCulture(_languageCode);
                if ( ! cultureInfo.IsNeutralCulture )
                    Thread.CurrentThread.CurrentCulture = cultureInfo;

                Thread.CurrentThread.CurrentUICulture = cultureInfo;

                _menuItems = MenuItemList.Create();

                _baseDirectory = GetBaseDirFromRegistry();

                _initialized = true;
            }
        }

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

        string IDMSConnector.ConnectorName
        {
            get { return _connectorName; }
        }

        int IDMSConnector.ParentWindow
        {
            set { _parentWindow = (IntPtr)value; }
        }

        #endregion Init / Shutdown


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

        

        string IDMSConnector.DocGetLocalFile(string docId)
        {
            Document doc = _documents[docId];
            return doc.LocalFileName;
        }

        OpenMode IDMSConnector.DocGetOpenMode(string docId)
        {
            Document doc = _documents[docId];
            return doc.OpenMode;
        }

        string IDMSConnector.DocGetTitle(string docId)
        {
            Document doc = _documents[docId];
            return doc.Title;
        }

        void IDMSConnector.DocModified(string docId)
        {
            // not used now
            // can be used to enable/disable different menu items
            // if the document was modified inside the application
            throw new NotImplementedException();
        }
        void IDMSConnector.DocClose(string docId, DMSConnector.CloseReason reason)
        {
            Document doc = _documents[docId];
            doc.Close();
            _documents.Remove(docId);
        }
        void IDMSConnector.DocOpen(string docId, OpenMode mode)
        {
            Document doc = _documents[docId];
            doc.Open(mode);
        }

        // this is a Save As... operation
        void IDMSConnector.DocPrepareSave(string docId, int menuItemId, string[] docProperties, out string targetFileName)
        {
            Document doc = _documents[docId];
            doc.PrepareSave(_parentWindow, BaseDirectory);
            targetFileName = Path.GetTempFileName();
        }

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

        string IDMSConnector.FileIsFromDms(string localFile)
        {
            // not used here
            // it could be used for example to launch the application with a local file
            // which was downloaded from a DMS, and then after opening that file
            // associate it with the correct Connector
            throw new NotImplementedException();
        }

        IPropertyPageHandler IDMSConnector.PropertyPageHandler
        {
            // this connector class implements IPropertyPageHandler itself
            get { return this; }
        }

        #endregion

        #region IPropertyPageHandler Members


        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        int IPropertyPageHandler.Create(int parenthWnd)
        {
            PropertySheet2 p = new PropertySheet2(this);
            SetParent(p.Handle, (IntPtr)parenthWnd);
            return (int)p.Handle.ToInt32();
        }

        void IPropertyPageHandler.ShowHelp(int hWnd)
        {
            PropertySheet2 p = PropertySheet2.GetSheet(hWnd);
            if (p != null)
                p.ShowHelp();
        }

        void IPropertyPageHandler.CheckData(int hWnd)
        {
            PropertySheet2 p = PropertySheet2.GetSheet(hWnd);
            if (p != null)
                p.CheckData();
        }

        void IPropertyPageHandler.UpdateData(int hWnd)
        {
            PropertySheet2 p = PropertySheet2.GetSheet(hWnd);
            if (p != null)
                p.UpdateData();
        }

        #endregion
    }
}
