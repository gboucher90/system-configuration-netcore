namespace System.Configuration.Test
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
