namespace System.Configuration.Test.Sections
{
    public class SimpleConfigSection : ConfigurationSection
    {
        [ConfigurationProperty("first", IsRequired = true)]
        public bool First
        {
            get
            {
                return (bool)this["first"];
            }
            set
            {
                this["first"] = value;
            }
        }
    }
}
