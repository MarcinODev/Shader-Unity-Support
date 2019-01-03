using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtentionViewShaderListener
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    class ExtensionlessViewCreationListener : IWpfTextViewCreationListener
    {
        [Import]
        internal IEditorFormatMapService FormatMapService = null;

        [Import]
        internal IContentTypeRegistryService ContentTypeRegistryService = null;

        [Import]
        internal SVsServiceProvider ServiceProvider = null;

        #region IWpfTextViewCreationListener Members

        void IWpfTextViewCreationListener.TextViewCreated(IWpfTextView textView)
        {
            try
            {
                var textDocument = (ITextDocument)textView.TextBuffer.Properties[typeof(ITextDocument)];
                if(textDocument.FilePath.Contains(".shader") || textDocument.FilePath.Contains(".cginc"))
                {
                    var contentType = ContentTypeRegistryService.GetContentType("cgshader");
                    textView.TextBuffer.ChangeContentType(contentType, null);
                    if(textDocument.FilePath.Contains(".cginc"))
                    {
                        if(CompletionCodeContainer.Instance != null)
						{
							CompletionCodeContainer.Instance.AddFileToGlobalCompletionList(textDocument.FilePath, textView.TextBuffer);
						}
					}
                }
                if(textDocument.FilePath.Contains(".compute"))
                {
                    var contentType = ContentTypeRegistryService.GetContentType("cgshader");
                    textView.TextBuffer.ChangeContentType(contentType, null);
                }
            }
            catch(Exception e)
            {
                DebugLog.Write(e);
            }
        }

        #endregion
    }
    
    [Export(typeof(IWpfTextViewConnectionListener))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    class ExtensionlessViewConnectionListener : IWpfTextViewConnectionListener
    {
        [Import]
        internal IEditorFormatMapService FormatMapService = null;

        [Import]
        internal IContentTypeRegistryService ContentTypeRegistryService = null;

        [Import]
        internal SVsServiceProvider ServiceProvider = null;
        
        public void SubjectBuffersConnected(IWpfTextView textView, ConnectionReason reason, System.Collections.ObjectModel.Collection<Microsoft.VisualStudio.Text.ITextBuffer> subjectBuffers)
        {
            if(reason == ConnectionReason.TextViewLifetime)
            {
                try
                {
                    var textDocument = (ITextDocument)subjectBuffers[0].Properties[typeof(ITextDocument)];
                    if(textDocument.FilePath.Contains(".shader") || textDocument.FilePath.Contains(".cginc"))
                    {
                        var contentType = ContentTypeRegistryService.GetContentType("cgshader");
                        subjectBuffers[0].ChangeContentType(contentType, null);
                        if(textDocument.FilePath.Contains(".cginc"))
                        {
                            if(CompletionCodeContainer.Instance != null)
                                CompletionCodeContainer.Instance.AddFileToGlobalCompletionList(textDocument.FilePath, textView.TextBuffer);
                        }
                    }
                    if(textDocument.FilePath.Contains(".compute"))
                    {
                        var contentType = ContentTypeRegistryService.GetContentType("cgshader");
                        textView.TextBuffer.ChangeContentType(contentType, null);
                    }
                }
                catch(Exception e)
                {
                    DebugLog.Write(e);
                }
            }
        }

        public void SubjectBuffersDisconnected(IWpfTextView textView, ConnectionReason reason, System.Collections.ObjectModel.Collection<Microsoft.VisualStudio.Text.ITextBuffer> subjectBuffers)
        {
           
        }
    }
}
