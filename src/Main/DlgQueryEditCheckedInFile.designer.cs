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
    partial class DlgQueryEditCheckedInFile
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DlgQueryEditCheckedInFile));
			this.msgText = new System.Windows.Forms.Label();
			this.btnCheckout = new System.Windows.Forms.Button();
			this.btnEdit = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.comboBoxChangeLists = new System.Windows.Forms.ComboBox();
			this.SuspendLayout();
			// 
			// msgText
			// 
			this.msgText.Location = new System.Drawing.Point(4, 13);
			this.msgText.Name = "msgText";
			this.msgText.Size = new System.Drawing.Size(517, 44);
			this.msgText.TabIndex = 0;
			this.msgText.Text = "The read only file\r\n{0}\r\nis under source control and checked in.What do you want " +
    "to do?";
			this.msgText.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// btnCheckout
			// 
			this.btnCheckout.Location = new System.Drawing.Point(57, 94);
			this.btnCheckout.Name = "btnCheckout";
			this.btnCheckout.Size = new System.Drawing.Size(108, 23);
			this.btnCheckout.TabIndex = 1;
			this.btnCheckout.Text = "Checkout the file";
			this.btnCheckout.UseVisualStyleBackColor = true;
			this.btnCheckout.Click += new System.EventHandler(this.btnCheckout_Click);
			// 
			// btnEdit
			// 
			this.btnEdit.Location = new System.Drawing.Point(198, 94);
			this.btnEdit.Name = "btnEdit";
			this.btnEdit.Size = new System.Drawing.Size(132, 23);
			this.btnEdit.TabIndex = 2;
			this.btnEdit.Text = "Edit the file in memory";
			this.btnEdit.UseVisualStyleBackColor = true;
			this.btnEdit.Click += new System.EventHandler(this.btnEdit_Click);
			// 
			// btnCancel
			// 
			this.btnCancel.Location = new System.Drawing.Point(361, 94);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(95, 23);
			this.btnCancel.TabIndex = 3;
			this.btnCancel.Text = "Cancel the edit";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			// 
			// comboBoxChangeLists
			// 
			this.comboBoxChangeLists.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxChangeLists.FormattingEnabled = true;
			this.comboBoxChangeLists.Location = new System.Drawing.Point(114, 60);
			this.comboBoxChangeLists.Name = "comboBoxChangeLists";
			this.comboBoxChangeLists.Size = new System.Drawing.Size(303, 21);
			this.comboBoxChangeLists.TabIndex = 4;
			// 
			// DlgQueryEditCheckedInFile
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(523, 129);
			this.Controls.Add(this.comboBoxChangeLists);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnEdit);
			this.Controls.Add(this.btnCheckout);
			this.Controls.Add(this.msgText);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "DlgQueryEditCheckedInFile";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Microsoft Visual Studio";
			this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label msgText;
        private System.Windows.Forms.Button btnCheckout;
        private System.Windows.Forms.Button btnEdit;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.ComboBox comboBoxChangeLists;
    }
}