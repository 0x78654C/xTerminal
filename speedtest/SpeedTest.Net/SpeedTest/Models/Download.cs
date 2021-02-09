using System.Xml.Serialization;

namespace SpeedTest.Models
{
    [XmlRoot("download")]
    public class Download
    {
        [XmlAttribute("testlength")]
        public int TestLength { get; set; }

        [XmlAttribute("initialtest")]
        public string InitialTest { get; set; }

        [XmlAttribute("mintestsize")]
        public string MinTestSize { get; set; }

        [XmlAttribute("threadsperurl")]
        public int ThreadsPerUrl { get; set; }
    }
}