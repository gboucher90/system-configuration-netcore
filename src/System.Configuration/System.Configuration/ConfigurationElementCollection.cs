//
// System.Configuration.ConfigurationElementCollection.cs
//
// Authors:
//	Tim Coleman (tim@timcoleman.com)
// 	Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (C) Tim Coleman, 2004
// Copyright (c) 2012 Xamarin Inc. (http://www.xamarin.com)

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

using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Xml;

namespace System.Configuration
{
    [DebuggerDisplay("Count = {Count}")]
    public abstract partial class ConfigurationElementCollection : ConfigurationElement, ICollection, IEnumerable
    {
        private readonly IComparer _comparer;
        private readonly ArrayList _list = new ArrayList();

        private ArrayList _inherited;
        private int _inheritedLimitIndex;
        private bool _modified;
        private ArrayList _removed;

        #region Constructors

        protected ConfigurationElementCollection()
        {
        }

        protected ConfigurationElementCollection(IComparer comparer)
        {
            _comparer = comparer;
        }

        internal override void InitFromProperty(PropertyInformation propertyInfo)
        {
            var colat = propertyInfo.Property.CollectionAttribute;

            if (colat == null)
                colat =
                    propertyInfo.Type.GetTypeInfo().GetCustomAttribute(typeof(ConfigurationCollectionAttribute)) as
                        ConfigurationCollectionAttribute;

            if (colat != null)
            {
                AddElementName = colat.AddItemName;
                ClearElementName = colat.ClearItemsName;
                RemoveElementName = colat.RemoveItemName;
            }
            base.InitFromProperty(propertyInfo);
        }

        #endregion // Constructors

        #region Properties

