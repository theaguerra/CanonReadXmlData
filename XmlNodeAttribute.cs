using System;
using System.Collections.Generic;
using System.Text;

namespace CanonXMLReaderApp
{
    public class XmlNodeName : Attribute
    {
        public string Value;
        public XmlNodeName(string value)
        {
            Value = value;
        }
    }
}
