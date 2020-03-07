using System;
using System.Management;

namespace Bank_ClientApp
{
    public static class ClientManager
    {
        public static string GenerateClientID() //Use WMI (Windows Management Instrumentation) data to get processor ID. 
        {
            string cpuID = string.Empty;
            ManagementClass mc = new ManagementClass("win32_processor");
            ManagementObjectCollection moc = mc.GetInstances();

            foreach (ManagementObject mo in moc)
            {
                cpuID = mo.Properties["processorID"].Value.ToString();
                break;
            }

            return cpuID;
        }

        public static string GenerateFakeClientID() //Create Random to generate Uniq ID. 
        {
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            char[] stringChars = new char[8];
            Random random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            return new string(stringChars);
        }
    }
}
