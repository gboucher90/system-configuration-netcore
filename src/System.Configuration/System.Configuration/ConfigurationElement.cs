//
// System.Configuration.ConfigurationElement.cs
//
// Authors:
//	Duncan Mak (duncan@ximian.com)
// 	Lluis Sanchez Gual (lluis@novell.com)
// 	Martin Baulig <martin.baulig@xamarin.com>
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
// Copyright (c) 2012 Xamarin Inc. (http://www.xamarin.com)
//

using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Xml;

namespace System.Configuration
{
    public abstract class ConfigurationElement
    {
        private ConfigurationElementCollection _defaultCollection;
        private ElementInformation _elementInfo;
        private ConfigurationElementProperty _elementProperty;
        private ConfigurationPropertyCollection _keyProps;

        private ConfigurationLockCollection _lockAllAttributesExcept;

        private ConfigurationLockCollection _lockAllElementsExcept;

        private ConfigurationLockCollection _lockAttributes;

        private ConfigurationLockCollection _lockElements;

        private ElementMap _map;
        private bool _modified;
        private string _rawXml;
        private bool _readOnly;

        private SaveContext _saveContext;

        internal Configuration Configuration { get; set; }

        public ElementInformation ElementInformation
        {
            get
            {
                if (_elementInfo == null)
                    _elementInfo = new ElementInformation(this, null);
                return _elementInfo;
            }
        }

        internal string RawXml
        {
            get { return _rawXml; }
            set
            {
                // FIXME: this hack is nasty. We should make
                // some refactory on the entire assembly.
                if (_rawXml == null || value != null)
                    _rawXml = value;
            }
        }

        protected internal virtual ConfigurationElementProperty ElementProperty
        {
            get
            {
                if (_elementProperty == null)
                    _elementProperty = new ConfigurationElementProperty(ElementInformation.Validator);
                return _elementProperty;
            }
        }

        protected ContextInformation EvaluationContext
        {
            get
            {
                if (Configuration != null)
                    return Configuration.EvaluationContext;
                throw new ConfigurationErrorsException(
                    "This element is not currently associated with any context.");
            }
        }

        public ConfigurationLockCollection LockAllAttributesExcept
        {
            get
            {
                if (_lockAllAttributesExcept == null)
                {
                    _lockAllAttributesExcept = new ConfigurationLockCollection(this,
                        ConfigurationLockType.Attribute | ConfigurationLockType.Exclude);
                }

                return _lockAllAttributesExcept;
            }
        }

        public ConfigurationLockCollection LockAllElementsExcept
        {
            get
            {
                if (_lockAllElementsExcept == null)
                {
                    _lockAllElementsExcept = new ConfigurationLockCollection(this,
                        ConfigurationLockType.Element | ConfigurationLockType.Exclude);
                }

                return _lockAllElementsExcept;
            }
        }

        public ConfigurationLockCollection LockAttributes
        {
            get
            {
                if (_lockAttributes == null)
                {
                    _lockAttributes = new ConfigurationLockCollection(this, ConfigurationLockType.Attribute);
                }

                return _lockAttributes;
            }
        }

        public ConfigurationLockCollection LockElements
        {
            get
            {
                if (_lockElements == null)
                {
                    _lockElements = new ConfigurationLockCollection(this, ConfigurationLockType.Element);
                }

                return _lockElements;
            }
        }

        public bool LockItem { get; set; }

        protected internal object this[ConfigurationProperty property]
        {
            get { return this[property.Name]; }
            set { this[property.Name] = value; }
        }

        protected internal object this[string propertyName]
        {
            get
            {
                var pi = ElementInformation.Properties[propertyName];
                if (pi == null)
                    throw new InvalidOperationException("Property '" + propertyName +
                                                        "' not found in configuration element");

                return pi.Value;
            }

            set
            {
                var pi = ElementInformation.Properties[propertyName];
                if (pi == null)
                    throw new InvalidOperationException("Property '" + propertyName +
                                                        "' not found in configuration element");

                SetPropertyValue(pi.Property, value, false);

                pi.Value = value;
                _modified = true;
            }
        }

