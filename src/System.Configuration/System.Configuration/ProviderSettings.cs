//
// System.Configuration.ProviderSettings.cs
//
// Authors:
//	Duncan Mak (duncan@ximian.com)
//      Lluis Sanchez Gual (lluis@novell.com)
//      Chris Toshok (toshok@ximian.com)
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
// Copyright (C) 2004,2005 Novell, Inc (http://www.novell.com)
//

using System.Collections.Specialized;

namespace System.Configuration
{
    public sealed class ProviderSettings : ConfigurationElement
    {
        private static readonly ConfigurationProperty NameProp;
        private static readonly ConfigurationProperty TypeProp;
        private static readonly ConfigurationPropertyCollection properties;
        private ConfigNameValueCollection _parameters;

        static ProviderSettings()
        {
            NameProp = new ConfigurationProperty("name", typeof(string), null,
                ConfigurationPropertyOptions.IsRequired | ConfigurationPropertyOptions.IsKey);
            TypeProp = new ConfigurationProperty("type", typeof(string), null, ConfigurationPropertyOptions.IsRequired);
            properties = new ConfigurationPropertyCollection {NameProp, TypeProp};
        }

        public ProviderSettings()
        {
        }

        public ProviderSettings(string name, string type)
        {
            Name = name;
            Type = type;
        }


        [ConfigurationProperty("name",
            Options = ConfigurationPropertyOptions.IsRequired | ConfigurationPropertyOptions.IsKey)]
        public string Name
        {
            get { return (string) this[NameProp]; }
            set { this[NameProp] = value; }
        }

        [ConfigurationProperty("type", Options = ConfigurationPropertyOptions.IsRequired)]
        public string Type
        {
            get { return (string) this[TypeProp]; }
            set { this[TypeProp] = value; }
        }

        protected internal override ConfigurationPropertyCollection Properties
        {
            get { return properties; }
        }

        public NameValueCollection Parameters
        {
            get
            {
                if (_parameters == null)
                    _parameters = new ConfigNameValueCollection();
                return _parameters;
            }
        }

        protected override bool OnDeserializeUnrecognizedAttribute(string name, string value)
        {
            if (_parameters == null)
                _parameters = new ConfigNameValueCollection();
            _parameters[name] = value;
            _parameters.ResetModified();
            return true;
        }

        protected internal override bool IsModified()
        {
            return (_parameters != null && _parameters.IsModified) || base.IsModified();
        }

        protected internal override void Reset(ConfigurationElement parentElement)
        {
            base.Reset(parentElement);

            var sec = parentElement as ProviderSettings;
            if (sec != null && sec._parameters != null)
                _parameters = new ConfigNameValueCollection(sec._parameters);
            else
                _parameters = null;
        }
    }
}