namespace Bank_Server
{
    public static class Encryptor
    {

        private const string KEY = "Ida-Virumaa Kutsehariduskeskus"; //Please, if you want to change key - change it in Encryptor.cs on Client, otherwise you will not be able to decrypt!
        private static int secondKey = GenerateSecondKey(); //Second key. Simply make encryption harder :)

        public static string EncryptString(string text) //Encrypt string with changing char value of symbols. For example: letter 'A' will be changed to something like '}'. 
        { 
            char[] arr = text.ToCharArray();

            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = (char)EClamp(EClamp((int)arr[i] + KEY.Length) - secondKey);
            }

            return new string(arr);
        }

        public static string DecryptString(string text) //DecryptString() method is inversed EncryptString() method. 
        {
            char[] arr = text.ToCharArray();

            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = (char)EClamp(EClamp((int)arr[i] - KEY.Length) + secondKey);
            }

            return new string(arr);
        }

        public static int EClamp(int value) //Special clamp-function to prevent char overbordered. 
        {
            if (value > char.MaxValue)
                return (value - char.MaxValue);
            else if (value < 0)
                return -value;
            else
                return value;
        }

        private static int GenerateSecondKey() //Second key based on vowel letters ('a', 'e', 'i'...).  
        {
            int a = 0;
            foreach (var s in KEY)
            {
                if (s == 'a' || s == 'e' || s == 'i' || s == 'o' || s == 'u' || s == 'i')
                    a++;
            }
            return a;
        }
    }
}
