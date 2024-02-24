/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Windows.Forms;

namespace BruSoft.VS2P4
{
    /// <summary>
    /// This class is a Form that will be displayed when a checked in file is edited.
    /// The user can choose to check out the file, continue editing the file in memory, or cancel the edit.
    /// </summary>
    public partial class DlgQueryEditCheckedInFile : Form
    {
        public const int qecifCheckout = 1;
        public const int qecifEditInMemory = 2;
        public const int qecifCancelEdit = 3;

        private static int lastUsedChangelist = 0;

        int _answer = qecifCancelEdit;

        public int Answer
        {
            get { return _answer; }
            set { _answer = value; }
        }

        public int SelectedChangelist { get; private set; }

        public DlgQueryEditCheckedInFile(string filename, Dictionary<int, string> changelists)
        {
            InitializeComponent();

            comboBoxChangeLists.Items.Add(new ChangelistItem() { Number = 0, Description = "<default>" });
            comboBoxChangeLists.Items.Add(new ChangelistItem() { Number = -1, Description = "New" });
            if (changelists != null)
            {
                foreach (var cl in changelists)
                {
                    comboBoxChangeLists.Items.Add(new ChangelistItem() { Number = cl.Key, Description = $"{cl.Key} - {cl.Value}" });
                }
            }
            comboBoxChangeLists.SelectedItem = comboBoxChangeLists.Items.Cast<ChangelistItem>().FirstOrDefault(s => s.Number == lastUsedChangelist);

            // Format the message text with the current file name
            msgText.Text = String.Format(CultureInfo.CurrentUICulture, msgText.Text, filename);
        }

        private void btnCheckout_Click(object sender, EventArgs e)
        {
            var clItem = comboBoxChangeLists.SelectedItem as ChangelistItem;
            lastUsedChangelist = clItem.Number;
            SelectedChangelist = clItem.Number;

            Answer = qecifCheckout;
            Close();
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            Answer = qecifEditInMemory;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Answer = qecifCancelEdit;
            Close();
        }

        private class ChangelistItem
        {
            public int Number { get; set; }
            public string Description { get; set; }
            public override string ToString() => Description;
        }
    }
}