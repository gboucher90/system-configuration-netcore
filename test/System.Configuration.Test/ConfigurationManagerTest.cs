using System.IO;
using NUnit.Framework;

namespace System.Configuration.Test
{
    [TestFixture]
    public class ConfigurationManagerTest
    {
        [SetUp]
        public void Setup()
        {
            ConfigurationManager.Initialize("Data" + Path.DirectorySeparatorChar + "App.config");
        }

        [Test]
        public void CanReadSimpleAppSettings()
        {
            Assert.AreEqual("Value1", ConfigurationManager.AppSettings["Setting1"]);
            Assert.AreEqual("Value2", ConfigurationManager.AppSettings["Setting2"]);
        }

        [Test]
        public void CanReadConnectionStrings()
        {
            const string expected = "Data Source=(LocalDB)\\v11.0;Initial Catalog=WingtipToys;Integrated Security=True;Pooling=False";

            Assert.AreEqual(expected, ConfigurationManager.ConnectionStrings["MyDataBase"].ConnectionString);
        }

        [Test]
        public void CustomSectionCanBeDeserialized()
        {
            var config = (PageAppearanceSection)ConfigurationManager.GetSection("pageAppearance");

            Assert.AreEqual(false, config.RemoteOnly);
            Assert.AreEqual("TimesNewRoman", config.Font.Name);
            Assert.AreEqual(15, config.Font.Size);
            Assert.AreEqual("FFDDAA", config.Color.Foreground);
            Assert.AreEqual("AAAAAA", config.Color.Background);
        }

        [Test]
        public void DefaultValuesAreCorrectlySet()
        {
            var config = (PageAppearanceSection)ConfigurationManager.GetSection("pageAppearanceEmpty");

            Assert.AreEqual(true, config.RemoteOnly);
            Assert.AreEqual("Arial", config.Font.Name);
            Assert.AreEqual(12, config.Font.Size);
            Assert.AreEqual("000000", config.Color.Foreground);
            Assert.AreEqual("FFFFFF", config.Color.Background);
        }

        [Test]
        public void ValidatorAreApplied()
        {
            Assert.Throws<ConfigurationErrorsException>(() => ConfigurationManager.GetSection("pageAppearanceTooShortString"));
            Assert.Throws<ConfigurationErrorsException>(() => ConfigurationManager.GetSection("pageAppearanceTooLongString"));
            Assert.Throws<ConfigurationErrorsException>(() => ConfigurationManager.GetSection("pageAppearanceInvalidChar"));
        }

        [Test]
        public void CanReadCollection()
        {
            var config = (EmployeesConfigSection)ConfigurationManager.GetSection("employeesConfig");

            Assert.AreEqual(3, config.Employees.Count);
            Assert.AreEqual("abraham", config.Employees[0].FirstName);
            Assert.AreEqual("lincoln", config.Employees[0].LastName);
            Assert.AreEqual("george", config.Employees[1].FirstName);
            Assert.AreEqual("washington", config.Employees[1].LastName);
            Assert.AreEqual("george", config.Employees[2].FirstName);
            Assert.AreEqual("bush", config.Employees[2].LastName);
        }

        [Test]
        public void ThrowIfRequiredAttributeIsMissing()
        {
            Assert.Throws<ConfigurationErrorsException>(() =>
            {
                var config = (SimpleConfigSection)ConfigurationManager.GetSection("simpleConfig");
            });
        }

    }
}
