using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Classification;
using System.Text.RegularExpressions;

/// <summary>
/// Language keyword classifier
/// </summary>
public class KeywordClassifier : IClassifier 
{
    IClassificationTypeRegistryService classificationRegistry;
    ITextBuffer buffer;

    public KeywordClassifier(ITextBuffer buffer, IClassificationTypeRegistryService classificationRegistry) 
    {
        this.classificationRegistry = classificationRegistry;
        this.buffer = buffer;
    }
       
    private readonly string[] keywords = new string[]
    {
        "fixed", "fixed2", "fixed3", "fixed4",
        "half", "half2", "half3", "half4",
        "float", "float2", "float3", "float4",
		"float2x2", "float3x3", "float4x4",
        "uint", "uint2", "uint3", "uint4",
        "int", "int2", "int3", "int4", "Fog", "Mode",
        "sampler1D", "sampler2D", "sampler3D", "samplerCUBE", 
        "Tags", "Blend", "Cull", "void", "sampler2D_float",
		"ZWrite", "ZTest", "AlphaTest", "Lighting", "Offset",
        "Shader", "SubShader", "Category", "Pass", "Properties"
    };

    
    public IEnumerable<Regex> Regexs
    {
        get
        {
            foreach(var keyword in keywords)
                yield return new Regex(string.Format(@"\b{0}\b", keyword));
        }
    }
    
    public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
    {
        //if (!span.IsInQueryTag()) return Enumerable.Empty<ClassificationSpan>().ToList();
        var startline = span.Start.GetContainingLine();
        var endline = (span.End - 1).GetContainingLine();
        var text = span.Snapshot.GetText(new SnapshotSpan(startline.Start, endline.End));

        return (from regex in Regexs
               from match in regex.Matches(text).OfType<Match>()
               select CreateSpan(span, match))
               .ToList();
    }

    private ClassificationSpan CreateSpan(SnapshotSpan span, Match match)
    {
        var snapshotSpan = new SnapshotSpan(span.Snapshot, 
                                            span.Start.Position + match.Index, 
                                            match.Length);

        return new ClassificationSpan(
            snapshotSpan, 
            classificationRegistry.GetClassificationType("Keyword"));
    }

    #pragma warning disable 67
    public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;
    #pragma warning restore 67
}