using TreeBased.Intilization;
using TreeBased.Energy;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TreeBased.ui;
using TreeBased.Properties;
using System.Windows.Threading;
using System.Threading;
using TreeBased.ControlPlane.NOS;
using TreeBased.ui.conts;
using TreeBased.ControlPlane.NOS.FlowEngin;
using TreeBased.Forwarding;
using TreeBased.Dataplane.PacketRouter;
using TreeBased.Dataplane.NOS;
using TreeBased.Models.MobileSink;
using TreeBased.Constructor;
using System.Diagnostics;
using TreeBased.Models.MobileModel;
using TreeBased.ControlPlane.DistributionWeights;
using TreeBased.Models.Energy;
using TreeBased.Models.Cell;

namespace TreeBased.Dataplane
{
    public enum SensorState { initalized, Active, Sleep } // defualt is not used. i 
    public enum EnergyConsumption { Transmit, Recive } // defualt is not used. i 


    /// <summary>
    /// Interaction logic for Node.xaml
    /// </summary>
    public partial class Sensor : UserControl
    {
        #region Common parameters.
        
        public Radar Myradar; 
        public List<Arrow> MyArrows = new List<Arrow>();
        public MainWindow MainWindow { get; set; } // the mian window where the sensor deployed.
        public static double SR { get; set; } // the radios of SENSING range.
        public double SensingRangeRadius { get { return SR; } }
        public static double CR { get; set; }  // the radios of COMUNICATION range. double OF SENSING RANGE
        public double ComunicationRangeRadius { get { return CR; } }
        public double BatteryIntialEnergy; // jouls // value will not be changed
        private double _ResidualEnergy; //// jouls this value will be changed according to useage of battery
        public List<int> DutyCycleString = new List<int>(); // return the first letter of each state.
        public BoXMAC Mac { get; set; } // the mac protocol for the node.
        public SensorState CurrentSensorState { get; set; } // state of node.
        public List<RoutingLog> Logs = new List<RoutingLog>();
        public List<NeighborsTableEntry> NeighborsTable = null; // neighboring table.
        public List<FlowTableEntry> TuftFlowTable = new List<FlowTableEntry>(); //flow table.
        private BatteryLevelThresh BT = new BatteryLevelThresh();
        public int NumberofPacketsGeneratedByMe = 0; // the number of packets sent by this packet.
        public FirstOrderRadioModel EnergyModel = new FirstOrderRadioModel(); // energy model.
        public int ID { get; set; } // the ID of sensor.
       
        public bool trun { get; set; }// this will be true if the node is already sent the beacon packet for discovering the number of hops to the sink.
        private DispatcherTimer SendPacketTimer = new DispatcherTimer();// 
        private DispatcherTimer QueuTimer = new DispatcherTimer();// to check the packets in the queue right now.
        public Queue<Packet> WaitingPacketsQueue = new Queue<Packet>(); // packets queue.
        public DispatcherTimer OldAgentTimer = new DispatcherTimer();
        public DispatcherTimer BatteryThreshTimer = new DispatcherTimer();
        public List<BatRange> BatRangesList = new List<Energy.BatRange>();

        public CaluclateWeights CW = new CaluclateWeights();

        public int inCell = -1;
        
        public CellNode TuftNodeTable = new CellNode();

        public Agent AgentNode = new Agent();
        public bool isSinkAgent = false;
        public Sensor SinkAdversary { get; set; }
        public Point SinkPosition { get; set; }
        public bool CanRecievePacket { get { return this.ResidualEnergy > 0; } }
        private Stopwatch QueryDelayStopwatch { get; set; }
        public int agentBufferCount { get {
            if (this.isSinkAgent)
            {
                return this.AgentNode.AgentBuffer.Count;
            }
            else
            {
                return 0;
            }
            } }
        public int cellHeaderBufferCount
        {
            get
            {
                if (this.inCell == -1)
                {
                    return 0;
                }
                else
                {
                    if (this.TuftNodeTable.myCellHeader.ID != this.ID)
                    {
                        return 0;
                    }
                    else
                    {

                        return this.TuftNodeTable.CellHeaderTable.CellHeaderBuffer.Count;
                     
                    }
                    
                }
                
            }
        }
        public double CellHeaderProbability { get; set; }

        public Stopwatch DelayStopWatch = new Stopwatch(); 
        /// <summary>
        /// CONFROM FROM NANO NO JOUL
        /// </summary>
        /// <param name="UsedEnergy_Nanojoule"></param>
        /// <returns></returns>
        public double ConvertToJoule(double UsedEnergy_Nanojoule) //the energy used for current operation
        {
            double _e9 = 1000000000; // 1*e^-9
            double _ONE = 1;
            double oNE_DIVIDE_e9 = _ONE / _e9;
            double re = UsedEnergy_Nanojoule * oNE_DIVIDE_e9;
            return re;
        }

        /// <summary>
        /// in JOULE
        /// </summary>
        public double ResidualEnergy // jouls this value will be changed according to useage of battery
        {
            get { return _ResidualEnergy; }
            set
            {
                _ResidualEnergy = value;
                Prog_batteryCapacityNotation.Value = _ResidualEnergy;
            }
        } //@unit(JOULS);


        /// <summary>
        /// 0%-100%
        /// </summary>
        public double ResidualEnergyPercentage
        {
            get { return (ResidualEnergy / BatteryIntialEnergy) * 100; }
        }
        /// <summary>
        /// visualized sensing range and comuinication range
        /// </summary>
        public double VisualizedRadius
        {
            get { return Ellipse_Sensing_range.Width / 2; }
            set
            {
                // sensing range:
                Ellipse_Sensing_range.Height = value * 2; // heigh= sen rad*2;
                Ellipse_Sensing_range.Width = value * 2; // Width= sen rad*2;
                SR = VisualizedRadius;
                CR = SR * 2; // comunication rad= sensing rad *2;

                // device:
                Device_Sensor.Width = value * 4; // device = sen rad*4;
                Device_Sensor.Height = value * 4;
                // communication range
                Ellipse_Communication_range.Height = value * 4; // com rang= sen rad *4;
                Ellipse_Communication_range.Width = value * 4;

                // battery:
                Prog_batteryCapacityNotation.Width = 8;
                Prog_batteryCapacityNotation.Height = 2;
            }
        }

        /// <summary>
        /// Real postion of object.
        /// </summary>
        public Point Position
        {
            get
            {
                double x = Device_Sensor.Margin.Left;
                double y = Device_Sensor.Margin.Top;
                Point p = new Point(x, y);
                return p;
            }
            set
            {
                Point p = value;
                Device_Sensor.Margin = new Thickness(p.X, p.Y, 0, 0);
            }
        }

        /// <summary>
        /// center location of node.
        /// </summary>
        public Point CenterLocation
        {
            get
            {
                double x = Device_Sensor.Margin.Left;
                double y = Device_Sensor.Margin.Top;
                Point p = new Point(x + CR, y + CR);
                return p;
            }
        }

