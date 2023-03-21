using System;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Visual Studio to Perforce")]
[assembly: AssemblyDescription("This package implements basic source control functionality via Perforce.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("BruSoft")]
[assembly: AssemblyProduct("VS2P4")]
[assembly: AssemblyCopyright("Copyright © Dale A. Brubaker 2010")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]   
[assembly: ComVisible(false)]     
[assembly: CLSCompliant(false)]
[assembly: NeutralResourcesLanguage("en-US")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Revision and Build Numbers 
// by using the '*' as shown below:

[assembly: AssemblyVersion("2.0.7.0")]
[assembly: AssemblyFileVersion("2.0.7.0")]

//[assembly: InternalsVisibleTo("UnitTests,               PublicKey=002400000480000094000000060200000024000052534131000400000100010015fe97944b2dbcc40990a5ce0f2c3511bef03f38c3fe93e232eda07dfe60389b01a7151c3fd5b6e873d82505e5b6956a6bebcc8a811049424411ed4155536aff217b6baaf1a21c198d84ef496ad1e84473daef26f201e318fc83c399fe0a202729bc52d17092256fe2c376225920562fa9731195f0ebe6c9e8a753fcd6e9fea1")]
[assembly: InternalsVisibleTo("VS2P4_IntegrationTests,  PublicKey=00240000048000009400000006020000002400005253413100040000010001009d53797d4f2156f6ad7e9a1271346185a85da6fd516272c7ca2ac78a4cd3e0d80a03054c6ab6b396293e0b2143d67275f6233217f7ed1fc34abb54bf88ed76a7f8fa8c6e8ee101aac63724acbde0658f4b2f16fce74403b2f987864f6cac5d13a36d1cda04a62b41e53503d3b2542d3f275d24299d0deda7479b1b2dd580169a")]
[assembly: InternalsVisibleTo("VS2P4_UnitTests,         PublicKey=00240000048000009400000006020000002400005253413100040000010001009d53797d4f2156f6ad7e9a1271346185a85da6fd516272c7ca2ac78a4cd3e0d80a03054c6ab6b396293e0b2143d67275f6233217f7ed1fc34abb54bf88ed76a7f8fa8c6e8ee101aac63724acbde0658f4b2f16fce74403b2f987864f6cac5d13a36d1cda04a62b41e53503d3b2542d3f275d24299d0deda7479b1b2dd580169a")]
[assembly: InternalsVisibleTo("SccProvider.UnitTests,               PublicKey=00240000048000009400000006020000002400005253413100040000010001009d53797d4f2156f6ad7e9a1271346185a85da6fd516272c7ca2ac78a4cd3e0d80a03054c6ab6b396293e0b2143d67275f6233217f7ed1fc34abb54bf88ed76a7f8fa8c6e8ee101aac63724acbde0658f4b2f16fce74403b2f987864f6cac5d13a36d1cda04a62b41e53503d3b2542d3f275d24299d0deda7479b1b2dd580169a")]
