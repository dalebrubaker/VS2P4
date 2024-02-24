using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BruSoft.VS2P4
{
    public partial class SccProviderCommandOptionsControl : UserControl
    {
        private SccProviderCommandOptions thisPage;

        public SccProviderCommandOptionsControl()
        {
            InitializeComponent();
            this.Load += new EventHandler(SccProviderCommandOptionsControl_Load);
            _isRevertEnabled.CheckedChanged += new EventHandler(_isRevertEnabled_CheckedChanged);
        }

        public SccProviderCommandOptions OptionsPage
        {
            set
            {
                thisPage = value;
            }
        }

        void SccProviderCommandOptionsControl_Load(object sender, EventArgs e)
        {
            SccProviderService sccProviderService = (SccProviderService)GetService(typeof(SccProviderService));
            if (sccProviderService != null)
            {
                _isCheckoutEnabled.Checked = sccProviderService.Options.IsCheckoutEnabled;
                _isAddEnabled.Checked = sccProviderService.Options.IsAddEnabled;
                _isRevertIfUnchangedEnabled.Checked = sccProviderService.Options.IsRevertIfUnchangedEnabled;
                _isRevertEnabled.Checked = sccProviderService.Options.IsRevertEnabled;
                _promptBeforeRevert.Checked = sccProviderService.Options.PromptBeforeRevert;
                _isGetLatestRevisionEnabled.Checked = sccProviderService.Options.IsGetLatestRevisionEnabled;
                _isRevisionHistoryEnabled.Checked = sccProviderService.Options.IsViewRevisionHistoryEnabled;
                _isDiffEnabled.Checked = sccProviderService.Options.IsViewDiffEnabled;
                _isTimeLapseEnabled.Checked = sccProviderService.Options.IsViewTimeLapseEnabled;
                _isOpenInSwarmEnabled.Checked = sccProviderService.Options.IsOpenInSwarmEnabled;
                _autoCheckoutOnEdit.Checked = sccProviderService.Options.AutoCheckoutOnEdit;
                _autoCheckoutOnSave.Checked = sccProviderService.Options.AutoCheckoutOnSave;
                _autoAdd.Checked = sccProviderService.Options.AutoAdd;
                _autoDelete.Checked = sccProviderService.Options.AutoDelete;
                _ignoreFilesNotUnderP4Root.Checked = sccProviderService.Options.IgnoreFilesNotUnderP4Root;
                EnableDisableDependentOptions();
            }
        }

        /// <summary>
        /// Enable or disable options that are dependent on another option.
        /// </summary>
        private void EnableDisableDependentOptions()
        {
            _promptBeforeRevert.Enabled = _isRevertEnabled.Checked;
        }

        void _isRevertEnabled_CheckedChanged(object sender, EventArgs e)
        {
            EnableDisableDependentOptions();
        }

        /// <summary>
        /// Persist options changes
        /// </summary>
        internal void Save()
        {
            SccProviderService sccProviderService = (SccProviderService)GetService(typeof(SccProviderService));
            if (sccProviderService != null)
            {
                PersistedP4OptionSettings persistedSettings;
                P4Options p4Options = sccProviderService.LoadOptions(out persistedSettings);
                p4Options.IsCheckoutEnabled = _isCheckoutEnabled.Checked;
                p4Options.IsAddEnabled = _isAddEnabled.Checked;
                p4Options.IsRevertIfUnchangedEnabled = _isRevertIfUnchangedEnabled.Checked;
                p4Options.IsRevertEnabled = _isRevertEnabled.Checked;
                p4Options.PromptBeforeRevert = _promptBeforeRevert.Checked;
                p4Options.IsGetLatestRevisionEnabled = _isGetLatestRevisionEnabled.Checked;
                p4Options.IsViewRevisionHistoryEnabled = _isRevisionHistoryEnabled.Checked;
                p4Options.IsViewDiffEnabled = _isDiffEnabled.Checked;
                p4Options.IsViewTimeLapseEnabled = _isTimeLapseEnabled.Checked;
                p4Options.IsOpenInSwarmEnabled = _isOpenInSwarmEnabled.Checked;
                p4Options.AutoCheckoutOnEdit = _autoCheckoutOnEdit.Checked;
                p4Options.AutoCheckoutOnSave = _autoCheckoutOnSave.Checked;
                p4Options.AutoAdd = _autoAdd.Checked;
                p4Options.AutoDelete = _autoDelete.Checked;
                p4Options.IgnoreFilesNotUnderP4Root = _ignoreFilesNotUnderP4Root.Checked;

                sccProviderService.SaveOptions(p4Options, persistedSettings);
            }
        }

    }
}
