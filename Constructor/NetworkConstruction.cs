using TreeBased.Dataplane;
using TreeBased.Dataplane.NOS;
using TreeBased.Dataplane.PacketRouter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TreeBased.Models.Cell;
using System.Windows.Media;

namespace TreeBased.Constructor
{
    class NetworkConstruction
    {
        public NetworkConstruction(Canvas Canvase_SensingFeild, String method)
        {

            if (method == "center")
            {
                buildFromCenter(Canvase_SensingFeild);


            }
            else if (method == "zero")
            {
                //buildFromZeroZero(Canvase_SensingFeild);
                
            }
        }
        private int assignID { get; set; }
        private string CellIDHolder { get; set; }
        private void addClustersToWindow(Canvas Canvas_SensingFeild)
        {
            Console.WriteLine("The count for clusters {0}", PublicParameters.networkCells.Count());
            foreach (CellGroup cluster in PublicParameters.networkCells)
             {
                 cluster.getNodesCenter();
                 cluster.setPositionOnWindow(Canvas_SensingFeild);
                 Canvas_SensingFeild.Children.Add(cluster.centerOfCluster);
             }
        }


        private static int AfterSmallAverage { get; set; }
        private static void getAfterAverage()
        {
            double avg = Math.Abs(CellGroup.getAverageSensors());
            avg-=2;
            double i = 0;
            double y = 0;
            foreach(CellGroup cell in PublicParameters.networkCells)
            {
                double count = cell.clusterNodes.Count;
                if(count > avg)
                {
                    i += count;
                    y++;
                }
            }
            AfterSmallAverage = Convert.ToInt32(i / y);
            //AfterSmallAverage = Math.Abs(i);

        }

       

        //Assign cluster IDs here

        private void addIdsToSensorA(CellGroup cluster)
        {

            foreach (Sensor sensor in cluster.getClusterNodes())
            {
                sensor.inCell = cluster.getID();
                //Console.WriteLine("Sensor {0} is in {1}", sensor.ID, cluster.getID());
            }

        }
        private static void addIdsToSensorFinal()
        {
            foreach (Sensor sen in PublicParameters.myNetwork)
            {
                sen.inCell = -1;

            }
            foreach (CellGroup cluster in PublicParameters.networkCells)
            { 
                foreach (Sensor sensor in cluster.getClusterNodes())
                {

                    sensor.inCell = cluster.getID();
                    sensor.TuftNodeTable.isEncapsulated = true;
                    sensor.TuftNodeTable.myCellHeader = cluster.CellTable.CellHeader;
                    sensor.TuftNodeTable.CellNumber = cluster.getID();

                }
            }
            CellFunctions.FillOutsideSensnors();
        }

        private static void populateClusterTables()
        {
            foreach (CellGroup cluster in PublicParameters.networkCells)
            {

                foreach (Sensor sensor in cluster.getClusterNodes())
                {
                    sensor.inCell = cluster.getID();
                    sensor.TuftNodeTable.isEncapsulated = true;
                    sensor.TuftNodeTable.myCellHeader = cluster.CellTable.CellHeader; ;

                }
            }
            CellFunctions.FillOutsideSensnors();
        }


        public static int NumberOfCells { get; set; }
        private void getNumberOfCells()
        {
            int numberofSens = PublicParameters.myNetwork.Count();
            if(numberofSens <= 200)
            {
                NumberOfCells = 4;
            }
            else { NumberOfCells = 16; }
        }
        public static double cellXEdgeLength { get; set; }
        public static double cellYEdgeLength { get; set; }
        private void getCellParameters()
        {
            double xEdge = SensingFieldArea.xEdge;
            double yEdge = SensingFieldArea.yEdge;
            cellXEdgeLength = xEdge / Math.Sqrt(NumberOfCells);
            cellYEdgeLength = yEdge / Math.Sqrt(NumberOfCells);
        }

        private int horizCount = 0;
        private int verticalCount = 0;
        private void buildFromCenter(Canvas Canvase_SensingFeild)
        {
            double canvasHeight = Canvase_SensingFeild.ActualHeight;
            double canvasWidth = Canvase_SensingFeild.ActualWidth;
            getNumberOfCells();
            getCellParameters();
            Point SenPointZero = SensingFieldArea.PointZero;


            CellGroup.getCenterOfNetwork();
            

            double xAxesCount = Math.Sqrt(NumberOfCells);
            double yAxesCount = Math.Sqrt(NumberOfCells);


            verticalCount = 1;
            horizCount = 1;

            Point startfrom = SenPointZero;
            for(int i =1; i <= yAxesCount; i++)
            {
                horizCount = 1;
                buildFromPointZero(startfrom,xAxesCount);
                verticalCount++;
                startfrom.Y -= (i) * cellYEdgeLength;
            }

            addIdsToSensorFinal();
     
         
            addClustersToWindow(Canvase_SensingFeild);

            //Tree tree = new Tree(Canvase_SensingFeild);
           // tree.displayTree();

           initAssignHead();
            populateClusterTables();
        }


