﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Runtime.InteropServices;

namespace PcComponentsMonitor
{
    public partial class App : Form
    {
        PerformanceCounter cpuCounter;
        PerformanceCounter ramCounter;
        PerformanceCounter ramCounter2;
        PerformanceCounter driveCounter;
        PerformanceCounter driveCounter2;

        private bool sendedWarning_cpu = false;
        private bool sendedWarning_cpu_usage = false;
        private bool sendedWarning_ram_usage = false;
        public static bool canBeTheTopMost = true;
        public static string reason = string.Empty;
        private static bool enableMoving = false;
        private static Point initialClickedPoint;

        public App()
        {
            InitializeComponent();
            //Cpu and ram counters
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            ramCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use");
            ramCounter2 = new PerformanceCounter("Memory", "Available MBytes");
            driveCounter = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
            driveCounter2 = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total");
        }


        private void timer1_Tick(object sender, EventArgs e)
        {
            double temperature = 0;
            //Create new ManagementObjectSearcher
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM MSAcpi_ThermalZoneTemperature");
            foreach (ManagementObject obj in searcher.Get())
            {
                //Gets the cpu temp and converts it to °C
                temperature = Convert.ToDouble(obj["CurrentTemperature"].ToString());
                temperature = (temperature - 2732) / 10.0;
                //Chanegs color of text dependign on temp
                ChangeCPUTempColor(temperature);

                //When cpu temps get too high it will send a warning
                Properties.Settings.Default.Reload();
                if (temperature > 75 && !sendedWarning_cpu && Properties.Settings.Default.sendMessage)
                {
                    new ToastContentBuilder()
                        .AddText("Warning!")
                        .AddText("Your cpu temps are getting too high!")
                        .Show();
                    sendedWarning_cpu = true;
                }
            }
            //Will set the text to the temperatures
            cpu_temp.Text = "Temp: " + temperature.ToString() + "°C";

            float cpu_usage_f = cpuCounter.NextValue();
            cpu_usage.Text = "Usage: " + Math.Round(cpu_usage_f) + "%";
            //Changes color of text depending on cpu usage
            ChangeCPUUsageColor(cpu_usage_f);

            //Sends warning message
            Properties.Settings.Default.Reload();
            if (!sendedWarning_cpu_usage && (Math.Round(cpu_usage_f)) > 75f && Properties.Settings.Default.sendMessage)
            {
                new ToastContentBuilder()
                        .AddText("Warning!")
                        .AddText("Your cpu usage is getting too high!")
                        .Show();
                sendedWarning_cpu_usage = true;
            }

            //Updates all available ram
            ManagementObjectSearcher ramAll = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
            foreach (ManagementObject obj in ramAll.Get())
            {
                ram_free_gb.Text = "Free: " + Math.Round(ramCounter2.NextValue() / 1024) + "GB/" + Math.Round(double.Parse(obj["TotalVisibleMemorySize"].ToString()) / 1048576, 2, MidpointRounding.ToEven) + "GB";
            }

            //Updates ram usage
            double ram_usage_f = Math.Round(ramCounter.NextValue());
            ram_usage.Text = "Usage: " + ram_usage_f.ToString() + "%";

            ChangeRAMUSageColor(ram_usage_f);

            //Sends warning when RAM usage gets too high
            Properties.Settings.Default.Reload();
            if (ram_usage_f > 75 && !sendedWarning_ram_usage && Properties.Settings.Default.sendMessage)
            {
                new ToastContentBuilder()
                        .AddText("Warning!")
                        .AddText("Your RAM usage is getting too high!")
                        .Show();
                sendedWarning_ram_usage = true;
            }

            //Gets C:\ drive info
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            foreach (DriveInfo drive in allDrives)
            {
                drive_free_space.Text = "Free space: " + drive.TotalFreeSpace / 1073741824 + "MB / " + drive.TotalSize / 1073741824 + "MB";
                float driveFull = drive.TotalSize / 1073741824;
                float driveUsed = driveFull - drive.TotalFreeSpace / 1073741824;
                float driveUsedPercentage = driveUsed * 100 / driveFull;
                drive_fullness.Text = "Fullness: " + Math.Round(driveUsedPercentage) + "%";
                ChangeDriveUsedColor(Math.Round(driveUsedPercentage));
                break;    
            }
            //Gets read and write speed of drive
            double write = driveCounter2.NextValue();
            double read = driveCounter.NextValue();
            drive_read_write.Text = "Read/Write: " + Math.Round(read / 1024, 2)  + "kB/s / " + Math.Round(write / 1024, 2) + "kB/s";

            //Resets the timer
            Timer timer = sender as Timer;
            timer.Interval = 5000;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //Dark mode
            if (Properties.Settings.Default.Darkmode)
            {
                this.BackColor = Color.FromArgb(41, 41, 41);
                this.ForeColor = Color.White;
                panelName.ForeColor = Color.White;
                panelName.BackColor = Color.FromArgb(55, 55, 55);
                btnExit.BackColor = Color.FromArgb(55, 55, 55);
                btnExit.FlatAppearance.BorderColor = Color.FromArgb(55, 55, 55);
                btnHide.BackColor = Color.FromArgb(55, 55, 55);
                btnHide.FlatAppearance.BorderColor = Color.FromArgb(55, 55, 55);
            }


            //Sets the styles
            this.FormBorderStyle = FormBorderStyle.None;
            Rectangle workingArea = Screen.GetWorkingArea(this);
            this.Location = new Point(workingArea.Right - Size.Width,
                                      workingArea.Bottom - Size.Height);
            this.TopMost = true;
            this.ShowInTaskbar = false;

            //Default panel
            switch (Properties.Settings.Default.DefaultPanel)
            {
                case "PC":
                    pc_info.Location = new Point(0, 27);
                    break;
                case "CPU":
                    CPU_info.Location = new Point(0, 27);
                    break;
                case "RAM":
                    RAM_info.Location = new Point(0, 27);
                    break;
                case "Drive":
                    Drive_info.Location = new Point(0, 27);
                    break;
            }

            //Form postion
            if (Properties.Settings.Default.DefaultPosX != 0 && Properties.Settings.Default.DefualtPosY != 0)
            {
                Point point = new Point();
                point.X = (int)Properties.Settings.Default.DefaultPosX;
                point.Y = (int)Properties.Settings.Default.DefualtPosY;
                this.Location = point;
            }
            
            else
            {
                switch (Properties.Settings.Default.Position)
                {
                    case "Right, Bottom":
                        //When ignore taskbar is off
                        if (!Properties.Settings.Default.IgnoreTaskbar) this.Location = new Point(workingArea.Right - Size.Width, workingArea.Bottom - Size.Height);

                        //When ignore taskbar is on
                        else this.Location = new Point(Screen.PrimaryScreen.Bounds.Right - this.Width, Screen.PrimaryScreen.Bounds.Bottom - this.Height);
                        break;
                    case "Right, Top":
                        this.Location = new Point(Screen.PrimaryScreen.Bounds.Right - this.Width, 0);
                        break;
                    case "Left, Bottom":
                        //When ignore taskbar is off
                        if (!Properties.Settings.Default.IgnoreTaskbar) this.Location = new Point(workingArea.Left, workingArea.Bottom - Size.Height);

                        //When ignore taskbar is on
                        else this.Location = new Point(Screen.PrimaryScreen.Bounds.Left, Screen.PrimaryScreen.Bounds.Bottom - this.Height);
                        break;
                    case "Left, Top":
                        this.Location = new Point(0, 0);
                        break;
                }
            }

            //Gets OS name
            pc_name.Text = "OS name: " + (from x in new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem").Get().Cast<ManagementObject>()
                                          select x.GetPropertyValue("Caption")).FirstOrDefault().ToString();
            //Gets cpu name
            ManagementClass mc = new ManagementClass("win32_processor");
            ManagementObjectCollection managCollec = mc.GetInstances();
            foreach (ManagementObject managObj in managCollec)
            {
                //Gets cpu name
                string cpuName = managObj.Properties["Name"].Value.ToString();
                pc_cpu.Text = "CPU: " + cpuName;
                break;
            }
            //Gets amount in GB of ram
            ObjectQuery wql = new ObjectQuery("SELECT * FROM Win32_OperatingSystem");
            ManagementObjectSearcher searcher1 = new ManagementObjectSearcher(wql);
            ManagementObjectCollection results = searcher1.Get();
            foreach (ManagementObject result in results)
            {
                pc_ram.Text = "RAM: " + Math.Round(double.Parse(result["TotalVisibleMemorySize"].ToString()) / 1048576, 2, MidpointRounding.ToEven) + "GB";
            }
            //Gets C:\Drive info
            DriveInfo[] allDrive = DriveInfo.GetDrives();
            foreach (DriveInfo drive in allDrive)
            {
                pc_os_drive.Text = "OS drive name: " + drive.VolumeLabel + " (" + drive.Name + ")";
                break;
            }



            double temperature = 0;
            //Gets cpu temperature
            ManagementObjectSearcher cpuObject = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM MSAcpi_ThermalZoneTemperature");
            foreach (ManagementObject obj in cpuObject.Get())
            {
                //Gets the cpu temp and converts it to °C
                temperature = Convert.ToDouble(obj["CurrentTemperature"].ToString());
                temperature = (temperature - 2732) / 10.0;
                ChangeCPUTempColor(temperature);
            }
            //Will set the text to the temperatures
            cpu_temp.Text = "Temp: " + temperature.ToString() + "°C";


            foreach (ManagementObject managObj in managCollec)
            {
                //Thing to get the processor name (Idk what it does I just copied it.)
                string cpuName = managObj.Properties["Name"].Value.ToString();
                cpu_name.Text = "Name: " + cpuName;
                break;
            }
            //Gets cpu usage in precentage
            cpu_usage.Text = "Usage: " + Math.Round(cpuCounter.NextValue(), MidpointRounding.ToEven) + "%";

            //Gets ram manufacturer
            ManagementObjectSearcher ramObject = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");

            foreach (ManagementObject obj in ramObject.Get())
            {
                ram_name.Text = "Manufacturer: " + obj["Manufacturer"].ToString();
                
            }

            //Gets all available ram
            ManagementObjectSearcher ramAll = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
            foreach (ManagementObject obj in ramAll.Get())
            {
                ram_free_gb.Text = "Free: " + Math.Round(ramCounter2.NextValue() / 1024) + "GB/" + Math.Round(double.Parse(obj["TotalVisibleMemorySize"].ToString()) / 1048576, 2, MidpointRounding.ToEven) + "GB";
            }

            //Gets ram usage
            double ram_usage_f = Math.Round(ramCounter.NextValue());
            ram_usage.Text = "Usage: " + ram_usage_f.ToString() + "%";
            ChangeRAMUSageColor(ram_usage_f);

            //Gets C:\ drive info
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            foreach (DriveInfo drive in allDrives)
            {
                
                drive_name.Text = "Name: " + drive.VolumeLabel + " (" + drive.Name + ")";
                drive_free_space.Text = "Free space: " + drive.TotalFreeSpace / 1073741824 + "MB / " + drive.TotalSize / 1073741824 + "MB";
                float driveFull = drive.TotalSize / 1073741824;
                float driveUsed = driveFull - drive.TotalFreeSpace / 1073741824;
                float driveUsedPercentage = driveUsed * 100 / driveFull;
                drive_fullness.Text = "Fullness: " + Math.Round(driveUsedPercentage) + "%";
                ChangeDriveUsedColor(Math.Round(driveUsedPercentage));
                break;
            }
            //Gets read and write speed of drive
            double write = driveCounter2.NextValue();
            double read = driveCounter.NextValue();
            drive_read_write.Text = "Read/Write: " + Math.Round(read / 1024, 2) + "kB/s / " + Math.Round(write / 1024, 2) + "kB/s"; 

            //Shows welcome message box
            if (Properties.Settings.Default.WelcomeUser)
            {
                MessageBox.Show("Thank you for downloading PC Components Monitor! You can now try features by yourself.", "Welcome", MessageBoxButtons.OK);
                Properties.Settings.Default.WelcomeUser = false;
                Properties.Settings.Default.Save();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            //Sets to the top most
            if (canBeTheTopMost) this.TopMost = true;
            //Dark mode
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.Darkmode)
            {

                this.BackColor = Color.FromArgb(41, 41, 41);
                this.ForeColor = Color.White;
                panelName.ForeColor = Color.White;
                panelName.BackColor = Color.FromArgb(55, 55, 55);
                btnExit.BackColor = Color.FromArgb(55, 55, 55);
                btnExit.FlatAppearance.BorderColor = Color.FromArgb(55, 55, 55);
                btnHide.BackColor = Color.FromArgb(55, 55, 55);
                btnHide.FlatAppearance.BorderColor = Color.FromArgb(55, 55, 55);
                if (this.cpu_temp.ForeColor == Color.Black)
                {
                    cpu_temp.ForeColor = Color.White;
                }
                if (this.cpu_usage.ForeColor == Color.Black)
                {
                    cpu_usage.ForeColor = Color.White;
                }
                if (this.ram_usage.ForeColor == Color.Black)
                {
                    ram_usage.ForeColor = Color.White;
                }
                if (this.drive_fullness.ForeColor == Color.Black)
                {
                    drive_fullness.ForeColor = Color.White;
                    drive_free_space.ForeColor = Color.White;
                }


            }
            else
            {

                this.BackColor = Color.White;
                this.ForeColor = Color.Black;
                panelName.ForeColor = Color.Black;
                panelName.BackColor = Color.FromArgb(224, 224, 224);
                btnExit.BackColor = Color.FromArgb(224, 224, 224);
                btnExit.FlatAppearance.BorderColor = Color.FromArgb(224, 224, 224);
                btnHide.BackColor = Color.FromArgb(224, 224, 224);
                btnHide.FlatAppearance.BorderColor = Color.FromArgb(224, 224, 224);
                if (this.cpu_temp.ForeColor == Color.White)
                {
                    cpu_temp.ForeColor = Color.Black;
                }
                if (this.cpu_usage.ForeColor == Color.White)
                {
                    cpu_usage.ForeColor = Color.Black;
                }
                if (this.ram_usage.ForeColor == Color.White)
                {
                    cpu_usage.ForeColor = Color.Black;
                }
                if (this.drive_fullness.ForeColor == Color.White)
                {
                    cpu_usage.ForeColor = Color.Black;
                }
                if (this.drive_fullness.ForeColor == Color.White)
                {
                    drive_fullness.ForeColor = Color.Black;
                    drive_free_space.ForeColor = Color.Black;
                }
            }
            //Resets timer
            timer2.Interval = 1000;
        }

