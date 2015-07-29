using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Web.Script.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Xml;

namespace Bermuda.AdminService.Test
{
    public class Program
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

            // Uri operationUri = new Uri("http://localhost:4652/api/deployment");
            Uri operationUri = new Uri("http://localhost:8080/api/deployment");
            // Uri operationUri = new Uri("http://ec2-23-23-186-66.compute-1.amazonaws.com/BermudaAdmin/api/deployment");

            HttpWebRequest httpWebRequest = CreateHttpWebRequest(operationUri, "POST");

            AzureDeployment azureDeployment = new AzureDeployment();

            azureDeployment.SubscriptionId = subscriptionId;
            azureDeployment.ServiceName = serviceName;
            azureDeployment.Label = label;
            azureDeployment.DeploymentName = deploymentName;
            azureDeployment.DeploymentSlot = deploymentSlot;

            try
            {
                azureDeployment.CertificateBytes = GetCertificateBytes(certificateFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Error: {0}\n{1}", ex.Message,
                                  ex.InnerException != null ? ex.InnerException.Message : string.Empty));

                responseString = string.Empty;
            }

            try
            {
                azureDeployment.InstanceCount = instanceCount;

                string jsonString = string.Empty;

                MemoryStream memStream = new MemoryStream();

                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(AzureDeployment));

                serializer.WriteObject(memStream, azureDeployment);

                memStream.Position = 0;

                StreamReader sr = new StreamReader(memStream);

                jsonString = sr.ReadToEnd();

                sr.Close();

                Byte[] postData = Encoding.UTF8.GetBytes(jsonString);

                httpWebRequest.ContentLength = postData.Length;

                using (Stream requestStream = httpWebRequest.GetRequestStream())
                {
                    requestStream.Write(postData, 0, postData.Length);
                }

                using (HttpWebResponse response = (HttpWebResponse)httpWebRequest.GetResponse())
                {
                    using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                    {
                        responseString = streamReader.ReadToEnd();

                        Console.WriteLine(responseString);

                        streamReader.Close();
                        response.Close();
                    }
                }
            }
            catch (WebException wex)
            {
                using (StreamReader exReader = new StreamReader(wex.Response.GetResponseStream()))
                {
                    responseString = exReader.ReadToEnd();

                    exReader.Close();

                    Console.WriteLine(responseString);
                }

                Console.WriteLine(string.Format("Error: {0}\n{1}", wex.Message,
                                  wex.InnerException != null ? wex.InnerException.Message : string.Empty));
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
            OperationStatus opsStatus = null;

            try
            {
                // Uri operationUri = new Uri("http://localhost:4652/api/operationstatus");
                Uri operationUri = new Uri("http://localhost:8080/api/deployment");
                // Uri operationUri = new Uri("http://ec2-23-23-186-66.compute-1.amazonaws.com/BermudaAdmin/api/operationstatus");
            
                HttpWebRequest httpWebRequest = CreateHttpWebRequest(operationUri, "POST");

                string jsonString = string.Empty;

                MemoryStream memStream = new MemoryStream();

                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(OperationStatus));

                serializer.WriteObject(memStream, operationStatus);

                memStream.Position = 0;

                StreamReader sr = new StreamReader(memStream);

                jsonString = sr.ReadToEnd();

                sr.Close();

                Byte[] postData = Encoding.UTF8.GetBytes(jsonString);

                httpWebRequest.ContentLength = postData.Length;

                using (Stream requestStream = httpWebRequest.GetRequestStream())
                {
                    requestStream.Write(postData, 0, postData.Length);
                }

                string jsonResponse = string.Empty;
        
                using (HttpWebResponse response = (HttpWebResponse)httpWebRequest.GetResponse())
                {
                    opsStatus = serializer.ReadObject(response.GetResponseStream()) as OperationStatus;

                    response.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Error: {0}\n{1}", ex.Message,
                                  ex.InnerException != null ? ex.InnerException.Message : string.Empty));

                opsStatus = null;
            }

            return opsStatus;
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
                string certificateFilePath = @"C:\Development\Certificates\BermudaAdminCertificate.cer";
                int instanceCount = 1;

                requestId = CreateDeployment(subscriptionId, serviceName,
                                             label, deploymentName, deploymentSlot, 
                                             certificateFilePath, instanceCount);

                if (string.IsNullOrWhiteSpace(requestId) != true &&
                    requestId != "\"\"")
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
