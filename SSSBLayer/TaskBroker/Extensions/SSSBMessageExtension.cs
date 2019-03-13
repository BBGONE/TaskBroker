using System.IO;
using System.Text;
using System.Xml.Linq;

namespace TaskBroker.SSSB.Core
{
    public static class SSSBMessageExtension
    {
        public static XElement GetMessageXML(this SSSBMessage message)
        {
            return GetMessageXML(message.Body);
        }

        public static XElement GetMessageXML(this byte[] body)
        {
            using (MemoryStream ms = new MemoryStream(body))
            using (StreamReader sr = new StreamReader(ms, Encoding.Unicode, true))
            {
                return XElement.Load(sr);
            }
        }

        public static byte[] ConvertToBytes(this XElement xml)
        {
            return Encoding.Unicode.GetBytes(xml.ToString());
        }
    }
}
