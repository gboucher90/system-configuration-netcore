//
// System.Configuration.Internal.IConfigErrorInfo.cs
//
// Authors:
//  Lluis Sanchez Gual (lluis@novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//


using System.IO;

namespace System.Configuration.Internal
{
    public class DelegatingConfigHost : IInternalConfigHost
    {
        protected DelegatingConfigHost()
        {
        }

        protected IInternalConfigHost Host { get; set; }

        public virtual object CreateConfigurationContext(string configPath, string locationSubPath)
        {
            return Host.CreateConfigurationContext(configPath, locationSubPath);
        }

        public virtual object CreateDeprecatedConfigContext(string configPath)
        {
            return Host.CreateDeprecatedConfigContext(configPath);
        }

        public virtual void DeleteStream(string streamName)
        {
            Host.DeleteStream(streamName);
        }

        public virtual string GetConfigPathFromLocationSubPath(string configPath, string locatinSubPath)
        {
            return Host.GetConfigPathFromLocationSubPath(configPath, locatinSubPath);
        }

        public virtual Type GetConfigType(string typeName, bool throwOnError)
        {
            return Host.GetConfigType(typeName, throwOnError);
        }

        public virtual string GetConfigTypeName(Type t)
        {
            return Host.GetConfigTypeName(t);
        }

        public virtual string GetStreamName(string configPath)
        {
            return Host.GetStreamName(configPath);
        }

        public void Init(IInternalConfigRoot root, params object[] hostInitParams)
        {
            Host.Init(root, hostInitParams);
        }


        public virtual void InitForConfiguration(ref string locationSubPath, out string configPath,
            out string locationConfigPath, IInternalConfigRoot root, params object[] hostInitConfigurationParams)
        {
            Host.InitForConfiguration(ref locationSubPath, out configPath, out locationConfigPath, root,
                hostInitConfigurationParams);
        }


        public virtual bool IsDefinitionAllowed(string configPath, ConfigurationAllowDefinition allowDefinition,
            ConfigurationAllowExeDefinition allowExeDefinition)
        {
            return Host.IsDefinitionAllowed(configPath, allowDefinition, allowExeDefinition);
        }

        public virtual Stream OpenStreamForRead(string streamName)
        {
            return Host.OpenStreamForRead(streamName);
        }

        public virtual Stream OpenStreamForRead(string streamName, bool assertPermissions)
        {
            return Host.OpenStreamForRead(streamName, assertPermissions);
        }

        public virtual void VerifyDefinitionAllowed(string configPath, ConfigurationAllowDefinition allowDefinition,
            ConfigurationAllowExeDefinition allowExeDefinition, IConfigErrorInfo errorInfo)
        {
            Host.VerifyDefinitionAllowed(configPath, allowDefinition, allowExeDefinition, errorInfo);
        }

        public virtual void WriteCompleted(string streamName, bool success, object writeContext)
        {
            Host.WriteCompleted(streamName, success, writeContext);
        }

        public virtual void WriteCompleted(string streamName, bool success, object writeContext, bool assertPermissions)
        {
            Host.WriteCompleted(streamName, success, writeContext, assertPermissions);
        }

        public virtual bool SupportsChangeNotifications
        {
            get { return Host.SupportsChangeNotifications; }
        }

        public virtual bool SupportsLocation
        {
            get { return Host.SupportsLocation; }
        }

        public virtual bool SupportsPath
        {
            get { return Host.SupportsPath; }
        }

        public virtual bool SupportsRefresh
        {
            get { return Host.SupportsRefresh; }
        }
    }
}