        //Settings
        private void btnSettings_Click(object sender, EventArgs e)
        {
            Form form = new Settings();
            form.Show();
            canBeTheTopMost = false;
            form.FormClosing += new FormClosingEventHandler(SettingsExit);
        }
        private void customButtons3_Click(object sender, EventArgs e)
        {
            Form form = new Settings();
            form.Show();
            canBeTheTopMost = false;
            form.FormClosing += new FormClosingEventHandler(SettingsExit);
        }
        private void customButtons2_Click(object sender, EventArgs e)
        {
            Form form = new Settings();
            form.Show();
            canBeTheTopMost = false;
            form.FormClosing += new FormClosingEventHandler(SettingsExit);
        }
        private void customButtons4_Click(object sender, EventArgs e)
        {
            Form form = new Settings();
            form.Show();
            canBeTheTopMost = false;
            form.FormClosing += new FormClosingEventHandler(SettingsExit);
        }

        //When user exits the settings
        private void SettingsExit(object sender, EventArgs e)
        {
            canBeTheTopMost = true;
            if (reason != string.Empty && reason == "apply")
            {
                //reload settings
                Properties.Settings.Default.Reload();
                //Sets forms position to one of the corners
                Rectangle workingArea = Screen.GetWorkingArea(this);
                switch (Properties.Settings.Default.Position)
                {
                    case "Right, Bottom":
                        //When ignore taskbar is off
                        if (!Properties.Settings.Default.IgnoreTaskbar) this.Location = new Point(workingArea.Right - Size.Width, workingArea.Bottom - Size.Height);

                        //When ignore taskbar is on
                        else this.Location = new Point(Screen.PrimaryScreen.Bounds.Right - this.Width, Screen.PrimaryScreen.Bounds.Bottom - this.Height);
                        break;
                    case "Right, Top":
                        this.Location = new Point(Screen.PrimaryScreen.Bounds.Right - this.Width, 0);
                        break;
                    case "Left, Bottom":
                        //When ignore taskbar is off
                        if (!Properties.Settings.Default.IgnoreTaskbar) this.Location = new Point(workingArea.Left, workingArea.Bottom - Size.Height);

                        //When ignore taskbar is on
                        else this.Location = new Point(Screen.PrimaryScreen.Bounds.Left, Screen.PrimaryScreen.Bounds.Bottom - this.Height);
                        break;
                    case "Left, Top":
                        this.Location = new Point(0, 0);
                        break;
                }
                reason = string.Empty;
            }
            //When user change the pos
            else if (reason == "change pos and apply")
            {
                Properties.Settings.Default.DefaultPosX = this.Location.X;
                Properties.Settings.Default.DefualtPosY = this.Location.Y;
                Properties.Settings.Default.Save();
            }
        }

