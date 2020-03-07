namespace Bank_ClientApp
{
    public static class Encryptor
    {
        //Same class as Encryptor.cs on server!

        private const string KEY = "Ida-Virumaa Kutsehariduskeskus";
        private static int secondKey = GenerateSecondKey();

        public static string EncryptString(string text)
        {
            char[] arr = text.ToCharArray();

            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = (char)EClamp(EClamp((int)arr[i] + KEY.Length) - secondKey);
            }

            return new string(arr);
        }

        public static string DecryptString(string text)
        {
            char[] arr = text.ToCharArray();

            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = (char)EClamp(EClamp((int)arr[i] - KEY.Length) + secondKey);
            }

            return new string(arr);
        }

        public static int EClamp(int value)
        {
            if (value > char.MaxValue)
                return (value - char.MaxValue);
            else if (value < 0)
                return -value;
            else
                return value;
        }

        private static int GenerateSecondKey()
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
