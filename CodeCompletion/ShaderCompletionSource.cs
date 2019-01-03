using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using System.Linq;

[Export(typeof(ICompletionSourceProvider))]
[ContentType("text")]
[Name("cgcompletion")]
[Order(After = "default")]
internal class ShaderCompletionSourceProvider : ICompletionSourceProvider
{
    [Import]
    internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }
        
    public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
    {
        try
        {
            var textDocument = (ITextDocument)textBuffer.Properties[typeof(ITextDocument)];
            if(textDocument.FilePath.Contains(".shader") || textDocument.FilePath.Contains(".cginc"))
			{
				return new ShaderCompletionSource(this, textBuffer, textDocument);
			}
			else
			{
				return null;
			}
		}
        catch(Exception e)
        {
            DebugLog.Write(e);
            return null;
        }
    }
}

internal class ShaderCompletionSource : ICompletionSource
{
    private ShaderCompletionSourceProvider sourceProvider;
    private ITextBuffer textBuffer;
    private bool isDisposed;
    private ITextDocument textDocument;

    public ShaderCompletionSource(ShaderCompletionSourceProvider sourceProvider, ITextBuffer textBuffer, ITextDocument textDocument)
    {
        this.sourceProvider = sourceProvider;
        this.textBuffer = textBuffer;
        this.textDocument = textDocument;
    }

    public void AugmentCompletionSession(Microsoft.VisualStudio.Language.Intellisense.ICompletionSession session, IList<Microsoft.VisualStudio.Language.Intellisense.CompletionSet> completionSets)
    {
        TextExtent extent;
        var trackSpan = FindTokenSpanAtPosition(session.GetTriggerPoint(textBuffer), session, out extent);
        var textToComplete = extent.Span.Snapshot.GetText(extent.Span.Start.Position, extent.Span.Length);
        var textToCompleteLower = textToComplete.ToLower();

		var completionList = CompletionCodeContainer.Instance.GetCompletionListForFile(textDocument.FilePath, textBuffer);
		var completionSet = new CompletionSet
        (
            "Tokens",    //the non-localized title of the tab 
            "Tokens",    //the display title of the tab
            trackSpan,
            completionList.Where(compl => 
            {
                string complStr = ((string)compl.Properties[CompletionCodeContainer.lowerDisplayName]);
                if(complStr.Contains(textToCompleteLower) && complStr != textToCompleteLower)
				{
					return true;
				}
				else
				{
					return false;
				}
			}),
            null
        );
        completionSets.Insert(0,completionSet);
    }

    private ITrackingSpan FindTokenSpanAtPosition(ITrackingPoint point, ICompletionSession session, out TextExtent extent)
    {
        SnapshotPoint currentPoint = (session.TextView.Caret.Position.BufferPosition) - 1;
        ITextStructureNavigator navigator = sourceProvider.NavigatorService.GetTextStructureNavigator(textBuffer);
        extent = navigator.GetExtentOfWord(currentPoint);
        return currentPoint.Snapshot.CreateTrackingSpan(extent.Span, SpanTrackingMode.EdgeInclusive);
    }

    public void Dispose()
    {
        if (!isDisposed)
        {
            GC.SuppressFinalize(this);
            isDisposed = true;
        }
    }

}
