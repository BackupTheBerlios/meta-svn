// Guids.cs
// MUST match guids.h
using System;

namespace MetaCommunity.Plugin
{
    static class GuidList
    {
        public const string guidPluginPkgString = "7b9eda2b-91d7-467c-9911-91b1e530688f";
        public const string guidPluginCmdSetString = "30565c7d-1111-4789-8661-a0a336743e80";
        public const string guidPluginEditorFactoryString = "4c90345d-f37f-408d-8c90-b990190c7d3e";

        public static readonly Guid guidPluginPkg = new Guid(guidPluginPkgString);
        public static readonly Guid guidPluginCmdSet = new Guid(guidPluginCmdSetString);
        public static readonly Guid guidPluginEditorFactory = new Guid(guidPluginEditorFactoryString);
    };
}