        public virtual ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.AddRemoveClearMap; }
        }

        private bool IsBasic
        {
            get
            {
                return CollectionType == ConfigurationElementCollectionType.BasicMap ||
                       CollectionType == ConfigurationElementCollectionType.BasicMapAlternate;
            }
        }

        private bool IsAlternate
        {
            get
            {
                return CollectionType == ConfigurationElementCollectionType.AddRemoveClearMapAlternate ||
                       CollectionType == ConfigurationElementCollectionType.BasicMapAlternate;
            }
        }

        public int Count
        {
            get { return _list.Count; }
        }

        protected virtual string ElementName
        {
            get { return string.Empty; }
        }

        public bool EmitClear { get; set; }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object SyncRoot
        {
            get { return this; }
        }

        protected virtual bool ThrowOnDuplicate
        {
            get
            {
                if (CollectionType != ConfigurationElementCollectionType.AddRemoveClearMap &&
                    CollectionType != ConfigurationElementCollectionType.AddRemoveClearMapAlternate)
                    return false;

                return true;
            }
        }

        protected internal string AddElementName { get; set; } = "add";

        protected internal string ClearElementName { get; set; } = "clear";

        protected internal string RemoveElementName { get; set; } = "remove";

        #endregion // Properties

        #region Methods

        protected virtual void BaseAdd(ConfigurationElement element)
        {
            BaseAdd(element, ThrowOnDuplicate);
        }

        protected void BaseAdd(ConfigurationElement element, bool throwIfExists)
        {
            if (IsReadOnly())
                throw new ConfigurationErrorsException("Collection is read only.");

            if (IsAlternate)
            {
                _list.Insert(_inheritedLimitIndex, element);
                _inheritedLimitIndex++;
            }
            else
            {
                var oldIndex = IndexOfKey(GetElementKey(element));
                if (oldIndex >= 0)
                {
                    if (element.Equals(_list[oldIndex]))
                        return;
                    if (throwIfExists)
                        throw new ConfigurationErrorsException("Duplicate element in collection");
                    _list.RemoveAt(oldIndex);
                }
                _list.Add(element);
            }

            _modified = true;
        }

        protected virtual void BaseAdd(int index, ConfigurationElement element)
        {
            if (ThrowOnDuplicate && BaseIndexOf(element) != -1)
                throw new ConfigurationErrorsException("Duplicate element in collection");
            if (IsReadOnly())
                throw new ConfigurationErrorsException("Collection is read only.");

            if (IsAlternate && (index > _inheritedLimitIndex))
                throw new ConfigurationErrorsException("Can't insert new elements below the inherited elements.");
            if (!IsAlternate && (index <= _inheritedLimitIndex))
                throw new ConfigurationErrorsException("Can't insert new elements above the inherited elements.");

            _list.Insert(index, element);
            _modified = true;
        }

        protected internal void BaseClear()
        {
            if (IsReadOnly())
                throw new ConfigurationErrorsException("Collection is read only.");

            _list.Clear();
            _modified = true;
        }

        protected internal ConfigurationElement BaseGet(int index)
        {
            return (ConfigurationElement) _list[index];
        }

        protected internal ConfigurationElement BaseGet(object key)
        {
            var index = IndexOfKey(key);
            if (index != -1) return (ConfigurationElement) _list[index];
            return null;
        }

        protected internal object[] BaseGetAllKeys()
        {
            var keys = new object[_list.Count];
            for (var n = 0; n < _list.Count; n++)
                keys[n] = BaseGetKey(n);
            return keys;
        }

        protected internal object BaseGetKey(int index)
        {
            if (index < 0 || index >= _list.Count)
                throw new ConfigurationErrorsException(string.Format("Index {0} is out of range", index));

            return GetElementKey((ConfigurationElement) _list[index]).ToString();
        }

        protected int BaseIndexOf(ConfigurationElement element)
        {
            return _list.IndexOf(element);
        }

        private int IndexOfKey(object key)
        {
            for (var n = 0; n < _list.Count; n++)
            {
                if (CompareKeys(GetElementKey((ConfigurationElement) _list[n]), key))
                    return n;
            }
            return -1;
        }

        protected internal bool BaseIsRemoved(object key)
        {
            if (_removed == null)
                return false;
            foreach (ConfigurationElement elem in _removed)
            {
                if (CompareKeys(GetElementKey(elem), key))
                    return true;
            }
            return false;
        }

        protected internal void BaseRemove(object key)
        {
            if (IsReadOnly())
                throw new ConfigurationErrorsException("Collection is read only.");

            var index = IndexOfKey(key);
            if (index != -1)
            {
                BaseRemoveAt(index);
                _modified = true;
            }
        }

        protected internal void BaseRemoveAt(int index)
        {
            if (IsReadOnly())
                throw new ConfigurationErrorsException("Collection is read only.");

            var elem = (ConfigurationElement) _list[index];
            if (!IsElementRemovable(elem))
                throw new ConfigurationErrorsException("Element can't be removed from element collection.");

            if (_inherited != null && _inherited.Contains(elem))
                throw new ConfigurationErrorsException("Inherited items can't be removed.");

            _list.RemoveAt(index);

            if (IsAlternate)
            {
                if (_inheritedLimitIndex > 0)
                    _inheritedLimitIndex--;
            }

            _modified = true;
        }

        private bool CompareKeys(object key1, object key2)
        {
            if (_comparer != null)
                return _comparer.Compare(key1, key2) == 0;
            return Equals(key1, key2);
        }

        public void CopyTo(ConfigurationElement[] array, int index)
        {
            _list.CopyTo(array, index);
        }

        protected abstract ConfigurationElement CreateNewElement();

        protected virtual ConfigurationElement CreateNewElement(string elementName)
        {
            return CreateNewElement();
        }

        private ConfigurationElement CreateNewElementInternal(string elementName)
        {
            ConfigurationElement elem;
            if (elementName == null)
                elem = CreateNewElement();
            else
                elem = CreateNewElement(elementName);
            elem.Init();
            return elem;
        }

        public override bool Equals(object compareTo)
        {
            var other = compareTo as ConfigurationElementCollection;
            if (other == null) return false;
            if (GetType() != other.GetType()) return false;
            if (Count != other.Count) return false;

            for (var n = 0; n < Count; n++)
            {
                if (!BaseGet(n).Equals(other.BaseGet(n)))
                    return false;
            }
            return true;
        }

        protected abstract object GetElementKey(ConfigurationElement element);

        public override int GetHashCode()
        {
            var code = 0;
            for (var n = 0; n < Count; n++)
                code += BaseGet(n).GetHashCode();
            return code;
        }

        void ICollection.CopyTo(Array arr, int index)
        {
            _list.CopyTo(arr, index);
        }

        public IEnumerator GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        protected virtual bool IsElementName(string elementName)
        {
            return false;
        }

        protected virtual bool IsElementRemovable(ConfigurationElement element)
        {
            return !IsReadOnly();
        }

        protected internal override bool IsModified()
        {
            if (_modified)
                return true;

            for (var n = 0; n < _list.Count; n++)
            {
                var elem = (ConfigurationElement) _list[n];
                if (!elem.IsModified())
                    continue;
                _modified = true;
                break;
            }

            return _modified;
        }


        public override bool IsReadOnly()
        {
            return base.IsReadOnly();
        }

        internal override void PrepareSave(ConfigurationElement parentElement, ConfigurationSaveMode mode)
        {
            var parent = (ConfigurationElementCollection) parentElement;
            base.PrepareSave(parentElement, mode);

            for (var n = 0; n < _list.Count; n++)
            {
                var elem = (ConfigurationElement) _list[n];
                var key = GetElementKey(elem);
                var pitem = parent != null ? parent.BaseGet(key) : null;

                elem.PrepareSave(pitem, mode);
            }
        }

        internal override bool HasValues(ConfigurationElement parentElement, ConfigurationSaveMode mode)
        {
            var parent = (ConfigurationElementCollection) parentElement;

            if (mode == ConfigurationSaveMode.Full)
                return _list.Count > 0;

            for (var n = 0; n < _list.Count; n++)
            {
                var elem = (ConfigurationElement) _list[n];
                var key = GetElementKey(elem);
                var pitem = parent != null ? parent.BaseGet(key) : null;

                if (elem.HasValues(pitem, mode))
                    return true;
            }

            return false;
        }

        protected internal override void Reset(ConfigurationElement parentElement)
        {
            var basic = IsBasic;

            var parent = (ConfigurationElementCollection) parentElement;
            for (var n = 0; n < parent.Count; n++)
            {
                var parentItem = parent.BaseGet(n);
                var item = CreateNewElementInternal(null);
                item.Reset(parentItem);
                BaseAdd(item);

                if (basic)
                {
                    if (_inherited == null)
                        _inherited = new ArrayList();
                    _inherited.Add(item);
                }
            }
            if (IsAlternate)
                _inheritedLimitIndex = 0;
            else
                _inheritedLimitIndex = Count - 1;
            _modified = false;
        }

        protected internal override void ResetModified()
        {
            _modified = false;
            for (var n = 0; n < _list.Count; n++)
            {
                var elem = (ConfigurationElement) _list[n];
                elem.ResetModified();
            }
        }


        protected internal override void SetReadOnly()
        {
            base.SetReadOnly();
        }

        protected internal override bool SerializeElement(XmlWriter writer, bool serializeCollectionKey)
        {
            if (serializeCollectionKey)
            {
                return base.SerializeElement(writer, true);
            }

            var wroteData = false;

            if (IsBasic)
            {
                for (var n = 0; n < _list.Count; n++)
                {
                    var elem = (ConfigurationElement) _list[n];
                    if (ElementName != string.Empty)
                        wroteData = elem.SerializeToXmlElement(writer, ElementName) || wroteData;
                    else
                        wroteData = elem.SerializeElement(writer, false) || wroteData;
                }
            }
            else
            {
                if (EmitClear)
                {
                    writer.WriteElementString(ClearElementName, "");
                    wroteData = true;
                }

                if (_removed != null)
                {
                    for (var n = 0; n < _removed.Count; n++)
                    {
                        writer.WriteStartElement(RemoveElementName);
                        ((ConfigurationElement) _removed[n]).SerializeElement(writer, true);
                        writer.WriteEndElement();
                    }
                    wroteData = wroteData || _removed.Count > 0;
                }

                for (var n = 0; n < _list.Count; n++)
                {
                    var elem = (ConfigurationElement) _list[n];
                    elem.SerializeToXmlElement(writer, AddElementName);
                }

                wroteData = wroteData || _list.Count > 0;
            }
            return wroteData;
        }

        protected override bool OnDeserializeUnrecognizedElement(string elementName, XmlReader reader)
        {
            if (IsBasic)
            {
                ConfigurationElement elem = null;

                if (elementName == ElementName)
                    elem = CreateNewElementInternal(null);
                if (IsElementName(elementName))
                    elem = CreateNewElementInternal(elementName);

                if (elem != null)
                {
                    elem.DeserializeElement(reader, false);
                    BaseAdd(elem);
                    _modified = false;
                    return true;
                }
            }
            else
            {
                if (elementName == ClearElementName)
                {
                    reader.MoveToContent();
                    if (reader.MoveToNextAttribute())
                        throw new ConfigurationErrorsException("Unrecognized attribute '" + reader.LocalName + "'.");
                    reader.MoveToElement();
                    reader.Skip();
                    BaseClear();
                    EmitClear = true;
                    _modified = false;
                    return true;
                }
                if (elementName == RemoveElementName)
                {
                    var elem = CreateNewElementInternal(null);
                    var removeElem = new ConfigurationRemoveElement(elem, this);
                    removeElem.DeserializeElement(reader, true);
                    BaseRemove(removeElem.KeyValue);
                    _modified = false;
                    return true;
                }
                if (elementName == AddElementName)
                {
                    var elem = CreateNewElementInternal(null);
                    elem.DeserializeElement(reader, false);
                    BaseAdd(elem);
                    _modified = false;
                    return true;
                }
            }

            return false;
        }

        protected internal override void Unmerge(ConfigurationElement sourceElement, ConfigurationElement parentElement,
            ConfigurationSaveMode updateMode)
        {
            var source = (ConfigurationElementCollection) sourceElement;
            var parent = (ConfigurationElementCollection) parentElement;

            for (var n = 0; n < source.Count; n++)
            {
                var sitem = source.BaseGet(n);
                var key = source.GetElementKey(sitem);
                var pitem = parent != null ? parent.BaseGet(key) : null;
                var nitem = CreateNewElementInternal(null);
                if (pitem != null && updateMode != ConfigurationSaveMode.Full)
                {
                    nitem.Unmerge(sitem, pitem, updateMode);
                    if (nitem.HasValues(pitem, updateMode))
                        BaseAdd(nitem);
                }
                else
                {
                    nitem.Unmerge(sitem, null, ConfigurationSaveMode.Full);
                    BaseAdd(nitem);
                }
            }

            if (updateMode == ConfigurationSaveMode.Full)
                EmitClear = true;
            else if (parent != null)
            {
                for (var n = 0; n < parent.Count; n++)
                {
                    var pitem = parent.BaseGet(n);
                    var key = parent.GetElementKey(pitem);
                    if (source.IndexOfKey(key) == -1)
                    {
                        if (_removed == null) _removed = new ArrayList();
                        _removed.Add(pitem);
                    }
                }
            }
        }

        #endregion // Methods
    }
}