        bool StartMove = false; // mouse start move.
        private void Device_Sensor_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Settings.Default.IsIntialized == false)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    System.Windows.Point P = e.GetPosition(MainWindow.Canvas_SensingFeild);
                    P.X = P.X - CR;
                    P.Y = P.Y - CR;
                    Position = P;
                    StartMove = true;
                }
            }
        }

        private void Device_Sensor_MouseMove(object sender, MouseEventArgs e)
        {
            if (Settings.Default.IsIntialized == false)
            {
                if (StartMove)
                {
                    System.Windows.Point P = e.GetPosition(MainWindow.Canvas_SensingFeild);
                    P.X = P.X - CR;
                    P.Y = P.Y - CR;
                    this.Position = P;
                }
            }
        }

        private void Device_Sensor_MouseUp(object sender, MouseButtonEventArgs e)
        {
            StartMove = false;
        }

        private void Prog_batteryCapacityNotation_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            
            double val = ResidualEnergyPercentage;
            if (val <= 0)
            {
                MainWindow.RandomSelectSourceNodesTimer.Stop();
                
                // dead certificate:
                ExpermentsResults.Lifetime.DeadNodesRecord recod = new ExpermentsResults.Lifetime.DeadNodesRecord();
                recod.DeadAfterPackets = PublicParameters.NumberofGeneratedDataPackets;
                recod.DeadOrder = PublicParameters.DeadNodeList.Count + 1;
                recod.Rounds = PublicParameters.Rounds + 1;
                recod.DeadNodeID = ID;
                recod.NOS = PublicParameters.NOS;
                recod.NOP = PublicParameters.NOP;
                PublicParameters.DeadNodeList.Add(recod);

                Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col0));
                Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col0));


                if (Settings.Default.StopeWhenFirstNodeDeid)
                {
                    MainWindow.TimerCounter.Stop();
                    MainWindow.RandomSelectSourceNodesTimer.Stop();
                    MainWindow.stopSimlationWhen = PublicParameters.SimulationTime;
                    MainWindow.top_menu.IsEnabled = true;
                    MobileModel.StopSinkMovement();
                }
                Mac.SwichToSleep();
                Mac.SwichOnTimer.Stop();
                Mac.ActiveSleepTimer.Stop();
                if (this.ResidualEnergy <= 0)
                {
                    while (this.WaitingPacketsQueue.Count > 0)
                    {
                        //PublicParameters.NumberofDropedPackets += 1;
                        Packet pack = WaitingPacketsQueue.Dequeue();
                        pack.isDelivered = false;
                       // PublicParameters.FinishedRoutedPackets.Add(pack);
                        Console.WriteLine("PID:" + pack.PID + " has been droped.");
                        updateStates(pack);
                        MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_Number_of_Droped_Packet.Content = PublicParameters.NumberofDropedPackets, DispatcherPriority.Send);

                    }
                    this.QueuTimer.Stop();
                    foreach(Sensor sen in PublicParameters.myNetwork)
                    {
                        if(sen.WaitingPacketsQueue.Count > 0)
                        {
                            while (sen.WaitingPacketsQueue.Count > 0)
                            {
                                Packet pkt = sen.WaitingPacketsQueue.Dequeue();
                                pkt.isDelivered = false;
                                pkt.DroppedReason = "DeadNode";
                                updateStates(pkt);
                            }
                        }
                        if (sen.TuftNodeTable.CellHeaderTable.isHeader)
                        {
                            if (sen.TuftNodeTable.CellHeaderTable.CellHeaderBuffer.Count > 0)
                            {
                                while(sen.TuftNodeTable.CellHeaderTable.CellHeaderBuffer.Count > 0)
                                {
                                    Packet pkt = sen.TuftNodeTable.CellHeaderTable.CellHeaderBuffer.Dequeue();
                                    pkt.isDelivered = false;
                                    pkt.DroppedReason = "DeadNode";
                                    updateStates(pkt);
                                }
                            }
                        }else if(sen.agentBufferCount > 0)
                        {
                            if (sen.AgentNode.AgentBuffer.Count > 0)
                            {
                                while (sen.AgentNode.AgentBuffer.Count > 0)
                                {
                                    Packet pkt = sen.AgentNode.AgentBuffer.Dequeue();
                                    pkt.isDelivered = false;
                                    pkt.DroppedReason = "DeadNode";
                                    updateStates(pkt);
                                }
                            }
                        }
                    }
                    if (Settings.Default.ShowRadar) Myradar.StopRadio();
                    QueuTimer.Stop();
                    Console.WriteLine("NID:" + this.ID + ". Queu Timer is stoped.");
                    MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Fill = Brushes.Transparent);
                    MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Visibility = Visibility.Hidden);

                    return;
                }
                return;


            }
            if (val >= 1 && val <= 9)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col1_9)));
               Dispatcher.Invoke(()=> Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col1_9)));
            }

            if (val >= 10 && val <= 19)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col10_19)));
                Dispatcher.Invoke(() => Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col10_19)));
            }

            if (val >= 20 && val <= 29)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col20_29)));
                Dispatcher.Invoke(() => Dispatcher.Invoke(() => Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col20_29))));
            }

            // full:
            if (val >= 30 && val <= 39)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col30_39)));
                Dispatcher.Invoke(() => Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col30_39)));
            }
            // full:
            if (val >= 40 && val <= 49)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col40_49)));
                Dispatcher.Invoke(() => Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col40_49)));
            }
            // full:
            if (val >= 50 && val <= 59)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col50_59)));
                Dispatcher.Invoke(() => Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col50_59)));
            }
            // full:
            if (val >= 60 && val <= 69)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col60_69)));
                Dispatcher.Invoke(() => Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col60_69)));
            }
            // full:
            if (val >= 70 && val <= 79)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col70_79)));
                Dispatcher.Invoke(() => Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col70_79)));
            }
            // full:
            if (val >= 80 && val <= 89)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col80_89)));
                Dispatcher.Invoke(() => Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col80_89)));
            }
            // full:
            if (val >= 90 && val <= 100)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col90_100)));
                Dispatcher.Invoke(() => Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col90_100)));
            }


            /*
            // update the battery distrubtion.
            int battper = Convert.ToInt16(val);
            if (battper > PublicParamerters.UpdateLossPercentage)
            {
                int rangeIndex = battper / PublicParamerters.UpdateLossPercentage;
                if (rangeIndex >= 1)
                {
                    if (BatRangesList.Count > 0)
                    {
                        BatRange range = BatRangesList[rangeIndex - 1];
                        if (battper >= range.Rang[0] && battper <= range.Rang[1])
                        {
                            if (range.isUpdated == false)
                            {
                                range.isUpdated = true;
                                // update the uplink.
                                UplinkRouting.UpdateUplinkFlowEnery(this,);

                            }
                        }
                    }
                }
            }*/
        }


        /// <summary>
        /// show or hide the arrow in seperated thread.
        /// </summary>
        /// <param name="id"></param>
        public void ShowOrHideArrow(int id) 
        {
            Thread thread = new Thread(() =>
            
            {
                lock (MyArrows)
                {
                    Arrow ar = GetArrow(id);
                    if (ar != null)
                    {
                        lock (ar)
                        {
                            if (ar.Visibility == Visibility.Visible)
                            {
                                Action action = () => ar.Visibility = Visibility.Hidden;
                                Dispatcher.Invoke(action);
                            }
                            else
                            {
                                Action action = () => ar.Visibility = Visibility.Visible;
                                Dispatcher.Invoke(action);
                            }
                        }
                    }
                }
            }
            );
            thread.Name = "Arrow for " + id;
            thread.Start();
        }


        // get arrow by ID.
        private Arrow GetArrow(int EndPointID)
        {
            foreach (Arrow arr in MyArrows) { if (arr.To.ID == EndPointID) return arr; }
            return null;
        }



       

        #endregion



       
       

        /// <summary>
        /// 
        /// </summary>
        public void SwichToActive()
        {
            Mac.SwichToActive();

        }

        /// <summary>
        /// 
        /// </summary>
        private void SwichToSleep()
        {
            Mac.SwichToSleep();
        }

       
        public Sensor(int nodeID)
        {
            InitializeComponent();
            //: sink is diffrent:
            if (nodeID == 0) BatteryIntialEnergy = PublicParameters.BatteryIntialEnergyForSink; // the value will not be change
            else
                BatteryIntialEnergy = PublicParameters.BatteryIntialEnergy;

            BatteryThreshTimer.Interval = TimeSpan.FromSeconds(15);
            BatteryThreshTimer.Tick += BatteryThreshTimer_Tick;
            BatteryThreshTimer.Start();
            ResidualEnergy = BatteryIntialEnergy;// joules. intializing.
            Prog_batteryCapacityNotation.Value = BatteryIntialEnergy;
            Prog_batteryCapacityNotation.Maximum = BatteryIntialEnergy;
            lbl_Sensing_ID.Content = nodeID;
            ID = nodeID;
            QueuTimer.Interval = PublicParameters.QueueTime;
            QueuTimer.Tick += DeliveerPacketsInQueuTimer_Tick;
            OldAgentTimer.Interval = TimeSpan.FromSeconds(3);
            OldAgentTimer.Tick += RemoveOldAgentTimer_Tick;
            //:

            SendPacketTimer.Interval = TimeSpan.FromSeconds(1);
           

        }

        private void BatteryThreshTimer_Tick(object sender, EventArgs e)
        {
            if (TuftNodeTable.CellHeaderTable.isHeader)
            {
             
                if (BT.threshReached(ResidualEnergyPercentage))
                {
                  
                    CellFunctions.ChangeCellHeader(this);
                    
                }
            }
        }

        private void UserControl_MouseLeave(object sender, MouseEventArgs e)
        {
            

        }

        /// <summary>
        /// hide all arrows.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserControl_MouseEnter(object sender, MouseEventArgs e)
        {
            /*
            Vertex ver = MainWindow.MyGraph[ID];
            foreach(Vertex v in ver.Candidates)
            {
                MainWindow.myNetWork[v.ID].lbl_Sensing_ID.Background = Brushes.Black;
            }*/
         
        }

        

        private void UserControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
           
        }

       

        public int ComputeMaxHopsUplink
        {
            get
            {
                double  DIS= Operations.DistanceBetweenTwoSensors(PublicParameters.SinkNode, this);
                return Convert.ToInt16(Math.Ceiling((Math.Sqrt(PublicParameters.Density) * (DIS / ComunicationRangeRadius))));
            }
        }

        public int ComputeMaxHopsDownlink(Sensor endNode)
        {
            double DIS = Operations.DistanceBetweenTwoSensors(PublicParameters.SinkNode, endNode);
            return Convert.ToInt16(Math.Ceiling((Math.Sqrt(PublicParameters.Density) * (DIS / ComunicationRangeRadius))));
        }

        #region Old Sending Data ///
      
        /// <summary>
        ///  data or control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="reciver"></param>
        /// <param name="packt"></param>
        
       
        
        public void IdentifySourceNode(Sensor source)
        {
            if (Settings.Default.ShowAnimation && source.ID != PublicParameters.SinkNode.ID)
            {
                Action actionx = () => source.Ellipse_indicator.Visibility = Visibility.Visible;
                Dispatcher.Invoke(actionx);

                Action actionxx = () => source.Ellipse_indicator.Fill = Brushes.Yellow;
                Dispatcher.Invoke(actionxx);
            }
        }

        public void UnIdentifySourceNode(Sensor source)
        {
            if (Settings.Default.ShowAnimation && source.ID != PublicParameters.SinkNode.ID)
            {
                Action actionx = () => source.Ellipse_indicator.Visibility = Visibility.Hidden;
                Dispatcher.Invoke(actionx);

                Action actionxx = () => source.Ellipse_indicator.Fill = Brushes.Transparent;
                Dispatcher.Invoke(actionxx);
            }
        }

        public void GenerateDataPacket()
        {
            if (Settings.Default.IsIntialized && this.ResidualEnergy > 0)
            {
                this.DissemenateData();

            }
        }

        public void GenerateMultipleDataPackets(int numOfPackets)
        {
            for (int i = 0; i < numOfPackets; i++)
            {
                GenerateDataPacket();
                //  Thread.Sleep(50);
            }
        }

        public void GenerateControlPacket(Sensor endNode)
        {
            if (Settings.Default.IsIntialized && this.ResidualEnergy > 0)
            {

                

            }
        }
        /// <summary>
        /// to the same endnode.
        /// </summary>
        /// <param name="numOfPackets"></param>
        /// <param name="endone"></param>
        public void GenerateMultipleControlPackets(int numOfPackets, Sensor endone)
        {
            for (int i = 0; i < numOfPackets; i++)
            {
                GenerateControlPacket(endone);
            }
        }

        public void IdentifyEndNode(Sensor endNode)
        {
            if (Settings.Default.ShowAnimation && endNode.ID != PublicParameters.SinkNode.ID)
            {
                Action actionx = () => endNode.Ellipse_indicator.Visibility = Visibility.Visible;
                Dispatcher.Invoke(actionx);

                Action actionxx = () => endNode.Ellipse_indicator.Fill = Brushes.DarkOrange;
                Dispatcher.Invoke(actionxx);
            }
        }

        public void UnIdentifyEndNode(Sensor endNode)
        {
            if (Settings.Default.ShowAnimation && endNode.ID != PublicParameters.SinkNode.ID)
            {
                Action actionx = () => endNode.Ellipse_indicator.Visibility = Visibility.Hidden;
                Dispatcher.Invoke(actionx);

                Action actionxx = () => endNode.Ellipse_indicator.Fill = Brushes.Transparent;
                Dispatcher.Invoke(actionxx);
            }
        }

        public void btn_send_packet_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Label lbl_title = sender as Label;
            switch (lbl_title.Name)
            {
                case "btn_send_1_packet":
                    {
                        if (this.ID != PublicParameters.SinkNode.ID)
                        {
                            // uplink:
                            GenerateMultipleDataPackets(1);
                        }
                        else
                        {
                            RandomSelectEndNodes(1);
                        }

                        break;
                    }
                case "btn_send_10_packet":
                    {
                        if (this.ID != PublicParameters.SinkNode.ID)
                        {
                            // uplink:
                            GenerateMultipleDataPackets(10);
                        }
                        else
                        {
                            RandomSelectEndNodes(10);
                        }
                        break;
                    }

                case "btn_send_100_packet":
                    {
                        if (this.ID != PublicParameters.SinkNode.ID)
                        {
                            // uplink:
                            GenerateMultipleDataPackets(100);
                        }
                        else
                        {
                            RandomSelectEndNodes(100);
                        }
                        break;
                    }

                case "btn_send_300_packet":
                    {
                        if (this.ID != PublicParameters.SinkNode.ID)
                        {
                            // uplink:
                            GenerateMultipleDataPackets(300);
                        }
                        else
                        {
                            RandomSelectEndNodes(300);
                        }
                        break;
                    }

                case "btn_send_1000_packet":
                    {
                        if (this.ID != PublicParameters.SinkNode.ID)
                        {
                            // uplink:
                            GenerateMultipleDataPackets(1000);
                        }
                        else
                        {
                            RandomSelectEndNodes(1000);
                        }
                        break;
                    }

                case "btn_send_5000_packet":
                    {
                        if (this.ID != PublicParameters.SinkNode.ID)
                        {
                            // uplink:
                            GenerateMultipleDataPackets(5000);
                        }
                        else
                        {
                            // DOWN
                            RandomSelectEndNodes(5000);
                        }
                        break;
                    }
            }
        }

        private void OpenChanel(int reciverID, long PID)
        {
            Thread thread = new Thread(() =>
            {
                lock (MyArrows)
                {
                    
                    Arrow ar = GetArrow(reciverID);
                    if (ar != null)
                    {
                        lock (ar)
                        {
                            if (ar.Visibility == Visibility.Hidden)
                            {
                                if (Settings.Default.ShowAnimation)
                                {
                                    Action actionx = () => ar.BeginAnimation(PID);
                                    Dispatcher.Invoke(actionx);
                                    Action action1 = () => ar.Visibility = Visibility.Visible;
                                    Dispatcher.Invoke(action1);
                                }
                                else
                                {
                                    Action action1 = () => ar.Visibility = Visibility.Visible;
                                    Dispatcher.Invoke(action1);
                                    Dispatcher.Invoke(() => ar.Stroke = new SolidColorBrush(Colors.Black));
                                    Dispatcher.Invoke(() => ar.StrokeThickness = 1);
                                    Dispatcher.Invoke(() => ar.HeadHeight = 1);
                                    Dispatcher.Invoke(() => ar.HeadWidth = 1);
                                }
                            }
                            else
                            {
                                if (Settings.Default.ShowAnimation)
                                {
                                    int cid = Convert.ToInt16(PID % PublicParameters.RandomColors.Count);
                                    Action actionx = () => ar.BeginAnimation(PID);
                                    Dispatcher.Invoke(actionx);
                                    Dispatcher.Invoke(() => ar.HeadHeight = 1);
                                    Dispatcher.Invoke(() => ar.HeadWidth = 1);
                                }
                                else
                                {
                                    Dispatcher.Invoke(() => ar.Stroke = new SolidColorBrush(Colors.Black));
                                    Dispatcher.Invoke(() => ar.StrokeThickness = 1);
                                    Dispatcher.Invoke(() => ar.HeadHeight = 1);
                                    Dispatcher.Invoke(() => ar.HeadWidth = 1);
                                }
                            }
                        }
                    }
                }
            }
           );
            thread.Name = "OpenChannel thread " + reciverID + "PID:" + PID;
            thread.Start();
            thread.Priority = ThreadPriority.Highest;
        }

        #endregion


        #region send data: /////////////////////////////////////////////////////////////////////////////

        public int maxHopsForDestination(Sensor destination)
        {
            if (destination != null)
            {
                try
                {
                    double DIS = Operations.DistanceBetweenTwoPoints(destination.CenterLocation, this.CenterLocation) * 1.5;
                    return PublicParameters.HopsErrorRange + Convert.ToInt16(Math.Ceiling(((PublicParameters.Density / 2) * (DIS / ComunicationRangeRadius))));
                }
                catch (NullReferenceException e)
                {
                    Console.WriteLine(e.Message + " destination node in max hops is null");
                    return 0;
                }

            }
            else { return 0; }

        }


        //**************Generating Packets and Data Dissemenation

        public void DissemenateData()
        {
            //MessageBox.Show("I am here");
            PublicParameters.NumberOfNodesDissemenating += 1;
            MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_num_of_disseminatingNodes.Content = PublicParameters.NumberOfNodesDissemenating.ToString());
            if (TuftNodeTable.CellHeaderTable.isHeader)
            {
                if (TuftNodeTable.CellHeaderTable.isRootCellHeader)
                {
                    if (TuftNodeTable.CellHeaderTable.hasSinkPosition)
                    {
                        GenerateDataToSink(TuftNodeTable.CellHeaderTable.SinkNode);
                    }
                }
                else
                {
                    if(TuftNodeTable.CellHeaderTable.isRootCellMyNei()){
                        GenerateDataToSink(TuftNodeTable.CellHeaderTable.OuterCycleSinkHeader);
                    }
                    else
                    {
                        GenerateDataToSink(TuftNodeTable.CellHeaderTable.OuterNeighbors[0]);
                    }
                }
            }
            else
            {
                if (TuftNodeTable.isEncapsulated)
                {
                    GenerateDataToSink(TuftNodeTable.myCellHeader);
                }
            }
           
 
        }

        public void GenerateDataToSink(Sensor destination)
        {
           
            Packet packet = new Packet();
            PublicParameters.NumberofGeneratedDataPackets += 1;
            packet.Source = this;
            packet.PacketLength = PublicParameters.RoutingDataLength;
            packet.PacketType = PacketType.Data;
            packet.PID = PublicParameters.OverallGeneratedPackets;
            packet.Path = "" + this.ID;
            packet.Destination = destination;
            packet.TimeToLive = this.maxHopsForDestination(destination);
            IdentifySourceNode(this);
            MainWindow.Dispatcher.Invoke(() => PublicParameters.SinkNode.MainWindow.lbl_num_of_gen_packets.Content = PublicParameters.NumberofGeneratedDataPackets, DispatcherPriority.Normal);
         
            this.sendDataPack(packet);
        }
        

        public void GenerateSinkCellHeaderInfo(Sensor CellHeader)
        {
            Packet ASNewAgent = new Packet();
            PublicParameters.NumberofGeneratedFollowUpPackets += 1;
            ASNewAgent.Source = this;
            ASNewAgent.PacketLength = PublicParameters.ControlDataLength;
            ASNewAgent.PacketType = PacketType.SinkAdv;
            ASNewAgent.PID = PublicParameters.OverallGeneratedPackets;
            ASNewAgent.Path = "" + this.ID;
            ASNewAgent.Destination = CellHeader;
            ASNewAgent.TimeToLive = this.maxHopsForDestination(CellHeader);
            IdentifySourceNode(this);
            //Fix this here
            MainWindow.Dispatcher.Invoke(() => PublicParameters.SinkNode.MainWindow.lbl_num_of_gen_followup.Content = PublicParameters.NumberofGeneratedFollowUpPackets, DispatcherPriority.Normal);
            this.SendSinkCellHeaderInfo(ASNewAgent);  

        }
        public void GenerateHeaderCycleFormation(Sensor CellHeader)
        {
            foreach(Sensor dist in TuftNodeTable.CellHeaderTable.OuterNeighbors)
            {
                PublicParameters.NumberofGeneratedFollowUpPackets += 1;
                Packet pkt = new Packet();
                pkt.Source = this;
                pkt.PacketLength = PublicParameters.ControlDataLength;
                pkt.PID = PublicParameters.OverallGeneratedPackets;
                pkt.PacketType = PacketType.CycleForm;
                pkt.Path = "" + ID;
                pkt.Destination = dist;
                pkt.TimeToLive = maxHopsForDestination(dist);
                IdentifySourceNode(this);
                //Fix this here
                // _ = MainWindow.Dispatcher.Invoke(() => PublicParameters.SinkNode.MainWindow.lbl_num_of_gen_followup.Content =
                //    PublicParameters.NumberofGeneratedFollowUpPackets, DispatcherPriority.Normal);
                // Add the sending method
                SendCycleFormation(pkt);
            }
        }

        public void GenerateCycleSharing(Sensor dist)
        {
            PublicParameters.NumberofGeneratedFollowUpPackets += 1;
            Packet pkt = new Packet();
            pkt.Source = this;
            pkt.PacketLength = PublicParameters.ControlDataLength;
            pkt.PacketType = PacketType.CellShare;
            pkt.PID = PublicParameters.OverallGeneratedPackets;
            pkt.Path = "" + ID;
            pkt.Destination = dist;
            pkt.TimeToLive = maxHopsForDestination(dist);
            IdentifySourceNode(this);
            //Fix this here
           // MainWindow.Dispatcher.Invoke(() => PublicParameters.SinkNode.MainWindow.lbl_num_of_gen_followup.Content =
           // PublicParameters.NumberofGeneratedFollowUpPackets, DispatcherPriority.Normal);
            // Add the sending method
            SendCycleShare(pkt);
        }


        //********************Sending

        public void sendDataPack(Packet pkt)
        {
            lock (TuftFlowTable)
            {
                if(pkt.Destination.ID == ID)
                {
                    Console.WriteLine();
                }

                Sensor Reciver = pkt.Destination;
                if (Operations.isInMyComunicationRange(this, Reciver))
                {
                    if (Reciver.CanRecievePacket && Reciver.CurrentSensorState == SensorState.Active)
                    {
                        ComputeOverhead(pkt, EnergyConsumption.Transmit, Reciver);
                        // Console.WriteLine("sucess:" + ID + "->" + Reciver.ID + ". PID: " + AS.PID);
                        Reciver.RecieveDataPack(pkt);
                    }
                    else
                    {
                        // Console.WriteLine("NID:" + this.ID + " Faild to sent PID:" + AS.PID);
                        WaitingPacketsQueue.Enqueue(pkt);
                        QueuTimer.Start();
                        // Console.WriteLine("NID:" + this.ID + ". Queu Timer is started.");

                        if (Settings.Default.ShowRadar) Myradar.StartRadio();
                        PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Fill = Brushes.DeepSkyBlue);
                        PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Visibility = Visibility.Visible);
                    }
                }
                else
                {
                    LinkRouting.GetD_Distribution(this, pkt);
                    FlowTableEntry FlowEntry = MatchFlow(pkt);
                    if (FlowEntry != null)
                    {
                        Reciver = FlowEntry.NeighborEntry.NeiNode;
                        ComputeOverhead(pkt, EnergyConsumption.Transmit, Reciver);
                        FlowEntry.DownLinkStatistics += 1;
                        Reciver.RecieveDataPack(pkt);

                    }
                    else
                    {
                        WaitingPacketsQueue.Enqueue(pkt);
                        QueuTimer.Start();
                        if (Settings.Default.ShowRadar) Myradar.StartRadio();
                        PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Fill = Brushes.DeepSkyBlue);
                        PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Visibility = Visibility.Visible);
                    }
                }


            }

        }

        public void SendCycleFormation(Packet pkt)
        {
            lock (TuftFlowTable)
            {

                Sensor Reciver = pkt.Destination;
                if (Operations.isInMyComunicationRange(this, Reciver))
                {
                    if (Reciver.CanRecievePacket && Reciver.CurrentSensorState == SensorState.Active)
                    {
                        ComputeOverhead(pkt, EnergyConsumption.Transmit, Reciver);
                        // Console.WriteLine("sucess:" + ID + "->" + Reciver.ID + ". PID: " + AS.PID);
                        Reciver.RecieveCycleFormation(pkt);
                    }
                    else
                    {
                        // Console.WriteLine("NID:" + this.ID + " Faild to sent PID:" + AS.PID);
                        WaitingPacketsQueue.Enqueue(pkt);
                        QueuTimer.Start();
                        // Console.WriteLine("NID:" + this.ID + ". Queu Timer is started.");

                        if (Settings.Default.ShowRadar) Myradar.StartRadio();
                        PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Fill = Brushes.DeepSkyBlue);
                        PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Visibility = Visibility.Visible);
                    }
                }
                else
                {
                    LinkRouting.GetD_Distribution(this, pkt);
                    FlowTableEntry FlowEntry = MatchFlow(pkt);
                    if (FlowEntry != null)
                    {
                        Reciver = FlowEntry.NeighborEntry.NeiNode;
                        ComputeOverhead(pkt, EnergyConsumption.Transmit, Reciver);
                        FlowEntry.DownLinkStatistics += 1;
                        Reciver.RecieveCycleFormation(pkt);

                    }
                    else
                    {
                        WaitingPacketsQueue.Enqueue(pkt);
                        QueuTimer.Start();
                        if (Settings.Default.ShowRadar) Myradar.StartRadio();
                        PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Fill = Brushes.DeepSkyBlue);
                        PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Visibility = Visibility.Visible);
                    }
                }


            }
        }

        public void SendSinkCellHeaderInfo(Packet AS)
        {
            lock (TuftFlowTable) {

                Sensor Reciver = AS.Destination;
                if (Operations.isInMyComunicationRange(this, Reciver))
                {
                    if (Reciver.CanRecievePacket && Reciver.CurrentSensorState == SensorState.Active)
                    {
                        ComputeOverhead(AS, EnergyConsumption.Transmit, Reciver);
                        // Console.WriteLine("sucess:" + ID + "->" + Reciver.ID + ". PID: " + AS.PID);
                        Reciver.RecieveSinkCellHeaderInfo(AS);
                    }
                    else
                    {
                        // Console.WriteLine("NID:" + this.ID + " Faild to sent PID:" + AS.PID);
                        WaitingPacketsQueue.Enqueue(AS);
                        QueuTimer.Start();
                        // Console.WriteLine("NID:" + this.ID + ". Queu Timer is started.");

                        if (Settings.Default.ShowRadar) Myradar.StartRadio();
                        PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Fill = Brushes.DeepSkyBlue);
                        PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Visibility = Visibility.Visible);
                    }
                }
                else
                {
                    LinkRouting.GetD_Distribution(this, AS);
                    FlowTableEntry FlowEntry = MatchFlow(AS);
                    if (FlowEntry != null)
                    {
                        Reciver = FlowEntry.NeighborEntry.NeiNode;
                        ComputeOverhead(AS, EnergyConsumption.Transmit, Reciver);
                        FlowEntry.DownLinkStatistics += 1;
                        Reciver.RecieveSinkCellHeaderInfo(AS);

                    }
                    else
                    {
                        WaitingPacketsQueue.Enqueue(AS);
                        QueuTimer.Start();
                        if (Settings.Default.ShowRadar) Myradar.StartRadio();
                        PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Fill = Brushes.DeepSkyBlue);
                        PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Visibility = Visibility.Visible);
                    }
                }


            }
           
        }


        public void SendCycleShare(Packet pkt)
        {
            lock (TuftFlowTable)
            {

                Sensor Reciver = pkt.Destination;
                if (Operations.isInMyComunicationRange(this, Reciver)) {
                    if (Reciver.CanRecievePacket && Reciver.CurrentSensorState == SensorState.Active)
                    {
                        ComputeOverhead(pkt, EnergyConsumption.Transmit, Reciver);
                        // Console.WriteLine("sucess:" + ID + "->" + Reciver.ID + ". PID: " + AS.PID);
                        Reciver.RecieveCycleShare(pkt);
                    }
                    else
                    {
                        // Console.WriteLine("NID:" + this.ID + " Faild to sent PID:" + AS.PID);
                        WaitingPacketsQueue.Enqueue(pkt);
                        QueuTimer.Start();
                        // Console.WriteLine("NID:" + this.ID + ". Queu Timer is started.");

                        if (Settings.Default.ShowRadar) Myradar.StartRadio();
                        PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Fill = Brushes.DeepSkyBlue);
                        PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Visibility = Visibility.Visible);
                    }
                }
                else
                {
                    LinkRouting.GetD_Distribution(this, pkt);
                    FlowTableEntry FlowEntry = MatchFlow(pkt);
                    if (FlowEntry != null)
                    {
                        Reciver = FlowEntry.NeighborEntry.NeiNode;
                        ComputeOverhead(pkt, EnergyConsumption.Transmit, Reciver);
                        FlowEntry.DownLinkStatistics += 1;
                        Reciver.RecieveCycleShare(pkt);

                    }
                    else
                    {
                        WaitingPacketsQueue.Enqueue(pkt);
                        QueuTimer.Start();
                        if (Settings.Default.ShowRadar) Myradar.StartRadio();
                        PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Fill = Brushes.DeepSkyBlue);
                        PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Visibility = Visibility.Visible);
                    }
                }

            }

        }


  

        //*******************Recieving 

    
        //Agent Selection
        public void RecieveSinkCellHeaderInfo(Packet AS)
        {
            AS.ReTransmissionTry = 0;
            if (this.CanRecievePacket)
            {
                AS.Path += ">" + this.ID;
                if (this.ID == AS.Destination.ID) {
                        //recieve by new agent 
                    AS.isDelivered = true;
                    updateStates(AS);
                    this.TuftNodeTable.CellHeaderTable.hasSinkPosition = true;
                    this.TuftNodeTable.CellHeaderTable.SinkNode = AS.Source;
                    //Need to inform other Cell Headers of this change
                    GenerateHeaderCycleFormation(this);
                }
                else
                {
                    if (AS.Hops > AS.TimeToLive)
                    {
                        // drop the paket.
                        AS.isDelivered = false;
                        AS.DroppedReason = "Hops > Time to live ";
                        updateStates(AS);
                    }
                    else
                    {
                        SendSinkCellHeaderInfo(AS);
                    }
                }
                
            }
            else
            {
                AS.isDelivered = false;
                AS.DroppedReason = "Node " + this.ID + " can't recieve packet";
                updateStates(AS);
            }
           
        }

        public void RecieveCycleShare(Packet pkt)
        {
            //Check if I have outer neighbors if i do share 
            pkt.ReTransmissionTry = 0;
            if (this.CanRecievePacket)
            {
                pkt.Path += ">" + this.ID;
                if (this.ID == pkt.Destination.ID)
                {
                    //recieve by new agent 
                    pkt.isDelivered = true;
                    updateStates(pkt);
                   
                }
                else
                {
                    if (pkt.Hops > pkt.TimeToLive)
                    {
                        // drop the paket.
                        pkt.isDelivered = false;
                        pkt.DroppedReason = "Hops > Time to live ";
                        updateStates(pkt);
                    }
                    else
                    {
                        SendCycleShare(pkt);
                    }
                }

            }
            else
            {
                pkt.isDelivered = false;
                pkt.DroppedReason = "Node " + this.ID + " can't recieve packet";
                updateStates(pkt);
            }
        }
        #endregion
        public void RecieveCycleFormation(Packet pkt)
        {
            pkt.ReTransmissionTry = 0;
            if (this.CanRecievePacket)
            {
                pkt.Path += ">" + this.ID;
                if (this.ID == pkt.Destination.ID)
                {
                    //recieve by new agent 
                    pkt.isDelivered = true;
                    updateStates(pkt);
                    List<int> packetPaths = Operations.PacketPathToIDS(pkt.Path);
                    foreach (Sensor neiCellHeader in TuftNodeTable.CellHeaderTable.OuterNeighbors)
                    {
                        if (!packetPaths.Contains(neiCellHeader.ID))
                        {
                            GenerateCycleSharing(neiCellHeader);
                        }
                    }

                }
                else
                {
                    if (pkt.Hops > pkt.TimeToLive)
                    {
                        // drop the paket.
                        pkt.isDelivered = false;
                        pkt.DroppedReason = "Hops > Time to live ";
                        updateStates(pkt);
                    }
                    else
                    {
                        SendCycleFormation(pkt);
                    }
                }

            }
            else
            {
                pkt.isDelivered = false;
                pkt.DroppedReason = "Node " + this.ID + " can't recieve packet";
                updateStates(pkt);
            }
        }

        public int counter = 0;
        public void RecieveDataPack(Packet pkt)
        {
          
            pkt.ReTransmissionTry = 0;
            if (this.CanRecievePacket)
            {
                pkt.Path += ">" + this.ID;
                if (ID == PublicParameters.SinkNode.ID)
                {
                    pkt.isDelivered = true;
                    updateStates(pkt);
                    return;
                }


                if (this.ID == pkt.Destination.ID)
                {
                    if (TuftNodeTable.CellHeaderTable.isHeader)
                    {
                        if (TuftNodeTable.CellHeaderTable.isRootCellHeader)
                        {
                            if (TuftNodeTable.CellHeaderTable.hasSinkPosition && TuftNodeTable.CellHeaderTable.SinkNode != null)
                            {
                                pkt.Destination = PublicParameters.SinkNode;
                                sendDataPack(pkt);
                            }
                            else
                            {
                                //Drop the packet sink is not contained 
                                pkt.isDelivered = false;
                                pkt.DroppedReason = "Root Header doesn't have sink position";
                                updateStates(pkt);
                                return;
                            }
                        }

                        else
                        {

                            //Give to one of my neighbors if root cell is not my neighbor
                            if (TuftNodeTable.CellHeaderTable.isRootCellMyNei())
                            {
                                pkt.Destination = TuftNodeTable.CellHeaderTable.OuterCycleSinkHeader;
                            }
                            else
                            {
                                Sensor dest = TuftNodeTable.CellHeaderTable.getNeiCellHeader(pkt);
                                if (dest != null)
                                {
                                    pkt.Destination = dest;
                                }
                                else
                                {
                                    pkt.isDelivered = false;
                                    pkt.DroppedReason = "Problem returning CellHeaderNei";
                                    updateStates(pkt);
                                    return;
                                }
    
                            }
                            pkt.TimeToLive += maxHopsForDestination(pkt.Destination);
                            sendDataPack(pkt);
                        }

                        
                    }
                    else
                    {
                        pkt.Destination = TuftNodeTable.myCellHeader;
                        sendDataPack(pkt);
                    }
                }
                else
                {
                    if (pkt.Hops > pkt.TimeToLive)
                    {
                        // drop the paket.
                        pkt.isDelivered = false;
                        pkt.DroppedReason = "Hops > Time to live ";
                        updateStates(pkt);
                    }
                    else
                    {
                        sendDataPack(pkt);
                    }
                }

            }
            else
            {
                pkt.isDelivered = false;
                pkt.DroppedReason = "Node " + this.ID + " can't recieve packet";
                updateStates(pkt);
            }
        }
     

   


        public static long PIDE = -1;


        public void updateStates(Packet packet)
        {
            if (packet.isDelivered)
            {
               
                if (packet.PacketType == PacketType.Data) { 
                    PublicParameters.NumberOfDelieveredDataPackets += 1;
                    packet.ComputeDelay();
                    PublicParameters.DataDelay += packet.Delay;    
                }
                else
                {
                  PublicParameters.NumberofDelieveredFollowUpPackets += 1;
                }

                PublicParameters.NumberofDeliveredPackets += 1;
               // Console.WriteLine("{2} Packet: {0} with Path: {1} delievered",packet.PID,packet.Path,packet.PacketType);
                PublicParameters.FinishedRoutedPackets.Add(packet);
                ComputeOverhead(packet, EnergyConsumption.Recive, null);
                MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_total_consumed_energy.Content = PublicParameters.TotalEnergyConsumptionJoule + " (JOULS)", DispatcherPriority.Send);

                MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_Number_of_Delivered_QPacket.Content = PublicParameters.NumberOfDelieveredQueryPackets, DispatcherPriority.Send);
                MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_Number_of_Delivered_CPacket.Content = PublicParameters.NumberofDelieveredFollowUpPackets, DispatcherPriority.Send);
                MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_Number_of_Delivered_Packet.Content = PublicParameters.NumberOfDelieveredDataPackets, DispatcherPriority.Send);

                MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_sucess_ratio.Content = PublicParameters.DeliveredRatio, DispatcherPriority.Send);
                MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_nymber_inQueu.Content = PublicParameters.InQueuePackets.ToString());
                MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_num_of_disseminatingNodes.Content = PublicParameters.NumberOfNodesDissemenating.ToString());
                MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_Average_QDelay.Content = PublicParameters.AverageQueryDelay.ToString());
                MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_Total_Delay.Content = PublicParameters.AverageTotalDelay.ToString());

                UnIdentifySourceNode(packet.Source);
                // Console.WriteLine("PID:" + packet.PID + " has been delivered.");
            }
            else
            {
               
                Console.WriteLine("Failed {2} PID: {0} Reason: {1}", packet.PID, packet.DroppedReason,packet.PacketType);
                PublicParameters.NumberofDropedPackets += 1;
                PublicParameters.FinishedRoutedPackets.Add(packet);
                //  Console.WriteLine("PID:" + packet.PID + " has been droped.");

                MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_Number_of_Droped_Packet.Content = PublicParameters.NumberofDropedPackets, DispatcherPriority.Send);
            }
        }

        private void DeliveerPacketsInQueuTimer_Tick(object sender, EventArgs e)
        {
            if(WaitingPacketsQueue.Count > 0)
            {
                Packet toppacket = WaitingPacketsQueue.Dequeue();
                toppacket.WaitingTimes += 1;
                toppacket.ReTransmissionTry += 1;
                PublicParameters.TotalWaitingTime += 1; // total;
                if (toppacket.ReTransmissionTry < 7)
                {
                    if (toppacket.PacketType == PacketType.Data)
                    {
                        sendDataPack(toppacket);
                    }
                    else if (toppacket.PacketType == PacketType.SinkAdv)
                    {
                        SendSinkCellHeaderInfo(toppacket);
                    }
                    else if (toppacket.PacketType == PacketType.CycleForm)
                    {
                        SendCycleFormation(toppacket);
                        //s/endFSA(toppacket);
                    }
                    else if (toppacket.PacketType == PacketType.CellShare)
                    {
                        SendCycleShare(toppacket);
                    }
                    else
                    {
                        MessageBox.Show("Unknown");
                    }
                }
                else
                {
                    // PublicParameters.NumberofDropedPackets += 1;
                    toppacket.isDelivered = false;
                    toppacket.DroppedReason = "Waiting times > 7";
                    updateStates(toppacket);
                    //  Console.WriteLine("Waiting times more for packet {0}", toppacket.PID);
                    // PublicParameters.FinishedRoutedPackets.Add(toppacket);
                    //    MessageBox.Show("PID:" + toppacket.PID + " has been droped. Packet Type = "+toppacket.PacketType);
                    MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_Number_of_Droped_Packet.Content = PublicParameters.NumberofDropedPackets, DispatcherPriority.Send);
                }
                if (WaitingPacketsQueue.Count == 0)
                {
                    if (Settings.Default.ShowRadar) Myradar.StopRadio();
                    QueuTimer.Stop();
                    // Console.WriteLine("NID:" + this.ID + ". Queu Timer is stoped.");
                    MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Fill = Brushes.Transparent);
                    MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Visibility = Visibility.Hidden);
                }
                MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_nymber_inQueu.Content = PublicParameters.InQueuePackets.ToString());
            }
            else
            {
                if (Settings.Default.ShowRadar) Myradar.StopRadio();
                QueuTimer.Stop();
                // Console.WriteLine("NID:" + this.ID + ". Queu Timer is stoped.");
                MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Fill = Brushes.Transparent);
                MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Visibility = Visibility.Hidden);
            }
            
            
        }



        private void RemoveOldAgentTimer_Tick(object sender, EventArgs e) {
            OldAgentTimer.Stop();
            this.AgentNode = new Agent();
        }

               
        public static int CountRedun =0;
        public void RedundantTransmisionCost(Packet pacekt, Sensor reciverNode)
        {
            // logs.
            PublicParameters.TotalReduntantTransmission += 1;       
            double UsedEnergy_Nanojoule = EnergyModel.Receive(PublicParameters.PreamblePacketLength); // preamble packet length.
            double UsedEnergy_joule = ConvertToJoule(UsedEnergy_Nanojoule);
            reciverNode.ResidualEnergy = reciverNode.ResidualEnergy - UsedEnergy_joule;
            pacekt.UsedEnergy_Joule += UsedEnergy_joule;
            PublicParameters.TotalEnergyConsumptionJoule += UsedEnergy_joule;
            PublicParameters.TotalWastedEnergyJoule += UsedEnergy_joule;
            MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_Redundant_packets.Content = PublicParameters.TotalReduntantTransmission);
            MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_Wasted_Energy_percentage.Content = PublicParameters.WastedEnergyPercentage);
        }

        /// <summary>
        /// the node which is active will send preample packet and will be selected.
        /// match the packet.
        /// </summary>
        public FlowTableEntry MatchFlow(Packet pacekt)
        {

            FlowTableEntry ret = null;
            try
            {
               
                if (TuftFlowTable.Count > 0)
                {
                  
                    foreach (FlowTableEntry selectedflow in TuftFlowTable)
                    {
                        if (selectedflow.NID != PublicParameters.SinkNode.ID)
                        {
                            if (selectedflow.SensorState == SensorState.Active && selectedflow.DownLinkAction == FlowAction.Forward && selectedflow.SensorBufferHasSpace)
                            {
                                if (ret == null)
                                {
                                    ret = selectedflow;
                                }
                                else
                                {
                                    RedundantTransmisionCost(pacekt, selectedflow.NeighborEntry.NeiNode);
                                }
                            }
                        }
                        
                    }
                }
                else
                {
                    MessageBox.Show("No Flow!!!. muach flow!");
                    return null;
                }
            }
            catch
            {
                ret = null;
                MessageBox.Show(" Null Match.!");
            }

            return ret;
        }

        // When the sensor open the channel to transmit the data.
  
        
        




        public void ComputeOverhead(Packet packt, EnergyConsumption enCon, Sensor Reciver)
        {
            if (enCon == EnergyConsumption.Transmit)
            {
                if (ID != PublicParameters.SinkNode.ID)
                {
                    // calculate the energy 
                    double Distance_M = Operations.DistanceBetweenTwoSensors(this, Reciver);
                    double UsedEnergy_Nanojoule = EnergyModel.Transmit(packt.PacketLength, Distance_M);
                    double UsedEnergy_joule = ConvertToJoule(UsedEnergy_Nanojoule);
                    ResidualEnergy = this.ResidualEnergy - UsedEnergy_joule;
                    PublicParameters.TotalEnergyConsumptionJoule += UsedEnergy_joule;
                    packt.UsedEnergy_Joule += UsedEnergy_joule;
                    packt.RoutingDistance += Distance_M;
                    packt.Hops += 1;
                    double delay = DelayModel.DelayModel.Delay(this, Reciver);
                    packt.Delay += delay;
                    PublicParameters.TotalDelayMs += delay;
                    

                    // for control packet.
                    if (packt.isAdvirtismentPacket())
                    {
                        // just to remember how much energy is consumed here.
                        PublicParameters.EnergyComsumedForControlPackets += UsedEnergy_joule;
                    }
                }

                if (Settings.Default.ShowRoutingPaths)
                {
                    OpenChanel(Reciver.ID, packt.PID);
                }

            }
            else if (enCon == EnergyConsumption.Recive)
            {

                double UsedEnergy_Nanojoule = EnergyModel.Receive(packt.PacketLength);
                double UsedEnergy_joule = ConvertToJoule(UsedEnergy_Nanojoule);
                ResidualEnergy = ResidualEnergy - UsedEnergy_joule;
                packt.UsedEnergy_Joule += UsedEnergy_joule;
                PublicParameters.TotalEnergyConsumptionJoule += UsedEnergy_joule;


                if (packt.isAdvirtismentPacket())
                {
                    // just to remember how much energy is consumed here.
                    PublicParameters.EnergyComsumedForControlPackets += UsedEnergy_joule;
                }


            }

        }

     






        private void lbl_MouseEnter(object sender, MouseEventArgs e)
        {
            ToolTip = new Label() { Content = "("+ID + ") [ " + ResidualEnergyPercentage + "% ] [ " + ResidualEnergy + " J ]" };
        }

        private void btn_show_routing_log_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(Logs.Count>0)
            {
                UiShowRelativityForAnode re = new ui.UiShowRelativityForAnode();
                re.dg_relative_shortlist.ItemsSource = Logs;
                re.Show();
            }
        }

        private void btn_draw_random_numbers_MouseDown(object sender, MouseButtonEventArgs e)
        {
            List<KeyValuePair<int, double>> rands = new List<KeyValuePair<int, double>>();
            int index = 0;
            foreach (RoutingLog log in Logs )
            {
                if(log.IsSend)
                {
                    index++;
                    rands.Add(new KeyValuePair<int, double>(index, log.ForwardingRandomNumber));
                }
            }
            UiRandomNumberGeneration wndsow = new ui.UiRandomNumberGeneration();
            wndsow.chart_x.DataContext = rands;
            wndsow.Show();
        }

        private void Ellipse_center_MouseEnter(object sender, MouseEventArgs e)
        {
            
        }

        private void btn_show_my_duytcycling_MouseDown(object sender, MouseButtonEventArgs e)
        {
           
        }

        private void btn_draw_paths_MouseDown(object sender, MouseButtonEventArgs e)
        {
            NetworkVisualization.UpLinksDrawPaths(this);
        }

       
         
        private void btn_show_my_flows_MouseDown(object sender, MouseButtonEventArgs e)
        {
           
            ListControl ConMini = new ui.conts.ListControl();
            ConMini.lbl_title.Content = "Mini-Flow-Table";
            ConMini.dg_date.ItemsSource = TuftFlowTable;


            ListControl ConNei = new ui.conts.ListControl();
            ConNei.lbl_title.Content = "Neighbors-Table";
            ConNei.dg_date.ItemsSource = NeighborsTable;

            UiShowLists win = new UiShowLists();
            win.stack_items.Children.Add(ConMini);
            win.stack_items.Children.Add(ConNei);
            win.Title = "Tables of Node " + ID;
            win.Show();
            win.WindowState = WindowState.Maximized;
        }

        private void btn_send_1_p_each1sec_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SendPacketTimer.Start();
            SendPacketTimer.Tick += SendPacketTimer_Random; // redfine th trigger.
        }



        public void RandomSelectEndNodes(int numOFpACKETS)
        {
            if (PublicParameters.SimulationTime > PublicParameters.MacStartUp)
            {
                int index = 1 + Convert.ToInt16(UnformRandomNumberGenerator.GetUniform(PublicParameters.NumberofNodes - 2));
                if (index != PublicParameters.SinkNode.ID)
                {
                    Sensor endNode = MainWindow.myNetWork[index];
                    GenerateMultipleControlPackets(numOFpACKETS, endNode);
                }
            }
        }

        private void SendPacketTimer_Random(object sender, EventArgs e)
        {
            if (ID != PublicParameters.SinkNode.ID)
            {
                // uplink:
                GenerateMultipleDataPackets(1);
            }
            else
            { //
                RandomSelectEndNodes(1);
            }
        }

        /// <summary>
        /// i am slected as end node.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Btn_select_me_as_end_node_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Label lbl_title = sender as Label;
            switch (lbl_title.Name)
            {
                case "Btn_select_me_as_end_node_1":
                    {
                       PublicParameters.SinkNode.GenerateMultipleControlPackets(1, this);

                        break;
                    }
                case "Btn_select_me_as_end_node_10":
                    {
                        PublicParameters.SinkNode.GenerateMultipleControlPackets(10, this);
                        break;
                    }
                //Btn_select_me_as_end_node_1_5sec

                case "Btn_select_me_as_end_node_1_5sec":
                    {
                        PublicParameters.SinkNode.SendPacketTimer.Start();
                        PublicParameters.SinkNode.SendPacketTimer.Tick += SelectMeAsEndNodeAndSendonepacketPer5s_Tick;
                        break;
                    }
            }
        }

        
        
        public void SelectMeAsEndNodeAndSendonepacketPer5s_Tick(object sender, EventArgs e)
        {
            PublicParameters.SinkNode.GenerateMultipleControlPackets(1, this);
        }





        /*** Vistualize****/

        public void ShowID(bool isVis )
        {
            if (isVis) { lbl_Sensing_ID.Visibility = Visibility.Visible; lbl_hops_to_sink.Visibility = Visibility.Visible; }
            else { lbl_Sensing_ID.Visibility = Visibility.Hidden; lbl_hops_to_sink.Visibility = Visibility.Hidden; }
        }

        public void ShowSensingRange(bool isVis)
        {
            if (isVis) Ellipse_Sensing_range.Visibility = Visibility.Visible;
            else Ellipse_Sensing_range.Visibility = Visibility.Hidden;
        }

        public void ShowComunicationRange(bool isVis)
        {
            if (isVis) Ellipse_Communication_range.Visibility = Visibility.Visible;
            else Ellipse_Communication_range.Visibility = Visibility.Hidden;
        }

        public void ShowBattery(bool isVis) 
        {
            if (isVis) Prog_batteryCapacityNotation.Visibility = Visibility.Visible;
            else Prog_batteryCapacityNotation.Visibility = Visibility.Hidden;
        }

        private void btn_update_mini_flow_MouseDown(object sender, MouseButtonEventArgs e)
        {
          
        }
    }
}
