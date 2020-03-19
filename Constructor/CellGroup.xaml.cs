using TreeBased.Dataplane;
using TreeBased.Dataplane.PacketRouter;
using TreeBased.Intilization;
using TreeBased.ui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TreeBased.Models.Cell;



namespace TreeBased.Constructor
{
    public partial class CellCoordinates
    {
        public CellCoordinates(double x11, double x22, double y11, double y22)
        {
            x1 = x11;
            x2 = x22;
            y1 = y11;
            y2 = y22;
        }
        public double x1 { get; set; }
        public double x2 { get; set; }
        public double y1 { get; set; }
        public double y2 { get; set; }
    }
    /// <summary>
    /// Interaction logic for Cluster.xaml
    /// </summary>
    public partial class CellGroup : UserControl
    {

        private double clusterHeight = NetworkConstruction.cellXEdgeLength;
        private double clusterWidth = NetworkConstruction.cellYEdgeLength;
        public List<Sensor> clusterNodes = new List<Sensor>();
        //Location variables for the center and the actual location of cluster
        public Point clusterLocMargin { get; set; }
        public Point clusterCenterComputed { get; set; }
        public Point clusterCenterMargin { get; set; }
        public Point clusterActualCenter { get; set; }
        public CellCoordinates myCoordinates { get; set; }
        public CellCenter centerOfCluster { get; set; }

       // public int clusterDepth { get; set; }

        public int buildClustersunderTop { get; set; }
      //  public Link clusterLinks = new Link();


        private List<int> neighborClusters = new List<int>();
        List<Sensor> myNetwork = PublicParameters.myNetwork;

        private static int assignID { set; get; }
        public static Point ptrail = new Point();
        public static List<CellGroup> changePosClus = new List<CellGroup>();

        //Tree heirarchry variables
        public CellGroup parentCluster;
        public List<CellGroup> childrenClusters = new List<CellGroup>();
        public bool isLeafNode = false;
        public bool isVisited = false;
        public int clusterLevel { get; set; }
        public Point treeParentNodePos { get; set; }
        public Point treeNodePos { get; set; }
        public int xValue { get; set; }


        public CellTable CellTable = new CellTable();
        //Cluster header
       // public ClusterHeaderTable clusterHeader = new ClusterHeaderTable();

        public CellGroup()
        {

        }
        public CellGroup(Point locatio, String id)
        {
            InitializeComponent();

            this.id = int.Parse(id);
            clusterLocMargin = locatio;


        }

        public void setPositionOnWindow(Canvas sensingField)
        {
            double xDif= NetworkConstruction.cellXEdgeLength;
            double yDif = NetworkConstruction.cellYEdgeLength;

            Point[] arr = new Point[4];
            Point one = clusterLocMargin;
            Point two = new Point(clusterLocMargin.X, clusterLocMargin.Y - yDif);
            Point three = new Point(clusterLocMargin.X + xDif, clusterLocMargin.Y);
            Point four = new Point(clusterLocMargin.X + xDif, clusterLocMargin.Y - yDif);
            arr[0] = one;
            arr[1] = two;
            arr[2] = three;
            arr[3] = four;
            for(int j=0; j < 3; j++)
            {
                //1-3 , 2-4
                int i = j;
                if(j < 2) { 
                Point from = arr[j];
                Point to = arr[i + 2];

                Line connection = new Line();
                connection.Stroke = Brushes.Black;
                connection.Fill = Brushes.Black;
                connection.X1 = from.X;
                connection.Y1 = from.Y;
                connection.X2 = to.X;
                connection.Y2 = to.Y;
                sensingField.Children.Add(connection);
                }
                
                if (j % 2 == 0)
                {
                    i = j;
                    Point from = arr[i];
                    Point to = arr[i+ 1];
                    Line connection2 = new Line();
                    connection2.Stroke = Brushes.Black;
                    connection2.Fill = Brushes.Black;
                    connection2.X1 = from.X;
                    connection2.Y1 = from.Y;
                    connection2.X2 = to.X;
                    connection2.Y2 = to.Y ;
                    sensingField.Children.Add(connection2);
                }
            }
            

            
            //Giving a margin for each cluster center (Margin inside the container)





        }


