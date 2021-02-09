using System;
using System.Xml.Serialization;

namespace SpeedTest.Models
{
    [XmlRoot("server")]
    public class Server
    {
        [XmlAttribute("id")]
        public int Id { get; set; }

        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("country")]
        public string Country { get; set; }

        [XmlAttribute("sponsor")]
        public string Sponsor { get; set; }

        [XmlAttribute("host")]
        public string Host { get; set; }

        [XmlAttribute("url")]
        public string Url { get; set; }

        [XmlAttribute("lat")]
        public double Latitude { get; set; }

        [XmlAttribute("lon")]
        public double Longitude { get; set; }

        public double Distance { get; set; }

        public int Latency { get; set; }

        private Lazy<Coordinate> geoCoordinate;
        public Coordinate GeoCoordinate
        {
            get { return geoCoordinate.Value; }
        }

        public Server()
        {
            // note: geo coordinate will not be recalculated on Latitude or Longitude change
            geoCoordinate = new Lazy<Coordinate>(() => new Coordinate(Latitude, Longitude));
        }
    }
}