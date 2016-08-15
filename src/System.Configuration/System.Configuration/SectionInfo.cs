//
// System.Configuration.SectionInfo.cs
//
// Authors:
//	Lluis Sanchez (lluis@novell.com)
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
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
//

using System.IO;
using System.Xml;

namespace System.Configuration
{
    internal class SectionInfo : ConfigInfo
    {
        private ConfigurationAllowDefinition _allowDefinition = ConfigurationAllowDefinition.Everywhere;

        private ConfigurationAllowExeDefinition _allowExeDefinition =
            ConfigurationAllowExeDefinition.MachineToApplication;

        private bool _allowLocation = true;
        private bool _requirePermission = true;
        private bool _restartOnExternalChanges;

        public SectionInfo()
        {
        }

        public SectionInfo(string sectionName, SectionInformation info)
        {
            Name = sectionName;
            TypeName = info.Type;
            _allowLocation = info.AllowLocation;
            _allowDefinition = info.AllowDefinition;
            _allowExeDefinition = info.AllowExeDefinition;
            _requirePermission = info.RequirePermission;
            _restartOnExternalChanges = info.RestartOnExternalChanges;
        }

        public override object CreateInstance()
        {
            var ob = base.CreateInstance();
            var sec = ob as ConfigurationSection;
            if (sec != null)
            {
                sec.SectionInformation.AllowLocation = _allowLocation;
                sec.SectionInformation.AllowDefinition = _allowDefinition;
                sec.SectionInformation.AllowExeDefinition = _allowExeDefinition;
                sec.SectionInformation.RequirePermission = _requirePermission;
                sec.SectionInformation.RestartOnExternalChanges = _restartOnExternalChanges;
                sec.SectionInformation.SetName(Name);
            }
            return ob;
        }

        public override bool HasDataContent(Configuration config)
        {
            return config.GetSectionInstance(this, false) != null || config.GetSectionXml(this) != null;
        }

        public override bool HasConfigContent(Configuration cfg)
        {
            return StreamName == cfg.FileName;
        }

        public override void ReadConfig(Configuration cfg, string streamName, XmlReader reader)
        {
            StreamName = streamName;
            ConfigHost = cfg.ConfigHost;

            while (reader.MoveToNextAttribute())
            {
                switch (reader.Name)
                {
                    case "allowLocation":
                        string allowLoc = reader.Value;
                        _allowLocation = allowLoc == "true";
                        if (!_allowLocation && allowLoc != "false")
                            ThrowException("Invalid attribute value", reader);
                        break;

                    case "allowDefinition":
                        string allowDef = reader.Value;
                        try
                        {
                            _allowDefinition = (ConfigurationAllowDefinition) Enum.Parse(
                                typeof(ConfigurationAllowDefinition), allowDef);
                        }
                        catch
                        {
                            ThrowException("Invalid attribute value", reader);
                        }
                        break;

                    case "allowExeDefinition":
                        string allowExeDef = reader.Value;
                        try
                        {
                            _allowExeDefinition = (ConfigurationAllowExeDefinition) Enum.Parse(
                                typeof(ConfigurationAllowExeDefinition), allowExeDef);
                        }
                        catch
                        {
                            ThrowException("Invalid attribute value", reader);
                        }
                        break;

                    case "type":
                        TypeName = reader.Value;
                        break;

                    case "name":
                        Name = reader.Value;
                        if (Name == "location")
                            ThrowException("location is a reserved section name", reader);
                        break;

                    case "requirePermission":
                        string reqPerm = reader.Value;
                        var reqPermValue = reqPerm == "true";
                        if (!reqPermValue && reqPerm != "false")
                            ThrowException("Invalid attribute value", reader);
                        _requirePermission = reqPermValue;
                        break;

                    case "restartOnExternalChanges":
                        string restart = reader.Value;
                        var restartValue = restart == "true";
                        if (!restartValue && restart != "false")
                            ThrowException("Invalid attribute value", reader);
                        _restartOnExternalChanges = restartValue;
                        break;

                    default:
                        ThrowException(string.Format("Unrecognized attribute: {0}", reader.Name), reader);
                        break;
                }
            }

            if (Name == null || TypeName == null)
                ThrowException("Required attribute missing", reader);

            reader.MoveToElement();
            reader.Skip();
        }

        public override void WriteConfig(Configuration cfg, XmlWriter writer, ConfigurationSaveMode mode)
        {
            writer.WriteStartElement("section");
            writer.WriteAttributeString("name", Name);
            writer.WriteAttributeString("type", TypeName);
            if (!_allowLocation)
                writer.WriteAttributeString("allowLocation", "false");
            if (_allowDefinition != ConfigurationAllowDefinition.Everywhere)
                writer.WriteAttributeString("allowDefinition", _allowDefinition.ToString());
            if (_allowExeDefinition != ConfigurationAllowExeDefinition.MachineToApplication)
                writer.WriteAttributeString("allowExeDefinition", _allowExeDefinition.ToString());
            if (!_requirePermission)
                writer.WriteAttributeString("requirePermission", "false");
            writer.WriteEndElement();
        }

        public override void ReadData(Configuration config, XmlReader reader, bool overrideAllowed)
        {
            if (!config.HasFile && !_allowLocation)
                throw new ConfigurationErrorsException(
                    "The configuration section <" + Name + "> cannot be defined inside a <location> element.", reader);
            if (!config.ConfigHost.IsDefinitionAllowed(config.ConfigPath, _allowDefinition, _allowExeDefinition))
            {
                var ctx = _allowExeDefinition != ConfigurationAllowExeDefinition.MachineToApplication
                    ? _allowExeDefinition
                    : (object) _allowDefinition;
                throw new ConfigurationErrorsException(
                    "The section <" + Name +
                    "> can't be defined in this configuration file (the allowed definition context is '" + ctx + "').",
                    reader);
            }
            if (config.GetSectionXml(this) != null)
                ThrowException("The section <" + Name + "> is defined more than once in the same configuration file.",
                    reader);
            config.SetSectionXml(this, reader.ReadOuterXml());
        }

        public override void WriteData(Configuration config, XmlWriter writer, ConfigurationSaveMode mode)
        {
            string xml;

            var section = config.GetSectionInstance(this, false);
            if (section != null)
            {
                var parentSection = config.Parent != null ? config.Parent.GetSectionInstance(this, false) : null;
                xml = section.SerializeSection(parentSection, Name, mode);

                var externalDataXml = section.ExternalDataXml;
                var filePath = config.FilePath;

                if (!string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(externalDataXml))
                {
                    string path = Path.Combine(Path.GetDirectoryName(filePath), section.SectionInformation.ConfigSource);

                    using (var sw = File.CreateText(path))
                    {
                        sw.Write(externalDataXml);
                    }
                }
            }
            else
            {
                xml = config.GetSectionXml(this);
            }

            if (!string.IsNullOrEmpty(xml))
            {
                writer.WriteRaw(xml);
/*				XmlTextReader tr = new XmlTextReader (new StringReader (xml));
				writer.WriteNode (tr, true);
				tr.Close ();*/
            }
        }

        internal override void Merge(ConfigInfo data)
        {
        }

        internal override bool HasValues(Configuration config, ConfigurationSaveMode mode)
        {
            var section = config.GetSectionInstance(this, false);
            if (section == null)
                return false;

            var parent = config.Parent != null ? config.Parent.GetSectionInstance(this, false) : null;
            return section.HasValues(parent, mode);
        }

        internal override void ResetModified(Configuration config)
        {
            var section = config.GetSectionInstance(this, false);
            if (section != null)
            {
            }
        }
    }
}