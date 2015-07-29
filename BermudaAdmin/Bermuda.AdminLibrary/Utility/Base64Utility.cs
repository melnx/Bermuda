using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Bermuda.AdminLibrary.Utility
{
    public class Base64Utility
    {
        public static String ConvertToBase64String(String value)
        {
            String base64String = string.Empty;

            try
            {
                Byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value);

                base64String = Convert.ToBase64String(bytes);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(string.Format("Error in ConvertToBase64String()  Error: {0}", ex.Message));

                base64String = string.Empty;
            }

            return base64String;
        }

        public static string EncodeToBase64String(string original)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(original));
        }

        public static string DecodeFromBase64String(string original)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(original));
        }
    }
}
