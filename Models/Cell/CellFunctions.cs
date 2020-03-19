using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using TreeBased.Constructor;
using TreeBased.Dataplane;
using TreeBased.Intilization;

namespace TreeBased.Models.Cell
{
    class CellFunctions
    {

        public static void FillOutsideSensnors()
        {  
            foreach (Sensor sen in PublicParameters.myNetwork)
            {
                if (sen.ID!= PublicParameters.SinkNode.ID && sen.inCell == -1)
                {
                    //Check the nearest cluster for it
                    double offset = 120;
                    int nearestID = 0;
                    foreach (CellGroup cluster in PublicParameters.networkCells)
                    {
                        double distance = Operations.DistanceBetweenTwoPoints(sen.CenterLocation, cluster.clusterActualCenter);
                        if (distance < offset)
                        {
                            nearestID = cluster.getID();
                            offset = distance;
                        }
                    }
                    sen.TuftNodeTable.NearestCellCenter = CellGroup.getClusterWithID(nearestID).clusterActualCenter;

                }
            }
        
        }

        public static void ChangeCellHeader(Sensor currentHeader)
        {
            CellGroup Cell = CellGroup.getClusterWithID(currentHeader.TuftNodeTable.CellNumber);
            Sensor newHeader = ReassignCellHeader(Cell);
            if (newHeader.ID != currentHeader.ID)
            {
                CellHeader oldHeaderCellTable = currentHeader.TuftNodeTable.CellHeaderTable;
                currentHeader.TuftNodeTable.CellHeaderTable = new CellHeader();
                Cell.CellTable.CellHeader = newHeader;
                PopulateHeaderInformation(Cell);
                ClearOldCellHeader(oldHeaderCellTable, newHeader);
                PublicParameters.SinkNode.MainWindow.Dispatcher.Invoke(() => currentHeader.Ellipse_HeaderAgent_Mark.Visibility = Visibility.Hidden);
                newHeader.Ellipse_HeaderAgent_Mark.Stroke = new SolidColorBrush(Colors.Red);
                PublicParameters.SinkNode.MainWindow.Dispatcher.Invoke(() => newHeader.Ellipse_HeaderAgent_Mark.Visibility = Visibility.Visible);
               // currentHeader.ReRoutePacketsInCellHeaderBuffer();
               // currentHeader.TuftNodeTable.CellHeaderTable.DidChangeHeader(currentHeader);
               if(currentHeader.WaitingPacketsQueue.Count > 0)
                {
                    Console.WriteLine();
                }

            }
           
           
        }

       

        public static void PopulateHeaderInformation(CellGroup Cell)
        {
            Sensor header = Cell.CellTable.CellHeader;
            foreach (Sensor cellNode in Cell.clusterNodes)
            {
                if (header.ID != cellNode.ID)
                {
                    cellNode.TuftNodeTable.myCellHeader = header;
                    cellNode.TuftNodeTable.CellHeaderTable.isHeader = false;
                }
                else
                {
                    cellNode.TuftNodeTable.myCellHeader = header;
                    cellNode.TuftNodeTable.CellHeaderTable.isHeader = true;
                }
                
            }

            List<CellGroup> holderCycle = new List<CellGroup>();
            string myID = Cell.getStringID();
            for(int i = 0; i < PublicParameters.networkCells.Count; i++)
            {
                if (PublicParameters.networkCells[i].getID() != Cell.getID())
                {
                    string oppoCellID = PublicParameters.networkCells[i].getStringID();
                    if(myID[0] == oppoCellID[0])
                    {
                        holderCycle.Add(PublicParameters.networkCells[i]);
                    }else if(myID[1] == oppoCellID[1])
                    {
                        holderCycle.Add(PublicParameters.networkCells[i]);
                    }
                    
                }
            }

            CellHeader ch = new CellHeader();
           // ch.atTreeDepth = Cell.clusterLevel;
            ch.isHeader = true;
            ch.encapsulatingCell = Cell;
            ch.OuterCycleLinks = holderCycle;
            header.TuftNodeTable.CellHeaderTable = ch;
        }

        private static void ClearOldCellHeader(CellHeader oldHeader,Sensor newHeader)
        {
            newHeader.TuftNodeTable.CellHeaderTable.headerChangeInformation(oldHeader);
           // oldHeader.TuftNodeTable.CellHeaderTable.isHeader = false;
          //  oldHeader.TuftNodeTable.CellHeaderTable.hasSinkPosition = false;
    

        }

        
        public static void ChangeOuterCycleFormation(CellGroup RootCell)
        {
            RootCell.CellTable.CellHeader.TuftNodeTable.CellHeaderTable.isRootCellHeader = true; ;
            foreach(CellGroup cell in PublicParameters.networkCells)
            {
                cell.CellTable.CellHeader.TuftNodeTable.CellHeaderTable.changeNewRootCellHeader(RootCell);
            }
        }

        public static Sensor ReassignCellHeader(CellGroup Cell){

                Sensor holder = null;
                // check according to remaining enery and distance
                double ENorm = 0;
                double DNorm = 0;
                double max = 0;

                foreach (Sensor sen in Cell.clusterNodes)
                {
                    ENorm = sen.ResidualEnergyPercentage;
                    DNorm = (Operations.DistanceBetweenTwoPoints(sen.CenterLocation, Cell.clusterCenterComputed));
                    ENorm /= 100;
                    DNorm = Math.Sqrt(Math.Pow(NetworkConstruction.cellXEdgeLength,2) + Math.Pow(NetworkConstruction.cellYEdgeLength, 2)) / DNorm;


                sen.CellHeaderProbability = ENorm + DNorm;

                    if (sen.CellHeaderProbability > max && Cell.CellTable.CellHeader.ID !=sen.ID) 
                    {
                        max = sen.CellHeaderProbability;
                        holder = sen;
                    }
                }

                try
                {
                    return holder;
                }
                catch
                {
                    holder = null;
                    return null;
                }
                /*if (holder.ID != Cell.CellTable.CellHeader.ID)
                {
                    Sensor oldheader = Cell.CellTable.CellHeader;
                    Cell.CellTable = new CellTable(holder, holder.CenterLocation);
                    PublicParameters.SinkNode.MainWindow.Dispatcher.Invoke(() => holder.Ellipse_HeaderAgent_Mark.Visibility = Visibility.Hidden);
                    RePopulateHeaderInformation(Cell, oldheader);
                }*/

          
        }


        public static void assignClusterHead(CellGroup Cell)
        {
            double offset = NetworkConstruction.cellXEdgeLength + NetworkConstruction.cellYEdgeLength;
            Sensor holder = null;

            foreach (Sensor sen in Cell.clusterNodes)
                {
                    double distance = Operations.DistanceBetweenTwoPoints(Cell.clusterCenterComputed, sen.CenterLocation);
                    if (distance < offset)
                    {
                        offset = distance;
                        holder = sen;
                    }
            }
                   
            try
            {
                Cell.CellTable = new CellTable(holder, holder.CenterLocation);
            }
            catch
            {
                holder = null;
                MessageBox.Show("Error in assiging Cluster Header");
                return;
            }
            holder.Ellipse_HeaderAgent_Mark.Stroke = new SolidColorBrush(Colors.Red);
            PublicParameters.SinkNode.MainWindow.Dispatcher.Invoke(() => holder.Ellipse_HeaderAgent_Mark.Visibility = Visibility.Visible);
            PopulateHeaderInformation(Cell);
        }
    }
}
