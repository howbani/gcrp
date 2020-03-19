using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TreeBased.Dataplane;

namespace TreeBased.Constructor
{
    public class SensingFieldArea
    {
        public static double lowestX{get;set;}
        public static double highestX { get; set; }
        public static double lowestY { get; set; }
        public static double highestY { get; set; }

        public static Point PointZero { get; set; }
        public static double xEdge { get; set; }
        public static double yEdge { get; set; }
        private static void GetSensorsCoordinates()
        {
            double holderLowestX = PublicParameters.BorderNodes[0].CenterLocation.X;
            double holderHighestX = PublicParameters.BorderNodes[0].CenterLocation.X;
            double holderLowestY = PublicParameters.BorderNodes[0].CenterLocation.Y;
            double holderHighestY = PublicParameters.BorderNodes[0].CenterLocation.Y;


            foreach(Sensor bordersen in PublicParameters.myNetwork)
            {
                if(bordersen.CenterLocation.X < holderLowestX)
                {
                    holderLowestX = bordersen.CenterLocation.X;
                }
                if(bordersen.CenterLocation.X > holderHighestX)
                {
                    holderHighestX = bordersen.CenterLocation.X;
                }
                if(bordersen.CenterLocation.Y < holderLowestY)
                {
                    holderLowestY = bordersen.CenterLocation.Y;
                }
                if(bordersen.CenterLocation.Y > holderHighestY)
                {
                    holderHighestY = bordersen.CenterLocation.Y;
                }
            }
            lowestX = holderLowestX - PublicParameters.CommunicationRangeRadius /2;
            lowestY = holderLowestY - PublicParameters.CommunicationRangeRadius / 2;
            highestX = holderHighestX + PublicParameters.CommunicationRangeRadius/2;
            highestY = holderHighestY + PublicParameters.CommunicationRangeRadius/2;
        }

  
        public static void GetAreaOfSensingField()
        {
            GetSensorsCoordinates();
            xEdge = highestX - lowestX;
            yEdge = highestY - lowestY;
            double area = xEdge * yEdge;
            PublicParameters.AreaofSensingField = area;
            PointZero = new Point(lowestX, highestY);
        }

    }
}
