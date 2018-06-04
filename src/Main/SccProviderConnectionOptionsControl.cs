
/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;
using System.IO;
using System.Windows.Forms;

namespace BruSoft.VS2P4
{
    /// <summary>
    /// This class is a UserControl that is hosted in the Options page.
    /// </summary>
    public class SccProviderConnectionOptionsControl : UserControl
    {
        private System.ComponentModel.IContainer components;
        private Label label1;
        private TextBox _server;
        private ToolTip toolTip1;
        private TextBox _user;
        private Label label2;
        private TextBox _password;
        private Label label3;
        private TextBox _workspace;
        private Label label4;
        private SccProviderService _sccProviderService;

        // The parent page, use to persist data
        private SccProviderConnectionOptions _customPage;
        private GroupBox groupBoxPerforceConnection;
        private ComboBox _cbLogLevel;
        private GroupBox groupBoxLogLevel;
        private CheckBox _useP4Config;
        private Button btnTestConnection;

        public SccProviderConnectionOptionsControl()
        {
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();


        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose( bool disposing )
        {
            if( disposing )
            {
                if(components != null)
                {
                    components.Dispose();
                }
                GC.SuppressFinalize(this);
            }
            base.Dispose( disposing );
        }

        #region Component Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SccProviderConnectionOptionsControl));
            this.label1 = new System.Windows.Forms.Label();
            this._server = new System.Windows.Forms.TextBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this._user = new System.Windows.Forms.TextBox();
            this._password = new System.Windows.Forms.TextBox();
            this._workspace = new System.Windows.Forms.TextBox();
            this._cbLogLevel = new System.Windows.Forms.ComboBox();
            this._useP4Config = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.groupBoxPerforceConnection = new System.Windows.Forms.GroupBox();
            this.btnTestConnection = new System.Windows.Forms.Button();
            this.groupBoxLogLevel = new System.Windows.Forms.GroupBox();
            this.groupBoxPerforceConnection.SuspendLayout();
            this.groupBoxLogLevel.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // _server
            // 
            resources.ApplyResources(this._server, "_server");
            this._server.Name = "_server";
            this.toolTip1.SetToolTip(this._server, resources.GetString("_server.ToolTip"));
            // 
            // _user
            // 
            resources.ApplyResources(this._user, "_user");
            this._user.Name = "_user";
            this.toolTip1.SetToolTip(this._user, resources.GetString("_user.ToolTip"));
            // 
            // _password
            // 
            resources.ApplyResources(this._password, "_password");
            this._password.Name = "_password";
            this.toolTip1.SetToolTip(this._password, resources.GetString("_password.ToolTip"));
            // 
            // _workspace
            // 
            resources.ApplyResources(this._workspace, "_workspace");
            this._workspace.Name = "_workspace";
            this.toolTip1.SetToolTip(this._workspace, resources.GetString("_workspace.ToolTip"));
            // 
            // _cbLogLevel
            // 
            this._cbLogLevel.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this._cbLogLevel.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this._cbLogLevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbLogLevel.FormattingEnabled = true;
            resources.ApplyResources(this._cbLogLevel, "_cbLogLevel");
            this._cbLogLevel.Name = "_cbLogLevel";
            this.toolTip1.SetToolTip(this._cbLogLevel, resources.GetString("_cbLogLevel.ToolTip"));
            // 
            // _useP4Config
            // 
            resources.ApplyResources(this._useP4Config, "_useP4Config");
            this._useP4Config.Checked = true;
            this._useP4Config.CheckState = System.Windows.Forms.CheckState.Checked;
            this._useP4Config.Name = "_useP4Config";
            this.toolTip1.SetToolTip(this._useP4Config, resources.GetString("_useP4Config.ToolTip"));
            this._useP4Config.UseVisualStyleBackColor = true;
            this._useP4Config.CheckedChanged += new System.EventHandler(this._useP4Config_CheckedChanged);
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // groupBoxPerforceConnection
            // 
            resources.ApplyResources(this.groupBoxPerforceConnection, "groupBoxPerforceConnection");
            this.groupBoxPerforceConnection.Controls.Add(this._useP4Config);
            this.groupBoxPerforceConnection.Controls.Add(this._server);
            this.groupBoxPerforceConnection.Controls.Add(this._workspace);
            this.groupBoxPerforceConnection.Controls.Add(this._user);
            this.groupBoxPerforceConnection.Controls.Add(this._password);
            this.groupBoxPerforceConnection.Controls.Add(this.btnTestConnection);
            this.groupBoxPerforceConnection.Controls.Add(this.label3);
            this.groupBoxPerforceConnection.Controls.Add(this.label2);
            this.groupBoxPerforceConnection.Controls.Add(this.label1);
            this.groupBoxPerforceConnection.Controls.Add(this.label4);
            this.groupBoxPerforceConnection.Name = "groupBoxPerforceConnection";
            this.groupBoxPerforceConnection.TabStop = false;
            // 
            // btnTestConnection
            // 
            resources.ApplyResources(this.btnTestConnection, "btnTestConnection");
            this.btnTestConnection.Name = "btnTestConnection";
            this.btnTestConnection.UseVisualStyleBackColor = true;
            this.btnTestConnection.Click += new System.EventHandler(this.btnTestConnection_Click);
            // 
            // groupBoxLogLevel
            // 
            this.groupBoxLogLevel.Controls.Add(this._cbLogLevel);
            resources.ApplyResources(this.groupBoxLogLevel, "groupBoxLogLevel");
            this.groupBoxLogLevel.Name = "groupBoxLogLevel";
            this.groupBoxLogLevel.TabStop = false;
            // 
            // SccProviderConnectionOptionsControl
            // 
            this.AllowDrop = true;
            this.Controls.Add(this.groupBoxPerforceConnection);
            this.Controls.Add(this.groupBoxLogLevel);
            this.Name = "SccProviderConnectionOptionsControl";
            resources.ApplyResources(this, "$this");
            this.Load += new System.EventHandler(this.SccProviderOptionsControl_Load);
            this.groupBoxPerforceConnection.ResumeLayout(false);
            this.groupBoxPerforceConnection.PerformLayout();
            this.groupBoxLogLevel.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        #endregion
    
        public SccProviderConnectionOptions OptionsPage
        {
            set
            {
                _customPage = value;
            }
        }

        private void SccProviderOptionsControl_Load(object sender, EventArgs e)
        {
            _sccProviderService = (SccProviderService)GetService(typeof(SccProviderService));
            if (_sccProviderService != null)
            {
                _useP4Config.Checked = _sccProviderService.Options.UseP4Config;
                _server.Text = _sccProviderService.Options.Server;
                _user.Text = _sccProviderService.Options.User;
                _password.Text = _sccProviderService.Options.Password;
                _workspace.Text = _sccProviderService.Options.Workspace;
                EnableDisableConnectionFields();

                _cbLogLevel.DataSource = Enum.GetValues(typeof(Log.Level));
                _cbLogLevel.SelectedItem = _sccProviderService.Options.LogLevel;
            }
            else
            {
                Log.Error("Unable to get SccProviderService");
            }
        } 
 

        /// <summary>
        /// Persist options changes
        /// </summary>
        internal void Save()
        {
            if (_sccProviderService != null)
            {
                PersistedP4OptionSettings persistedSettings;
                P4Options p4Options = _sccProviderService.LoadOptions(out persistedSettings);
                p4Options.Server = persistedSettings.PerforceServer = _server.Text;
                p4Options.User = persistedSettings.PerforceUser = _user.Text;
                p4Options.Password = _password.Text;
                p4Options.Workspace = persistedSettings.PerforceWorkspace = _workspace.Text;
                p4Options.UseP4Config = _useP4Config.Checked;
                persistedSettings.UseP4Config = (bool?)_useP4Config.Checked;
                p4Options.LogLevel = (Log.Level)_cbLogLevel.SelectedItem;

                _sccProviderService.SaveOptions(p4Options, persistedSettings);
            }
        }

        private void btnTestConnection_Click(object sender, EventArgs e)
        {
            if (_sccProviderService == null)
            {
                Log.Error("Missing SccProviderService");
                return;
            }
            P4Service p4Service;
            if (_useP4Config.Checked)
            {
                string solutionName = _sccProviderService.SccProvider.GetSolutionFileName();
                var solutionPath = "";
                if (!String.IsNullOrEmpty(solutionName))
                {
                    solutionPath = Path.GetDirectoryName(solutionName);
                }
                p4Service = new P4Service("", "", "", "", true, solutionPath, _sccProviderService.Map);
            }
            else
            {
                p4Service = new P4Service(_server.Text, _user.Text, _password.Text, _workspace.Text, _useP4Config.Checked, null, _sccProviderService.Map);
            }

            bool isSuccessful = true;
            try
            {
                p4Service.Connect();
            }
            catch (Exception ex)
            {
                isSuccessful = false;
                MessageBox.Show(ex.Message, Resources.CONNECTION_FAILED, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                p4Service.Disconnect();
                p4Service.Dispose();
                if (isSuccessful)
                {
                    var msg = Resources.CONNECTION_SUCCEEDED;
                    if (_sccProviderService != null && _sccProviderService.Map != null)
                    {
                        var root = _sccProviderService.Map.Root;
                        msg += "\nPerforce root is " + root;
                    }
                    MessageBox.Show(msg);
                }
            }
        }

        private void _useP4Config_CheckedChanged(object sender, EventArgs e)
        {
            EnableDisableConnectionFields();
        }

        private void EnableDisableConnectionFields()
        {
            _server.Enabled = !_useP4Config.Checked;
            _user.Enabled = !_useP4Config.Checked;
            _password.Enabled = !_useP4Config.Checked;
            _workspace.Enabled = !_useP4Config.Checked;
            //btnTestConnection.Enabled = !_useP4Config.Checked;
        }
    }

}
