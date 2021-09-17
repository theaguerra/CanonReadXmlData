using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CanonXMLReaderApp
{
    public class XMLReader
    {
        private const string XML_ATTRIBUTE = "Name";
        private const string XPATH_PCKTDOCFLD = "/BASELINEEXPORT/PCKTDOC/PCKTDOCFLD";
        private const string XPATH_KTAFLDVAL = "/BASELINEEXPORT/PCKTDOC/KTAFLDVAL";
        private const string XPATH_TBL = "/BASELINEEXPORT/PCKTDOC/TBL/ROW";
        private const string FILE_PATH_ARCHIVE = @"D:\Personal\Career\Job Applications\Canon\Instructions\File Results\ARCHIVE";
        private const string FILE_PATH_ERROR = @"D:\Personal\Career\Job Applications\Canon\Instructions\File Results\ERROR";
        private const string FILE_PATH_OUTPUT = @"D:\Personal\Career\Job Applications\Canon\Instructions\File Results\OUTPUT";
        private const string FILENAME = "CanonExportedData";

        private IReadOnlyDictionary<string, Tuple<string, int, char>> _rules = new Dictionary<string, Tuple<string, int, char>>
       {
            {"state", Tuple.Create("#00", 3, 'r')},
            {"createdat", Tuple.Create("#00", 18, 'r')},
            {"frickform", Tuple.Create("#00", 3, 'l')},
            {"documentid", Tuple.Create("#04", 8, 'n')},
            {"firstname", Tuple.Create("#07", 15, 'r')},
            {"middleinitial", Tuple.Create("#07", 1, 'n')},
            {"lastname", Tuple.Create("#07", 30, 'r')},
            {"ssn", Tuple.Create("#08", 9, 'n')},
            {"amount", Tuple.Create("#09", 1, 'r')}
        };

        private IReadOnlyDictionary<int, Tuple<string, string>> _parentRowIndList = new SortedDictionary<int, Tuple<string, string>>
        {
            {1, Tuple.Create("#03", "01")},
            {2, Tuple.Create("#04", "DocumentID")},
            {3, Tuple.Create("#05", "           ")},
            {4, Tuple.Create("#07", "FirstName|MiddleInitial|LastName")},
            {5, Tuple.Create("#08", "SSN")},
            {6, Tuple.Create("#09", "Amount")},
            {7, Tuple.Create("#12", "           ")}
        };

        public XMLReader()
        {
        }

        public async Task Execute(string xmlFilePath)
        {
            string uniqueKey = String.Format("{0}_{1}", DateTime.Now.ToString("MMddyyyyhhmmss"), new Random().Next());
            bool isSuccess = true;
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(xmlFilePath);

                CanonExportData data = new CanonExportData();
                MapXmlToCanonData(xmlDoc, XPATH_PCKTDOCFLD, data);
                MapXmlToCanonData(xmlDoc, XPATH_KTAFLDVAL, data);

                MapXmlToCanonDataArray(xmlDoc, data);
                await CreateTextFile(data, uniqueKey);
            }
            catch (Exception ex)
            {
                // Log error
                isSuccess = false;
            }
            finally
            {
                MoveFile(isSuccess, xmlFilePath, uniqueKey);
            }
        }

        private void SetPropertyValue(PropertyInfo pi, string value, object objData)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                if (pi.Name.ToLower() == "amount")
                {
                    pi.SetValue(objData, SetPadding(pi.Name,
                                                    String.Format("{0:0.00}", double.Parse(value))));
                }
                else if (pi.Name.ToLower() == "createdat")
                {
                    pi.SetValue(objData, SetPadding(pi.Name,
                                                    String.Format("{0:MMddyyyy hh:mm:ss}",
                                                       DateTime.ParseExact(value, "MM/dd/yyyy h:mm:ss tt", CultureInfo.InvariantCulture))));
                }
                else if (pi.Name.ToLower() == "ssn")
                {
                    pi.SetValue(objData, SetPadding(pi.Name,
                                                    String.Format("XXXXX{0}", value.Substring(value.Length - 4))));
                }
                else
                {
                    pi.SetValue(objData, SetPadding(pi.Name, value));
                }
            }
            else
            {
                pi.SetValue(objData, SetPadding(pi.Name, string.Empty));
            }
        }

        private string SetPadding(string propName, string valueBeforePad)
        {
            string valueAfterPad = string.Empty;
            if (_rules.ContainsKey(propName.ToLower()))
            {
                int totalLength = _rules[propName.ToLower()].Item2;
                if (propName.ToLower() == "amount")
                {
                    totalLength = valueBeforePad.Length + _rules[propName.ToLower()].Item2;
                }
                switch (_rules[propName.ToLower()].Item3)
                {
                    case 'r':
                        valueAfterPad = valueBeforePad.PadRight(totalLength, ' ');
                        break;
                    case 'l':
                        valueAfterPad = valueBeforePad.PadLeft(totalLength, ' ');
                        break;
                    default:
                        valueAfterPad = valueBeforePad;
                        break;
                }
            }
            return valueAfterPad;
        }

        private void MapXmlToCanonData(XmlDocument xmlDoc, string xmlXPath, CanonExportData data)
        {
            string nodeElement = xmlXPath.Substring(xmlXPath.LastIndexOf('/') + 1);
            XmlNodeList xmlNodeList = xmlDoc.SelectNodes(xmlXPath);
            if (xmlNodeList.Count > 0)
            {
                foreach (XmlNode xmlNode in xmlNodeList)
                {
                    foreach (PropertyInfo pi in typeof(CanonExportData).GetProperties())
                    {
                        if (pi.GetCustomAttribute<XmlNodeName>().Value == nodeElement)
                        {
                            if (xmlNode.Attributes[XML_ATTRIBUTE].Value.ToLower() == pi.Name.ToLower())
                            {
                                SetPropertyValue(pi, xmlNode.InnerText, data);
                            }
                        }
                    }
                }
            }
        }

        private void MapXmlToCanonDataArray(XmlDocument xmlDoc, CanonExportData data)
        {
            XmlNodeList xmlNodeList = xmlDoc.SelectNodes(XPATH_TBL);
            if (xmlNodeList.Count > 0)
            {
                data.PIData = new PersonalInfo[xmlNodeList.Count];
                for (int index = 0; index < xmlNodeList.Count; index++)
                {
                    PersonalInfo person = new PersonalInfo();
                    foreach (XmlNode xmlNodeCol in xmlNodeList[index].ChildNodes)
                    {
                        foreach (PropertyInfo pi in typeof(PersonalInfo).GetProperties())
                        {
                            if (xmlNodeCol.Attributes[XML_ATTRIBUTE].Value.ToLower() == pi.Name.ToLower())
                            {
                                SetPropertyValue(pi, xmlNodeCol.InnerText, person);
                            }
                        }
                    }
                    data.PIData[index] = person;
                }
            }
        }

        private async Task CreateTextFile(CanonExportData data, string uniqueKey)
        {
            StringBuilder sb = new StringBuilder(String.Format("#00: {0} {1}D{2}", data.State, data.CreatedAt, data.FrickForm));
            foreach (PersonalInfo personalInfo in data.PIData)
            {
                foreach (Tuple<string, string> parentRowInd in _parentRowIndList.Values)
                {
                    sb.AppendLine();
                    sb.Append(parentRowInd.Item1 + " ");
                    if (!string.IsNullOrWhiteSpace(parentRowInd.Item2))
                    {
                        string[] props = parentRowInd.Item2.Split('|');
                        foreach (string prop in props)
                        {
                            PropertyInfo? pi = data.GetType().GetProperty(prop);
                            if (pi != null)
                            {
                                sb.Append(pi.GetValue(data));
                            }
                            else
                            {
                                PropertyInfo? personalPi = personalInfo.GetType().GetProperty(prop);
                                if (personalPi != null)
                                {
                                    sb.Append(personalPi.GetValue(personalInfo));
                                }
                                else
                                {
                                    sb.Append(parentRowInd.Item2);
                                }
                            }
                        }
                    }
                    else
                    {
                        sb.Append(parentRowInd.Item2);
                    }
                }
            }

            // Create File to Output
            if (!Directory.Exists(FILE_PATH_OUTPUT))
            {
                Directory.CreateDirectory(FILE_PATH_OUTPUT);
            }
            await File.AppendAllTextAsync(Path.Combine(FILE_PATH_OUTPUT, FILENAME + "_" + uniqueKey + ".txt"), sb.ToString());
        }

        private void MoveFile(bool isSuccess, string xmlFilePath, string uniqueKey)
        {
            try
            {
                if (isSuccess)
                {
                    if (!Directory.Exists(FILE_PATH_ARCHIVE))
                    {
                        Directory.CreateDirectory(FILE_PATH_ARCHIVE);
                    }
                    File.Move(xmlFilePath, Path.Combine(FILE_PATH_ARCHIVE,
                                                        String.Format("{0}_{1}{2}", Path.GetFileNameWithoutExtension(xmlFilePath), uniqueKey, Path.GetExtension(xmlFilePath))));
                }
                else
                {
                    if (!Directory.Exists(FILE_PATH_ERROR))
                    {
                        Directory.CreateDirectory(FILE_PATH_ERROR);
                    }
                    File.Move(xmlFilePath, Path.Combine(FILE_PATH_ERROR, Path.GetFileName(xmlFilePath)));
                }
            }
            catch (Exception ex)
            {
                // log exception
            }
        }
    }
}

