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
    /// This class is a Form that will be displayed when a connection fails.
    /// The user can enter a password and VS2P4 will attempt a login.
    /// </summary>
    public partial class DlgLogin : Form
    {
        private string _password = "";

        public string Password
        {
            get { return _password; }
            set { _password = value; }
        }

        public DlgLogin(string server, string port)
        {
            InitializeComponent();

            textBoxPassword.Focus();
            msgText.Text = String.Format(CultureInfo.CurrentUICulture, msgText.Text, server, port);
        }

        private void butCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void butLogin_Click(object sender, EventArgs e)
        {
            _password = textBoxPassword.Text;
            DialogResult = DialogResult.OK;
        }

        private void DlgLogin_Load(object sender, EventArgs e)
        {
            Activate();
        }
    }
}