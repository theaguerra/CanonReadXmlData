using System;
using System.Collections.Generic;
using System.Text;

namespace CanonXMLReaderApp
{
    public class CanonExportData
    {
        [XmlNodeName("PCKTDOCFLD")]
        public string DocumentID { get; set; }
        [XmlNodeName("PCKTDOCFLD")]
        public string FrickForm { get; set; }
        [XmlNodeName("PCKTDOCFLD")]
        public string State { get; set; }
        [XmlNodeName("KTAFLDVAL")]
        public string CreatedAt { get; set; }
        [XmlNodeName("TBL")]
        public PersonalInfo[] PIData { get; set; }
    }
}
