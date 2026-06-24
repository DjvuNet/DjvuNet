using System;
using DjvuNet.Graphics;

namespace DebugRle
{
    class Program
    {
        static void Main(string[] args)
        {
            try 
            {
                Console.WriteLine("Starting test...");
                Bitmap source = new Bitmap();
                source.Init(2, 64, 0);
                source.Grays = 2;
                Console.WriteLine($"Initialized. Data is null? {source.Data == null}");
                source.Compress();
                Console.WriteLine($"Compressed. Data is null? {source.Data == null}");
                Console.WriteLine($"RleData is null? {source._RleData == null}");
                if (source._RleData != null)
                {
                    Console.WriteLine($"RleData length: {source._RleData.Length}");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Exception: {ex}");
            }
        }
    }
}
