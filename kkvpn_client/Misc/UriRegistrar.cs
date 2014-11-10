using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kkvpn_client.Misc
{
    static class UriRegistrar
    {
        static public void RegisterUri(string protocol, string app)
        {
            RegistryKey reg = Registry.ClassesRoot.CreateSubKey(protocol + "\\shell\\open\\command\\");
            reg.SetValue("", "\"" + app + "\" \"%1\"");

            reg = Registry.ClassesRoot.CreateSubKey(protocol);
            reg.SetValue("URL Protocol", "");
        }

        static public void UnregisterUri(string protocol)
        {
            Registry.ClassesRoot.DeleteSubKeyTree(protocol);
        }
    }
}
