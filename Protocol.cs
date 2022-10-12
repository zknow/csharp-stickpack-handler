using System.Text;

public class Protocol
{
    public static string Header => "KeyWord";

    public static int HeaderLen => Encoding.Unicode.GetBytes(Header).Length;

    public static int DataByteLen = 4;
}
