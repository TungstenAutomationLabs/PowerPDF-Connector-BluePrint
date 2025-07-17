using DMSConnector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CurrentDocumentBluePrint
{
    [Flags]
    public enum DOCSTATE
    {
        DEFAULT = 0,
        FROMDMS = 1,
        MODIFIED = 2,
        CLOSED = 4,
    };

    public class WindowWrapper : System.Windows.Forms.IWin32Window
    {
        public WindowWrapper(IntPtr ip)
        {
            Handle = ip;
        }

        public IntPtr Handle
        {
            get;
            private set;
        }
    }

    public class Document
    {
        // unique Id used for the document Id
        // (this might not be the best method to obtain a document Id for every Connector)
        protected string _uniqueId = Guid.NewGuid().ToString();

        protected string _localFileName;
        protected string _filePath;
        protected string _saveToPath;
        protected DOCSTATE _state = DOCSTATE.DEFAULT;
        protected string _title;
        protected OpenMode _openMode = OpenMode.OPEN_NONE;

        public string UniqueId
        { get { return _uniqueId; } }
        public string LocalFileName
        { get { return _localFileName; } }
        public string Title
        { get { return _title; } }
        public OpenMode OpenMode
        { get { return _openMode; } }

        public void Open(OpenMode mode)
        {
            _localFileName = Path.ChangeExtension(Path.GetTempFileName(), Path.GetExtension(_filePath));
            File.Copy(_filePath, _localFileName, true);
            _openMode = mode;
        }

        public void Close()
        {
            if (!String.IsNullOrEmpty(_localFileName))
            {
                File.Delete(_localFileName);
            }
            _localFileName = null;
            _state |= DOCSTATE.CLOSED;
            _openMode = OpenMode.OPEN_NONE;
        }

        public void PrepareSave(IntPtr hwnd, string baseDir)
        {
            WindowWrapper parentWindow = new WindowWrapper(hwnd);

            // Save As
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.DefaultExt = Path.GetExtension(_filePath);
            dlg.RestoreDirectory = true;
            dlg.OverwritePrompt = true;
            dlg.Title = "Save " + Path.GetFileName(_filePath) + " to";
            dlg.Filter = "All Files (*.*)|*.*";
            if (!String.IsNullOrEmpty(dlg.DefaultExt))
            {
                dlg.Filter = "*." + dlg.DefaultExt + "|*" + dlg.DefaultExt + "|" + dlg.Filter;
            }
            dlg.FilterIndex = 1;
            DialogResult res = dlg.ShowDialog(parentWindow);
            if (res != DialogResult.OK)
            {
                throw new COMException("Operation cancelled", ERRORS.E_CANCELLED);
            }

            _saveToPath = dlg.FileName;
        }

        public Document Save(string targetFileName)
        {
            if (String.IsNullOrEmpty(targetFileName))
            {
                throw new ArgumentException("empty argument", "targetFileName");
            }

            if (String.IsNullOrEmpty(_saveToPath))
            {
                // if no saveToPath is set, overwrite the localFile
                File.Copy(targetFileName, _filePath, true);
                if (targetFileName != _localFileName)
                {
                    File.Delete(targetFileName);
                }

                // there's no new document in this case
                return null;
            }
            else
            {
                File.Copy(targetFileName, _saveToPath, true);
                Document doc = new Document();
                doc._filePath = _saveToPath;
                _saveToPath = null;
                return doc;
            }
        }

        // prompts for a new filename, creates a new document, and copies
        // the argument file to the new filename
        public static Document CreateNewDocument(IntPtr hwnd, string baseDir,
                                                 string fileName, string title)
        {
            WindowWrapper parentWindow = new WindowWrapper(hwnd);

            if (String.IsNullOrEmpty(title))
                title = fileName;

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.DefaultExt = Path.GetExtension(fileName);
            dlg.RestoreDirectory = true;
            dlg.OverwritePrompt = true;
            dlg.Title = "Save " + title;
            dlg.Filter = "All Files (*.*)|*.*";
            if (!String.IsNullOrEmpty(dlg.DefaultExt))
            {
                dlg.Filter = "*." + dlg.DefaultExt + "|*." + dlg.DefaultExt + "|" + dlg.Filter;
            }
            dlg.FilterIndex = 1;
            dlg.InitialDirectory = baseDir;
            dlg.AutoUpgradeEnabled = true;

            DialogResult res = DialogResult.None;
            while (res != DialogResult.OK)
            {
                res = dlg.ShowDialog(parentWindow);
                if (res == DialogResult.Cancel)
                {
                    throw new COMException("Operation cancelled", ERRORS.E_CANCELLED);
                }

                // verify that the selected file is under the base directory path
                if (!IsFileUnderDirectory(baseDir, dlg.FileName))
                {
                    MessageBox.Show(parentWindow, "You must select a file under " + baseDir);
                    res = DialogResult.None;
                }
            }

            Document doc = new Document();
            doc._filePath = dlg.FileName;

            File.Copy(fileName, doc._filePath);

            return doc;
        }

        // prompts the user to select file(s) for opening
        public static Document[] SelectFiles(IntPtr hwnd, string baseDir, bool multiSelect)
        {
            WindowWrapper parentWindow = new WindowWrapper(hwnd);
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.RestoreDirectory = true;
            dlg.Title = "Open file";
            dlg.Filter = "PDF files (*.pdf)|*.pdf|All Files (*.*)|*.*";
            dlg.FilterIndex = 1;
            dlg.Multiselect = multiSelect;
            dlg.InitialDirectory = baseDir;
            DialogResult res = DialogResult.None;
            while (res != DialogResult.OK)
            {
                res = dlg.ShowDialog(parentWindow);
                if (res == DialogResult.Cancel)
                {
                    throw new COMException("Operation cancelled", ERRORS.E_CANCELLED);
                }

                foreach (string file in dlg.FileNames)
                {
                    if (!IsFileUnderDirectory(baseDir, file))
                    {
                        MessageBox.Show("You must select a file(s) under " + baseDir);
                        res = DialogResult.None;
                        break;
                    }
                }
            }

            List<Document> docs = new List<Document>();
            foreach (string file in dlg.FileNames)
            {
                Document doc = new Document();
                doc._filePath = file;
                docs.Add(doc);
            }

            return docs.ToArray();
        }

        // verify if the filename is under the directory
        private static bool IsFileUnderDirectory(string directory, string filename)
        {
            return filename.StartsWith(directory, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}