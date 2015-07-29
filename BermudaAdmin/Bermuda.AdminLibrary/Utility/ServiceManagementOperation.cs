using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;
using Microsoft.Practices.EnterpriseLibrary.Logging;

namespace Bermuda.AdminLibrary.Utility
{
    public class ServiceManagementOperation
    {
        #region Private Members
        private String _thumbprint = string.Empty;
        private List<Byte> _certificateBytes = null;
        private String _versionId = "2011-10-01";
        #endregion Private Members

        #region Properties
        public String Thumbprint
        {
            get { return _thumbprint; }
            set { _thumbprint = value; }
        }

        public List<Byte> CertificateBytes
        {
            get { return _certificateBytes; }
            set { _certificateBytes = value; }
        }

        public String VersionId
        {
            get { return _versionId; }
            set { _versionId = value; }
        }
        #endregion Properties

        #region Constructors
        public ServiceManagementOperation(String thumbprint)
        {
            this.Thumbprint = thumbprint;
        }

        public ServiceManagementOperation(List<Byte> certificateBytes)
        {
            this.CertificateBytes = certificateBytes;
        }
        #endregion Constructors

        #region Private Methods
        private HttpWebRequest CreateHttpWebRequest(Uri uri, String httpWebRequestMethod)
        {
            HttpWebRequest httpWebRequest = null;

            X509Certificate2 x509Certificate2 = null;

            try
            {
                if (string.IsNullOrWhiteSpace(this.Thumbprint) != true)
                {
                    x509Certificate2 = CertificateUtility.GetX509Certificate2(_thumbprint);
                }
                else if (this.CertificateBytes != null)
                {
                    x509Certificate2 = CertificateUtility.GetX509Certificate2(_certificateBytes.ToArray());
                }

                httpWebRequest = (HttpWebRequest)HttpWebRequest.Create(uri);

                Logger.Write("Service Uri: " + uri.ToString());

                httpWebRequest.Method = httpWebRequestMethod;

                httpWebRequest.Headers.Add("x-ms-version", this.VersionId);

                httpWebRequest.ClientCertificates.Add(x509Certificate2);

                httpWebRequest.ContentType = "application/xml";
            }
            catch (Exception ex)
            {
                Logger.Write(string.Format("Error in CreateHttpWebRequest()  Error: {0}", ex.Message));

                httpWebRequest = null;
            }

            return httpWebRequest;
        }
        #endregion Private Methods

        #region Public Methods
        // Invoking a GET operation
        public XDocument Invoke(String uri)
        {
            XDocument responsePayload;

            try
            {
                Uri operationUri = new Uri(uri);

                HttpWebRequest httpWebRequest = CreateHttpWebRequest(operationUri, "GET");

                MemoryStream memStream = null;
        
                using (HttpWebResponse response = (HttpWebResponse)httpWebRequest.GetResponse())
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        using (TextReader txtReader = new StreamReader(responseStream))
                        {
                            string responseString = txtReader.ReadToEnd();

                            responseString = responseString.Replace(" xmlns=\"http://schemas.microsoft.com/windowsazure\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"", "");

                            memStream = new MemoryStream(Encoding.UTF8.GetBytes(responseString));

                            txtReader.Close();
                            response.Close();
                        }
                    }

                    responsePayload = XDocument.Load(memStream);

                    // responsePayload.Declaration = new XDeclaration("1.0", "utf-8", "yes");
                }
            }
            catch (Exception ex)
            {
                Logger.Write(string.Format("Error in Invoke(String uri)  Error: {0}\nInner Exception: {1}", 
                                            ex.Message,
                                            ex.InnerException != null ? 
                                            ex.InnerException.Message : 
                                            string.Empty));

                Console.WriteLine(string.Format("Error in Invoke(String uri)  Error: {0}\nInner Exception: {1}",
                                                ex.Message,
                                                ex.InnerException != null ?
                                                ex.InnerException.Message :
                                                string.Empty));

                responsePayload = null;
            }

            return responsePayload;
        }

        // Invoking a POST operation
        public String Invoke(String uri, XDocument payload)
        {
            String requestId = string.Empty;

            try
            {
                Uri operationUri = new Uri(uri);

                Logger.Write("Create Deployment Invoke Uri: " + uri);

                Logger.Write("Payload: " + payload.ToString());

                HttpWebRequest httpWebRequest = CreateHttpWebRequest(operationUri, "POST");

                httpWebRequest.Accept = "*/*";

                using (Stream requestStream = httpWebRequest.GetRequestStream())
                {
                    using (StreamWriter streamWriter = new StreamWriter(requestStream, System.Text.UTF8Encoding.UTF8))
                    {
                        payload.Save(streamWriter, SaveOptions.DisableFormatting);
                    }
                }

                using (HttpWebResponse response = (HttpWebResponse)httpWebRequest.GetResponse())
                {
                    requestId = response.Headers["x-ms-request-id"];

                    Logger.Write("x-ms-request-id: " + requestId);
                }
            }
            catch (WebException wex)
            {
                string responseMessage = string.Empty;

                using (StreamReader reader = new StreamReader(wex.Response.GetResponseStream()))
                {
                    responseMessage = reader.ReadToEnd();

                    reader.Close();
                }

                Logger.Write(string.Format("Error in Invoke(String uri, XDocument payload)  Error: {0}\n{1}",
                             wex.Message,
                             wex.InnerException != null ?
                             wex.InnerException.Message :
                             string.Empty));

                Console.WriteLine(string.Format("Error in Invoke(String uri, XDocument payload)  Error: {0}\n{1}",
                             wex.Message,
                             wex.InnerException != null ?
                             wex.InnerException.Message :
                             string.Empty));

                Logger.Write("Response: " + responseMessage);
                Console.WriteLine("Response: " + responseMessage);

                requestId = string.Empty;

            }
            catch (Exception ex)
            {
                Logger.Write(string.Format("Error in Invoke(String uri, XDocument payload)  Error: {0}\n{1}", 
                             ex.Message,
                             ex.InnerException != null ? 
                             ex.InnerException.Message : 
                             string.Empty));

                Console.WriteLine(string.Format("Error in Invoke(String uri, XDocument payload)  Error: {0}\n{1}",
                             ex.Message,
                             ex.InnerException != null ?
                             ex.InnerException.Message :
                             string.Empty));

                requestId = string.Empty;
            }

            return requestId;
        }

        // Invoking a DELETE operation
        public String Invoke(String uri, bool delete)
        {
            String requestId = string.Empty;

            try
            {
                Uri operationUri = new Uri(uri);

                HttpWebRequest httpWebRequest = CreateHttpWebRequest(operationUri, "DELETE");

                using (HttpWebResponse response = (HttpWebResponse)httpWebRequest.GetResponse())
                {
                    requestId = response.Headers["x-ms-request-id"];
                }
            }
            catch (Exception ex)
            {
                Logger.Write(string.Format("Invoke(String uri, bool delete = true)  Error: {0}", ex.Message));

                requestId = string.Empty;
            }

            return requestId;
        }
        #endregion Public Methods
    }
}
