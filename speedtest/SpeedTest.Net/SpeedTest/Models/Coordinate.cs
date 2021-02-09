using System;

namespace SpeedTest.Models
{
    public class Coordinate
    {
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }

        public Coordinate(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        public double GetDistanceTo(Coordinate other)
        {
            if (double.IsNaN(Latitude) || double.IsNaN(Longitude) || double.IsNaN(other.Latitude) ||
                double.IsNaN(other.Longitude))
            {
                throw new ArgumentException("Argument latitude or longitude is not a number");
            }

            var d1 = Latitude * (Math.PI / 180.0);
            var num1 = Longitude * (Math.PI / 180.0);
            var d2 = other.Latitude * (Math.PI / 180.0);
            var num2 = other.Longitude * (Math.PI / 180.0) - num1;
            var d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) +
                     Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0);

            return 6376500.0 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d3)));
        }
    }
}
