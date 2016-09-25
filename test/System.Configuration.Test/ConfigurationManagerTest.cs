using System.Configuration.Test.Sections;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace System.Configuration.Test
{
    [TestFixture]
    public class ConfigurationManagerTest
    {

        [TestCase("App.implicit.config")]
        [TestCase("App.explicit.config")]
        public void CanReadSimpleAppSettings(string configName)
        {
            ConfigurationManager.Initialize(GetConfigPath(configName));

            Assert.AreEqual("Value1", ConfigurationManager.AppSettings["Setting1"]);
            Assert.AreEqual("Value2", ConfigurationManager.AppSettings["Setting2"]);
        }

        [TestCase("App.implicit.config")]
        [TestCase("App.explicit.config")]
        public void CanReadConnectionStrings(string configName)
        {
            ConfigurationManager.Initialize(GetConfigPath(configName));

            var connectionStringSettings = ConfigurationManager.ConnectionStrings["MyDataBase"];

            Assert.AreEqual("MyDataBase", connectionStringSettings.Name);
            Assert.AreEqual("Data Source=(LocalDB)\\v11.0;Initial Catalog=WingtipToys;Integrated Security=True;Pooling=False", connectionStringSettings.ConnectionString);
            Assert.AreEqual("SomeProvider", connectionStringSettings.ProviderName);
        }

        [Test]
        public void CustomSectionCanBeDeserialized()
        {
            var configuration = ConfigurationManager.OpenExeConfiguration(GetConfigPath("App.customSections.config"));
            var section = (PageAppearanceSection)configuration.GetSection("pageAppearance");

            Assert.AreEqual(false, section.RemoteOnly);
            Assert.AreEqual("TimesNewRoman", section.Font.Name);
            Assert.AreEqual(15, section.Font.Size);
            Assert.AreEqual("FFDDAA", section.Color.Foreground);
            Assert.AreEqual("AAAAAA", section.Color.Background);
        }

        [Test]
        public void DefaultValuesAreCorrectlySet()
        {
            var configuration = ConfigurationManager.OpenExeConfiguration(GetConfigPath("App.customSections.config"));
            var section = (PageAppearanceSection)configuration.GetSection("pageAppearanceEmpty");

            Assert.AreEqual(true, section.RemoteOnly);
            Assert.AreEqual("Arial", section.Font.Name);
            Assert.AreEqual(12, section.Font.Size);
            Assert.AreEqual("000000", section.Color.Foreground);
            Assert.AreEqual("FFFFFF", section.Color.Background);
        }

        [Test]
        public void ValidatorAreApplied()
        {
            var configuration = ConfigurationManager.OpenExeConfiguration(GetConfigPath("App.customSections.config"));

            Assert.Throws<ConfigurationErrorsException>(() => configuration.GetSection("pageAppearanceTooShortString"));
            Assert.Throws<ConfigurationErrorsException>(() => configuration.GetSection("pageAppearanceTooLongString"));
            Assert.Throws<ConfigurationErrorsException>(() => configuration.GetSection("pageAppearanceInvalidChar"));
        }

        [Test]
        public void CanReadCollection()
        {
            var configuration = ConfigurationManager.OpenExeConfiguration(GetConfigPath("App.customSections.config"));
            var section = (EmployeesConfigSection)configuration.GetSection("employeesConfig");

            Assert.AreEqual(3, section.Employees.Count);
            Assert.AreEqual("abraham", section.Employees[0].FirstName);
            Assert.AreEqual("lincoln", section.Employees[0].LastName);
            Assert.AreEqual("george", section.Employees[1].FirstName);
            Assert.AreEqual("washington", section.Employees[1].LastName);
            Assert.AreEqual("george", section.Employees[2].FirstName);
            Assert.AreEqual("bush", section.Employees[2].LastName);
        }

        [Test]
        public void ThrowIfRequiredAttributeIsMissing()
        {
            var configuration = ConfigurationManager.OpenExeConfiguration(GetConfigPath("App.customSections.config"));

            Assert.Throws<ConfigurationErrorsException>(() =>
            {
                var config = (SimpleConfigSection)configuration.GetSection("simpleConfig");
            });
        }

        private static string GetConfigPath(string configName)
        {
            return Path.Combine(GetTestAssemblyDirectory(), "Data", configName);
        }

        private static string GetTestAssemblyDirectory()
        {
            var assemblyLocation = typeof(ConfigurationManagerTest).GetTypeInfo().Assembly.Location;
            return Path.GetDirectoryName(assemblyLocation);
        }
    }
}
