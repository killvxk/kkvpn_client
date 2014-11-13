using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace kkvpn_client
{
    public class AppSettings
    {
        public static string ConfigFile = "config.xml";

        [XmlAttribute("version")]
        public string version = "0.1";
        [XmlElement("SubnetworkName")]
        public string SubnetworkName;
        [XmlElement("PeerName")]
        public string PeerName;
        [XmlElement("SubnetworkAddress")]
        public string SubnetworkAddress;
        [XmlElement("SubnetworkCIDR")]
        public int SubnetworkCIDR;

        public AppSettings()
        {

        }

        public AppSettings(bool defaultValues)
        {
            if (defaultValues)
            {
                SubnetworkName = "Nowa sieć";
                SubnetworkAddress = "10.254.0.0";
                SubnetworkCIDR = 24;
                PeerName = "Użytkownik";
            }
        }

        public void SaveToFile()
        {
            using (StreamWriter sw = new StreamWriter(ConfigFile))
            {
                XmlSerializer xml = new XmlSerializer(typeof(AppSettings));
                xml.Serialize(sw, this);
            }
        }
    }
}
