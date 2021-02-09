using System.Collections.Generic;
using System.Xml.Serialization;

namespace SpeedTest.Models
{
    [XmlRoot("settings")]
    public class Settings
    {
        [XmlElement("client")]
        public Client Client { get; set; }

        [XmlElement("times")]
        public Times Times { get; set; }

        [XmlElement("download")]
        public Download Download { get; set; }

        [XmlElement("upload")]
        public Upload Upload { get; set; }

        [XmlElement("server-config")]
        public ServerConfig ServerConfig { get; set; }

        public List<Server> Servers { get; set; }

        public Settings()
        {
            Servers = new List<Server>();
        }
    }
}