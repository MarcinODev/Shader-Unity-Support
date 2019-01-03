using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace ShaderContentType
{
    internal static class FileAndContentTypeDefinitions
    {
        [Export]
        [Name("cgshader")]
        [BaseDefinition("C/C++")]
        internal static ContentTypeDefinition hidingContentTypeDefinition;

        [Export]
        [FileExtension(".shader")]
        [ContentType("cgshader")]
        internal static FileExtensionToContentTypeDefinition shaderFileExtensionDefinition;

        [Export]
        [FileExtension(".cginc")]
        [ContentType("cgshader")]
        internal static FileExtensionToContentTypeDefinition cgincFileExtensionDefinition;
        
        [Export]
        [FileExtension(".compute")]
        [ContentType("cgshader")]
        internal static FileExtensionToContentTypeDefinition shaderFileExtensionDefinitionForCompute;
    
    }
}