using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MiniSimconnect
{
    public enum DUMMYENUM
    {
        Dummy = 0
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Contains the list of all the SimConnect properties we will read, the unit is separated by coma by our own code.
        /// </summary>
        Dictionary<int, string> simConnectProperties = new Dictionary<int, string>
        {
            {1,"PLANE LONGITUDE,degree" },
            {2,"PLANE LATITUDE,degree" },
            {3,"PLANE HEADING DEGREES MAGNETIC,degree" },
            {4,"PLANE ALTITUDE,feet" },
            {5,"AIRSPEED INDICATED,knots" },
        };

        /// User-defined win32 event => put basically any number?
        public const int WM_USER_SIMCONNECT = 0x0402;

        SimConnect sim;

        /// <summary>
        ///  Direct reference to the window pointer
        /// </summary>
        /// <returns></returns>
        protected HwndSource GetHWinSource()
        {
            return PresentationSource.FromVisual(this) as HwndSource;
        }
        
        /// <summary>
        /// Returns a label based on a uid number
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        private Label GetLabelForUid(int uid)
        {
            return (Label)mainGrid.Children
                 .Cast<UIElement>()
                 .First(row => row.Uid == uid.ToString());
        }

        public MainWindow()
        {
            InitializeComponent();

            // Starts our connection and poller
            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick; ;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (sim == null) // We are not connected, let's try to connect
                Connect();
            else // We are connected, let's try to grab the data from the Sim
            {
                try
                {
                    foreach (var toConnect in simConnectProperties)
                        sim.RequestDataOnSimObjectType((DUMMYENUM)toConnect.Key, (DUMMYENUM)toConnect.Key, 0, SIMCONNECT_SIMOBJECT_TYPE.USER);
                }
                catch
                {
                    Disconnect();
                }
            }
        }

        /// <summary>
        /// We received a disconnection from SimConnect
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="data"></param>
        private void Sim_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            lblStatus.Content = "Disconnected";
        }

        /// <summary>
        /// We received a connection from SimConnect.
        /// Let's register all the properties we need.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="data"></param>
        private void Sim_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            lblStatus.Content = "Connected";

            foreach (var toConnect in simConnectProperties)
            {
                var values = toConnect.Value.Split(new char[] { ',' });
                /// Define a data structure
                sim.AddToDataDefinition((DUMMYENUM)toConnect.Key, values[0], values[1], SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                GetLabelForUid(100 + toConnect.Key).Content = values[1];
                /// IMPORTANT: Register it with the simconnect managed wrapper marshaller
                /// If you skip this step, you will only receive a uint in the .dwData field.
                sim.RegisterDataDefineStruct<double>((DUMMYENUM)toConnect.Key);
            }
        }

        /// <summary>
        /// Try to connect to the Sim, and in case of success register the hooks
        /// </summary>
        private void Connect()
        {
            /// The constructor is similar to SimConnect_Open in the native API
            try
            {
                // Pass the self defined ID which will be returned on WndProc
                sim = new SimConnect(this.Title, GetHWinSource().Handle, WM_USER_SIMCONNECT, null, 0);
                sim.OnRecvOpen += Sim_OnRecvOpen;
                sim.OnRecvQuit += Sim_OnRecvQuit;
                sim.OnRecvSimobjectDataBytype += Sim_OnRecvSimobjectDataBytype;
            }
            catch
            {
                sim = null;
            }
        }

        /// <summary>
        /// Received data from SimConnect
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="data"></param>
        private void Sim_OnRecvSimobjectDataBytype(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA_BYTYPE data)
        {
            int iRequest = (int)data.dwRequestID;
            double dValue = (double)data.dwData[0];

            GetLabelForUid(iRequest).Content = dValue.ToString();
        }

        public void ReceiveSimConnectMessage()
        {
            sim?.ReceiveMessage();
        }

        /// <summary>
        /// Let's disconnect from SimConnect
        /// </summary>
        public void Disconnect()
        {
            if (sim != null)
            {
                sim.Dispose();
                sim = null;
                lblStatus.Content = "Disconnected";
            }
        }

        /// <summary>
        /// Handles Windows events directly, for example to grab the SimConnect connection
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="iMsg"></param>
        /// <param name="hWParam"></param>
        /// <param name="hLParam"></param>
        /// <param name="bHandled"></param>
        /// <returns></returns>
        private IntPtr WndProc(IntPtr hWnd, int iMsg, IntPtr hWParam, IntPtr hLParam, ref bool bHandled)
        {
            try
            {
                if (iMsg == WM_USER_SIMCONNECT)
                    ReceiveSimConnectMessage();
            }
            catch
            {
                Disconnect();
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Once the window is loaded, let's hook to the WinProc
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var windowsSource = GetHWinSource();
            windowsSource.AddHook(WndProc);

            Connect();
        }

        /// <summary>
        /// Called while the window is closed, dispose SimConnect
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            Disconnect();
        }
    }
}