        //When user wants to change panel
        private void Previouse_RAM_Click(object sender, EventArgs e)
        {
            RAM_info.Location = new Point(0, 127);
            CPU_info.Location = new Point(0, 27);
        }

        private void Next_cpu_Click(object sender, EventArgs e)
        {
            CPU_info.Location = new Point(0, 127);
            RAM_info.Location = new Point(0, 27);
        }
        private void Next_RAM_Click(object sender, EventArgs e)
        {
            RAM_info.Location = new Point(0, 127);
            Drive_info.Location = new Point(0, 27);
        }
        private void Previouse_Drive_Click(object sender, EventArgs e)
        {
            RAM_info.Location = new Point(0, 27);
            Drive_info.Location = new Point(0, 127);
        }
        private void customButtons1_Click(object sender, EventArgs e)
        {
            pc_info.Location = new Point(0, 127);
            CPU_info.Location = new Point(0, 27);
        }
        private void previouse_cpu_Click(object sender, EventArgs e)
        {
            CPU_info.Location = new Point(0, 127);
            pc_info.Location = new Point(0, 27);
        }

        //When user clicks hide button
        private void btnHide_Click(object sender, EventArgs e)
        {
            Hide();
            notifyIcon1.Visible = true;
            notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
            notifyIcon1.BalloonTipTitle = "PC Components Monitor";
            notifyIcon1.BalloonTipText = "Click here or on the icon to make the app visible again";
            notifyIcon1.ShowBalloonTip(1000);
        }

