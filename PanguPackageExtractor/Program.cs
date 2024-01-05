using System.IO;

using Syroot.BinaryData;

namespace PanguPackageExtractor
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Netease Pangu Package (.pg) extractor by Nenkai");
                Console.WriteLine("Usage: <path to res_base_0.pg> <output directory>");
                return;
            }

            if (!File.Exists(args[0]))
            {
                Console.WriteLine("ERROR: Input file does not exist");
                return;
            }

            if (File.Exists(args[1]))
            {
                Console.WriteLine("ERROR: Output directory is a file");
                return;
            }

            var package = new PanguPackage();
            package.Open(args[0]);

            int i = 0;
            foreach (KeyValuePair<string, string> v in package.HashToPath)
            {
                package.ExtractFile(v.Value, args[1]);
                i++;
            }
            
        }
    }

}