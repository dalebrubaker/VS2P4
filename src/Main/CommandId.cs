/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/


namespace BruSoft.VS2P4
{
	/// <summary>
	/// This class is used to expose the list of the IDs of the commands implemented
	/// by the client package. This list of IDs must match the set of IDs defined inside the
	/// VSCT file.
    /// This is a list of GUIDs specific to this package, especially the package GUID and the commands group GUID.
    /// It also includes GUIDs of other elements used by this package.
	/// </summary>
	public static class CommandId
	{
		// Define the list a set of public static members.

        // Define the list of menus (these include toolbars)
        public const int icmdCheckout = 0x100;
        public const int icmdMarkForAdd	= 0x101;
        public const int icmdRevertIfUnchanged = 0x102;
        public const int icmdRevert = 0x103;
        public const int icmdGetLatestRevison = 0x104;
        public const int icmdRevisionHistory = 0x105;
        public const int icmdDiff = 0x106;
        public const int icmdTimeLapse = 0x107;
        public const int icmdRefresh = 0x108;
        public const int icmdOpenInSwarm = 0x109;

        //public const int icmdViewToolWindow = 0x108;
        //public const int icmdToolWindowToolbarCommand   = 0x109;

        //public const int imnuToolWindowToolbarMenu = 0x201;

        // Define the list of icons (use decimal numbers here, to match the resource IDs)
        public const int iiconProductIcon               = 400;

        // Define the list of bitmaps (use decimal numbers here, to match the resource IDs)
        public const int ibmpToolbarMenusImages         = 500;
        //public const int ibmpToolWindowsImages          = 501;

        //// Glyph indexes in the bitmap used for toolwindows (ibmpToolWindowsImages)
        //public const int iconSccProviderToolWindow      = 1; // DAB changed from 0 to 1, but neither seems to cause the window to show an icon.
	}
}
