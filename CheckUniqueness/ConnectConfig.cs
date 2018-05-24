using System;
using System.Xml;

namespace utilites
{
    public class ConnectConfig
    {
        const string ATTR_NAME = "name";
        const string ATTR_CONNECTION_STRING = "connectionString";

        public string DbName { get; }
        public string CfgFile { get; }
        public string ConnectionParametrs { get; set; }

        public ConnectConfig(string dbName, string cfgFile)
        {
            DbName = dbName.Trim().ToLower();
            CfgFile = cfgFile;
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(CfgFile);
            XmlElement xRoot = xmlDocument.DocumentElement;
            foreach (XmlNode xNode in xRoot)
            {
                if (xNode.Attributes.Count > 0)
                {
                    XmlNode attr_name = xNode.Attributes.GetNamedItem(ATTR_NAME);
                    if (attr_name != null)
                    {
                        if (attr_name.Value.Trim().ToLower() == DbName)
                        {
                            XmlNode attr_connStr = xNode.Attributes.GetNamedItem(ATTR_CONNECTION_STRING);
                            if (attr_connStr != null)
                            {
                                ConnectionParametrs = attr_connStr.Value;
                            }
                        }
                    }
                }
            }
        }
    }
}