        private int id { set; get; }

        public String getStringID()
        {
            return "" + id;
        }
        public bool isNotEmpty()
        {
            if (this.getClusterNodes().Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public int getID()
        {
            return this.id;
        }

        public List<Sensor> getClusterNodes()
        {
            return clusterNodes;
        }

        public bool isNear(Point p1)
        {
            double offset = PublicParameters.cellRadius / 2;
            Point p2 = new Point(this.clusterLocMargin.X + offset, this.clusterLocMargin.Y + offset);
            double x = Operations.DistanceBetweenTwoPoints(p1, p2);
           
            if (x <= offset)
            {
                
                return true;
            }
            else
            {
                return false;
            }
        }



        //This function find the nearest sensor to the point needed 
        public void findNearestSensor(bool isReCheck)
        {
            double x1 = this.clusterLocMargin.X;
            double x2 = x1 + NetworkConstruction.cellXEdgeLength;
            double y1 = this.clusterLocMargin.Y;
            double y2 = y1 - NetworkConstruction.cellYEdgeLength;
            List<Sensor> nearestSen = new List<Sensor>();
            bool clusterDouble = false;
            foreach (Sensor sensor in myNetwork)
            {
                if (sensor.ID != PublicParameters.SinkNode.ID)
                {
                    if (sensor.CenterLocation.X > x1 && sensor.CenterLocation.X < x2
                        && sensor.CenterLocation.Y < y1 && sensor.CenterLocation.Y > y2)
                    {
                       /* if (sensor.inCell != -1)
                        {
                            clusterDouble = true;
                            break;
                        }
                        */
                        nearestSen.Add(sensor);
                    }
                }
            }
            this.clusterNodes = nearestSen;
            if (nearestSen.Count > 0 && !isReCheck && !clusterDouble)
            {
                PublicParameters.networkCells.Add(this);
            }
            else if (isReCheck)
            {
            }
        }

        public void findNearestSensorRecheck(bool isFinal)
        {
            double radius = PublicParameters.cellRadius;
            List<Sensor> nearestSen = new List<Sensor>();
            nearestSen.Clear();
            foreach (Sensor sensor in myNetwork)
            {
                Point p = new Point(sensor.CenterLocation.X, sensor.CenterLocation.Y);
                if (sensor.ID != PublicParameters.SinkNode.ID)
                {
                    if (this.isNear(p))
                    {
                        nearestSen.Add(sensor);
                    }
                }
            }
            this.clusterNodes = nearestSen;
            if (nearestSen.Count > 0 && !isFinal)
            {
                changePosClus.Add(this);

            }
            if (nearestSen.Count > 0 && isFinal)
            {

                for (int i = 0; i < PublicParameters.networkCells.Count(); i++)
                {
                    if (PublicParameters.networkCells[i].getID() == this.getID())
                    {
                        PublicParameters.networkCells.Remove(PublicParameters.networkCells[i]);
                        PublicParameters.networkCells.Add(this);
                    }
                }


            }




        }

        public static void getCenterOfNetwork()
        {
            double sumX = 0;
            double sumY = 0;
            double count = 0;
            foreach (Sensor sensor in PublicParameters.myNetwork)
            {
                sumX += sensor.CenterLocation.X;
                sumY += sensor.CenterLocation.Y;
                count++;
            }
            sumX /= count;
            sumY /= count;
            PublicParameters.networkCenter = new Point(sumX, sumY);
           


        }

        public void getNodesCenter()
        {
            double halfX = NetworkConstruction.cellXEdgeLength / 2;
            double halfY = NetworkConstruction.cellYEdgeLength / 2;
        //    double halfRad = PublicParameters.cellRadius / 2;
            double clusterX = this.clusterLocMargin.X + halfX;
            double clusterY = this.clusterLocMargin.Y - halfY;
            clusterActualCenter = new Point(clusterX, clusterY);

            double x1 = clusterLocMargin.X;
            double x2 = x1 + halfX * 2;
            double y1 = clusterLocMargin.Y;
            double y2 = y1 - halfY * 2;
            this.myCoordinates = new CellCoordinates(x1, x2, y1, y2);


            double sumX = 0;
            double sumY = 0;
  

            sumX = clusterX;
            sumY = clusterY;

            //double marginTop = Math.Floor(clusterY - this.clusterLocMargin.Y) - label_clustercenter.Height/2;
            // double marginLeft =Math.Floor( clusterX - this.clusterLocMargin.X) - label_clustercenter.Width/2;


            clusterCenterMargin = new Point(sumX, sumY);
            CellCenter center = new CellCenter(clusterCenterMargin, this.getID());
            this.centerOfCluster = center;

            clusterCenterComputed = new Point(sumX, sumY);


        }

        public static double getAverageSensors()
        {
            double sum = 0;
            double clusterCount = PublicParameters.networkCells.Count();
            foreach (CellGroup cluster in PublicParameters.networkCells)
            {
                sum += cluster.clusterNodes.Count();
            }
            Console.WriteLine("AVG {0}", (sum / clusterCount));
            return Math.Floor(sum / clusterCount);
        }


        public void incDecPos(int direction, double multiply)
        {
            double radius = PublicParameters.cellRadius;
            double distance = (radius + (radius / 2));
            Point moveTo = new Point(this.clusterLocMargin.X, this.clusterLocMargin.Y);
            switch (direction)
            {
                case 1:
                    moveTo.X += distance * multiply;
                    break;
                case 2:
                    moveTo.X -= distance * multiply;
                    break;
                case 3:
                    moveTo.Y -= distance * multiply;
                    break;
                case 4:
                    moveTo.Y += distance * multiply;
                    break;
                case 5:
                    moveTo.X += distance * multiply;
                    moveTo.Y -= distance * multiply;
                    break;
                case 6:
                    moveTo.X -= distance * multiply;
                    moveTo.Y += distance * multiply;
                    break;
                case 7:
                    moveTo.X -= distance * multiply;
                    moveTo.Y -= distance * multiply;
                    break;
                case 8:
                    moveTo.X += distance * multiply;
                    moveTo.Y += distance * multiply;
                    break;
            }
            this.clusterLocMargin = moveTo;

        }

        public CellGroup getMaxSensors()
        {
            List<int> sensorsCount = new List<int>();
            foreach (CellGroup tempClus in changePosClus)
            {
                sensorsCount.Add(tempClus.clusterNodes.Count());

            }
            int maxCount = 0;
            foreach (int i in sensorsCount)
            {
                if (maxCount < i)
                {
                    maxCount = i;
                }
            }
            CellGroup foundMax = new CellGroup();
            foreach (CellGroup tempClus in changePosClus)
            {
                if (tempClus.clusterNodes.Count == maxCount)
                {
                    foundMax = tempClus;
                }
            }
            changePosClus.Clear();
            return foundMax;
        }

        public void checkNearClusters()
        {

        }

        public static CellGroup getClusterWithID(int findID)
        {
            CellGroup getCluster = new CellGroup();


            foreach (CellGroup findCluster in PublicParameters.networkCells)
            {
                if (findCluster.getID() == findID)
                {

                    // Console.WriteLine("Searching in {0}", findCluster.getID());
                    getCluster = findCluster;
                    //  Console.WriteLine("Returning");
                    return findCluster;

                }
            }
            //   Console.WriteLine("Returned");
            // if (!foundCluster)
            //{
            //     throw new System.ArgumentException("Cluster not found", "original");
            //  }
            //  else
            // {
            return getCluster;
            // }




        }

        /// <summary>
        /// Used to set or change the cluster head
        /// </summary>
        /// <param name="isRechange">isReachange = false if it's used to set for the first time</param>
        



    }





}
