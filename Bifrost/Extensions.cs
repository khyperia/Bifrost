using System;

namespace Bifrost
{
    public static class Extensions
    {
        public static ushort[] ConvertToShorts(byte[] bytes, bool bigEndian)
        {
            var result = new ushort[bytes.Length / 2];
            if (BitConverter.IsLittleEndian == bigEndian)
            {
                for (var i = 0; i < bytes.Length / 2; i++)
                {
                    var old = bytes[2 * i];
                    bytes[2 * i] = bytes[2 * i + 1];
                    bytes[2 * i + 1] = old;
                }
            }
            for (var i = 0; i < result.Length; i++)
                result[i] = BitConverter.ToUInt16(bytes, i * 2);
            return result;
        }
    }
}