        protected internal virtual ConfigurationPropertyCollection Properties
        {
            get
            {
                if (_map == null)
                    _map = ElementMap.GetMap(GetType());
                return _map.Properties;
            }
        }

        internal bool IsElementPresent { get; private set; }

        internal virtual void InitFromProperty(PropertyInformation propertyInfo)
        {
            _elementInfo = new ElementInformation(this, propertyInfo);
            Init();
        }

        protected internal virtual void Init()
        {
        }


        protected virtual void ListErrors(IList list)
        {
            throw new NotImplementedException();
        }


        protected void SetPropertyValue(ConfigurationProperty prop, object value, bool ignoreLocks)
        {
            try
            {
                if (value != null)
                {
                    /* XXX all i know for certain is that Validation happens here */
                    prop.Validate(value);

                    /* XXX presumably the actual setting of the
					 * property happens here instead of in the
					 * set_Item code below, but that would mean
					 * the Value needs to be stuffed in the
					 * property, not the propertyinfo (or else the
					 * property needs a ref to the property info
					 * to correctly set the value). */
                }
            }
            catch (Exception e)
            {
                throw new ConfigurationErrorsException(
                    string.Format("The value for the property '{0}' on type {1} is not valid.", prop.Name,
                        ElementInformation.Type), e);
            }
        }

        internal ConfigurationPropertyCollection GetKeyProperties()
        {
            if (_keyProps != null) return _keyProps;

            var tmpkeyProps = new ConfigurationPropertyCollection();
            foreach (ConfigurationProperty prop in Properties)
            {
                if (prop.IsKey)
                    tmpkeyProps.Add(prop);
            }

            return _keyProps = tmpkeyProps;
        }

        internal ConfigurationElementCollection GetDefaultCollection()
        {
            if (_defaultCollection != null) return _defaultCollection;

            ConfigurationProperty defaultCollectionProp = null;

            foreach (ConfigurationProperty prop in Properties)
            {
                if (prop.IsDefaultCollection)
                {
                    defaultCollectionProp = prop;
                    break;
                }
            }

            if (defaultCollectionProp != null)
            {
                _defaultCollection = this[defaultCollectionProp] as ConfigurationElementCollection;
            }

            return _defaultCollection;
        }

