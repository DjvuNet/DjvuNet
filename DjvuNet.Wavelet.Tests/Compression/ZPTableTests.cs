using Xunit;
using DjvuNet.Compression;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjvuNet.Compression.Tests
{
    public class ZPTableTests
    {
        [Fact()]
        public void ZPTableTest001()
        {
            ZPTable table = new ZPTable(1, 2, 3, 4);
            Assert.Equal<uint>(1, table.PValue);
            Assert.Equal<uint>(2, table.MValue);
            Assert.Equal<uint>(3, table.Up);
            Assert.Equal<uint>(4, table.Down);

            ZPTable table2 = new ZPTable(ushort.MaxValue, ushort.MaxValue, byte.MaxValue, byte.MaxValue);
            Assert.Equal<uint>(ushort.MaxValue, table2.PValue);
            Assert.Equal<uint>(ushort.MaxValue, table2.MValue);
            Assert.Equal<uint>(byte.MaxValue, table2.Up);
            Assert.Equal<uint>(byte.MaxValue, table2.Down);
        }

        [Fact()]
        public void ZPTableTest002()
        {
            ZPTable table = new ZPTable(1, 2, 3, 4);
            Assert.Equal<uint>(1, table.PValue);
            Assert.Equal<uint>(2, table.MValue);
            Assert.Equal<uint>(3, table.Up);
            Assert.Equal<uint>(4, table.Down);

            table.PValue = 321;
            table.MValue = 7091;
            table.Down = 209;
            table.Up = 103;
            Assert.Equal<uint>(321, table.PValue);
            Assert.Equal<uint>(7091, table.MValue);
            Assert.Equal<uint>(209, table.Down);
            Assert.Equal<uint>(103, table.Up);
        }
    }
}