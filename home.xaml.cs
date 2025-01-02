using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Media;
using static cnc_gui.DatabaseModel;
using System.Threading.Tasks.Sources;
using System.Text.RegularExpressions;


namespace cnc_gui
{
    /// <summary>
    /// home.xaml 的互動邏輯
    /// </summary>
    public partial class home : Page
    {
        public static core tscore;
        private setting settingsPage;
        private DatabaseModel dbModel;
        homeViewModel viewModel = new homeViewModel();
        public Ip_port ip_port { get; set; }
        public home()
        {
            tscore = new core();
            dbModel = new DatabaseModel();
            this.DataContext = viewModel;
            settingsPage = new setting();
            dbModel.GetIp_Port();
            LoadData();
            viewModel.StartCameraPreviewAsync();
            InitializeComponent();
            // 在初始化時，將 setting.xaml 中的靜態變數的值顯示在 TextBlock

            home_ip.Text = ip_port.Cncip;
            home_port.Text = ip_port.Cncport;

            flusher_lv1_str.Text = (string)dbModel.GetColumnValueById("Level_set_to_string", "Level_1_to_str", 1);
            flusher_lv2_str.Text = (string)dbModel.GetColumnValueById("Level_set_to_string", "Level_2_to_str", 1);
            flusher_lv3_str.Text = (string)dbModel.GetColumnValueById("Level_set_to_string", "Level_3_to_str", 1);
            flusher_lv4_str.Text = (string)dbModel.GetColumnValueById("Level_set_to_string", "Level_4_to_str", 1);
            flusher_lv5_str.Text = (string)dbModel.GetColumnValueById("Level_set_to_string", "Level_5_to_str", 1);

            flusher_lv1_time.Text = dbModel.GetColumnValueById("Level_set_time", "Level_1_time", 1).ToString();
            flusher_lv2_time.Text = dbModel.GetColumnValueById("Level_set_time", "Level_2_time", 1).ToString();
            flusher_lv3_time.Text = dbModel.GetColumnValueById("Level_set_time", "Level_3_time", 1).ToString();
            flusher_lv4_time.Text = dbModel.GetColumnValueById("Level_set_time", "Level_4_time", 1).ToString();
            flusher_lv5_time.Text = dbModel.GetColumnValueById("Level_set_time", "Level_5_time", 1).ToString();

            excluder_lv1_str.Text = (string)dbModel.GetColumnValueById("Level_set_to_string", "Level_1_to_str", 2);
            excluder_lv2_str.Text = (string)dbModel.GetColumnValueById("Level_set_to_string", "Level_2_to_str", 2);
            excluder_lv3_str.Text = (string)dbModel.GetColumnValueById("Level_set_to_string", "Level_3_to_str", 2);
            excluder_lv4_str.Text = (string)dbModel.GetColumnValueById("Level_set_to_string", "Level_4_to_str", 2);
            excluder_lv5_str.Text = (string)dbModel.GetColumnValueById("Level_set_to_string", "Level_5_to_str", 2);

            excluder_lv1_time.Text = dbModel.GetColumnValueById("Level_set_time", "Level_1_time", 2).ToString();
            excluder_lv2_time.Text = dbModel.GetColumnValueById("Level_set_time", "Level_2_time", 2).ToString();
            excluder_lv3_time.Text = dbModel.GetColumnValueById("Level_set_time", "Level_3_time", 2).ToString();
            excluder_lv4_time.Text = dbModel.GetColumnValueById("Level_set_time", "Level_4_time", 2).ToString();
            excluder_lv5_time.Text = dbModel.GetColumnValueById("Level_set_time", "Level_5_time", 2).ToString();


        }

        public void LoadData()
        {
            ip_port = dbModel.GetIp_Port();
        }

        private void program_start_Checked(object sender, RoutedEventArgs e)
        {
            core.MainStart();
            //MessageBox.Show("程式已啟動");

        }

        private void program_stop_Checked(object sender, RoutedEventArgs e)
        {
            core.MainStop();
            //MessageBox.Show("程式已停止");
        }


        private void flusher_button_Click(object sender, RoutedEventArgs e)
        {
            core.TestFlusher();
        }
    }
}
