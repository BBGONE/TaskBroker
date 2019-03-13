using System;
using System.Collections.Specialized;
using System.Xml.Linq;

namespace TaskBroker.SSSB.Core
{
    public class MessageAtributes
    {
        public int? TaskID { get; set; }
        public bool IsMultiStepTask { get; set; }
        public DateTime EventDate { get; set; }
        public NameValueCollection Parameters { get; set; }
        public bool IsDefered { get; set; }
        public int AttemptNumber { get; set; }
    }

    public static class MessageAtributesHelper
    {
        public static NameValueCollection ExtractParameters(this XElement xml)
        {
            NameValueCollection nvcol = new NameValueCollection();

            XElement params_xml = xml.Element("params");
            if (params_xml != null && params_xml.HasElements)
            {
                foreach (XElement param in params_xml.Elements("param"))
                {
                    string name = param.Attribute("name").Value;
                    string value = null;
                    if (param.Attribute("value") != null)
                        value = param.Attribute("value").Value;
                    nvcol.Add(name, value);
                }
            }

            return nvcol;
        }

        public static MessageAtributes GetMessageAttributes(this XElement xml)
        {
            int? taskID = null;
            bool isMultiStepTask = false;
            DateTime eventDate = DateTime.Now;
            NameValueCollection parameters = null;

            taskID = int.Parse(xml.Element("task").Value);
            string multyStep = xml.Element("multy-step").Value;
            if (!string.IsNullOrEmpty(multyStep))
                bool.TryParse(multyStep, out isMultiStepTask);
            string str_date = xml.Element("date").Value;
            eventDate = DateTime.ParseExact(str_date, "yyyy-MM-dd HH:mm:ss", null);
            parameters = ExtractParameters(xml);
            return new MessageAtributes() { TaskID = taskID, IsMultiStepTask = isMultiStepTask, EventDate = eventDate, Parameters = parameters };
        }
    }
}
