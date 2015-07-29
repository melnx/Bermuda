using Bermuda.AdminLibrary.Azure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Bermuda.AdminLibrary.Models;
using Bermuda.AdminLibrary.Utility;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;

namespace AzureStorageUtilityTests
{
    /// <summary>
    ///This is a test class for DeploymentHelperTest and is intended
    ///to contain all DeploymentHelperTest Unit Tests
    ///</summary>
    [TestClass()]
    public class DeploymentHelperTest
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
        ///A test for GetOperationStatus
        ///</summary>
        [TestMethod()]
        public void GetOperationStatusTest()
        {
            DeploymentHelper target = new DeploymentHelper();
            string subscriptionId = "e57cc5fa-5cf7-41c0-a33c-3adaf2944c4a";

            List<Byte> certificateBytes = null; // TODO: Initialize to an appropriate value

            string requestId = "";

            string expected = string.Empty; // TODO: Initialize to an appropriate value
            
            string actual;

            string status = string.Empty;

            actual = target.GetOperationStatus(subscriptionId, certificateBytes, requestId, out status);
            
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for GetHostedService
        ///</summary>
        [TestMethod()]
        public void GetHostedServiceTest()
        {
            HostedServiceHelper target = new HostedServiceHelper();

            string subscriptionId = "32e65da4-1eaf-4073-940a-51d7357d321b";

            string filePath = @"C:\Development\Certificates\AzureManagementCertificate.cer";

            List<Byte> certificateBytes = CertificateUtility.GetCertificateBytes(filePath);

            string serviceName = "evoappweb";

            HostedService expected = null;
            HostedService actual;

            actual = target.GetHostedService(subscriptionId, certificateBytes, serviceName);
            
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for DeleteDeployment
        ///</summary>
        [TestMethod()]
        public void DeleteDeploymentTest()
        {
            DeploymentHelper target = new DeploymentHelper();

            string subscriptionId = "e57cc5fa-5cf7-41c0-a33c-3adaf2944c4a";

            string filePath = @"C:\Development\Certificates\AzureManagementCertificate.cer";

            List<Byte> certificateBytes = CertificateUtility.GetCertificateBytes(filePath);

            string serviceName = "commandlinetest";

            string deploymentSlot = "staging";

            string expected = string.Empty; 
            string actual;

            actual = target.DeleteDeployment(subscriptionId, certificateBytes, serviceName, deploymentSlot);

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
