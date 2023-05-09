using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using NModbus;
using SuitCase_FINAL.data;//config
namespace SuitCase_FINAL
{
    internal class Program
    {
        
        config config = new config();
        
        private int Active, NotActive, HaltTime;//Duration
        private int PassCount, DefectCount, HaltCount = 0;//Count of
        private bool ON, OFF, HaltFlag = false;//ON/OFF status of switch
        private string STATUS = "ON";//Status of system
        private UInt16 DS = 0;//Distance sensor val
        private float TemperatureVal = 0;
        private long TimeTakenRead4012 = 0;
        private long TimeTakenWrite4012 = 0;
        private long TimeTakenWrite4060 = 0;
        private long TimeTakenReadESP = 0;
        private long endReadESP = 0;
        private long endRead4012 = 0;
        private long endWrite4012 = 0;
        private long endWrite4060 = 0;
        private long endProduct = 0;
        Random random = new Random();
        Stopwatch timerA = new Stopwatch();//Active
        Stopwatch timerNA = new Stopwatch();//NotActive
        Stopwatch timerH = new Stopwatch();// Halt Time
        
        
        public Program()//Start running system
        {
            Write4012("ON");
            Write4060("ON");
            RunTime();
        }
        public void Read4012()
        {

            using (TcpClient client = new TcpClient(config.Wise4012address, config.ModBusPort))
            {
                long start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var factory = new ModbusFactory();//container for modbus function services
                IModbusMaster master = factory.CreateMaster(client);
                bool[] coils = master.ReadCoils(0, 0, 2);//address of device,start address,how many
                UInt16[] register = master.ReadInputRegisters(0, 0, 1);
                ON = coils[0];
                OFF = coils[1];
                DS = register[0];
                TimeTakenRead4012 = start - endRead4012;
                endRead4012 = start;

            }
        }
        public void ReadESP()
        {
            using (TcpClient client = new TcpClient(config.ESPaddress, config.ModBusPort))
            {
                long start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var factory = new ModbusFactory();
                IModbusMaster master = factory.CreateMaster(client);
                UInt16[] register = master.ReadHoldingRegisters(0, 0, 2);
                TemperatureVal = float.Parse(register[0] + "." + register[1]);
                TimeTakenReadESP = start - endReadESP;
                endReadESP = start;

            }
        }

        private void Write4012(string x)
        {
            using (TcpClient client = new TcpClient(config.Wise4012address, config.ModBusPort))
            {
                long start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var factory = new ModbusFactory();
                IModbusMaster master = factory.CreateMaster(client);
                if (x == "ON")
                {
                    master.WriteSingleCoil(0, 16, true);
                }
                else if (x == "OFF")
                {
                    master.WriteSingleCoil(0, 16, false);
                }
                else if (x == "HALT")
                {
                    master.WriteSingleCoil(0, 16, false);
                }
                TimeTakenWrite4012 = start - endWrite4012;
                endWrite4012 = start;
            }
        }
        private void Write4060(string x)
        {
            using (TcpClient client = new TcpClient(config.Wise4060address, config.ModBusPort))
            {
                long start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var factory = new ModbusFactory();
                IModbusMaster master = factory.CreateMaster(client);
                if (x == "ON")
                {
                    master.WriteMultipleCoils(0, 17, new bool[3] { false, false, true });
                }
                else if (x == "OFF")
                {
                    master.WriteMultipleCoils(0, 17, new bool[3] { true, false, false });
                }
                else if (x == "HALT")
                {
                    master.WriteMultipleCoils(0, 17, new bool[3] { false, true, false });
                }
                TimeTakenWrite4060 = start - endWrite4060;
                endWrite4060 = start;
            }
        }
        public void ProductCounter()
        {

            double RandomNumber = random.NextDouble();
            long StartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (STATUS == "ON")
            {
                //Console.WriteLine("Start: "+StartTime.ToString()+"  "+"End: "+EndTime.ToString()+"  "+"Result: "+(StartTime-EndTime).ToString());

                if ((StartTime - endProduct) >= 10000)
                {
                    endProduct = StartTime;
                    if (RandomNumber >= 0.5)
                    {
                        PassCount++;
                    }
                    else
                    {
                        DefectCount++;
                    }
                }
            }
        }
        public void RunTime()
        {

            if (STATUS == "ON")
            {
                timerA.Start();
                timerNA.Stop();
                timerH.Stop();
            }
            else if (STATUS == "OFF")
            {
                timerA.Stop();
                timerNA.Start();
                timerH.Stop();
            }

            if (STATUS == "HALT")
            {
                timerA.Stop();
                timerNA.Stop();
                timerH.Start();
            }
            Active = Convert.ToInt32(timerA.Elapsed.TotalSeconds);
            NotActive = Convert.ToInt32(timerNA.Elapsed.TotalSeconds);
            HaltTime = Convert.ToInt32(timerH.Elapsed.TotalSeconds);
        }