        public override bool Equals(object compareTo)
        {
            var other = compareTo as ConfigurationElement;
            if (other == null) return false;
            if (GetType() != other.GetType()) return false;

            foreach (ConfigurationProperty prop in Properties)
            {
                if (!Equals(this[prop], other[prop]))
                    return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            var code = 0;
            object o;

            foreach (ConfigurationProperty prop in Properties)
            {
                o = this[prop];
                if (o == null)
                    continue;

                code += o.GetHashCode();
            }

            return code;
        }

        internal virtual bool HasLocalModifications()
        {
            foreach (PropertyInformation pi in ElementInformation.Properties)
                if (pi.ValueOrigin == PropertyValueOrigin.SetHere && pi.IsModified)
                    return true;

            return false;
        }

        protected internal virtual void DeserializeElement(XmlReader reader, bool serializeCollectionKey)
        {
            var readProps = new Hashtable();

            reader.MoveToContent();
            IsElementPresent = true;

            while (reader.MoveToNextAttribute())
            {
                var prop = ElementInformation.Properties[reader.LocalName];
                if (prop == null || (serializeCollectionKey && !prop.IsKey))
                {
                    /* handle the built in ConfigurationElement attributes here */
                    if (reader.LocalName == "lockAllAttributesExcept")
                    {
                        LockAllAttributesExcept.SetFromList(reader.Value);
                    }
                    else if (reader.LocalName == "lockAllElementsExcept")
                    {
                        LockAllElementsExcept.SetFromList(reader.Value);
                    }
                    else if (reader.LocalName == "lockAttributes")
                    {
                        LockAttributes.SetFromList(reader.Value);
                    }
                    else if (reader.LocalName == "lockElements")
                    {
                        LockElements.SetFromList(reader.Value);
                    }
                    else if (reader.LocalName == "lockItem")
                    {
                        LockItem = reader.Value.ToLowerInvariant() == "true";
                    }
                    else if (reader.LocalName == "xmlns")
                    {
                        /* ignore */
                    }
                    else if (this is ConfigurationSection && reader.LocalName == "configSource")
                    {
                        /* ignore */
                    }
                    else if (!OnDeserializeUnrecognizedAttribute(reader.LocalName, reader.Value))
                        throw new ConfigurationErrorsException("Unrecognized attribute '" + reader.LocalName + "'.",
                            reader);

                    continue;
                }

                if (readProps.ContainsKey(prop))
                    throw new ConfigurationErrorsException(
                        "The attribute '" + prop.Name + "' may only appear once in this element.", reader);

                string value = null;
                try
                {
                    value = reader.Value;
                    ValidateValue(prop.Property, value);
                    prop.SetStringValue(value);
                }
                catch (ConfigurationErrorsException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    var msg = string.Format("The value for the property '{0}' is not valid. The error is: {1}",
                        prop.Name, ex.Message);
                    throw new ConfigurationErrorsException(msg, reader);
                }
                readProps[prop] = prop.Name;
            }

            reader.MoveToElement();
            if (reader.IsEmptyElement)
            {
                reader.Skip();
            }
            else
            {
                int depth = reader.Depth;

                reader.ReadStartElement();
                reader.MoveToContent();

                do
                {
                    if (reader.NodeType != XmlNodeType.Element)
                    {
                        reader.Skip();
                        continue;
                    }

                    var prop = ElementInformation.Properties[reader.LocalName];
                    if (prop == null || (serializeCollectionKey && !prop.IsKey))
                    {
                        if (!OnDeserializeUnrecognizedElement(reader.LocalName, reader))
                        {
                            if (prop == null)
                            {
                                var c = GetDefaultCollection();
                                if (c != null && c.OnDeserializeUnrecognizedElement(reader.LocalName, reader))
                                    continue;
                            }
                            throw new ConfigurationErrorsException("Unrecognized element '" + reader.LocalName + "'.",
                                reader);
                        }
                        continue;
                    }

                    if (!prop.IsElement)
                        throw new ConfigurationErrorsException("Property '" + prop.Name +
                                                               "' is not a ConfigurationElement.");

                    if (readProps.Contains(prop))
                        throw new ConfigurationErrorsException(
                            "The element <" + prop.Name + "> may only appear once in this section.", reader);

                    var val = (ConfigurationElement) prop.Value;
                    val.DeserializeElement(reader, serializeCollectionKey);
                    readProps[prop] = prop.Name;

                    if (depth == reader.Depth)
                        reader.Read();
                } while (depth < reader.Depth);
            }

            _modified = false;

            foreach (PropertyInformation prop in ElementInformation.Properties)
                if (!string.IsNullOrEmpty(prop.Name) && prop.IsRequired && !readProps.ContainsKey(prop))
                {
                    var p = ElementInformation.Properties[prop.Name];
                    if (p == null)
                    {
                        var val = OnRequiredPropertyNotFound(prop.Name);
                        if (!Equals(val, prop.DefaultValue))
                        {
                            prop.Value = val;
                            prop.IsModified = false;
                        }
                    }
                }

            PostDeserialize();
        }

        protected virtual bool OnDeserializeUnrecognizedAttribute(string name, string value)
        {
            return false;
        }

        protected virtual bool OnDeserializeUnrecognizedElement(string element, XmlReader reader)
        {
            return false;
        }

        protected virtual object OnRequiredPropertyNotFound(string name)
        {
            throw new ConfigurationErrorsException("Required attribute '" + name + "' not found.");
        }

        protected virtual void PreSerialize(XmlWriter writer)
        {
        }

        protected virtual void PostDeserialize()
        {
        }

        protected internal virtual void InitializeDefault()
        {
        }

        protected internal virtual bool IsModified()
        {
            if (_modified)
                return true;

            foreach (PropertyInformation prop in ElementInformation.Properties)
            {
                if (!prop.IsElement)
                    continue;
                var element = prop.Value as ConfigurationElement;
                if ((element == null) || !element.IsModified())
                    continue;

                _modified = true;
                break;
            }

            return _modified;
        }

        protected internal virtual void SetReadOnly()
        {
            _readOnly = true;
        }

        public virtual bool IsReadOnly()
        {
            return _readOnly;
        }

        protected internal virtual void Reset(ConfigurationElement parentElement)
        {
            IsElementPresent = false;

            if (parentElement != null)
                ElementInformation.Reset(parentElement.ElementInformation);
            else
                InitializeDefault();
        }

        protected internal virtual void ResetModified()
        {
            _modified = false;

            foreach (PropertyInformation p in ElementInformation.Properties)
            {
                p.IsModified = false;

                var element = p.Value as ConfigurationElement;
                if (element != null)
                    element.ResetModified();
            }
        }

        protected internal virtual bool SerializeElement(XmlWriter writer, bool serializeCollectionKey)
        {
            PreSerialize(writer);

            if (serializeCollectionKey)
            {
                var props = GetKeyProperties();
                foreach (ConfigurationProperty prop in props)
                    writer.WriteAttributeString(prop.Name, prop.ConvertToString(this[prop.Name]));
                return props.Count > 0;
            }

            var wroteData = false;

            foreach (PropertyInformation prop in ElementInformation.Properties)
            {
                if (prop.IsElement)
                    continue;

                if (_saveContext == null)
                    throw new InvalidOperationException();
                if (!_saveContext.HasValue(prop))
                    continue;

                writer.WriteAttributeString(prop.Name, prop.GetStringValue());
                wroteData = true;
            }

            foreach (PropertyInformation prop in ElementInformation.Properties)
            {
                if (!prop.IsElement)
                    continue;

                var val = (ConfigurationElement) prop.Value;
                if (val != null)
                    wroteData = val.SerializeToXmlElement(writer, prop.Name) || wroteData;
            }
            return wroteData;
        }

        protected internal virtual bool SerializeToXmlElement(
            XmlWriter writer, string elementName)
        {
            if (_saveContext == null)
                throw new InvalidOperationException();
            if (!_saveContext.HasValues())
                return false;

            if (elementName != null && elementName != "")
                writer.WriteStartElement(elementName);
            var res = SerializeElement(writer, false);
            if (elementName != null && elementName != "")
                writer.WriteEndElement();
            return res;
        }

        protected internal virtual void Unmerge(
            ConfigurationElement source, ConfigurationElement parent,
            ConfigurationSaveMode updateMode)
        {
            if (parent != null && source.GetType() != parent.GetType())
                throw new ConfigurationErrorsException("Can't unmerge two elements of different type");

            var isMinimalOrModified = updateMode == ConfigurationSaveMode.Minimal ||
                                      updateMode == ConfigurationSaveMode.Modified;

            foreach (PropertyInformation prop in source.ElementInformation.Properties)
            {
                if (prop.ValueOrigin == PropertyValueOrigin.Default)
                    continue;

                var unmergedProp = ElementInformation.Properties[prop.Name];

                var sourceValue = prop.Value;
                if (parent == null || !parent.HasValue(prop.Name))
                {
                    unmergedProp.Value = sourceValue;
                    continue;
                }

                if (sourceValue == null)
                    continue;

                var parentValue = parent[prop.Name];
                if (!prop.IsElement)
                {
                    if (!Equals(sourceValue, parentValue) ||
                        (updateMode == ConfigurationSaveMode.Full) ||
                        (updateMode == ConfigurationSaveMode.Modified && prop.ValueOrigin == PropertyValueOrigin.SetHere))
                        unmergedProp.Value = sourceValue;
                    continue;
                }

                var sourceElement = (ConfigurationElement) sourceValue;
                if (isMinimalOrModified && !sourceElement.IsModified())
                    continue;
                if (parentValue == null)
                {
                    unmergedProp.Value = sourceValue;
                    continue;
                }

                var parentElement = (ConfigurationElement) parentValue;
                var copy = (ConfigurationElement) unmergedProp.Value;
                copy.Unmerge(sourceElement, parentElement, updateMode);
            }
        }

        internal bool HasValue(string propName)
        {
            var info = ElementInformation.Properties[propName];
            return info != null && info.ValueOrigin != PropertyValueOrigin.Default;
        }

        internal bool IsReadFromConfig(string propName)
        {
            var info = ElementInformation.Properties[propName];
            return info != null && info.ValueOrigin == PropertyValueOrigin.SetHere;
        }

        private void ValidateValue(ConfigurationProperty p, string value)
        {
            ConfigurationValidatorBase validator;
            if (p == null || (validator = p.Validator) == null)
                return;

            if (!validator.CanValidate(p.Type))
                throw new ConfigurationErrorsException(
                    string.Format("Validator does not support type {0}", p.Type));
            validator.Validate(p.ConvertFromString(value));
        }

        /*
		 * FIXME: LAMESPEC
		 * 
		 * SerializeElement() and SerializeToXmlElement() need to emit different output
		 * based on the ConfigurationSaveMode that's being used.  Unfortunately, neither
		 * of these methods take it as an argument and there seems to be no documented way
		 * how to get it.
		 * 
		 * The parent element is needed because the element could be set to a different
		 * than the default value in a parent configuration file, then set locally to that
		 * same value.  This makes the element appear locally modified (so it's included
		 * with ConfigurationSaveMode.Modified), but it should not be emitted with
		 * ConfigurationSaveMode.Minimal.
		 * 
		 * In theory, we could save it into some private field in Unmerge(), but the
		 * problem is that Unmerge() is kinda expensive and we also need a way of
		 * determining whether or not the configuration has changed in Configuration.Save(),
		 * prior to opening the output file for writing.
		 * 
		 * There are two places from where HasValues() is called:
		 * a) From Configuration.Save() / SaveAs() to check whether the configuration needs
		 *    to be saved.  This check is done prior to opening the file for writing.
		 * b) From SerializeToXmlElement() to check whether to emit the element, using the
		 *    parent and mode values from the cached 'SaveContext'.
		 * 
		 */

        /*
		 * Check whether property 'prop' should be included in the serialized XML
		 * based on the current ConfigurationSaveMode.
		 */

        internal bool HasValue(ConfigurationElement parent, PropertyInformation prop,
            ConfigurationSaveMode mode)
        {
            if (prop.ValueOrigin == PropertyValueOrigin.Default)
                return false;

            if (mode == ConfigurationSaveMode.Modified &&
                prop.ValueOrigin == PropertyValueOrigin.SetHere && prop.IsModified)
            {
                // Value has been modified locally, so we always emit it
                // with ConfigurationSaveMode.Modified.
                return true;
            }

            /*
			 * Ok, now we have to check whether we're different from the inherited
			 * value - which could either be a value that's set in a parent
			 * configuration file or the default value.
			 */

            var hasParentValue = parent != null && parent.HasValue(prop.Name);
            var parentOrDefault = hasParentValue ? parent[prop.Name] : prop.DefaultValue;

            if (!prop.IsElement)
                return !Equals(prop.Value, parentOrDefault);

            /*
			 * Ok, it's an element that has been set in a parent configuration file.			 * 
			 * Recursively call HasValues() to check whether it's been locally modified.
			 */
            var element = (ConfigurationElement) prop.Value;
            var parentElement = (ConfigurationElement) parentOrDefault;

            return element.HasValues(parentElement, mode);
        }

        /*
		 * Check whether this element should be included in the serialized XML
		 * based on the current ConfigurationSaveMode.
		 * 
		 * The 'parent' value is needed to determine whether the element currently
		 * has a different value from what's been set in the parent configuration
		 * hierarchy.
		 */

        internal virtual bool HasValues(ConfigurationElement parent, ConfigurationSaveMode mode)
        {
            if (mode == ConfigurationSaveMode.Full)
                return true;
            if (_modified && (mode == ConfigurationSaveMode.Modified))
                return true;

            foreach (PropertyInformation prop in ElementInformation.Properties)
            {
                if (HasValue(parent, prop, mode))
                    return true;
            }

            return false;
        }

        /*
		 * Cache the current 'parent' and 'mode' values for later use in SerializeToXmlElement()
		 * and SerializeElement().
		 * 
		 * Make sure to call base when overriding this in a derived class.
		 */

        internal virtual void PrepareSave(ConfigurationElement parent, ConfigurationSaveMode mode)
        {
            _saveContext = new SaveContext(this, parent, mode);

            foreach (PropertyInformation prop in ElementInformation.Properties)
            {
                if (!prop.IsElement)
                    continue;

                var elem = (ConfigurationElement) prop.Value;
                if (parent == null || !parent.HasValue(prop.Name))
                    elem.PrepareSave(null, mode);
                else
                {
                    var parentValue = (ConfigurationElement) parent[prop.Name];
                    elem.PrepareSave(parentValue, mode);
                }
            }
        }

        private class SaveContext
        {
            public readonly ConfigurationElement Element;
            public readonly ConfigurationSaveMode Mode;
            public readonly ConfigurationElement Parent;

            public SaveContext(ConfigurationElement element, ConfigurationElement parent,
                ConfigurationSaveMode mode)
            {
                Element = element;
                Parent = parent;
                Mode = mode;
            }

            public bool HasValues()
            {
                if (Mode == ConfigurationSaveMode.Full)
                    return true;
                return Element.HasValues(Parent, Mode);
            }

            public bool HasValue(PropertyInformation prop)
            {
                if (Mode == ConfigurationSaveMode.Full)
                    return true;
                return Element.HasValue(Parent, prop, Mode);
            }
        }
    }

