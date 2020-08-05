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
using System.Windows.Markup;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Threading;
using System.Windows.Threading;

namespace HMX_Thermal_V3
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        String processObject = "";
        int measuringState = 0;
        int moveAwayCounter = 0;
        int feverCounter = 0;
        string state = "S0";
        float calibration = 0;
        int mDistance = 0;
        float mTemp = 0;

        DispatcherTimer timer = new DispatcherTimer();
        public MainWindow()
        {
            InitializeComponent();
            SelPort();
            navigateFrame("F0");
            
            btnDisconnect.IsEnabled = false;
            gridHelp.Visibility = Visibility.Hidden;
        }

        System.IO.Ports.SerialPort serialPort = null;
        public class ComPort
        {
            public string DeviceID { get; set; }
            public string Description { get; set; }
        }

        /*----------------------------
         * Emurate serial-ports
         *---------------------------*/
        private void SelPort()
        {
            // Get serial-port name
            string[] PortList = SerialPort.GetPortNames();
            var MyList = new ObservableCollection<ComPort>();
            foreach (string p in PortList)
            {
                System.Console.WriteLine(p);
                MyList.Add(new ComPort { DeviceID = p, Description = p });
            }
            cmb.ItemsSource = MyList;
            cmb.SelectedValuePath = "DeviceID";
            cmb.DisplayMemberPath = "Description";
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            if (gridHelp.Visibility == Visibility.Visible)
            {
                gridHelp.Visibility = Visibility.Hidden;
            }
            else
            {
                gridHelp.Visibility = Visibility.Visible;
            }
        }

        private void SerialPort_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            // Check connected or disconnected
            if (serialPort == null) return;
            if (serialPort.IsOpen == false) return;
            receiveText.Dispatcher.Invoke(
                    new Action(() =>
                    {
                        receiveText.Clear();
                    })
                );

            // Show in the textbox
            try
            {
                receiveText.Dispatcher.Invoke(
                    new Action(() =>
                    {
                        while (serialPort.BytesToRead > 0)
                        {
                            string inbound = serialPort.ReadLine();
                            //MessageBox.Show(inbound);
                            receiveText.Text += inbound;
                        }
                        if (state == "S0")
                        {
                            //receiveText.Clear();
                        }
                        else if (receiveText.Text.Length > 8 && receiveText.Text.Length < 14)
                        {
                            processReadings(receiveText.Text);
                            //receiveText.Clear();
                        }
                    })

                );
            }
            catch
            {
                receiveText.Dispatcher.Invoke(
                    new Action(() =>
                    {
                        receiveText.Text = "!Error! cannot connect to " + serialPort.PortName;
                    })
                );
            }
            serialPort.DiscardInBuffer();
        }

        private void processReadings(string readings)
        {
            if (processObject != readings.Trim())
            {
                processObject = readings.Trim();
                if (processObject.Length > 8)
                {
                    processObject = processObject.Substring(processObject.IndexOf('(') + 1, processObject.IndexOf(')') - 1);
                    string[] x = processObject.Split(',');
                    float temp = float.Parse(x[0].Substring(0,x[0].Length - 1));
                    int dist = Int16.Parse(x[1]);
                    temp += calibration; //add calibration variable

                    if (state == "S0") // Settings Page
                    {

                    }
                    if (state == "S1") //Stand in yellow box page
                    {
                        if (dist < 100)
                        {
                            mDistance = dist;
                            mTemp = temp;
                            setFrame("F2");
                            //navigateFrame("F2");
                            //determineFrame(temp, dist);
                        }
                    }
                    if (state == "S2")
                    {
                        mDistance = dist;
                        mTemp = temp;
                        // check fever
                        if (temp > 35.9 && temp < 37.5)
                        {
                            setFrame("F3");
                        }
                        else if (temp >= 38)
                        {
                            setFrame("F4");
                        }
                    }
                    //determineFrame(temp, dist);
                }
            }
        }

        private void playTone()
        {
            MediaPlayer myplayer = new MediaPlayer();
            var uri = new Uri("success.mp3", UriKind.Relative);
            myplayer.Open(uri);
            myplayer.Play();
        }
        private void playAlarm()
        {
            MediaPlayer myplayer = new MediaPlayer();
            var uri = new Uri("alert.mp3", UriKind.Relative);
            myplayer.Open(uri);
            myplayer.Play();
        }

        private void setFrame(string f)
        {
            if (f == "F0")
            {
                state = "S0";
                Frame0.Visibility = Visibility.Visible;
                Frame1.Visibility = Visibility.Hidden;
                Frame2.Visibility = Visibility.Hidden;
                Frame3.Visibility = Visibility.Hidden;
                Frame4.Visibility = Visibility.Hidden;
            }
            else if (f == "F1")
            {
                state = "S1";
                //SendToESP(state);
                Frame1.Visibility = Visibility.Visible;
                Frame2.Visibility = Visibility.Hidden;
                Frame3.Visibility = Visibility.Hidden;
                Frame4.Visibility = Visibility.Hidden;
            }
            else if (f == "F2")
            {
                state = "S2";
                f2txtBlkTop.Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 185, 7, 7));
                f2txtBlkMid.Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 185, 7, 7));
                f2lblDistance.Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 127, 127, 127));
                f2lblDistance.Content = "Distance: " + mDistance.ToString() + "cm.";
                Frame1.Visibility = Visibility.Hidden;
                Frame2.Visibility = Visibility.Visible;
                Frame3.Visibility = Visibility.Hidden;
                Frame4.Visibility = Visibility.Hidden;
            }
            else if (f == "F3")
            {
                state = "S3";
                playTone();
                //Set Temp Value for Frame 3
                f3lblTemp.Content = "Temperature: " + mTemp.ToString() + "°C";
                restartFrame();
                feverCounter = 0;
                Frame1.Visibility = Visibility.Hidden;
                Frame2.Visibility = Visibility.Hidden;
                Frame3.Visibility = Visibility.Visible;
                Frame4.Visibility = Visibility.Hidden;
            }
            else if (f == "F4")
            {
                state = "S4";
                playAlarm();
                if (feverCounter == 0)
                {
                    feverCounter++;
                    f4lblTemp.Content = "Temperature: " + mTemp.ToString() + "°C";
                    f4rtbFever.Visibility = Visibility.Collapsed;
                    f4rtbWarning.Visibility = Visibility.Visible;
                    f4lblTemp2.Visibility = Visibility.Collapsed;
                    restartFrame();

                }
                else
                {
                    f4lblTemp2.Content = "Temperature: " + mTemp.ToString() + "°C";
                    f4rtbFever.Visibility = Visibility.Visible;
                    f4rtbWarning.Visibility = Visibility.Collapsed;
                    f4lblTemp2.Visibility = Visibility.Visible;
                    restartFrame();
                    feverCounter = 0;
                }
                Frame1.Visibility = Visibility.Hidden;
                Frame2.Visibility = Visibility.Hidden;
                Frame3.Visibility = Visibility.Hidden;
                Frame4.Visibility = Visibility.Visible;
            }
            else
            {

                navigateFrame("F1");
            }
        }

        private void determineFrame(float temperature, int distance) // Determine which frame to call
        {
            // If no users
            if (distance > 90)
            {
                if (moveAwayCounter == 3)
                {
                    if (state == "S1")
                    {
                        navigateFrame("F1");
                    }

                    moveAwayCounter = 0;
                }
                else
                {
                    if (state == "S1")
                    {
                        moveAwayCounter++;
                    }
                }

            }
            else 
            {
                if (Frame1.Visibility == Visibility.Visible) //if person is standing in front of counter during idle, ask to move closer
                {
                    navigateFrame("F2");
                }
                /*
                if (measuringState == 0)
                {
                    //if not meaasuring
                    navigateFrame("F2");
                }*/
                if (temperature < 36) //if too far
                {
                    f2txtBlkTop.Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 185, 7, 7));
                    f2txtBlkMid.Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 185, 7, 7));
                    f2lblDistance.Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 127, 127, 127));
                    f2lblDistance.Content = "Distance: " + distance.ToString() + "cm.";
                }
                else // if temp is within range
                {
                    f2txtBlkTop.Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0, 0, 205));
                    f2txtBlkMid.Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0, 0, 205));
                    f2lblDistance.Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0, 176, 80));
                    f2lblDistance.Content = "Distance: " + distance.ToString() + "cm.";

                    if (measuringState == 0)
                    {
                        //enter measuring state
                        measuringState++;
                    }
                    else
                    { //in measuring state
                        state = "S0";
                        //SendToESP(state);
                        if (temperature >= 38)
                        {
                            //Open Frame 4
                            navigateFrame("F4");
                            if (feverCounter == 0)
                            {
                                feverCounter++;
                                f4lblTemp.Content = "Temperature: " + temperature.ToString() + "°C";
                                f4rtbFever.Visibility = Visibility.Collapsed;
                                f4rtbWarning.Visibility = Visibility.Visible;
                                f4lblTemp2.Visibility = Visibility.Collapsed;
                                restartFrame();

                            }
                            else
                            {
                                f4lblTemp2.Content = "Temperature: " + temperature.ToString() + "°C";
                                f4rtbFever.Visibility = Visibility.Visible;
                                f4rtbWarning.Visibility = Visibility.Collapsed;
                                f4lblTemp2.Visibility = Visibility.Visible;
                                restartFrame();
                                feverCounter = 0;
                            }
                        }
                        else
                        {
                            //Open Frame 3
                            f3lblTemp.Content = "Temperature: " + temperature.ToString() + "°C";
                            navigateFrame("F3");
                        }
                        measuringState = 0;
                    }

                }
            }

        }

        private void restartFrame()
        {
            timer.Interval = TimeSpan.FromSeconds(5);
            timer.Tick += timer_Tick;
            timer.Start();
        }
        void timer_Tick(object sender, EventArgs e)
        {
            mDistance = 0;
            mTemp = 0;
            navigateFrame("F1");
            timer.Stop();
        }

        private void navigateFrame(string f)
        {
            if (f == "F0")
            {
                state = "S0";
                Frame0.Visibility = Visibility.Visible;
                Frame1.Visibility = Visibility.Hidden;
                Frame2.Visibility = Visibility.Hidden;
                Frame3.Visibility = Visibility.Hidden;
                Frame4.Visibility = Visibility.Hidden;
            }
            else if (f == "F1")
            {
                state = "S1";
                //SendToESP(state);
                Frame1.Visibility = Visibility.Visible;
                Frame2.Visibility = Visibility.Hidden;
                Frame3.Visibility = Visibility.Hidden;
                Frame4.Visibility = Visibility.Hidden;
            }
            else if (f == "F2")
            {
                state = "S2";
                Frame1.Visibility = Visibility.Hidden;
                Frame2.Visibility = Visibility.Visible;
                Frame3.Visibility = Visibility.Hidden;
                Frame4.Visibility = Visibility.Hidden;
            }
            else if (f == "F3")
            {
                state = "S3";
                //Set Temp Value for Frame 3
                restartFrame();
                feverCounter = 0;
                Frame1.Visibility = Visibility.Hidden;
                Frame2.Visibility = Visibility.Hidden;
                Frame3.Visibility = Visibility.Visible;
                Frame4.Visibility = Visibility.Hidden;
            }
            else if (f == "F4")
            {
                state = "S4";
                Frame1.Visibility = Visibility.Hidden;
                Frame2.Visibility = Visibility.Hidden;
                Frame3.Visibility = Visibility.Hidden;
                Frame4.Visibility = Visibility.Visible;
            }
            else
            {

                navigateFrame("F1");
            }
        }

        private void btnBtSettings_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("control", "bthprops.cpl");
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            //playTone();
            calibration = float.Parse(tbOffset.Text);
            if (cmb.SelectedValue != null)
            {
                // Get serial-port name
                var port = cmb.SelectedValue.ToString();
                // When disconnected
                if (serialPort == null)
                {
                    // Setting serial-port
                    serialPort = new SerialPort
                    {
                        PortName = port,
                        BaudRate = 9600,
                        DataBits = 8,
                        Parity = Parity.None,
                        StopBits = StopBits.One,
                        Encoding = Encoding.UTF8,
                        ReadTimeout = 500,
                        WriteTimeout = 500
                    };

                    // When received data
                    serialPort.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(SerialPort_DataReceived);

                    // Try to connect
                    try
                    {
                        // Open serial-port
                        serialPort.Open();
                        //statusBar.Text = "Opening";
                        //statusBar.Background = Brushes.LimeGreen;
                        btnConnect.IsEnabled = false;
                        btnDisconnect.IsEnabled = true;
                        navigateFrame("F1");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
        }

        private void disconnect_Click(object sender, RoutedEventArgs e)
        {
            if (serialPort != null && serialPort.IsOpen == true)
            {
                state = "S0";
                //SendToESP(state);
                serialPort.Close();
                //statusBar.Text = "Closed";
               //statusBar.Background = Brushes.LightGray;
                btnDisconnect.IsEnabled = false;
                btnConnect.IsEnabled = true;
                serialPort = null;
                setFrame("F0");
            }
        }

        private void btnQuit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SendToESP(String s)
        {
            // Check connected or disconnected
            if (serialPort == null) return;
            if (serialPort.IsOpen == false) return;

            // Get text from textbox
            String data = s + "\n";

            try
            {
                // Send text
                serialPort.Write(data);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            serialPort.DiscardOutBuffer();

        }

        private void receiveText_TextChanged(object sender, TextChangedEventArgs e)
        {
            
        }

        private void RichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
