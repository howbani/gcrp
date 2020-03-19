using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using TreeBased.Constructor;
using TreeBased.Dataplane;
using TreeBased.Dataplane.NOS;
using TreeBased.Intilization;

namespace TreeBased.Models.Cell
{
    public class CellHeader
    {
        //Cell Header Main Variables
        public bool isHeader = false;
       // public Point ParentCellCenter { get; set; }        
        public bool hasSinkPosition = false;
        public Sensor SinkNode { get; set; }
        //   public int atTreeDepth { get; set; }
        // public double DistanceFromRoot { get { return PublicParameters.cellRadius * atTreeDepth; } }
        //public bool isRootHeader = false;

        public List<CellGroup> OuterCycleLinks = new List<CellGroup>();

        public CellGroup encapsulatingCell { get; set; }

        public List<Sensor> OuterNeighbors = new List<Sensor>();
        public Sensor OuterCycleSinkHeader { get; set; }
        public CellGroup RootCell { get; set; }

        public bool isRootCellHeader = false;

        public bool isRootCellMyNei()
        {
            if(OuterCycleLinks.Count > 0)
            {
                if (OuterCycleLinks.Contains(RootCell))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
            
        }
        public void changeNewRootCellHeader(CellGroup RCell)
        {
            if(RCell.getID() != encapsulatingCell.getID())
            {
                hasSinkPosition = false;
                SinkNode = null;
                isRootCellHeader = false;

            }
            else
            {
                isRootCellHeader = true;
                RootCell = null;
            }
            if (OuterNeighbors.Count > 0)
            {
                OuterNeighbors.Clear();
            }
            OuterCycleSinkHeader = RCell.CellTable.CellHeader;
            this.RootCell = RCell;

            foreach(CellGroup cell in OuterCycleLinks)
            {
                if(cell.getID()!= RCell.getID())
                {
                    OuterNeighbors.Add(cell.CellTable.CellHeader);
                }
            }
        }

        public Sensor getNeiCellHeader(Packet pkt)
        {
            List<int> path = Operations.PacketPathToIDS(pkt.Path);
            foreach(Sensor sen in OuterNeighbors)
            {
                if (!path.Contains(sen.ID))
                {
                    return sen;
                    
                }
            }
            return null;
        }


        public void headerChangeInformation(CellHeader old)
        {
   
            RootCell = old.RootCell;
            isRootCellHeader = old.isRootCellHeader;
            SinkNode = old.SinkNode;
            hasSinkPosition = old.hasSinkPosition;
            OuterCycleSinkHeader = old.OuterCycleSinkHeader;
            isHeader = true;
            OuterNeighbors = old.OuterNeighbors;
        }



        private Sensor me { get; set; }
        public bool isNewHeaderAvail = false;
        private DispatcherTimer OldHeaderTimer;

        //Cell Header Buffer
        public Queue<Packet> CellHeaderBuffer = new Queue<Packet>();
        public void StoreInCellHeaderBuffer(Packet packet)
        {
            CellHeaderBuffer.Enqueue(packet);
        }

        public void DidChangeHeader(Sensor m)
        {
            isNewHeaderAvail = true;
           // OldHeaderTimer = new DispatcherTimer();
           // OldHeaderTimer.Interval = TimeSpan.FromSeconds(3);
          //  OldHeaderTimer.Start();
            me = m;
           // OldHeaderTimer.Tick += OldHeaderTimer_Tick;
            hasSinkPosition = false;
            if (CellHeaderBuffer.Count > 0)
            {
                //me.ReRoutePacketsInCellHeaderBuffer();
            }
            ClearData();
            
        }

        void OldHeaderTimer_Tick(object sender, EventArgs e)
        {
            //Sensor.needtoCheck = true;
            if (CellHeaderBuffer.Count > 0)
            {
              //  me.ReRoutePacketsInCellHeaderBuffer();
            }
            isNewHeaderAvail = false;
            ClearData();
            OldHeaderTimer.Stop();
            OldHeaderTimer = null;

        }
       private void ClearData(){
             isHeader = false;     
             hasSinkPosition = false;
             SinkNode =null;
           //  isRootHeader = false;
        }
        

        


    }
}
