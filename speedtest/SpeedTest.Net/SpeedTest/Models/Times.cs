using System.Xml.Serialization;

namespace SpeedTest.Models
{
    [XmlRoot("times")]
    public class Times
    {
        [XmlAttribute("dl1")]
        public int Download1 { get; set; }

        [XmlAttribute("dl2")]
        public int Download2 { get; set; }

        [XmlAttribute("dl3")]
        public int Download3 { get; set; }

        [XmlAttribute("ul1")]
        public int Upload1 { get; set; }

        [XmlAttribute("ul2")]
        public int Upload2 { get; set; }

        [XmlAttribute("ul3")]
        public int Upload3 { get; set; }
    }
}