using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPP.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            SamplePackage package = new SamplePackage();

            string header = "type: esp 01 header \nmethod: get\n";
            string data = "this is an pure text data";

            package.SetHeader(SamplePackage.HeaderTextEncoding.GetBytes(header));
            package.SetData(SamplePackage.HeaderTextEncoding.GetBytes(data));

            string datName = "hello.dat";

            using (var fs = new FileStream(datName, FileMode.OpenOrCreate))
            {
                var flushMd = package.CopyTo(fs);

                Console.WriteLine("Header:\t" + verify(flushMd[0]));
                Console.WriteLine("Data:\t" + verify(flushMd[1]));

                fs.Flush();
            }
            Console.WriteLine("write finished");

            using (var fs = File.OpenRead(datName))
            {
                var pkg = SamplePackage.GetPackage(fs, out var hmd5, out var dmd5);

                Console.WriteLine($"Header:({hmd5.Length})\t" + verify(hmd5));
                Console.WriteLine($"Data:({dmd5.Length})\t" + verify(dmd5));
                Console.WriteLine(pkg.GetHeader());
                Console.WriteLine(SamplePackage.HeaderTextEncoding.GetString(pkg.Data));
            }

            Console.WriteLine("-------------------------------");
            Console.WriteLine("finished.");

            Console.ReadKey();
        }

        static string verify(byte[] arr)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < arr.Length; i++)
            {
                sb.Append(arr[i].ToString("X"));
            }
            return sb.ToString().ToLower();
        }

    }
}