        public void Post()//Update constant tags on webaccess
        {
            string jsonString = @"{
                ""Tags"":[{
                    ""Name"":""PassCount"",
                    ""Value"":" + PassCount + @"
                },{
                    ""Name"":""HaltCount"",
                    ""Value"":" + HaltCount + @"
                },{
                    ""Name"":""DefectCount"",
                    ""Value"":" + DefectCount + @"
                },{
                    ""Name"":""Active"",
                    ""Value"":" + Active + @"
                },{
                    ""Name"":""NotActive"",
                    ""Value"":" + NotActive + @"
                },{
                    ""Name"":""TEMP"",
                    ""Value"":" + TemperatureVal + @"
                },{
                    ""Name"":""HaltTime"",
                    ""Value"":" + HaltTime + @"
                }]}";

            try
            {
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("admin" + ":" + "")));
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
                var responseTask = httpClient.PostAsync(config.WebAccessSetTagAPI, content);
                var response = responseTask.Result;
                httpClient.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString() + "\n");
                Console.WriteLine("Failed to post to WA\n");
            }
        }



        private static async Task<int> Main(string[] args)
        {
            Program program = new Program();

            while (true)
            {
                try
                {

                    await Task.Run(() => { });
                    program.Read4012();
                    program.RunTime();
                    program.ReadESP();
                    program.ProductCounter();
                    program.Post();
                    /*
                    Console.WriteLine("On: " + program.ON + "\n" +
                                      "Off: " + program.OFF + "\n" +
                                      "DS:" + program.DS + "\n" +
                                      "Temp:" + program.TemperatureVal + "\n" +
                                      "Active:" + program.Active + "\n" +
                                      "Not Active:" + program.NotActive + "\n" +
                                      "Halt Time:" + program.HaltTime + "\n" +
                                      "Pass Count:" + program.PassCount + "\n" +
                                      "Defect Count:" + program.DefectCount + "\n" +
                                      "Halt Count:" + program.HaltCount + "\n" +
                                      "Time taken Read 4012: " + program.TimeTakenRead4012 + "\n" +
                                      "Time taken Write 4012: " + program.TimeTakenWrite4012 + "\n" +
                                      "Time taken Write 4060: " + program.TimeTakenWrite4060 + "\n" +
                                      "Time taken Read ESP: " + program.TimeTakenReadESP + "\n");*/

                    if (program.DS >= 2000)
                    {
                        program.Write4012("HALT");
                        program.Write4060("HALT");
                        program.STATUS = "HALT";
                        if (!program.HaltFlag)
                        {
                            program.HaltFlag = true;
                            program.HaltCount++;
                        }
                    }
                    if (program.ON == true)
                    {
                        program.Write4012("ON");
                        program.Write4060("ON");
                        program.STATUS = "ON";
                        program.HaltFlag = false;
                    }
                    if (program.OFF == true)
                    {
                        program.Write4012("OFF");
                        program.Write4060("OFF");
                        program.STATUS = "OFF";
                        program.HaltFlag = false;
                    }
                }

                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

        }
    }
}
//Duration of Read/Write functions individually take around 20ms approx
//Cannot connect to modbus server, connecting to wise device individually
//400+ms delay due to if statements?
//NModbus doc https://nmodbus.github.io/api/NModbus.html