using System;
using System.IO;
using System.Configuration;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Linq;

namespace ConsoleIOService
{
    class Program
    {
        public static long FunctionFileCounter { get; set; }
        public static long AllFileCounter { get; set; }
        public static long SizeOfFolder { get; set; }

        // Create a writter to open the file:
        public static StreamWriter Functionlog { get; set; }
        public static StreamWriter FailedFunctionlog { get; set; }

        static void Main(string[] args)
        {
            bool actionflag = true;
            FileStream ostrm;
            StreamWriter writer;
            TextWriter oldOut = Console.Out;
            var logpath = "./MainLog_" + DateTime.Today.ToString("yyyy.MM.dd") + ".txt";

            try
            {
                ostrm = new FileStream(logpath, FileMode.Append, FileAccess.Write);
                writer = new StreamWriter(ostrm);
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot open {0} for writing", logpath);
                Console.WriteLine(e.Message);
                return;
            }
            Console.SetOut(writer);

            int daysCount = 0;
            string sourcePath = "";
            string destinationPath = "";
            string actiontype = "";
            string excludePaths = "";

            if (args == null || args.Length == 0)
            {
                try
                {
                    Console.WriteLine(DateTime.Now + " : args is null");

                    daysCount = int.Parse(ConfigurationManager.AppSettings.Get("CounterDays"));
                    Console.WriteLine(DateTime.Now + " : Date Count : " + daysCount);

                    sourcePath = ConfigurationManager.AppSettings.Get("SourcePath");
                    Console.WriteLine(DateTime.Now + " : Source Path : " + sourcePath);

                    actiontype = ConfigurationManager.AppSettings.Get("ActionType").ToLower();
                    Console.WriteLine(DateTime.Now + " : Action Type : " + actiontype);

                    if (actiontype == "cp" || actiontype == "mv" || actiontype == "md")
                    {
                        destinationPath = ConfigurationManager.AppSettings.Get("DestinationPath");
                        Console.WriteLine(DateTime.Now + " : Destination Path : " + destinationPath);
                    }

                    excludePaths = ConfigurationManager.AppSettings.Get("ExcludePaths");
                    Console.WriteLine(DateTime.Now + " : Exclude Paths : " + excludePaths);

                    if (ConfigurationManager.AppSettings.Get("AutoPrompt") == "0")
                    {
                        Console.SetOut(oldOut);
                        Console.WriteLine("Enter \'Y\' to Confirm This Action \"{0}\" Or Any other for exit : ", actiontype); // Prompt

                        Console.SetOut(writer);
                        if (Console.ReadKey().Key != ConsoleKey.Y)
                        {
                            Console.WriteLine(DateTime.Now + " : Action is Canceled !!!");

                            Console.SetOut(oldOut);
                            writer.Close();
                            ostrm.Close();

                            return;
                        }

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(DateTime.Now + " : args is less or more than Usual !!! " + ex);
                }
            }
            else if (args.Length == 1)
            {


                if (args[0].ToLower() == "c")
                {
                    Console.WriteLine(DateTime.Now + " : Configuration Start ");

                    // run Configuration in Visual Form
                    //Application.EnableVisualStyles();
                    //Application.Run(new SettingForm());

                    Console.WriteLine(DateTime.Now + " : End of Configuration ");
                    return;
                }
                Console.WriteLine(DateTime.Now + " : Wrong Parameter !");
                return;
            }
            else
            {
                try
                {
                    Console.WriteLine(DateTime.Now + " : args length is " + args.Length);

                    if (args.Length > 2 && args.Length < 5)
                    {
                        daysCount = int.Parse(args[0]);
                        Console.WriteLine(DateTime.Now + " : Date Count : " + args[0]);

                        sourcePath = args[1];
                        Console.WriteLine(DateTime.Now + " : Source Path : " + args[1]);

                        actiontype = args[2].ToLower();
                        Console.WriteLine(DateTime.Now + " : Action Type : " + args[2]);

                        excludePaths = args[3];
                        Console.WriteLine(DateTime.Now + " : Exclude Paths : " + args[3]);

                        if (actiontype == "cp" || actiontype == "mv" || actiontype == "md")
                        {
                            destinationPath = args[3];
                            Console.WriteLine(DateTime.Now + " : Destination Path : " + args[3]);
                        }

                    }
                    else
                    {
                        Console.WriteLine(DateTime.Now + " : args is less or more than Usual !!!");
                    }
                }
                catch (Exception)
                {

                    Console.WriteLine(DateTime.Now +
                                      " : Something is wrong! Check Your args ( days sourcePath action ) Or For Move,Copy Files ( days sourcePath action destinationPath )");
                }
            }

            FunctionFileCounter = 0;
            AllFileCounter = 0;
            SizeOfFolder = 0;

            string[] sourcePathArrays;
            string[] destinationPathArrays;

            switch (actiontype)
            {
                case "cp":
                    Console.WriteLine(DateTime.Now + " : Copy Files Task Start");

                    //Task cpTask = null;
                    sourcePathArrays = sourcePath.Split(',');
                    destinationPathArrays = destinationPath.Split(',');

                    if (sourcePathArrays.Length == destinationPathArrays.Length)
                    {
                        for (var i = 0; i < sourcePathArrays.Length; i++)
                        {
                            var pNum = i;

                            var desPath = destinationPathArrays[pNum];

                            if (ConfigurationManager.AppSettings.Get("AddNameDate") == "1")
                            {
                                string tempDaysText = "";
                                if (ConfigurationManager.AppSettings.Get("ShowCounterDays") == "1") tempDaysText = "_" + ConfigurationManager.AppSettings.Get("CounterDays") + "Days";

                                desPath = desPath + ConfigurationManager.AppSettings.Get("PreBackupName") +
                                          DateTime.Now.ToString(ConfigurationManager.AppSettings.Get("DateFormat")) + tempDaysText + "\\";
                            }

                            if (ConfigurationManager.AppSettings.Get("AddAllEmptyFolder") == "1")
                            {
                                using (
                                    Task makeDirectoryTask =
                                        Task.Factory.StartNew(
                                            () =>
                                                CallMakeDirTask(sourcePathArrays[pNum], desPath,
                                                    sourcePathArrays[pNum])))
                                {
                                    Task.WaitAll(makeDirectoryTask);
                                }
                            }

                            Console.WriteLine(DateTime.Now + " : Source Path : {0} to Destination Path : {1}",
                                sourcePathArrays[pNum], desPath);
                            Task cpTask =
                                Task.Factory.StartNew(
                                    () =>
                                        CallCopyTask(daysCount * -1, sourcePathArrays[pNum], desPath,
                                            sourcePathArrays[pNum], excludePaths));
                            Task.WaitAll(cpTask);

                            if (ConfigurationManager.AppSettings.Get("Zip") == "1")
                            {
                                var zipPath = "";
                                try
                                {
                                    string tempDaysText = "";
                                    if (ConfigurationManager.AppSettings.Get("ShowCounterDays") == "1") tempDaysText = "_" + ConfigurationManager.AppSettings.Get("CounterDays") + "Days";

                                    zipPath = destinationPathArrays[pNum] + ConfigurationManager.AppSettings.Get("PreBackupName") + DateTime.Now.ToString(ConfigurationManager.AppSettings.Get("DateFormat")) + tempDaysText + ".Zip";
                                    ZipFile.CreateFromDirectory(desPath, zipPath);

                                    if (ConfigurationManager.AppSettings.Get("TestZip") == "1")
                                        ZipFile.OpenRead(zipPath);
                                }
                                catch (Exception)
                                {

                                    Console.WriteLine(DateTime.Now + " : {0}  zip file is corrupted !!!", zipPath);
                                }
                            }

                        }
                    }
                    else
                    {
                        Console.WriteLine(
                            DateTime.Now + " : Source Paths {0} and Destination Paths {1} are not equal !!!",
                            sourcePathArrays.Length, destinationPathArrays.Length);
                    }


                    Console.WriteLine(DateTime.Now + " : Count Files in the folder : {0} ", AllFileCounter);
                    Console.WriteLine(DateTime.Now + " : Count Copied Files : {0} ", FunctionFileCounter);
                    Console.WriteLine(DateTime.Now + " : Copy Files Task Successfully Ended !");

                    break;

                case "mv":
                    Console.WriteLine(DateTime.Now + " : Move Files Task Start");


                    //Task mvTask = null;
                    sourcePathArrays = sourcePath.Split(',');

                    destinationPathArrays = destinationPath.Split(',');

                    if (sourcePathArrays.Length == destinationPathArrays.Length)
                    {
                        for (var i = 0; i < sourcePathArrays.Length; i++)
                        {
                            var pNum = i;

                            var desPath = destinationPathArrays[pNum];

                            if (ConfigurationManager.AppSettings.Get("AddNameDate") == "1")
                            {
                                string tempDaysText = "";
                                if (ConfigurationManager.AppSettings.Get("ShowCounterDays") == "1") tempDaysText = "_" + ConfigurationManager.AppSettings.Get("CounterDays") + "Days";

                                desPath = desPath + ConfigurationManager.AppSettings.Get("PreBackupName") +
                                          DateTime.Now.ToString(ConfigurationManager.AppSettings.Get("DateFormat")) + tempDaysText + "\\";
                            }

                            if (ConfigurationManager.AppSettings.Get("AddAllEmptyFolder") == "1")
                            {
                                using (
                                    Task makeDirectoryTask =
                                        Task.Factory.StartNew(
                                            () =>
                                                CallMakeDirTask(sourcePathArrays[pNum], desPath,
                                                    sourcePathArrays[pNum])))
                                {
                                    Task.WaitAll(makeDirectoryTask);
                                }
                            }

                            Console.WriteLine(DateTime.Now + " : Source Path : {0} to Destination Path : {1}",
                                sourcePathArrays[pNum], desPath);
                            Task mvTask =
                                Task.Factory.StartNew(
                                    () =>
                                        CallMoveTask(daysCount * -1, sourcePathArrays[pNum], desPath,
                                            sourcePathArrays[pNum], excludePaths));
                            Task.WaitAll(mvTask);

                            if (ConfigurationManager.AppSettings.Get("Zip") == "1")
                            {
                                var zipPath = "";
                                try
                                {
                                    string tempDaysText = "";
                                    if (ConfigurationManager.AppSettings.Get("ShowCounterDays") == "1") tempDaysText = "_" + ConfigurationManager.AppSettings.Get("CounterDays") + "Days";

                                    zipPath = destinationPathArrays[pNum] + ConfigurationManager.AppSettings.Get("PreBackupName") + DateTime.Now.ToString(ConfigurationManager.AppSettings.Get("DateFormat")) + tempDaysText + ".Zip";
                                    ZipFile.CreateFromDirectory(desPath, zipPath);

                                    if (ConfigurationManager.AppSettings.Get("TestZip") == "1")
                                        ZipFile.OpenRead(zipPath);
                                }
                                catch (Exception)
                                {
                                    Console.WriteLine(DateTime.Now + " : {0}  zip file is corrupted !!!", zipPath);
                                }

                            }

                        }
                    }
                    else
                    {
                        Console.WriteLine(DateTime.Now + " : Source Paths {0} and Destination Paths {1} are not equal !!!", sourcePathArrays.Length, destinationPathArrays.Length);
                    }

                    Console.WriteLine(DateTime.Now + " : Count Files in the folder : {0} ", AllFileCounter);
                    Console.WriteLine(DateTime.Now + " : Count Moved Files : {0} ", FunctionFileCounter);

                    Console.WriteLine(DateTime.Now + " : Moved Files Task Successfully Ended !");
                    break;

                case "rm":
                    Console.WriteLine(DateTime.Now + " : Remove Files Task Start");

                    Task rmTask = Task.Factory.StartNew(() => CallRemoveTask(daysCount * -1, sourcePath, sourcePath));
                    Task.WaitAll(rmTask);

                    Console.WriteLine(DateTime.Now + " : Count Files in the folder : {0} ", AllFileCounter);
                    Console.WriteLine(DateTime.Now + " : Count Removed Files : {0} ", FunctionFileCounter);

                    Console.WriteLine(DateTime.Now + " : Remove Files Task Successfully Ended !");
                    break;

                case "sf":
                    Console.WriteLine(DateTime.Now + " : Size of Folder Task Start");

                    Task sfTask = Task.Factory.StartNew(() => CallSizeTask(daysCount * -1, sourcePath, sourcePath));
                    Task.WaitAll(sfTask);
                    decimal fsfd = (decimal)SizeOfFolder / 1073741824;
                    Console.WriteLine(DateTime.Now + " : Size Of Folder is : {0} GB ", Math.Round(fsfd, 3));

                    Console.WriteLine(DateTime.Now + " : Size of Folder Task Successfully Ended !");
                    break;

                case "md":
                    Console.WriteLine(DateTime.Now + " : Make Directory Task Start");

                    Task mdTask = Task.Factory.StartNew(() => CallMakeDirTask(sourcePath, destinationPath, sourcePath));
                    Task.WaitAll(mdTask);
                    Console.WriteLine(DateTime.Now + " : Make Directory Count : {0} ", FunctionFileCounter);

                    Console.WriteLine(DateTime.Now + " : Make Directory Task Successfully Ended !");
                    break;

                case "rd":
                    Console.WriteLine(DateTime.Now + " : Remove Directory Task Start");

                    Task rdTask = Task.Factory.StartNew(() => CallRemoveDirTask(daysCount * -1, sourcePath, sourcePath));
                    Task.WaitAll(rdTask);

                    Console.WriteLine(DateTime.Now + " : Count Directory in the Path : {0} ", AllFileCounter);
                    Console.WriteLine(DateTime.Now + " : Count Directory : {0} ", FunctionFileCounter);

                    Console.WriteLine(DateTime.Now + " : Remove Folder Task Successfully Ended !");
                    break;

                default:
                    actionflag = false;
                    Console.WriteLine(DateTime.Now + " : Action Type is Wrong !!!");
                    break;
            }

            Console.SetOut(oldOut);
            writer.Close();
            ostrm.Close();

            if (SizeOfFolder != 0) { decimal sfd = (decimal)SizeOfFolder / 1073741824; Console.WriteLine(DateTime.Now + " : Size Of Folder is : {0} GB ", Math.Round(sfd, 3)); }
            if (FunctionFileCounter != 0) { Console.WriteLine(DateTime.Now + " : Count Files effected is : {0} ", FunctionFileCounter); }
            if (AllFileCounter != 0) { Console.WriteLine(DateTime.Now + " : Count Files in the folder : {0} ", AllFileCounter); }

            if (actionflag == false)
            {
                Console.WriteLine(DateTime.Now + " : Action Type is Wrong !!! But Log File Saved.");
            }
            else
            {
                Console.WriteLine(DateTime.Now + " : Jobs Done And Log File Saved.");
            }
        }

        public static void CallCopyTask(int days, string spath, string dpath, string cpath, string excludePaths)
        {
            DateTime morethanNDays = DateTime.Now.AddDays(days);

            var logpath = "./appLogCopy_" + DateTime.Today.ToString("yyyy.MM.dd") + ".txt";
            var failedlogpath = "./appLogFailedCopy_" + DateTime.Today.ToString("yyyy.MM.dd") + ".txt";


            if (cpath == spath)
            {
                try
                {
                    Functionlog = !File.Exists(logpath) ? new StreamWriter(logpath) : File.AppendText(logpath);

                    FailedFunctionlog = !File.Exists(failedlogpath) ? new StreamWriter(failedlogpath) : File.AppendText(failedlogpath);
                }
                catch (Exception e)
                {
                    Console.WriteLine(DateTime.Now + " : Cannot open {0} or {1} for writing", logpath, failedlogpath);
                    Console.WriteLine(e.Message);
                    return;
                }
            }

            try
            {
                string[] excludePathArrays = { };

                if (excludePaths != "")
                {
                    excludePathArrays = excludePaths.Split(',');
                }

                foreach (string nextPath in Directory.GetDirectories(cpath).Where(d => !excludePathArrays.Any(x => d.StartsWith(x, StringComparison.OrdinalIgnoreCase))))
                {
                    foreach (var file in Directory.GetFiles(nextPath, "*.*"))
                    {
                        AllFileCounter++;
                        FileInfo fi = new FileInfo(file);
                        var despath = "";
                        try
                        {
                            //FileInfo fi = new FileInfo(file);
                            if (fi.LastWriteTime < morethanNDays)
                            {
                                despath = dpath + nextPath.Substring(spath.Length) + "\\" + fi.Name;

                                var dirPath = despath.Substring(0, despath.LastIndexOf("\\", StringComparison.Ordinal) + 1);
                                Directory.CreateDirectory(dirPath);

                                //Copylog.WriteLineAsync(string.Format(DateTime.Now + " : nextpath = {0} ", fi.Name));
                                //Copylog.WriteLineAsync(string.Format(DateTime.Now + " : nextpath = {0} ", nextPath ));
                                //Copylog.WriteLineAsync(string.Format(DateTime.Now + " : despath = {0} ", despath));

                                // Ensure that the target does not exist.
                                if (File.Exists(despath))
                                    File.Delete(despath);

                                // Copy the file.
                                File.Copy(nextPath + "\\" + fi.Name, despath, true);

                                var logtext = string.Format(DateTime.Now + " : Copy file From : {0} To : {1}", nextPath + "\\" + fi.Name, despath);

                                Console.WriteLine(logtext);
                                Functionlog.WriteLineAsync(logtext);

                                FunctionFileCounter++;
                            }
                        }
                        catch (Exception ex)
                        {
                            var logtext = string.Format(DateTime.Now + " : Failed Copy file From : {0} To : {1} Reason is : {2} ", nextPath + "\\" + fi.Name, despath, ex);
                            Console.WriteLine(logtext);
                            FailedFunctionlog.WriteLine(logtext);
                            //ignore;
                        }

                    }
                    CallCopyTask(days, spath, dpath, nextPath, excludePaths);
                }
            }
            catch (Exception excpt)
            {
                var logtext = string.Format(DateTime.Now + " : Error = " + excpt.Message);
                Console.WriteLine(logtext);
                FailedFunctionlog.WriteLine(logtext);
            }
        }

        public static void CallMoveTask(int days, string spath, string dpath, string cpath, string excludePaths)
        {
            DateTime morethanNDays = DateTime.Now.AddDays(days);

            var logpath = "./appLogMove_" + DateTime.Today.ToString("yyyy.MM.dd") + ".txt";
            var failedlogpath = "./appLogFailedMove_" + DateTime.Today.ToString("yyyy.MM.dd") + ".txt";


            if (cpath == spath)
            {
                try
                {
                    Functionlog = !File.Exists(logpath) ? new StreamWriter(logpath) : File.AppendText(logpath);

                    FailedFunctionlog = !File.Exists(failedlogpath) ? new StreamWriter(failedlogpath) : File.AppendText(failedlogpath);
                }
                catch (Exception e)
                {
                    Console.WriteLine(DateTime.Now + " : Cannot open {0} or {1} for writing", logpath, failedlogpath);
                    Console.WriteLine(e.Message);
                    return;
                }
            }

            try
            {
                //var excludeflag = false;
                //var exculdefound = false;
                string[] excludePathArrays = { };

                if (excludePaths != "")
                {
                    excludePathArrays = excludePaths.Split(',');
                    //excludeflag = true;
                }

                foreach (string nextPath in Directory.GetDirectories(cpath).Where(d => !excludePathArrays.Any(x => d.StartsWith(x, StringComparison.OrdinalIgnoreCase))))
                {
                    // if (excludeflag)
                    // {
                    //     foreach (var expath in excludePathArrays)
                    //     {
                    //         if (nextPath == expath)
                    //         {
                    //             exculdefound = true;
                    //             break;
                    //         }
                    //     }
                    //     if (exculdefound)
                    //     {
                    //         var logtext = string.Format(DateTime.Now + " : Ignored Directory From this path : {0} ", nextPath);
                    //         Console.WriteLine(logtext);
                    //         Functionlog.WriteLineAsync(logtext);
                    //         continue;
                    //     }
                    //}

                    foreach (var file in Directory.GetFiles(nextPath, "*.*"))
                    {
                        AllFileCounter++;
                        FileInfo fi = new FileInfo(file);
                        var despath = "";
                        try
                        {
                            //var logfiletext = string.Format(DateTime.Now + " : Path file : {0} Date : {1} < Date {2}", nextPath + "\\" + fi.Name, fi.LastWriteTime, morethanNDays);
                            //Console.WriteLine(logfiletext);

                            //FileInfo fi = new FileInfo(file);
                            if (fi.LastWriteTime < morethanNDays)
                            {
                                despath = dpath + nextPath.Substring(spath.Length) + "\\" + fi.Name;

                                var dirPath = despath.Substring(0, despath.LastIndexOf("\\", StringComparison.Ordinal) + 1);
                                Directory.CreateDirectory(dirPath);

                                // Ensure that the target does not exist.
                                if (File.Exists(despath))
                                    File.Delete(despath);

                                // Move the file.
                                File.Move(nextPath + "\\" + fi.Name, despath);

                                var logtext = string.Format(DateTime.Now + " : Move file From : {0} To : {1}", nextPath + "\\" + fi.Name, despath);

                                Console.WriteLine(logtext);
                                Functionlog.WriteLineAsync(logtext);

                                FunctionFileCounter++;
                            }
                        }
                        catch (Exception ex)
                        {
                            var logtext = string.Format(DateTime.Now + " : Failed Move file From : {0} To : {1}  Reason is : {2}", nextPath + "\\" + fi.Name, despath, ex);
                            Console.WriteLine(logtext);
                            FailedFunctionlog.WriteLine(logtext);
                        }

                    }
                    CallMoveTask(days, spath, dpath, nextPath, excludePaths);
                }
            }
            catch (Exception excpt)
            {
                var logtext = string.Format(DateTime.Now + " : Error = " + excpt.Message);
                Console.WriteLine(logtext);
                FailedFunctionlog.WriteLine(logtext);
            }
        }

        public static void CallRemoveTask(int days, string spath, string cpath)
        {
            DateTime morethanNDays = DateTime.Now.AddDays(days);

            var logpath = "./appLogRemove_" + DateTime.Today.ToString("yyyy.MM.dd") + ".txt";
            var failedlogpath = "./appLogFailedRemove_" + DateTime.Today.ToString("yyyy.MM.dd") + ".txt";


            if (cpath == spath)
            {
                try
                {
                    Functionlog = !File.Exists(logpath) ? new StreamWriter(logpath) : File.AppendText(logpath);

                    FailedFunctionlog = !File.Exists(failedlogpath) ? new StreamWriter(failedlogpath) : File.AppendText(failedlogpath);
                }
                catch (Exception e)
                {
                    Console.WriteLine(DateTime.Now + " : Cannot open {0} or {1} for writing", logpath, failedlogpath);
                    Console.WriteLine(e.Message);
                    return;
                }
            }

            try
            {
                foreach (string nextPath in Directory.GetDirectories(cpath))
                {
                    foreach (var file in Directory.GetFiles(nextPath, "*.*"))
                    {
                        AllFileCounter++;
                        FileInfo fi = new FileInfo(file);
                        string fpath = "";
                        try
                        {
                            if (fi.LastWriteTime < morethanNDays)
                            {
                                fpath = nextPath + "\\" + fi.Name;

                                //Delete File
                                fi.Delete();

                                var logtext = string.Format(DateTime.Now + " : Remove file : {0} ", fpath);

                                Console.WriteLine(logtext);
                                Functionlog.WriteLineAsync(logtext);

                                FunctionFileCounter++;
                            }
                        }
                        catch (Exception ex)
                        {
                            var logtext = string.Format(DateTime.Now + " : Failed Remove file : {0}  Reason is : {1}", fpath, ex);
                            Console.WriteLine(logtext);
                            FailedFunctionlog.WriteLine(logtext);
                        }

                    }
                    CallRemoveTask(days, spath, nextPath);
                }
            }
            catch (Exception excpt)
            {
                var logtext = string.Format(DateTime.Now + " : Error = " + excpt.Message);
                Console.WriteLine(logtext);
                FailedFunctionlog.WriteLine(logtext);
            }
        }

        public static void CallSizeTask(int days, string spath, string cpath)
        {
            DateTime morethanNDays = DateTime.Now.AddDays(days);

            try
            {
                foreach (string nextPath in Directory.GetDirectories(cpath))
                {
                    foreach (var file in Directory.GetFiles(nextPath, "*.*"))
                    {
                        AllFileCounter++;
                        FileInfo fi = new FileInfo(file);
                        try
                        {
                            if (fi.LastWriteTime < morethanNDays)
                            {
                                SizeOfFolder += fi.Length;
                            }
                        }
                        catch (Exception)
                        {
                            var logtext = string.Format(DateTime.Now + " : Failed Calculate file Size : {0} ", nextPath + "\\" + fi.Name);
                            Console.WriteLine(logtext);
                        }

                    }
                    CallSizeTask(days, spath, nextPath);
                }
            }
            catch (Exception excpt)
            {
                var logtext = string.Format(DateTime.Now + " : Error = " + excpt.Message);
                Console.WriteLine(logtext);
            }
        }

        public static void CallMakeDirTask(string spath, string dpath, string cpath)
        {

            try
            {
                foreach (string nextPath in Directory.GetDirectories(cpath))
                {
                    var despath = "";
                    try
                    {
                        despath = dpath + nextPath.Substring(spath.Length) + "\\";

                        var dirPath = despath.Substring(0, despath.LastIndexOf("\\", StringComparison.Ordinal) + 1);
                        Directory.CreateDirectory(dirPath);

                        var logtext = string.Format(DateTime.Now + " : Make Directory : {0} ", dirPath);

                        Console.WriteLine(logtext);
                        FunctionFileCounter++;
                    }
                    catch (Exception)
                    {
                        var logtext = string.Format(DateTime.Now + " : Failed Make Directory : {0} ", despath);
                        Console.WriteLine(logtext);
                    }
                    CallMakeDirTask(spath, dpath, nextPath);
                }
            }
            catch (Exception excpt)
            {
                var logtext = string.Format(DateTime.Now + " : Error = " + excpt.Message);
                Console.WriteLine(logtext);
            }
        }

        public static void CallRemoveDirTask(int days, string spath, string cpath)
        {
            try
            {
                foreach (var directory in Directory.GetDirectories(cpath))
                {
                    CallRemoveDirTask(days, spath, directory);
                    if (Directory.GetFiles(directory).Length == 0 && Directory.GetDirectories(directory).Length == 0)
                    {
                        // Get the creation time of a well-known directory.
                        DateTime dt = Directory.GetCreationTime(Environment.CurrentDirectory);

                        if (DateTime.Now.Subtract(dt).TotalDays > days)
                        {
                            Directory.Delete(directory, false);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                var logtext = string.Format(DateTime.Now + " : Error = " + e.Message);
                Console.WriteLine(logtext);
            }
        }
    }
}
