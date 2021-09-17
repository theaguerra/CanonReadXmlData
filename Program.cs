using System;
using System.Threading.Tasks;

namespace CanonXMLReaderApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length > 0)
            {
                XMLReader xmlReader = new XMLReader();
                foreach (string arg in args)
                {
                    await xmlReader.Execute(arg);
                }
            }
        }

        //private static async Task ExecuteXmlTaskAsync(string xmlPath, XMLReader xmlReader)
        //{
        //    await xmlReader.Execute(xmlPath);
        //}

        //private static async Task<int> AsyncConsoleWork()
        //{
        //    // Main body here
        //    return 0;
        //}
    }
}
