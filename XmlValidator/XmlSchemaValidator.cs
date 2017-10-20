using System;
using System.Xml;
using System.Xml.Schema;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace XmlValidator
{
    public class XmlSchemaValidator
    {
        private bool isValidXml = true;
        private string validationError = "";
        private string xmlFilePath = "";
        private string xsdSchemaPath = "";

        public string XsdSchemaPath
        {
            set { this.xsdSchemaPath = value; }
        }
        public string XmlFilePath
        {
            set { this.xmlFilePath = value; }
        }

        public String ValidationError
        {
            get { return this.validationError; }
            set { this.validationError = value; }
        }

        public bool IsValidXml
        {
            get { return this.isValidXml; }
        }

        public XmlSchemaValidator()
        {

        }

        private void ValidationCallBack(object sender, ValidationEventArgs args)
        {
            if (args.Severity == XmlSeverityType.Warning)
            {
                this.ValidationError += args.Message;
                isValidXml = false;
            }
            else
            {
                this.ValidationError += args.Message;
                isValidXml = false;
            }
        }

        public bool Validate(string xmlFileName, XmlSchemaSet schemaSet)
        {
            isValidXml = true;

            XmlSchema compiledSchema = null;
            foreach (XmlSchema schema in schemaSet.Schemas())
            {
                compiledSchema = schema;
            }

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.Schemas.Add(compiledSchema);
            settings.ValidationEventHandler += new ValidationEventHandler(ValidationCallBack);
            settings.ValidationType = ValidationType.Schema;

            XmlReader reader = null;

            try
            {
                reader = XmlReader.Create(xmlFileName, settings);
                while (reader.Read())
                {
                    //Console.WriteLine(reader.Name);
                }
                reader.Close();
            }
            catch (Exception e)
            {
                //Console.WriteLine(e.Message);
                this.ValidationError = e.Message;
                return false;
            }
            finally
            {
                reader = null;
                compiledSchema = null;
                settings = null;
            }
            return isValidXml;
        }
    }
}

