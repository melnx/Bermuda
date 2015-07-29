using Bermuda.AdminLibrary.Azure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Bermuda.AdminLibrary.Utility;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;

namespace AzureStorageUtilityTests
{
    /// <summary>
    ///This is a test class for CreateDeploymentHelperTest and is intended
    ///to contain all CreateDeploymentHelperTest Unit Tests
    ///</summary>
    [TestClass()]
    public class CreateDeploymentHelperTest
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
        ///A test for CreateDeployment
        ///</summary>
        [TestMethod()]
        public void CreateDeploymentTest()
        {
            DeploymentHelper target = new DeploymentHelper();

            string subscriptionId = "e57cc5fa-5cf7-41c0-a33c-3adaf2944c4a";

            string filePath = @"C:\Development\Certificates\AzureManagementCertificate.cer";

            List<Byte> certificateBytes = CertificateUtility.GetCertificateBytes(filePath);

            string serviceName = "commandlinetest";
            string deploymentName = "TestDeployment";
            
            string deploymentSlot = "staging";

            string label = "Hosted Service Test 2.0";
            
            string expected = string.Empty; 

            string actual;
            
            actual = target.CreateDeployment(subscriptionId, certificateBytes, serviceName, deploymentName, deploymentSlot, label, 2);

            string opsStatus = string.Empty;

            string status = "InProgress";

            while (status == "InProgress")
            {
                opsStatus = target.GetOperationStatus(subscriptionId, certificateBytes, actual, out status);

                Trace.WriteLine("Ops Status = " + status);

                Thread.Sleep(2000);
            }

            Assert.AreEqual(expected, actual);
        }
    }
}