    internal class ElementMap
    {
        private static readonly Hashtable ElementMaps = Hashtable.Synchronized(new Hashtable());

        public ElementMap(Type t)
        {
            Properties = new ConfigurationPropertyCollection();

            CollectionAttribute =
                t.GetTypeInfo().GetCustomAttribute(typeof(ConfigurationCollectionAttribute)) as
                    ConfigurationCollectionAttribute;
            PropertyInfo[] props =
                t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                                BindingFlags.Instance);
            foreach (var prop in props)
            {
                var at =
                    prop.GetCustomAttribute(typeof(ConfigurationPropertyAttribute)) as ConfigurationPropertyAttribute;
                if (at == null) continue;
                string name = at.Name != null ? at.Name : prop.Name;

                var validatorAttr =
                    prop.GetCustomAttribute(typeof(ConfigurationValidatorAttribute)) as ConfigurationValidatorAttribute;
                var validator = validatorAttr != null ? validatorAttr.ValidatorInstance : null;


                var convertAttr = (TypeConverterAttribute) prop.GetCustomAttribute(typeof(TypeConverterAttribute));
                var converter = convertAttr != null
                    ? (TypeConverter) Activator.CreateInstance(Type.GetType(convertAttr.ConverterTypeName), true)
                    : null;
                var cp = new ConfigurationProperty(name, prop.PropertyType, at.DefaultValue, converter, validator,
                    at.Options);

                cp.CollectionAttribute =
                    prop.GetCustomAttribute(typeof(ConfigurationCollectionAttribute)) as
                        ConfigurationCollectionAttribute;
                Properties.Add(cp);
            }
        }

        public ConfigurationCollectionAttribute CollectionAttribute { get; }

        public bool HasProperties
        {
            get { return Properties.Count > 0; }
        }

        public ConfigurationPropertyCollection Properties { get; }

        public static ElementMap GetMap(Type t)
        {
            var map = ElementMaps[t] as ElementMap;
            if (map != null) return map;
            map = new ElementMap(t);
            ElementMaps[t] = map;
            return map;
        }
    }
}