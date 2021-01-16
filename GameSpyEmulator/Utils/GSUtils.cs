using System;
using System.Text;
using Reality.Net.GameSpy.Servers;

namespace GameSpyEmulator
{
    public static class GSUtils
    {
        public static string DecryptPassword(string password)
        {
            string decrypted = GsBase64Decode(password, password.Length);
            GsEncode(ref decrypted);
            return decrypted;
        }

        public static int GsEncode(ref string password)
        {
            byte[] pass = DataFunctions.StringToBytes(password);

            int i;
            int a;
            int c;
            int d;
            int num = 0x79707367;   // "gspy"
            int passlen = pass.Length;

            if (num == 0)
                num = 1;
            else
                num &= 0x7fffffff;

            for (i = 0; i < passlen; i++)
            {
                d = 0xff;
                c = 0;
                d -= c;
                if (d != 0)
                {
                    num = GsLame(num);
                    a = num % d;
                    a += c;
                }
                else
                    a = c;

                pass[i] ^= (byte)(a % 256);
            }

            password = DataFunctions.BytesToString(pass);
            return passlen;
        }

        public static int GsLame(int num)
        {
            int a;
            int c = (num >> 16) & 0xffff;

            a = num & 0xffff;
            c *= 0x41a7;
            a *= 0x41a7;
            a += ((c & 0x7fff) << 16);

            if (a < 0)
            {
                a &= 0x7fffffff;
                a++;
            }

            a += (c >> 15);

            if (a < 0)
            {
                a &= 0x7fffffff;
                a++;
            }

            return a;
        }
        public static string GsBase64Decode(string s, int size)
        {
            byte[] data = DataFunctions.StringToBytes(s);

            int len;
            int xlen;
            int a = 0;
            int b = 0;
            int c = 0;
            int step;
            int limit;
            int y = 0;
            int z = 0;

            byte[] buff;
            byte[] p;

            char[] basechars = new char[128]
            {   // supports also the Gamespy base64
				'\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00',
                '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00',
                '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x3e', '\x00', '\x00', '\x00', '\x3f',
                '\x34', '\x35', '\x36', '\x37', '\x38', '\x39', '\x3a', '\x3b', '\x3c', '\x3d', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00',
                '\x00', '\x00', '\x01', '\x02', '\x03', '\x04', '\x05', '\x06', '\x07', '\x08', '\x09', '\x0a', '\x0b', '\x0c', '\x0d', '\x0e',
                '\x0f', '\x10', '\x11', '\x12', '\x13', '\x14', '\x15', '\x16', '\x17', '\x18', '\x19', '\x3e', '\x00', '\x3f', '\x00', '\x00',
                '\x00', '\x1a', '\x1b', '\x1c', '\x1d', '\x1e', '\x1f', '\x20', '\x21', '\x22', '\x23', '\x24', '\x25', '\x26', '\x27', '\x28',
                '\x29', '\x2a', '\x2b', '\x2c', '\x2d', '\x2e', '\x2f', '\x30', '\x31', '\x32', '\x33', '\x00', '\x00', '\x00', '\x00', '\x00'
            };

            if (size <= 0)
                len = data.Length;
            else
                len = size;

            xlen = ((len >> 2) * 3) + 1;
            buff = new byte[xlen % 256];
            if (buff.Length == 0) return null;

            p = buff;
            limit = data.Length + len;

            for (step = 0; ; step++)
            {
                do
                {
                    if (z >= limit)
                    {
                        c = 0;
                        break;
                    }
                    if (z < data.Length)
                        c = data[z];
                    else
                        c = 0;
                    z++;
                    if ((c == '=') || (c == '_'))
                    {
                        c = 0;
                        break;
                    }
                } while (c != 0 && ((c <= (byte)' ') || (c > 0x7f)));
                if (c == 0) break;

                switch (step & 3)
                {
                    case 0:
                        a = basechars[c];
                        break;
                    case 1:
                        b = basechars[c];
                        p[y++] = (byte)(((a << 2) | (b >> 4)) % 256);
                        break;
                    case 2:
                        a = basechars[c];
                        p[y++] = (byte)((((b & 15) << 4) | (a >> 2)) % 256);
                        break;
                    case 3:
                        p[y++] = (byte)((((a & 3) << 6) | basechars[c]) % 256);
                        break;
                    default:
                        break;
                }
            }
            p[y] = 0;

            len = p.Length - buff.Length;

            if (size != 0)
                size = len;

            if ((len + 1) != xlen)
                if (buff.Length == 0) return null;

            return DataFunctions.BytesToString(buff).Substring(0, y);
        }

        public static byte[] XorBytes(this byte[] data, int start, int count, string keystr)
        {
            byte[] key = Encoding.ASCII.GetBytes(keystr);

            for (int i = start; i < count; i++)
                data[i] = (byte)(data[i] ^ key[i % key.Length]);

            return data;
        }

        public static byte[] XorBytes(this string str, string keystr, int lengthOffset = 0)
        {
            byte[] data = Encoding.UTF8.GetBytes(str);
            byte[] key = Encoding.UTF8.GetBytes(keystr);

            var length = data.Length - lengthOffset;

            for (int i = 0; i < length; i++)
                data[i] = (byte)(data[i] ^ key[i % key.Length]);

            return data;
        }
    }
}
