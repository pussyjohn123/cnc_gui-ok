using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using static cnc_gui.Focas1;
using cnc_gui;
using System.Threading.Tasks;
namespace cnc_gui
{
    public class core
    {
        private static readonly object lockObj = new object();
        public static Thread mainThread; 
        public static Thread excluderThread; 
        public static Thread RdspmeterThread; 
        private static  bool start;
        private static homeViewModel viewModel;
        private static int T;  // 計數器
        private static int C; // 平台換算成積屑等級
        private static int CurrentParam = 5; // 目前排屑機所帶入的c值
        public static string CncIp;
        public static ushort CncPort;
        public static short FIdCode;  // cnc沖水點位idcord
        public static short EIdCode;  // cnc排屑點位idcord
        public static ushort Fdatano; // cnc沖水點位address
        public static ushort Edatano; // cnc沖水點位address
        private static int Excluder_Period; // 排屑機啟動週期
        public static long spindle;
        public static short connect_r4;
        private static DatabaseModel databaseModel;
        public core()
        {
            viewModel = new homeViewModel();
            databaseModel = new DatabaseModel();
            LoadData();
        }

        public static void MainStart()
        {
            var core = new core();
            start = true;
            if (mainThread == null || !mainThread.IsAlive)
            {
                mainThread = new Thread(() => Flusher());
                mainThread.Start();
            }

            if (excluderThread == null || !excluderThread.IsAlive)
            {
                excluderThread = new Thread(() => Excluder());
                excluderThread.Start();
            }

            if (RdspmeterThread == null || !RdspmeterThread.IsAlive)
            {
                RdspmeterThread = new Thread(() => Rdspmeter());
                RdspmeterThread.Start();
            }
        }

        public static void MainStop()
        {
            start = false;
            mainThread?.Join(1000);
            excluderThread?.Join(1000);
            RdspmeterThread?.Join(1000);
            mainThread = null;
            excluderThread = null;
            RdspmeterThread = null;
        }

        public static void Flusher()
        {
            while (start)
            {
                ushort FFlibHndl1;
                DateTime startTime = DateTime.Now;
                short R = Focas1.cnc_allclibhndl3(CncIp, CncPort, 1, out FFlibHndl1);
                viewModel.CaptureImage(@"C:\Users\user\Desktop\cnc_gui_master\method_AI\origin\origin.jpg");
                ImageProcess();
                int level = databaseModel.GetLevelResult().Flusher_level_result;
                Flusher(level, Fdatano, Fdatano, FIdCode, FFlibHndl1);
                lock (lockObj)
                {
                    if (T < 5)
                    {
                        C += level;
                        T += 1;
                    }
                    else if (T == 5)
                    {
                        CurrentParam = C;
                        C = 0;
                        T = 0;
                    }
                }
                DateTime endTime = DateTime.Now;
                TimeSpan duration = (endTime - startTime);
                int remainingTime = 100000-(int)duration.TotalMilliseconds;
                if (remainingTime > 0)
                {
                    Thread.Sleep(remainingTime);
                }
                Focas1.cnc_freelibhndl(FFlibHndl1);
            }
        }


        public static void Excluder()
        {
            while (start == true)
            {
                ushort FFlibHndl2;
                short R2 = Focas1.cnc_allclibhndl3(CncIp, CncPort, 1, out FFlibHndl2);
                int currentParam;

                lock (lockObj)
                {
                    currentParam = CurrentParam; // 讀取靜態 CurrentParam
                }

                Excluder(currentParam, Fdatano, Fdatano, FIdCode, FFlibHndl2);
                Focas1.cnc_freelibhndl(FFlibHndl2);
            }
        }

        public static void Rdspmeter()
        {
            while (start == true)
            {
                ushort FFlibHndl3;
                short R3 = Focas1.cnc_allclibhndl3(CncIp, CncPort, 1, out FFlibHndl3);
                long get = GetData(FFlibHndl3);
                spindle = get;
                Thread.Sleep(1000);
                Focas1.cnc_freelibhndl(FFlibHndl3);
            }
        }

