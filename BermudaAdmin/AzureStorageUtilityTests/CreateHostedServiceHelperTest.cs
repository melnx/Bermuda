using Bermuda.AdminLibrary.Azure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace AzureStorageUtilityTests
{
    /// <summary>
    ///This is a test class for CreateHostedServiceHelperTest and is intended
    ///to contain all CreateHostedServiceHelperTest Unit Tests
    ///</summary>
    [TestClass()]
    public class CreateHostedServiceHelperTest
    {
        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A test for CreateHostedService
        ///</summary>
        [TestMethod()]
        public void CreateHostedServiceTest()
        {
            HostedServiceHelper target = new HostedServiceHelper();

            string subscriptionId = "e57cc5fa-5cf7-41c0-a33c-3adaf2944c4a";
            string thumbprint = "AEBCC83D2678B39712BFEAC43D7DA94140D2FCE6";

            string serviceName = "commandlinetest";

            string label = "Hosted Service Test 2.0"; 
            
            string description = "Test Hosted Service Creation";

            string location = "North Central US"; 
            
            string affinityGroup = string.Empty;

            string expected = string.Empty;
            string actual;

            actual = target.CreateHostedService(subscriptionId, thumbprint, serviceName, label, description, location, affinityGroup);

            Assert.AreEqual(expected, actual);
        }
    }
}
