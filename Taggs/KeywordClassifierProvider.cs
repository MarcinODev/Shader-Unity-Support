using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Classification;

[Export(typeof(IClassifierProvider))]
[ContentType("cgshader")]
public class KeywordClassifierProvider : IClassifierProvider 
{
    [Import]
    internal IClassificationTypeRegistryService ClassificationRegistry = null;
    
    public IClassifier GetClassifier(ITextBuffer buffer) 
    {
        return buffer.Properties.GetOrCreateSingletonProperty<KeywordClassifier>(delegate { return new KeywordClassifier(buffer, ClassificationRegistry); });
    }
}
