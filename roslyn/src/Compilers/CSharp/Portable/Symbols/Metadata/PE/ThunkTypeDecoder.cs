// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE
{
    internal class ThunkTypeDecoder
    {
        internal static TypeSymbol TransformType(TypeSymbol typeSymbol, PEModuleSymbol corModule)
        {
            if (typeSymbol.HasUseSiteError)
            {
                if (typeSymbol.ContainingAssembly.MetadataReferenceResolver != null)
                {
                    var thunkResolver = typeSymbol.ContainingAssembly.MetadataReferenceResolver as IThunkResolver;

                    if (thunkResolver != null)
                    {
                        var typeName = typeSymbol.ISymbol.ToString();
                        var assemblyName = typeSymbol.ContainingAssembly.Identity.ToString();
                        var type = thunkResolver.TransformType(assemblyName, typeName);
                        var assemblyMetadata = AssemblyMetadata.CreateFromFile(type.Assembly.Location);
                        var moduleMetadata = ModuleMetadata.CreateFromFile(type.Assembly.Location);
                        var peModule = moduleMetadata.Module;
                        var array = System.Collections.Immutable.ImmutableArray.Create(peModule);
                        var referenceManager = typeSymbol.ContainingAssembly.ReferenceManager;

                        var moduleReferences = new ModuleReferences<AssemblySymbol>(System.Collections.Immutable.ImmutableArray.Create<AssemblyIdentity>(), System.Collections.Immutable.ImmutableArray.Create<AssemblySymbol>(), System.Collections.Immutable.ImmutableArray.Create<UnifiedAssembly<AssemblySymbol>>());
                        PEAssembly peAssembly;
                        PEAssemblySymbol peAssemblySymbol;
                        NamedTypeSymbol namedTypeSymbol;

                        peAssembly = new PEAssembly(assemblyMetadata, array);
                        peAssemblySymbol = new PEAssemblySymbol(peAssembly, DocumentationProvider.Default, isLinked: false, MetadataImportOptions.All);

                        peAssemblySymbol.SetCorLibrary(corModule.ContainingAssembly.CorLibrary);

                        namedTypeSymbol = peAssemblySymbol.GetTypeByMetadataName(type.FullName);

                        namedTypeSymbol.ContainingModule.SetReferences(moduleReferences);

                        return namedTypeSymbol;
                    }
                }
            }

            return typeSymbol;
        }
    }
}
