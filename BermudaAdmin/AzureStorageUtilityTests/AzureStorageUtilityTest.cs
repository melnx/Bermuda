using Bermuda.AdminLibrary.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Microsoft.WindowsAzure.StorageClient;
using System.Collections.Generic;

namespace AzureStorageUtilityTests
{
    /// <summary>
    ///This is a test class for AzureStorageUtilityTest and is intended
    ///to contain all AzureStorageUtilityTest Unit Tests
    ///</summary>
    [TestClass()]
    public class AzureStorageUtilityTest
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
        ///A test for ReadBlobFile
        ///</summary>
        [TestMethod()]
        public void ReadBlobFileTest()
        {
            string fileName = "Bermuda/Bermuda.Azure.cspkg";

            string expected = null; 
            string actual;
            
            actual = AzureStorageUtility.ReadBlobFile(fileName);

            // AzureStorageUtility.UploadBlobFile(actual, "Bermuda/Test.cspkg");
 
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for ListBlobs
        ///</summary>
        [TestMethod()]
        public void ListBlobsTest()
        {
            IEnumerable<IListBlobItem> expected = null; 

            IEnumerable<IListBlobItem> actual;
            
            actual = AzureStorageUtility.ListBlobs();
            
            Assert.AreEqual(expected, actual);
        }
    }
}
