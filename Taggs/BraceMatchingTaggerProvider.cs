using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[Export(typeof(IViewTaggerProvider))]
[ContentType("cgshader")]
[TagType(typeof(TextMarkerTag))]
internal class BraceMatchingTaggerProvider : IViewTaggerProvider
{
    public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
    {
        if(textView == null)
		{
			return null;
		}

		//provide highlighting only on the top-level buffer 
		if(textView.TextBuffer != buffer)
		{
			return null;
		}

		return new BraceMatchingTagger(textView, buffer) as ITagger<T>;
    }
}