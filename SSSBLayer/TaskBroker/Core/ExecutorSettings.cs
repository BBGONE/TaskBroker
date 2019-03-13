using System;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace TaskBroker.SSSB.Core
{
    public class ExecutorSettings
    {
        private readonly object _syncRoot = new Object();
        private readonly string _serilalized;
        private object _deserialized;

        public ExecutorSettings(string serialized)
        {
            this._serilalized = serialized;
            this._deserialized = null;
        }

        public T GetDeserialized<T>()
        {
            if (this._deserialized== null)
            {
                lock (this._syncRoot)
                {
                    if (this._deserialized == null)
                    {
                        if (typeof(T) == typeof(XElement))
                        {
                            _deserialized = (T)((object)XElement.Parse(_serilalized));
                        }
                        else
                        {
                            XmlSerializer serializer = new XmlSerializer(typeof(T));
                            _deserialized = (T)serializer.Deserialize(new System.IO.StringReader(_serilalized));
                        }
                    }
                }
            }

            return (T)_deserialized;
        }

        public string Serialized
        {
            get
            {
                return this._serilalized;
            }
        }
    }
}
