/*
 * Portions of this file from https://github.com/DVMProject/fnecore
 */


namespace Common
{
    public static class Utils
    {
        /// <summary>
        /// Helper to display the ASCII representation of a hex dump.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private static string DisplayHexChars(Span<byte> buffer, int offset)
        {
            int bCount = 0;

            string _out = string.Empty;
            for (int i = offset; i < buffer.Length; i++)
            {
                // stop every 16 bytes...
                if (bCount == 16)
                    break;

                byte b = buffer[i];
                char c = Convert.ToChar(b);

                // make control and illegal characters spaces
                if (c >= 0x00 && c <= 0x1F)
                    c = ' ';
                if (c >= 0x7F)
                    c = ' ';

                _out += c;

                bCount++;
            }

            return _out;
        }

        /// <summary>
        /// Helper to display the ASCII representation of a hex dump.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private static string DisplayHexChars(byte[] buffer, int offset)
        {
            int bCount = 0;

            string _out = string.Empty;
            for (int i = offset; i < buffer.Length; i++)
            {
                // stop every 16 bytes...
                if (bCount == 16)
                    break;

                byte b = buffer[i];
                char c = Convert.ToChar(b);

                // make control and illegal characters spaces
                if (c >= 0x00 && c <= 0x1F)
                    c = ' ';
                if (c >= 0x7F)
                    c = ' ';

                _out += c;

                bCount++;
            }

            return _out;
        }

        /// <summary>
        /// Perform a hex dump of a buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        public static string HexDump(short[] buffer, int offset = 0)
        {
            int bCount = 0, j = 0;

            // iterate through buffer printing all the stored bytes
            string res = "\n\tDUMP " + j.ToString("X4") + ":  ";
            for (int i = offset; i < buffer.Length; i++)
            {
                short b = buffer[i];

                // split the message every 16 bytes...
                if (bCount == 16)
                {
                    //res += "    *" + DisplayHexChars(buffer, j) + "*\n";
                    res += "\n";
                    bCount = 0;
                    j += 16;
                    res += "\tDUMP " + j.ToString("X4") + ":  ";
                }
                else
                    res += (bCount > 0) ? " " : "";

                res += b.ToString("X4");
                bCount++;
            }

            // if the byte count at this point is non-zero print the message
            if (bCount != 0)
            {
                if (bCount < 16)
                {
                    for (int i = bCount; i < 16; i++)
                        res += "   ";
                }

                //res += "    *" + DisplayHexChars(buffer, j) + "*";
            }

            return res;
        }

        /// <summary>
        /// Perform a hex dump of a buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        public static string HexDump(Memory<byte> buffer, int offset = 0)
        {
            int bCount = 0, j = 0;

            // iterate through buffer printing all the stored bytes
            string res = "\n\tDUMP " + j.ToString("X4") + ":  ";
            for (int i = offset; i < buffer.Length; i++)
            {
                byte b = buffer.Span[i];

                // split the message every 16 bytes...
                if (bCount == 16)
                {
                    res += "    *" + DisplayHexChars(buffer.Span, j) + "*\n";
                    bCount = 0;
                    j += 16;
                    res += "\tDUMP " + j.ToString("X4") + ":  ";
                }
                else
                    res += (bCount > 0) ? " " : "";

                res += b.ToString("X2");
                bCount++;
            }

            // if the byte count at this point is non-zero print the message
            if (bCount != 0)
            {
                if (bCount < 16)
                {
                    for (int i = bCount; i < 16; i++)
                        res += "   ";
                }

                res += "    *" + DisplayHexChars(buffer.Span, j) + "*";
            }

            return res;
        }
    }
}
