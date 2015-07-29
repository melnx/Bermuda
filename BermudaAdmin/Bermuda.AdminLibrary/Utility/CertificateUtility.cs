using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Practices.EnterpriseLibrary.Logging;

namespace Bermuda.AdminLibrary.Utility
{
    public class CertificateUtility
    {
        #region Public Methods
        public static List<Byte> GetCertificateBytes(string certificateFilePath)
        {
            List<Byte> certificateBytes = null;

            try
            {
                X509Certificate2 certificate = new X509Certificate2(certificateFilePath);

                certificateBytes = certificate.Export(X509ContentType.Cert).ToList<Byte>();
            }
            catch (Exception ex)
            {
                Logger.Write(string.Format("Error in GetCertificateBytes(string certificateFilePath)  Error: {0}", ex.Message));

                certificateBytes = null;
            }

            return certificateBytes;
        }

        public static X509Certificate2 GetX509Certificate2(String thumbprint)
        {
            X509Certificate2 x509Certificate2 = null;

            X509Store store = new X509Store("My", StoreLocation.LocalMachine);

            try
            {
                store.Open(OpenFlags.ReadOnly);

                X509Certificate2Collection x509Certificate2Collection = store.Certificates.Find(X509FindType.FindByThumbprint,
                                                                                                thumbprint,
                                                                                                false);

                x509Certificate2 = x509Certificate2Collection[0];
            }
            catch (Exception ex)
            {
                Logger.Write(string.Format("Error in GetX509Certificate2(String thumbprint)  Error: {0}", ex.Message));

                x509Certificate2 = null;
            }
            finally
            {
                store.Close();
            }

            return x509Certificate2;
        }

        public static X509Certificate2 GetX509Certificate2(Byte[] certificateBytes)
        {
            X509Certificate2 x509Certificate2 = null;

            try
            {
                x509Certificate2 = new X509Certificate2(certificateBytes);
            }
            catch (Exception ex)
            {
                Logger.Write(string.Format("Error in GetX509Certificate2(Byte[] certificateBytes)  Error: {0}", ex.Message));

                x509Certificate2 = null;
            }

            return x509Certificate2;
        }
        #endregion Public Methods
    }
}