        //Notify icon will reset when user opens again the app
        private void notifyIcon1_BalloonTipClicked(object sender, EventArgs e)
        {
            Show();
            notifyIcon1.Visible = false;
            notifyIcon1.BalloonTipIcon = ToolTipIcon.None;
            notifyIcon1.BalloonTipTitle = string.Empty;
            notifyIcon1.BalloonTipText = string.Empty;
        }

        //Notify icon will reset when user opens again the app
        private void notifyIcon1_Click(object sender, EventArgs e)
        {
            Show();
            notifyIcon1.Visible = false;
            notifyIcon1.BalloonTipIcon = ToolTipIcon.None;
            notifyIcon1.BalloonTipTitle = string.Empty;
            notifyIcon1.BalloonTipText = string.Empty;
        }

        //Function to change text color depnding on temperature
        private void ChangeCPUTempColor(double temperature)
        {
            if (temperature > 40 && temperature < 50)
            {
                cpu_temp.ForeColor = Color.Orange;
                sendedWarning_cpu = false;
            }
            else if (temperature > 50 && temperature < 75) cpu_temp.ForeColor = Color.DarkOrange;
            else if (temperature > 75)
            {
                cpu_temp.ForeColor = Color.Red;
            }
            else if (temperature < 40)
            {
                cpu_temp.ForeColor = this.ForeColor;
                sendedWarning_cpu = false;
            }
        }
        //Chanegs cpu usage text color
        private void ChangeCPUUsageColor(float cpu_usage_f)
        {
            if (Math.Round(cpu_usage_f) < 25f)
            {
                cpu_usage.ForeColor = this.ForeColor;
                sendedWarning_cpu_usage = false;
            }
            else if (Math.Round(cpu_usage_f) > 25f && Math.Round(cpu_usage_f) < 50f)
            {
                cpu_usage.ForeColor = Color.Orange;
                sendedWarning_cpu_usage = false;
            }
            else if (Math.Round(cpu_usage_f) > 50f && Math.Round(cpu_usage_f) < 75f)
            {
                cpu_usage.ForeColor = Color.DarkOrange;
            }
            else if (Math.Round(cpu_usage_f) > 75f && Math.Round(cpu_usage_f) < 101f)
            {
                cpu_usage.ForeColor = Color.Red;
            }

        }
        //Changes ram text color
        private void ChangeRAMUSageColor(double usage)
        {
            if (usage < 25f)
            {
                ram_usage.ForeColor = this.ForeColor;
                ram_free_gb.ForeColor = this.ForeColor;
                sendedWarning_ram_usage = false;
            }
            else if (usage > 25f && usage < 50f)
            {
                ram_usage.ForeColor = Color.Orange;
                ram_free_gb.ForeColor = Color.Orange;
                sendedWarning_ram_usage = false;
            }
            else if (usage > 50f && usage < 75f)
            {
                ram_usage.ForeColor = Color.DarkOrange;
                ram_free_gb.ForeColor = Color.DarkOrange;
            }
            else if (usage > 75f && usage < 90f)
            {
                ram_usage.ForeColor = Color.OrangeRed;
                ram_free_gb.ForeColor = Color.OrangeRed;
            }
            else if (usage > 90f && usage <= 100f)
            {
                ram_usage.ForeColor = Color.Red;
                ram_usage.ForeColor = Color.Red;
            }
        }

