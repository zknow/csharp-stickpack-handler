using System.Text;

public class PackUtility
{
    // 封包
    public static byte[] Pack(byte[] bytes)
    {
        var buf = new List<byte>() { };
        buf.AddRange(Encoding.ASCII.GetBytes(Protocol.Header));
        buf.AddRange(BitConverter.GetBytes(bytes.Length));
        buf.AddRange(bytes);
        return buf.ToArray();
    }

    // 用Slice方式解包
    public static List<byte> UnpackWithSlice(List<byte> packList, Action<byte[]> legitPackCallback = null)
    {
        byte[] buf = packList.ToArray();
        int len = buf.Length;

        int i = 0;
        for (i = 0; i < len; i++)
        {
            // Check封包長度是否大於等於HeaderLen長度，否=>不處理，等待下一包
            if (len < i + Protocol.HeaderLen + Protocol.DataByteLen)
                break;

            string bufHeader = Encoding.Unicode.GetString(buf[i..(i + Protocol.HeaderLen)]);
            if (bufHeader == Protocol.Header)
            {
                byte[] msgLenBytes = buf[(i + Protocol.HeaderLen)..(i + Protocol.HeaderLen + Protocol.DataByteLen)];
                int msgLen = BitConverter.ToInt32(msgLenBytes);

                // Check封包長度是否大於等於HeaderLen+ContentLen長度，否=>不處理，等待下一包封包
                int unhandledPackLen = Protocol.HeaderLen + Protocol.DataByteLen + msgLen;
                if (len < i + unhandledPackLen)
                    break;

                int unHandledPackStartIndex = (i + Protocol.HeaderLen + Protocol.DataByteLen);
                byte[] legitPack = buf[(unHandledPackStartIndex)..(unHandledPackStartIndex + msgLen)];
                legitPackCallback?.Invoke(legitPack);

                i += unhandledPackLen - 1;
            }
        }

        // i == len：代表整個封包已成功解析且合法，不需要把剩餘的byte回傳
        // buf[^i]：回傳位置i以後的bytes，因為之前的bytes皆解析過，已經不重要了，只需把未檢查的內容回傳
        return (i == len) ? new List<byte>() : new List<byte>(buf[^i]);
    }

    // 用Segment方式解包
    public static List<byte> UnpackWithSegment(List<byte> packList, Action<byte[]> legitPackCallback = null)
    {
        int len = packList.Count();

        int i = 0;
        for (i = 0; i < len; i = i + 1)
        {
            // Check封包長度是否大於等於HeaderLen長度，否=>不處理，等待下一包
            if (len < i + Protocol.HeaderLen + Protocol.DataByteLen)
                break;

            // 將封包轉Segment處理
            var seg = new ArraySegment<byte>(packList.ToArray());
            byte[] headerBytes = seg.Slice(i, Protocol.HeaderLen).ToArray();
            if (Encoding.Unicode.GetString(headerBytes) == Protocol.Header)
            {
                byte[] msgLenBytes = seg.Slice(i + Protocol.HeaderLen, Protocol.DataByteLen).ToArray();
                int msgLen = BitConverter.ToInt32(msgLenBytes);

                // Check封包長度是否大於等於HeaderLen+ContentLen長度，否=>不處理，等待下一包封包
                int unhandledPackLen = Protocol.HeaderLen + Protocol.DataByteLen + msgLen;
                if (len < i + unhandledPackLen)
                    break;

                var legitPack = seg.Slice(i + Protocol.HeaderLen + Protocol.DataByteLen, msgLen).ToArray();
                legitPackCallback?.Invoke(legitPack);

                i += unhandledPackLen - 1;
            }
        }

        // i == len：代表整個封包已成功解析且合法，不需要把剩餘的byte回傳
        // Skip(i + 1)：回傳位置i以後的bytes，因為之前的bytes皆解析過，已經不重要了，只需把未檢查的內容回傳
        return (i == len) ? new List<byte>() : packList.Skip(i + 1).ToList();
    }
}