        private void LoadData()
        {
            CncIp = databaseModel.GetIp_Port().Cncip;
            CncPort = StringToUshort(databaseModel.GetIp_Port().Cncport);
            FIdCode = (short)databaseModel.GetPlcControlById(1).Handl;
            EIdCode = (short)databaseModel.GetPlcControlById(2).Handl;
            Fdatano = (ushort)databaseModel.GetPlcControlById(1).Address;
            Excluder_Period = databaseModel.GetSetting().Excluder_Period;
        }
        //沖水按鈕
        public static void TestFlusher()
        {
            ushort FFlibHndl4;
            short R5 = Focas1.cnc_allclibhndl3(CncIp, CncPort, 1, out FFlibHndl4);
            WritePmcData(Fdatano, Fdatano, 1, FIdCode,  FFlibHndl4);
        }
        public static int StringToInt(string inString)
        {
            int.TryParse(inString, out int result);
            return result;
        }
        public static ushort StringToUshort(string inString)
        {
            ushort.TryParse(inString, out ushort result);
            return result;
        }
        public static short StringToShort(string inString)
        {
            short.TryParse(inString, out short result);
            return result;

        }
        //影像處理+AI
        public static void ImageProcess()
        {
            // 設定 Python檔絕對路徑
            string pythonFilePath = @"D:\cnc_gui_s-master\method_AI\1227_app.py";
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = @"C:\Program Files\Python311\python.exe"; 
            psi.Arguments = pythonFilePath;
            psi.CreateNoWindow = true;                 // 不顯示命令行視窗
            psi.UseShellExecute = false;               // 必須設為 false，以便不重定向輸出
            psi.RedirectStandardOutput = true;         // 不需要捕捉標準輸出
            psi.RedirectStandardError = true;          // 不需要捕捉標準錯誤
            try
            {
                using (Process process = Process.Start(psi))
                {
                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
            }
        }
        //讀取主軸負載
        public static long GetData(ushort FFlibHnd1)
        {
            long data = -1;
            short data_num = 1;
            Focas1.Odbspload spindleLoad = new Focas1.Odbspload();
            var ret = Focas1.cnc_rdspmeter(FFlibHnd1, 0, ref data_num, spindleLoad);

            if (ret == Focas1.EW_OK)
            {
                data = spindleLoad.spload_data.spload.data;


            }
            else
            {
                Console.WriteLine("failed to read");
            }
            return data;
        }

        //10進制轉2進制陣列
        public static int[] ConvertToBinaryArray(int decimalNumber)
        {
            int[] binaryarr = new int[8];
            for (int i = 0; i < 8; i++)
            {
                binaryarr[7 - i] = (decimalNumber >> i) & 1;
            }
            return binaryarr;
        }

        //2進制轉10進制
        public static int ConvertBinaryArrayToDecimal(int[] binaryArray)
        {
            int decimalValue = 0;
            int length = binaryArray.Length;
            for (int i = 0; i < length; i++)
            {
                if (binaryArray[i] != 0 && binaryArray[i] != 1)
                    throw new FormatException("錯誤的二進制數值。");
                decimalValue += binaryArray[length - 1 - i] * (int)Math.Pow(2, i);
            }
            return decimalValue;
        }


        //讀取十進制數值，之前資料型態是long
        public static int ReadByteParam(ushort datano_s, ushort datano_e, short IdCode, ushort FFlibHndl) //起始位置，結束位置，idcode
        {
            ushort length = (ushort)(8 + (datano_e - datano_s + 1));
            Focas1.Iodbpmc buf = new Focas1.Iodbpmc();
            short ret = Focas1.pmc_rdpmcrng(FFlibHndl, IdCode, 0, datano_e, datano_s, length, buf);
            return buf.cdata[0];
        }
        //寫入修改好的10進制數值，要修改的時候就呼叫一次
        public static void WritePmcData(ushort datano_s, ushort datano_e, int i, short IdCode, ushort FFlibHndl) //起始位置，結束位置，i=要修改的bit，idcode
        {
            ReadByteParam(datano_s, datano_e, IdCode, FFlibHndl);
            ushort length = (ushort)(8 + (datano_e - datano_s + 1));
            Focas1.Iodbpmc buf = new Focas1.Iodbpmc();
            short ret = Focas1.pmc_rdpmcrng(FFlibHndl, IdCode, 0, datano_e, datano_s, length, buf);
            int[] binaryArray = ConvertToBinaryArray(buf.cdata[0]); //10轉2
            if (binaryArray.Length > 0)
            {
                int machineIndex = binaryArray.Length - 1 - i; // 映射i到機器的位址
                binaryArray[machineIndex] = binaryArray[machineIndex] == 0 ? 1 : 0;
            }
            int modifiedDecimalValue = ConvertBinaryArrayToDecimal(binaryArray);//2轉10
            buf.cdata[0] = (byte)modifiedDecimalValue;
            short rt = Focas1.pmc_wrpmcrng(FFlibHndl, (short)length, buf);
        }
        //底座環沖控制
        public static void Flusher(int level, ushort datano_s, ushort datano_e, short IdCode, ushort FFlibHndl)
        {
            if (level == 1)
            {
                //level1不沖水
            }

            else if (level == 2)
            {
                WritePmcData(datano_s, datano_e, 1, IdCode, FFlibHndl);
                Thread.Sleep(2000);
                WritePmcData(datano_s, datano_e, 1, IdCode, FFlibHndl);
            }
            else if (level == 3)
            {
                WritePmcData(datano_s, datano_e, 1, IdCode, FFlibHndl);
                Thread.Sleep(3000);
                WritePmcData(datano_s, datano_e, 1, IdCode, FFlibHndl);
            }
            else if (level == 4)
            {
                WritePmcData(datano_s, datano_e, 1, IdCode, FFlibHndl);
                Thread.Sleep(5000);
                WritePmcData(datano_s, datano_e, 1, IdCode, FFlibHndl);
            }
            else if (level == 5)
            {
                WritePmcData(datano_s, datano_e, 1, IdCode, FFlibHndl);
                Thread.Sleep(6000);
                WritePmcData(datano_s, datano_e, 1, IdCode, FFlibHndl);
            }
        }
        //排屑機控制
        public static void Excluder(int c, ushort datano_s, ushort datano_e, short IdCode, ushort FFlibHndl)
        {
            if (c == Excluder_Period)
            {
                DateTime startTime = DateTime.Now;
                databaseModel.UpdateLevelResult("excluder_level_result",1);
                WritePmcData(datano_s, datano_e, 0, IdCode, FFlibHndl);
                Thread.Sleep(10000);
                WritePmcData(datano_s, datano_e, 0, IdCode, FFlibHndl);
                DateTime endTime = DateTime.Now;
                TimeSpan Duratiom = (endTime - startTime);
                int remainingTime = 100000 - (int)Duratiom.TotalMilliseconds;
                if (remainingTime > 0)
                {
                    Thread.Sleep(remainingTime);
                }
            }
            else if (c > Excluder_Period && c < (Excluder_Period * 2) + 1)
            {
                DateTime startTime = DateTime.Now;
                databaseModel.UpdateLevelResult("excluder_level_result", 2);
                WritePmcData(datano_s, datano_e, 0, IdCode, FFlibHndl);
                Thread.Sleep(30000);
                WritePmcData(datano_s, datano_e, 0, IdCode, FFlibHndl);
                DateTime endTime = DateTime.Now;
                TimeSpan Duratiom = (endTime - startTime);
                int remainingTime = 100000 - (int)Duratiom.TotalMilliseconds;
                if (remainingTime > 0)
                {
                    Thread.Sleep(remainingTime);
                }
            }
            else if (c > (Excluder_Period * 2) && c < (Excluder_Period * 3) + 1)
            {
                DateTime startTime = DateTime.Now;
                databaseModel.UpdateLevelResult("excluder_level_result", 3);
                WritePmcData(datano_s, datano_e, 0, IdCode, FFlibHndl);
                Thread.Sleep(50000);
                WritePmcData(datano_s, datano_e, 0, IdCode, FFlibHndl);
                DateTime endTime = DateTime.Now;
                TimeSpan Duratiom = (endTime - startTime);
                int remainingTime = 100000 - (int)Duratiom.TotalMilliseconds;
                if (remainingTime > 0)
                {
                    Thread.Sleep(remainingTime);
                }
            }
            else if (c > (Excluder_Period * 3) && c < (Excluder_Period * 4) + 1)
            {
                DateTime startTime = DateTime.Now;
                databaseModel.UpdateLevelResult("excluder_level_result", 4);
                WritePmcData(datano_s, datano_e, 0, IdCode, FFlibHndl);
                Thread.Sleep(70000);
                WritePmcData(datano_s, datano_e, 0, IdCode, FFlibHndl);
                DateTime endTime = DateTime.Now;
                TimeSpan Duratiom = (endTime - startTime);
                int remainingTime = 100000 - (int)Duratiom.TotalMilliseconds;
                if (remainingTime > 0)
                {
                    Thread.Sleep(remainingTime);
                }
            }
            else if (c > (Excluder_Period * 4) && c < (Excluder_Period * 5) + 1)
            {
                DateTime startTime = DateTime.Now;
                databaseModel.UpdateLevelResult("excluder_level_result", 5);
                WritePmcData(datano_s, datano_e, 0, IdCode, FFlibHndl);
                Thread.Sleep(100000);
                WritePmcData(datano_s, datano_e, 0, IdCode, FFlibHndl);
                DateTime endTime = DateTime.Now;
                TimeSpan Duratiom = (endTime - startTime);
                int remainingTime = 100000 - (int)Duratiom.TotalMilliseconds;
                if (remainingTime > 0)
                {
                    Thread.Sleep(remainingTime);
                }
            }
        }
    }
    public class Focas1
    {
        // Declare constants and methods from FOCAS library
        public const short EW_OK = 0;
        [DllImport("./Fwlib32.dll")]
        public static extern short cnc_allclibhndl3(string ip, ushort port, int timeout, out ushort libhndl);

