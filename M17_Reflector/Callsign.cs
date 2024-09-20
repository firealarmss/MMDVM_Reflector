namespace M17_Reflector
{
    public class Callsign
    {
        private const string M17CHARACTERS = " ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-/.";
        private ulong coded;
        private char[] cs = new char[10];

        public Callsign()
        {
            Array.Fill(cs, '\0');
            coded = 0;
        }

        public Callsign(byte[] input)
        {
            CodeIn(input);
        }

        public void CodeIn(byte[] input)
        {
            if (input.Length < 6)
            {
                throw new ArgumentException("Input array must be at least 6 bytes long.");
            }

            Array.Fill(cs, '\0');
            coded = input[0];
            for (int i = 1; i < 6; i++)
            {
                coded = (coded << 8) | input[i];
            }

            if (coded > 0xEE6B27FFFFFFu)
            {
                Console.WriteLine($"Callsign code is too large: 0x{coded:X}");
                return;
            }

            int index = 0;
            ulong temp = coded;
            while (temp > 0 && index < 10)
            {
                cs[index++] = M17CHARACTERS[(int)(temp % 40)];
                temp /= 40;
            }
        }

        public string GetCS(int length = 10)
        {
            return new string(cs, 0, Math.Min(length, cs.Length)).TrimEnd();
        }

        public override string ToString()
        {
            return GetCS();
        }
    }
}