        private void initAssignHead()
        {
            foreach (CellGroup cluster in PublicParameters.networkCells)
            {
                CellFunctions.assignClusterHead(cluster);
               
            }
        }

        
        private void buildFromPointZero(Point startFrom, double count)
        {
            for (int i = 0; i < count; i++)
            {
                string cellID = "" + verticalCount + "" + horizCount;
                CellGroup cluster0 = new CellGroup(startFrom, cellID);
                cluster0.findNearestSensor(true);
                if (cluster0.isNotEmpty())
                {
                    //ui.MainWindow.net
                    cluster0.findNearestSensor(false);
                    addIdsToSensorA(cluster0);
                    horizCount++;
                }
                startFrom.X += cellXEdgeLength;
            }
        }

        private void clusterIncPos(int dir, int myPositionHorizontal,int myPositionVertical, Point myStartingPoint)
        {
            //dir==1 is to the right side // dir == 2 is to the top from the starting point
            if(dir == 1)
            {

            }
            else
            {

            }
        }
        /*private void buildFromDirection(String direction, Point startFrom, double count)
        {

            switch (direction)
            {
                case "center":

                    CellGroup cluster = new CellGroup(startFrom, assignID);
                    cluster.findNearestSensor(false);
                    if (cluster.isNotEmpty())
                    {
                        addIdsToSensorA(cluster);
                        assignID++;
                    }
                    break;
                case "right":
                    for (int i = 0; i < count; i++)
                    {
                        CellGroup cluster0 = new CellGroup(startFrom, assignID);
                        cluster0.incDecPos(1, i + 1);
                        cluster0.findNearestSensor(true);
                        if (cluster0.isNotEmpty())
                        {
                            //ui.MainWindow.net
                            cluster0.findNearestSensor(false);
                            addIdsToSensorA(cluster0);
                            assignID++;
                        }
                    }
                    break;
                case "left":
                    for (int i = 0; i < count; i++)
                    {
                        CellGroup cluster1 = new CellGroup(startFrom, assignID);
                        cluster1.incDecPos(2, i + 1);
                        cluster1.findNearestSensor(true);
                        if (cluster1.isNotEmpty())
                        {

                            cluster1.findNearestSensor(false);
                            addIdsToSensorA(cluster1);
                            assignID++;
                        }
                    }
                    break;
                case "up":
                    for (int i = 0; i < count; i++)
                    {
                        CellGroup cluster2 = new CellGroup(startFrom, assignID);
                        cluster2.incDecPos(3, i + 1);
                        cluster2.findNearestSensor(true);
                        if (cluster2.isNotEmpty())
                        {

                            cluster2.findNearestSensor(false);
                            addIdsToSensorA(cluster2);
                            assignID++;

                        }
                    }
                    break;
                case "down":

                    for (int i = 0; i < count; i++)
                    {
                        CellGroup cluster3 = new CellGroup(startFrom, assignID);
                        cluster3.incDecPos(4, i + 1);
                        cluster3.findNearestSensor(true);
                        if (cluster3.isNotEmpty())
                        {

                            cluster3.findNearestSensor(false);
                            addIdsToSensorA(cluster3);
                            assignID++;

                        }
                    }
                    break;
                case "upright":

                    for (int i = 0; i < count; i++)
                    {
                        CellGroup cluster4 = new CellGroup(startFrom, assignID);
                        cluster4.incDecPos(5, i + 1);
                        cluster4.findNearestSensor(true);
                        if (cluster4.isNotEmpty())
                        {
                            cluster4.buildClustersunderTop = i + 1;
                            cluster4.findNearestSensor(false);
                            addIdsToSensorA(cluster4);
                            assignID++;


                        }
                    }
                    break;
                case "downleft":

                    for (int i = 0; i < count; i++)
                    {
                        CellGroup cluster5 = new CellGroup(startFrom, assignID);
                        cluster5.incDecPos(6, i + 1);
                        cluster5.findNearestSensor(true);
                        if (cluster5.isNotEmpty())
                        {
                            cluster5.buildClustersunderTop = i + 1;
                            cluster5.findNearestSensor(false);
                            addIdsToSensorA(cluster5);
                            assignID++;

                        }

                    }

                    break;
                case "upleft":

                    for (int i = 0; i < count; i++)
                    {
                        CellGroup cluster6 = new CellGroup(startFrom, assignID);
                        cluster6.incDecPos(7, i + 1);
                        cluster6.findNearestSensor(true);
                        if (cluster6.isNotEmpty())
                        {
                            cluster6.buildClustersunderTop = i + 1;
                            cluster6.findNearestSensor(false);
                            addIdsToSensorA(cluster6);
                            assignID++;

                        }
                    }
                    break;
                case "downright":

                    for (int i = 0; i < count; i++)
                    {
                        CellGroup cluster7 = new CellGroup(startFrom, assignID);
                        cluster7.incDecPos(8, i + 1);
                        cluster7.findNearestSensor(true);
                        if (cluster7.isNotEmpty())
                        {
                            cluster7.buildClustersunderTop = i + 1;
                            cluster7.findNearestSensor(false);
                            addIdsToSensorA(cluster7);
                            assignID++;

                        }

                    }
                    break;

            }
        }
        */

        public static void sendTrial(int count)
        {           
         
        }
    }
}
