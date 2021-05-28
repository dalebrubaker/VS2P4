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
    partial class DlgLogin
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
			this.msgText = new System.Windows.Forms.Label();
			this.textBoxPassword = new System.Windows.Forms.TextBox();
			this.butCancel = new System.Windows.Forms.Button();
			this.butLogin = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// msgText
			// 
			this.msgText.Location = new System.Drawing.Point(12, 9);
			this.msgText.Name = "msgText";
			this.msgText.Size = new System.Drawing.Size(219, 31);
			this.msgText.TabIndex = 1;
			this.msgText.Text = "Enter the password to log in ({0}:{1}):";
			this.msgText.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// textBoxPassword
			// 
			this.textBoxPassword.Location = new System.Drawing.Point(13, 44);
			this.textBoxPassword.Name = "textBoxPassword";
			this.textBoxPassword.PasswordChar = '●';
			this.textBoxPassword.Size = new System.Drawing.Size(218, 20);
			this.textBoxPassword.TabIndex = 2;
			// 
			// butCancel
			// 
			this.butCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.butCancel.Location = new System.Drawing.Point(13, 70);
			this.butCancel.Name = "butCancel";
			this.butCancel.Size = new System.Drawing.Size(75, 23);
			this.butCancel.TabIndex = 3;
			this.butCancel.Text = "&Cancel";
			this.butCancel.UseVisualStyleBackColor = true;
			this.butCancel.Click += new System.EventHandler(this.butCancel_Click);
			// 
			// butLogin
			// 
			this.butLogin.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.butLogin.Location = new System.Drawing.Point(94, 70);
			this.butLogin.Name = "butLogin";
			this.butLogin.Size = new System.Drawing.Size(137, 23);
			this.butLogin.TabIndex = 4;
			this.butLogin.Text = "&Log In";
			this.butLogin.UseVisualStyleBackColor = true;
			this.butLogin.Click += new System.EventHandler(this.butLogin_Click);
			// 
			// DlgLogin
			// 
			this.AcceptButton = this.butLogin;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(243, 106);
			this.Controls.Add(this.butLogin);
			this.Controls.Add(this.butCancel);
			this.Controls.Add(this.textBoxPassword);
			this.Controls.Add(this.msgText);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "DlgLogin";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "VS2P4 Login";
			this.TopMost = true;
			this.Load += new System.EventHandler(this.DlgLogin_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label msgText;
        private System.Windows.Forms.TextBox textBoxPassword;
        private System.Windows.Forms.Button butCancel;
        private System.Windows.Forms.Button butLogin;
    }
}