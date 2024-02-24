using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using MsVsShell = Microsoft.VisualStudio.Shell;

namespace BruSoft.VS2P4
{
    [Guid("A9528774-0ADB-46BB-8623-E4AB7A4EB7E8")]
    [ComVisible(true)]
    public class SccProviderCommandOptions : MsVsShell.DialogPage
    {
        private SccProviderCommandOptionsControl page;

        /// <include file='doc\DialogPage.uex' path='docs/doc[@for="DialogPage".Window]' />
        /// <devdoc>
        ///     The window this dialog page will use for its UI.
        ///     This window handle must be constant, so if you are
        ///     returning a Windows Forms control you must make sure
        ///     it does not recreate its handle.  If the window object
        ///     implements IComponent it will be sited by the 
        ///     dialog page so it can get access to global services.
        /// </devdoc>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        protected override IWin32Window Window
        {
            get
            {
                page = new SccProviderCommandOptionsControl();
                page.Location = new Point(0, 0);
                page.OptionsPage = this;
                return page;
            }
        }

        /// <include file='doc\DialogPage.uex' path='docs/doc[@for="DialogPage.OnActivate"]' />
        /// <devdoc>
        ///     This method is called when VS wants to activate this
        ///     page.  If the Cancel property of the event is set to true, the page is not activated.
        /// </devdoc>
        protected override void OnActivate(CancelEventArgs e)
        {
            base.OnActivate(e);
        }

        /// <include file='doc\DialogPage.uex' path='docs/doc[@for="DialogPage.OnClosed"]' />
        /// <devdoc>
        ///     This event is raised when the page is closed.   
        /// </devdoc>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }

        /// <include file='doc\DialogPage.uex' path='docs/doc[@for="DialogPage.OnDeactivate"]' />
        /// <devdoc>
        ///     This method is called when VS wants to deactviate this
        ///     page.  If true is set for the Cancel property of the event, 
        ///     the page is not deactivated.
        /// </devdoc>
        protected override void OnDeactivate(CancelEventArgs e)
        {
            base.OnDeactivate(e);
        }

        /// <summary>
        /// This method is called when VS wants to save the user's changes then the dialog is dismissed.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnApply(PageApplyEventArgs e)
        {
            page.Save();
            base.OnApply(e);
        }
    }
}
