using SharpAdbClient;
using System.Runtime.InteropServices;

namespace LemonADBBridge
{
    public partial class MainForm : Form
    {
        private AdbServer adbServer;
        private AdbClient adbClient;
        private DeviceComboBox deviceComboBox;

        public MainForm()
        {
            InitializeComponent();
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            statusText.Text = "CHECKING ADB...";
            await Task.Delay(100);
            await ADBCheck.CheckAndExtract();
            statusText.Text = "DISCONNECTED";

            adbServer = new AdbServer();
            adbServer.StartServer(StaticStuff.ADBPath, restartServerIfNewer: true);
            adbClient = new AdbClient();

            deviceComboBox = new DeviceComboBox(devicesComboBox);

            foreach (var device in adbClient.GetDevices())
            {
                deviceComboBox.AddItem(device);
            }
            //AllocConsole();
        }

        [DllImport("kernel32.dll")]
        public static extern bool AllocConsole();

        private void MainForm_Close(object sender, EventArgs e)
        {
            UninstallationHandler.Dispose();
        }

        private void RefreshDevices(object sender, EventArgs e)
        {
            button2.Enabled = false;
            deviceComboBox.ClearAll();
            foreach (var device in adbClient.GetDevices())
            {
                deviceComboBox.AddItem(device);
            }
        }

        private async void ConfirmDevice(object sender, EventArgs e)
        {
            devicesComboBox.Enabled = false;
            button1.Enabled = false;
            button2.Enabled = false;

            DeviceData confirmedData = deviceComboBox.GetSelectedData();
            await UninstallationHandler.Run(adbClient, confirmedData, this);
        }

        private void devicesComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            button2.Enabled = devicesComboBox.SelectedIndex > -1;
        }
    }
}
