public class DocumentStatic
{
    public static string buildId(int pageTotal, long fileSize)
    {
        string s = pageTotal.ToString();
        switch (s.Length)
        {
            default: s = "100000"; break;
            case 1: s = "10000" + s; break;
            case 2: s = "1000" + s; break;
            case 3: s = "100" + s; break;
            case 4: s = "10" + s; break;
            case 5: s = "1" + s; break;
        }
        string key = string.Format("{0}{1}", s, fileSize);
        return key;
    }
}
