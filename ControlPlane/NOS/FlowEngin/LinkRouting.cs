using TreeBased.Dataplane;
using TreeBased.Dataplane.NOS;
using TreeBased.Dataplane.PacketRouter;
using TreeBased.Intilization;
using TreeBased.Properties;
using TreeBased.ui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TreeBased.ControlPlane.NOS.FlowEngin
{
    public class MiniFlowTableSorterDownLinkPriority : IComparer<FlowTableEntry>
    {

        public int Compare(FlowTableEntry y, FlowTableEntry x)
        {
            return x.DownLinkPriority.CompareTo(y.DownLinkPriority);
        }
    }


    public class LinkFlowEnery
    {
        public Sensor Current { get; set; }
        public Sensor Next { get; set; }
        public Sensor Target { get; set; }

        // Elementry values:
        public double D { get; set; } // direction value tworads the end node
        public double DN { get; set; } // R NORMALIZEE value of To. 
        public double DP { get; set; } // defual.

        public double L { get; set; } // remian energy
        public double LN { get; set; } // L normalized
        public double LP { get; set; } // L value of To.

        public double R { get; set; } // riss
        public double RN { get; set; } // R NORMALIZEE value of To. 
        public double RP { get; set; } // R NORMALIZEE value of To. 

        //Perpendicular Distance
        public double pirDis { get; set; }
        public double pirDisNorm { get; set; }



        //
        public double Pr
        {
            get;
            set;
        }

        // return:
        public double Mul
        {
            get
            {
                return LP * DP * RP;
            }
        }

        public int IindexInMiniFlow { get; set; }
        public FlowTableEntry FlowTableEntry { get; set; }
    }



    public class LinkRouting
    {
        public static double srcPerDis { get; set; }

        public static FlowTableEntry getBiggest(List<FlowTableEntry> table)
        {
            double offset = -10;
            FlowTableEntry biggest = null;
            foreach (FlowTableEntry entry in table)
            {                
                if (entry.DownLinkPriority > offset)
                {
                    offset = entry.DownLinkPriority;
                    biggest = entry;
                }

            }
            return biggest;
        }
        public static void sortTable(Sensor sender)
        {

            List<FlowTableEntry> beforeSort = sender.TuftFlowTable;
            List<FlowTableEntry> afterSort = new List<FlowTableEntry>();
            do
            {
                FlowTableEntry big = null;
                try
                {
                    big = getSmallest(beforeSort);
                    afterSort.Add(big);
                    beforeSort.Remove(big);
                }
                catch
                {
                    big = null;
                    Console.WriteLine();
                }



            } while (beforeSort.Count > 0);
            sender.TuftFlowTable.Clear();
            sender.TuftFlowTable = afterSort;

        }
        public static FlowTableEntry getSmallest(List<FlowTableEntry> table)
        {
            double offset = table[0].DownLinkPriority + PublicParameters.CommunicationRangeRadius;
            FlowTableEntry biggest = null;
            foreach (FlowTableEntry entry in table)
            {
                if (entry.DownLinkPriority < offset)
                {
                    offset = entry.DownLinkPriority;
                    biggest = entry;
                }

            }
            return biggest;
        }



        public static void GetD_Distribution(Sensor sender, Packet packet)
        {
            List<int> path = Operations.PacketPathToIDS(packet.Path);
            sender.TuftFlowTable.Clear();
            List<int> PacketPath = Operations.PacketPathToIDS(packet.Path);

            Sensor sourceNode;// = packet.Source;
            Point endNodePosition = packet.Destination.CenterLocation;

            double distSrcToEnd = Operations.DistanceBetweenTwoPoints(sender.CenterLocation, endNodePosition);


            double n = Convert.ToDouble(sender.NeighborsTable.Count) + 1;

            foreach (NeighborsTableEntry neiEntry in sender.NeighborsTable)
            {
                if (neiEntry.NeiNode.ResidualEnergyPercentage > 0)
                {
                    if (neiEntry.ID != PublicParameters.SinkNode.ID)
                    {
                        FlowTableEntry MiniEntry = new FlowTableEntry();
                        MiniEntry.SID = sender.ID;
                        MiniEntry.NeighborEntry = neiEntry;
                        MiniEntry.DownLinkPriority = Operations.DistanceBetweenTwoPoints(endNodePosition, MiniEntry.NeighborEntry.CenterLocation);
                        sender.TuftFlowTable.Add(MiniEntry);
                    }                   
                }
            }

           
         
            sortTable(sender);

            //int a = packet.Hops;
            //a ++;  

            int minus = 0;
    
            if (path.Count < 2)
            {
                minus = 1;
            }
            else
            {
                minus = 2;
            }

            int lastForwarder = path[path.Count - minus];
            foreach (FlowTableEntry MiniEntry in sender.TuftFlowTable)
            {
                if (MiniEntry.NID != PublicParameters.SinkNode.ID)
                {
                    double srcEnd = Operations.DistanceBetweenTwoPoints(sender.CenterLocation, endNodePosition);
                    double candEnd = Operations.DistanceBetweenTwoPoints(MiniEntry.NeighborEntry.CenterLocation, endNodePosition);

                    if ((path.Contains(MiniEntry.NID) && (candEnd < srcEnd)))
                    {
                        MiniEntry.DownLinkAction = FlowAction.Forward;
                    }
                    else
                    {
                        if (!(path.Contains(MiniEntry.NID)))
                        {
                            MiniEntry.DownLinkAction = FlowAction.Forward;
                        }
                        else
                        {
                            MiniEntry.DownLinkAction = FlowAction.Drop;
                        }
                    }

                }
            }





        }

         
    }
}
