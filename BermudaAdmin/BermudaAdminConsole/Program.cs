using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using Bermuda.AdminService.Test;
using System.Security.Cryptography.X509Certificates;
using Bermuda.AdminLibrary.Azure;

namespace BermudaAdminConsole
{
    class Program
    {
        static List<Byte> GetCertificateBytes(string certificateFilePath)
        {
            List<Byte> certificateBytes = null;

            try
            {
                X509Certificate2 certificate = new X509Certificate2(certificateFilePath);

                certificateBytes = certificate.Export(X509ContentType.Cert).ToList<Byte>();
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Error in GetCertificateBytes(string certificateFilePath)  Error: {0}\n{1}", ex.Message,
                                  ex.InnerException != null ? ex.InnerException.Message : string.Empty));

                certificateBytes = null;
            }

            return certificateBytes;
        }

        static HttpWebRequest CreateHttpWebRequest(Uri uri, String httpWebRequestMethod)
        {
            HttpWebRequest httpWebRequest = null;

            try
            {
                httpWebRequest = (HttpWebRequest)HttpWebRequest.Create(uri);

                httpWebRequest.Method = httpWebRequestMethod;

                httpWebRequest.Accept = "application/json";
                httpWebRequest.ContentType = "application/json";
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Error in CreateHttpWebRequest()  Error: {0}\n{1}", ex.Message,
                                  ex.InnerException != null ? ex.InnerException.Message : string.Empty));

                httpWebRequest = null;
            }

            return httpWebRequest;
        }

        static string CreateDeployment(string subscriptionId, string serviceName,
                                       string label, string deploymentName,
                                       string deploymentSlot, string certificateFilePath,
                                       int instanceCount)
        {
            string responseString = string.Empty;

            try
            {
                DeploymentHelper deploymentHelper = new DeploymentHelper();

                List<Byte> certificateBytes = GetCertificateBytes(certificateFilePath);

                responseString = deploymentHelper.CreateDeployment( subscriptionId, certificateBytes, 
                                                                    serviceName, deploymentName, 
                                                                    deploymentSlot, label, instanceCount);

                Console.WriteLine();

                Console.WriteLine("Response String: {0}", responseString);

                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Error: {0}\n{1}", ex.Message,
                                  ex.InnerException != null ? ex.InnerException.Message : string.Empty));

                responseString = string.Empty;
            }

            return responseString;
        }

        static OperationStatus GetOperationStatus(OperationStatus operationStatus)
        {
            try
            {
                DeploymentHelper deploymentHelper = new DeploymentHelper();

                string status = string.Empty;

                string responseString = deploymentHelper.GetOperationStatus(operationStatus.SubscriptionId, 
                                                                            operationStatus.CertificateBytes,
                                                                            operationStatus.RequestId, out status);

                operationStatus.ResponseString = responseString;
                operationStatus.Status = status;
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Error: {0}\n{1}", ex.Message,
                                  ex.InnerException != null ? ex.InnerException.Message : string.Empty));

                operationStatus = null;
            }

            return operationStatus;
        }

        static void Main(string[] args)
        {
            Console.Clear();

            String requestId = string.Empty;

            try
            {
                string subscriptionId = "e57cc5fa-5cf7-41c0-a33c-3adaf2944c4a";
                string serviceName = "commandlinetest";
                string label = "Hosted Service Test";
                string deploymentName = "TestDeployment";
                string deploymentSlot = "staging";
                string certificateFilePath = @"C:\Development\Certificates\AzureManagementCertificate.cer";
                int instanceCount = 1;

                requestId = CreateDeployment(subscriptionId, serviceName,
                                             label, deploymentName, deploymentSlot,
                                             certificateFilePath, instanceCount);

                Console.WriteLine();

                Console.WriteLine("Request Id: {0}", requestId);

                Console.WriteLine();

                if (string.IsNullOrWhiteSpace(requestId) != true)
                {
                    OperationStatus opsStatus = new OperationStatus()
                    {
                        SubscriptionId = subscriptionId,
                        RequestId = requestId.Replace("\"", ""),
                        CertificateBytes = GetCertificateBytes(certificateFilePath).ToList<Byte>(),
                        ResponseString = "",
                        Status = "InProgress"
                    };

                    while (opsStatus.Status == "InProgress")
                    {
                        opsStatus = GetOperationStatus(opsStatus);

                        Console.WriteLine(opsStatus.Status);

                        Thread.Sleep(1500);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Error: {0}\n{1}", ex.Message,
                                  ex.InnerException != null ? ex.InnerException.Message : string.Empty));
            }

            Console.WriteLine();
            Console.WriteLine();

            Console.WriteLine("Please press a key to exit . . .");

            Console.WriteLine();
            Console.WriteLine();

            Console.ReadKey();
        }
    }
}