        [DllImport("./Fwlib32.dll")]
        public static extern short cnc_freelibhndl(ushort libhndl);

        [DllImport("./Fwlib32.dll")]
        public static extern short cnc_rdspmeter(ushort libhndl, short type, ref short data_num, [MarshalAs(UnmanagedType.LPStruct), Out] Odbspload spmeter);

        [DllImport("./Fwlib32.dll")]
        public static extern short pmc_rdpmcrng(ushort FlibHndl, short adr_type, short data_type, ushort s_number, ushort e_number, ushort length, [MarshalAs(UnmanagedType.LPStruct), Out] Iodbpmc buf);

        [DllImport("./Fwlib32.dll")]
        public static extern short pmc_wrpmcrng(ushort FlibHndl, short length, [MarshalAs(UnmanagedType.LPStruct), In] Iodbpmc buf);
        [StructLayout(LayoutKind.Sequential)]

        public class Odbspload
        {
            public Odbspload_data spload_data = new Odbspload_data();
        }
        [StructLayout(LayoutKind.Sequential)]
        public class Odbspload_data
        {
            public Loadlm spload = new Loadlm();
            public Loadlm spspeed = new Loadlm();
        }
        [StructLayout(LayoutKind.Sequential)]
        public class Loadlm
        {
            public long data;       /* load meter data, motor speed */
            public short dec;        /* place of decimal point */
            public short unit;       /* unit */
            public char name;       /* spindle name */
            public char suff1;      /* subscript of spindle name 1 */
            public char suff2;      /* subscript of spindle name 2 */
            public char reserve;    /* */

        }

        [StructLayout(LayoutKind.Explicit)]
        public class Iodbpmc
        {
            [FieldOffset(0)]
            public short type_a;   /* Kind of PMC address */
            [FieldOffset(2)]
            public short type_d;   /* Type of the PMC data */
            [FieldOffset(4)]
            public ushort datano_s; /* Start PMC address number */
            [FieldOffset(6)]
            public ushort datano_e;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 200)]
            [FieldOffset(8)]
            public byte[] cdata;
        }
    }
}