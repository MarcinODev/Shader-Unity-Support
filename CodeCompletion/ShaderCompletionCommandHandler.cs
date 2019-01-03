using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace ShaderCompletionCommandHandler
{
	/// <summary>
	/// Listener 
	/// </summary>
    [Export(typeof(IVsTextViewCreationListener))]
    [Name("token completion handler")]
    [ContentType("cgshader")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal class ShaderCompletionHandlerProvider : IVsTextViewCreationListener
    {
        [Import]
        internal IVsEditorAdaptersFactoryService AdapterService = null;
        [Import]
        internal ICompletionBroker CompletionBroker { get; set; }
        [Import]
        internal SVsServiceProvider ServiceProvider { get; set; }
        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            ITextView textView = AdapterService.GetWpfTextView(textViewAdapter);
            if (textView == null)
                return;
            
            Func<ShaderCompletionHandler> createCommandHandler = delegate() { return new ShaderCompletionHandler(textViewAdapter, textView, this); };
            textView.Properties.GetOrCreateSingletonProperty(createCommandHandler);
        }

    }

	/// <summary>
	/// Handles pressed keyboard commands
	/// </summary>
    internal class ShaderCompletionHandler : IOleCommandTarget
    {
        private IOleCommandTarget m_nextCommandHandler;
        private ITextView m_textView;
        private ShaderCompletionHandlerProvider m_provider;
        private ICompletionSession m_session;

        internal ShaderCompletionHandler(IVsTextView textViewAdapter, ITextView textView, ShaderCompletionHandlerProvider provider)
        {
            this.m_textView = textView;
            this.m_provider = provider;

            textViewAdapter.AddCommandFilter(this, out m_nextCommandHandler);
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
	        for(int i = 0; i < cCmds;i++)
	        {
		        var status = QueryStatusImpl(pguidCmdGroup, prgCmds[i]);
		        if (status == VSConstants.E_FAIL)
			        return m_nextCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
		        else
			        prgCmds[i].cmdf = (uint)status;
	        }
	        return VSConstants.S_OK;
        }

        private int QueryStatusImpl(Guid pguidCmdGroup, OLECMD cmd)
        {
	        if (pguidCmdGroup == typeof(VSConstants.VSStd2KCmdID).GUID)
	        {
		        switch ((VSConstants.VSStd2KCmdID)cmd.cmdID)
		        {
			        case VSConstants.VSStd2KCmdID.SHOWMEMBERLIST:
			        case VSConstants.VSStd2KCmdID.COMPLETEWORD:
			        case VSConstants.VSStd2KCmdID.PARAMINFO:
			        case VSConstants.VSStd2KCmdID.QUICKINFO:
				        return (int)OLECMDF.OLECMDF_ENABLED | (int)OLECMDF.OLECMDF_SUPPORTED;
		        }
	        }
	        return VSConstants.E_FAIL;
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (VsShellUtilities.IsInAutomationFunction(m_provider.ServiceProvider))
            {
                return m_nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            }
            uint commandID = nCmdID;
            char typedChar = char.MinValue;
            if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR)
            {
                typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
            }

            VSConstants.VSStd2KCmdID nCmdIDEnum = (VSConstants.VSStd2KCmdID)nCmdID;
            //System.Console.WriteLine("Keyboard command: " + nCmdIDEnum);

            if (nCmdID == (uint)VSConstants.VSStd2KCmdID.RETURN
                || nCmdID == (uint)VSConstants.VSStd2KCmdID.TAB
                /*|| (char.IsWhiteSpace(typedChar) || char.IsPunctuation(typedChar))*/)//this made auto complete on point or space
            {
                if (m_session != null && !m_session.IsDismissed)
                {
                    if (m_session.SelectedCompletionSet.SelectionStatus.IsSelected)
                    {
                        m_session.Commit();
                        return VSConstants.S_OK;
                    }
                    else
                    {
                        m_session.Dismiss();
                    }
                }
            }

            int retVal = m_nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            bool handled = false;
            if(!typedChar.Equals(char.MinValue) && char.IsLetterOrDigit(typedChar) || nCmdIDEnum == VSConstants.VSStd2KCmdID.COMPLETEWORD)
            {
                if (m_session == null || m_session.IsDismissed) 
                {
                    TriggerCompletion();
                }
                else    
                {
                    m_session.Filter();
                }
                handled = true;
            }
            else if (commandID == (uint)VSConstants.VSStd2KCmdID.BACKSPACE  
                || commandID == (uint)VSConstants.VSStd2KCmdID.DELETE)
            {
                if (m_session != null && !m_session.IsDismissed)
                    m_session.Filter();
                handled = true;
            }
            if (handled) return VSConstants.S_OK;
            return retVal;
        }

        private bool TriggerCompletion()
		{
			SnapshotPoint? caretPoint =
            m_textView.Caret.Position.Point.GetPoint(
            textBuffer => (!textBuffer.ContentType.IsOfType("projection")), PositionAffinity.Predecessor);
            if (!caretPoint.HasValue)
            {
                return false;
            }

            m_session = m_provider.CompletionBroker.CreateCompletionSession(m_textView,
                caretPoint.Value.Snapshot.CreateTrackingPoint(caretPoint.Value.Position, PointTrackingMode.Positive), true);

            m_session.Dismissed += this.OnSessionDismissed;
            m_session.Start();

            return true;
        }

        private void OnSessionDismissed(object sender, EventArgs e)
        {
            m_session.Dismissed -= this.OnSessionDismissed;
            m_session = null;
		}
	}
}
