using System.Collections.Generic;

namespace System.Configuration.Test.Sections
{
    /// <summary>
    /// Example from https://hoanghaivm.wordpress.com/net/custom-configuration-for-net-20/
    /// </summary>
    public class EmployeesConfigSection : ConfigurationSection
    {
        [ConfigurationProperty("employees", IsDefaultCollection = true)]
        public EmployeeCollection Employees

        {
            get { return (EmployeeCollection) base["employees"]; }
        }
    }


    public sealed class EmployeeElement : ConfigurationElement

    {
        [ConfigurationProperty("EmployeeID", IsKey = true, IsRequired = true)]
        public string EmployeeID

        {
            get { return (string) this["EmployeeID"]; }

            set { this["EmployeeID"] = value; }
        }

        [ConfigurationProperty("FirstName", IsRequired = true)]
        public string FirstName

        {
            get { return (string) this["FirstName"]; }

            set { this["FirstName"] = value; }
        }

        [ConfigurationProperty("LastName", IsRequired = true)]
        public string LastName

        {
            get { return (string) this["LastName"]; }

            set { this["LastName"] = value; }
        }
    }


    public sealed class EmployeeCollection : ConfigurationElementCollection

    {
        public override ConfigurationElementCollectionType CollectionType

        {
            get { return ConfigurationElementCollectionType.BasicMap; }
        }

        protected override string ElementName

        {
            get { return "employee"; }
        }

        public EmployeeElement this[int index]

        {
            get { return (EmployeeElement) BaseGet(index); }

            set

            {
                if (BaseGet(index) != null)

                {
                    BaseRemoveAt(index);
                }

                BaseAdd(index, value);
            }
        }



        protected override ConfigurationElement CreateNewElement()

        {
            return new EmployeeElement();
        }

        protected override object GetElementKey(ConfigurationElement element)

        {
            return ((EmployeeElement) element).EmployeeID;
        }

        public bool ContainsKey(string key)

        {
            var result = false;

            object[] keys = BaseGetAllKeys();

            foreach (var obj in keys)

            {
                if ((string) obj == key)

                {
                    result = true;

                    break;
                }
            }

            return result;
        }

        public new IEnumerator<EmployeeElement> GetEnumerator()
        {
            foreach (var key in base.BaseGetAllKeys())
            {
                yield return (EmployeeElement)BaseGet(key);
            }
        }
    }
}