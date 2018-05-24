using System;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _550
{
    class DbConnectParametrs
    {
        public string DBName { get;  }
        public string ConfigFile { get;  }
        public string ConnectionString { get; }
        public DbConnectParametrs(string dbName,string cfgFile)
        {
            DBName = dbName;
            ConfigFile = cfgFile;
        }
        private void GetConnectionString()
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(ConfigFile);

        }
    }
}
