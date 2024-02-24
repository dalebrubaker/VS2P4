/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;
using System.Globalization;
using System.Windows.Forms;

namespace BruSoft.VS2P4
{
    /// <summary>
    /// This class is a Form that will be displayed when a checked in file is saved.
    /// The user can choose to checkout the file, discard the in-memory changes and skip saving it, save the file with a different name, or cancel the operation.
    /// </summary>
    public partial class DlgQuerySaveCheckedInFile : Form
    {
        public const int qscifCheckout = 1;
        public const int qscifSkipSave = 2;
        public const int qscifForceSaveAs = 3;
        public const int qscifCancel = 4;

        int _answer = qscifCancel;

        public int Answer
        {
            get { return _answer; }
            set { _answer = value; }
        }

        public DlgQuerySaveCheckedInFile(string filename)
        {
            InitializeComponent();

            // Format the message text with the current file name
            msgText.Text = String.Format(CultureInfo.CurrentUICulture, msgText.Text, filename);
        }

        private void btnCheckout_Click(object sender, EventArgs e)
        {
            Answer = qscifCheckout;
            Close();
        }

        private void btnSkipSave_Click(object sender, EventArgs e)
        {
            Answer = qscifSkipSave;
            Close();
        }

        private void btnSaveAs_Click(object sender, EventArgs e)
        {
            Answer = qscifForceSaveAs;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Answer = qscifCancel;
            Close();
        }
    }
}