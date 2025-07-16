using DMSConnector;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace CurrentDocumentBluePrint
{
    // enum for menuItemId values
    public enum ItemId
    {
        DEFAULT= 0, // can be used for separators too
        SEPARATOR = 0,
        TryMe   = 1,
        SAVE   = 2,
        SAVEAS = 3
    }

    // simple helper class to make it easier to create a table-like definition of menu items
    public class MenuItemDefinition
    {
        public ItemId id;
        public string resText;
        public string resTooltip;
        public bool isPartOfToolbar;
        public CallbackType cbType;
        public string resIconBig;
        public string resIconBig_150;
        public string resIconBig_200;
        public string resIconSmall;
        public string resIconSmall_150;
        public string resIconSmall_200;
        public bool enabledWithoutDoc;

        // simple constuctor to help create a table-like definition
        public MenuItemDefinition(ItemId id, string resText, string resTooltip,
            bool isPartOfToolbar, CallbackType cbType,
            string resIconBig, string resIconBig_150, string resIconBig_200,
            string resIconSmall, string resIconSmall_150, string resIconSmall_200,
            bool enabledWithoutDoc)
        {
            this.id = id;
            this.resText = resText;
            this.resTooltip = resTooltip;
            this.isPartOfToolbar = isPartOfToolbar;
            this.cbType = cbType;
            this.resIconBig = resIconBig;
            this.resIconSmall = resIconSmall;
            this.resIconBig_150 = resIconBig_150;
            this.resIconSmall_150 = resIconSmall_150;
            this.resIconBig_200 = resIconBig_200;
            this.resIconSmall_200 = resIconSmall_200;
            this.enabledWithoutDoc = enabledWithoutDoc;
        }

        // static definition of our menu layout
        // the Text/Tooltip/BigIcon/SmallIcon are resource identifiers!
        internal static MenuItemDefinition[] menuDefinitions = {
                                //  MenuItemId        Text (res)     ToolTip  Toolbar       cbType                      BigIcon (res)                                     SmallIcon (res)                                                       enabled
            new MenuItemDefinition( ItemId.TryMe     , "MenuTryMe"    , ""    ,   true, CallbackType.CALLBACK_MENUITEM,       "Image_Open", "Image_Open_150", "Image_Open_200", "Image_Open_Small" , "Image_Open_Small_150" , "Image_Open_Small_200", true )
           };
    }

    public class MenuItemList : List<MenuItem>
    {
        // create a list from the static definition of the menu (see below)
        // it loads the resource strings and bitmaps based on the static definition
        // and creates a list which is suitable for the DMSConnector interface functions
        public static MenuItemList Create()
        {
            return new MenuItemList(MenuItemDefinition.menuDefinitions);
        }

        // there's no need to call this constructor directly
        // Create() should be enough
        private MenuItemList(MenuItemDefinition[] definitions)
        {
            foreach (MenuItemDefinition def in definitions)
            {
                Add(new MenuItem(def));
            }
        }

    }

    // holder class for menu item definition, with string and bitmap resources already resolved
    public class MenuItem
    {
        public int menuItemId;
        public string text;
        public string tooltip;
        public bool isPartOfToolbar;
        public CallbackType cbType;
        public IntPtr hIconBig;
        public IntPtr hIconSmall;
        public bool enabledWithoutDoc;

        static readonly int LOGPIXELSX = 88;

        [DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        public MenuItem(MenuItemDefinition definition)
        {
            string resBig = definition.resIconBig;
            string resSmall = definition.resIconSmall;
            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
            {
                IntPtr hdc = g.GetHdc();

                int lx = GetDeviceCaps(hdc, LOGPIXELSX);
                if (lx >= 192)
                {
                    resBig = definition.resIconBig_200;
                    resSmall = definition.resIconSmall_200;
                }
                else
                    if (lx >= 144)
                    {
                        resBig = definition.resIconBig_150;
                        resSmall = definition.resIconSmall_150;
                    }

                g.ReleaseHdc();
            }

            menuItemId = (int)definition.id;
            text = GetStringResource(definition.resText);
            tooltip = GetStringResource(definition.resTooltip);
            isPartOfToolbar = definition.isPartOfToolbar;
            cbType = definition.cbType;
            hIconBig = GetHBitmapResource(resBig);
            hIconSmall = GetHBitmapResource(resSmall);
            enabledWithoutDoc = definition.enabledWithoutDoc;
        }

        #region Private members

        // Load a resource string or return empty string ("") if it doesn't exist
        private string GetStringResource(string resName)
        {
            if (!String.IsNullOrEmpty(resName))
                return Resources.Resources.ResourceManager.GetString(resName);
            else
                return String.Empty;
        }

        // load a bitmap resource if exists and return a HBITMAP from it
        // the HBITMAP needs to be deleted with DeleteObject
        private IntPtr GetHBitmapResource(string resName)
        {
            if (String.IsNullOrEmpty(resName))
                return IntPtr.Zero;

            Bitmap bmp = Resources.Resources.ResourceManager.GetObject(resName) as Bitmap;
            if (bmp != null)
                return bmp.GetHbitmap(Color.Black);
            else
                return IntPtr.Zero;
        }
        #endregion Private members

        #region IDisposable Members

        // we will need to call DeleteObject on the HTBITMAPs
        // careful, it's need to be called only once, for example through calling
        // MenuItemList.Dispose()
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        ~MenuItem()
        {
            if (hIconBig.ToInt64() != 0)
            {
                DeleteObject(hIconBig);
                hIconBig = IntPtr.Zero;
            }
            if (hIconSmall.ToInt64() != 0)
            {
                DeleteObject(hIconSmall);
                hIconSmall = IntPtr.Zero;
            }
        }

        #endregion
    }

}