        //Changes drive text color
        private void ChangeDriveUsedColor(double percentage)
        {
            if (percentage <= 25)
            {
                drive_free_space.ForeColor = ForeColor;
                drive_fullness.ForeColor = ForeColor;
            }
            else if (percentage > 25 && percentage <= 50)
            {
                drive_free_space.ForeColor = Color.Orange;
                drive_fullness.ForeColor = Color.Orange;
            }
            else if (percentage > 50 && percentage <= 90)
            {
                drive_free_space.ForeColor = Color.OrangeRed;
                drive_fullness.ForeColor = Color.OrangeRed;
            }
            else if (percentage > 90 && percentage <= 100)
            {
                drive_free_space.ForeColor = Color.Red;
                drive_fullness.ForeColor = Color.Red;
            }
        }

        //Let user to change default position
        private void panelName_MouseDown(object sender, MouseEventArgs e)
        {
            enableMoving = true;
            initialClickedPoint = e.Location;
        }

        private void panelName_MouseUp(object sender, MouseEventArgs e)
        {
            enableMoving = false;
        }

        private void panelName_MouseMove(object sender, MouseEventArgs e)
        {
            Properties.Settings.Default.Reload();
            if (enableMoving && Properties.Settings.Default.ChangeDefaultPosition)
            {
                this.Location = new Point(e.X + this.Left - initialClickedPoint.X,
                        e.Y + this.Top - initialClickedPoint.Y);
            }
        }
    }
}
