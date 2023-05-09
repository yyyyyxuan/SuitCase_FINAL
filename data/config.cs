using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuitCase_FINAL.data
{
    internal class config
    {
        public string ESPaddress { get; set; }
        public string Wise4012address;
        public string Wise4060address;
        public int ModBusPort;

        public string WebAccessSetTagAPI;

        public config() {
            this.ESPaddress = "192.168.1.214";
            this.Wise4012address = "192.168.1.103";
            this.Wise4060address = "192.168.1.101";
            this.ModBusPort = 502;

            WebAccessSetTagAPI = "http://localhost/WaWebService/Json/SetTagValue/SuitCase";

        }
    }
   
}
