using Xunit;
using DjvuNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using DjvuNet.Compression;
using DjvuNet.Tests.Xunit;

namespace DjvuNet.Tests
{
    public class DjvuWriterTests
    {
        public static IEnumerable<object[]> StringTestData
        {
            get
            {
                List<object[]> retVal = new List<object[]>();

                retVal.Add(new object[] { "免去于革胜的全国社会保障基金理事会副理事长" });
                retVal.Add(new object[] { "재정난이 심해져 조직 내 구조조정과 임금 삭감이" });
                retVal.Add(new object[] { "وتهدف العملية إلى حماية المدنيين ومنع تحركات الحوثيين وقوات الرئيس المخلوع علي عبد الله صالح، وتوسيع وتوطيد التعاون" });
                retVal.Add(new object[] { "กรมศิลปากรได้พิจารณาแล้วเห็นว่า ตาม พ.ร.บ.โบราณสถาน โบราณวัตถุ ศิลปวัตถุ และพิพิธภัณฑสถานแห่งชาติ พ.ศ.2504 แก้ไขเพิ่มเติม" });
                retVal.Add(new object[] { "След като месеци наред отричаше да има подобно намерение, сега Мей сподели" });
                retVal.Add(new object[] { "Die Premierministerin Großbritanniens erhofft sich von Neuwahlen ein stärkeres Mandat für die Verhandlungen mit Brüssel" });
                retVal.Add(new object[] { "Παρά τις προβλέψεις για άμεσο οικονομικό κίνδυνο, μετά το δημοψήφισμα του περασμένου καλοκαιριού είδαμε ότι η εμπιστοσύνη" });
                retVal.Add(new object[] { "על-פי הערכות, נתניהו לא עודכן על קיום מסיבת העיתונאים של כחלון וגם לא על תוכן התוכנית שהוצגה" });
                retVal.Add(new object[] { "ホテル近くに横浜市の新市庁舎が移転することから「外国人観光客の増加も見込まれるが" });
                retVal.Add(new object[] { "В ходы войны город Идлиб переходил из рук в руки, но в итоге остался под контролем оппозиционеров" });
                retVal.Add(new object[] { "बयान में कहा गया, ‘एनएसए मैकमास्टर ने भारत-अमेरिका के सामरिक रिश्तों पर जोर दिया और भारत के एक" });
                retVal.Add(new object[] { "Mağazalarla ek 300 kişiye istihdam sağlayacaklarının altını çizen Serbes, dolaylı olarak da 1000 kişiye iş yaratılacağını belirtti" });
                retVal.Add(new object[] { "Cả hai đội đều có những thay đổi về đội hình ra sân. Bale vắng mặt nên Isco được đá chính trên hàng công" });
                retVal.Add(new object[] { "Nie wszystko dało się przewidzieć, stąd drobne opóźnienie – tłumaczy Sylwester Puczen, rzecznik Toru Służewiec" });
                retVal.Add(new object[] { "Guðlaugur Þór Þórðarson utanríkisráðherra átti í dag fund með Boris Johnson, utanríkisráðherra Bretlands í Lundúnum þar sem þeir ræddu útgöngu Breta úr Evrópusambandinu og leiðir til að efla samskipti Íslands og Bretlands" });
                retVal.Add(new object[] { "Konservatiivipuolueen kannattajilleen ja toimittajille lähettämässä kirjeessäkin puhutaan pelitermein \"vahvemmasta kädestä\" eli pääministerille halutaan paremmat kortit käteen kun hän lähtee EU" });
                retVal.Add(new object[] { "Det er mye som lykkes for Ap-leder Jonas Gahr Støre. På borgerlig side er samarbeidet gått surt, og kaoset truer. Meningsmålingene har gitt Ap" });
                retVal.Add(new object[] { "Em carta divulgada na segunda-feira (17), o ex-presidente da Câmara Eduardo Cunha rebatou as afirmações do presidente" });
                return retVal;
            }
        }

        public static IEnumerable<object[]> StringEncodingTestData
        {
            get
            {
                List<object[]> retVal = new List<object[]>();
                UTF32Encoding encoding = new UTF32Encoding(false, false);
                UTF32Encoding encoding2 = new UTF32Encoding(true, true);

                retVal.Add(new object[] { "免去于革胜的全国社会保障基金理事会副理事长", encoding });
                retVal.Add(new object[] { "재정난이 심해져 조직 내 구조조정과 임금 삭감이", encoding });
                retVal.Add(new object[] { "وتهدف العملية إلى حماية المدنيين ومنع تحركات الحوثيين وقوات الرئيس المخلوع علي عبد الله صالح، وتوسيع وتوطيد التعاون", encoding });
                retVal.Add(new object[] { "กรมศิลปากรได้พิจารณาแล้วเห็นว่า ตาม พ.ร.บ.โบราณสถาน โบราณวัตถุ ศิลปวัตถุ และพิพิธภัณฑสถานแห่งชาติ พ.ศ.2504 แก้ไขเพิ่มเติม", encoding });
                retVal.Add(new object[] { "След като месеци наред отричаше да има подобно намерение, сега Мей сподели", encoding });
                retVal.Add(new object[] { "Die Premierministerin Großbritanniens erhofft sich von Neuwahlen ein stärkeres Mandat für die Verhandlungen mit Brüssel", encoding });
                retVal.Add(new object[] { "Παρά τις προβλέψεις για άμεσο οικονομικό κίνδυνο, μετά το δημοψήφισμα του περασμένου καλοκαιριού είδαμε ότι η εμπιστοσύνη", encoding });
                retVal.Add(new object[] { "על-פי הערכות, נתניהו לא עודכן על קיום מסיבת העיתונאים של כחלון וגם לא על תוכן התוכנית שהוצגה", encoding });
                retVal.Add(new object[] { "ホテル近くに横浜市の新市庁舎が移転することから「外国人観光客の増加も見込まれるが", encoding });
                retVal.Add(new object[] { "В ходы войны город Идлиб переходил из рук в руки, но в итоге остался под контролем оппозиционеров", encoding });
                retVal.Add(new object[] { "बयान में कहा गया, ‘एनएसए मैकमास्टर ने भारत-अमेरिका के सामरिक रिश्तों पर जोर दिया और भारत के एक", encoding });
                retVal.Add(new object[] { "Mağazalarla ek 300 kişiye istihdam sağlayacaklarının altını çizen Serbes, dolaylı olarak da 1000 kişiye iş yaratılacağını belirtti", encoding });
                retVal.Add(new object[] { "Cả hai đội đều có những thay đổi về đội hình ra sân. Bale vắng mặt nên Isco được đá chính trên hàng công", encoding });
                retVal.Add(new object[] { "Nie wszystko dało się przewidzieć, stąd drobne opóźnienie – tłumaczy Sylwester Puczen, rzecznik Toru Służewiec", encoding });
                retVal.Add(new object[] { "Guðlaugur Þór Þórðarson utanríkisráðherra átti í dag fund með Boris Johnson, utanríkisráðherra Bretlands í Lundúnum þar sem þeir ræddu útgöngu Breta úr Evrópusambandinu og leiðir til að efla samskipti Íslands og Bretlands", encoding });
                retVal.Add(new object[] { "Konservatiivipuolueen kannattajilleen ja toimittajille lähettämässä kirjeessäkin puhutaan pelitermein \"vahvemmasta kädestä\" eli pääministerille halutaan paremmat kortit käteen kun hän lähtee EU", encoding });
                retVal.Add(new object[] { "Det er mye som lykkes for Ap-leder Jonas Gahr Støre. På borgerlig side er samarbeidet gått surt, og kaoset truer. Meningsmålingene har gitt Ap", encoding });
                retVal.Add(new object[] { "Em carta divulgada na segunda-feira (17), o ex-presidente da Câmara Eduardo Cunha rebatou as afirmações do presidente", encoding });

                retVal.Add(new object[] { "免去于革胜的全国社会保障基金理事会副理事长", encoding2 });
                retVal.Add(new object[] { "재정난이 심해져 조직 내 구조조정과 임금 삭감이", encoding2 });
                retVal.Add(new object[] { "وتهدف العملية إلى حماية المدنيين ومنع تحركات الحوثيين وقوات الرئيس المخلوع علي عبد الله صالح، وتوسيع وتوطيد التعاون", encoding2 });
                retVal.Add(new object[] { "กรมศิลปากรได้พิจารณาแล้วเห็นว่า ตาม พ.ร.บ.โบราณสถาน โบราณวัตถุ ศิลปวัตถุ และพิพิธภัณฑสถานแห่งชาติ พ.ศ.2504 แก้ไขเพิ่มเติม", encoding2 });
                retVal.Add(new object[] { "След като месеци наред отричаше да има подобно намерение, сега Мей сподели", encoding2 });
                retVal.Add(new object[] { "Die Premierministerin Großbritanniens erhofft sich von Neuwahlen ein stärkeres Mandat für die Verhandlungen mit Brüssel", encoding2 });
                retVal.Add(new object[] { "Παρά τις προβλέψεις για άμεσο οικονομικό κίνδυνο, μετά το δημοψήφισμα του περασμένου καλοκαιριού είδαμε ότι η εμπιστοσύνη", encoding2 });
                retVal.Add(new object[] { "על-פי הערכות, נתניהו לא עודכן על קיום מסיבת העיתונאים של כחלון וגם לא על תוכן התוכנית שהוצגה", encoding2 });
                retVal.Add(new object[] { "ホテル近くに横浜市の新市庁舎が移転することから「外国人観光客の増加も見込まれるが", encoding2 });
                retVal.Add(new object[] { "В ходы войны город Идлиб переходил из рук в руки, но в итоге остался под контролем оппозиционеров", encoding2 });
                retVal.Add(new object[] { "बयान में कहा गया, ‘एनएसए मैकमास्टर ने भारत-अमेरिका के सामरिक रिश्तों पर जोर दिया और भारत के एक", encoding2 });
                retVal.Add(new object[] { "Mağazalarla ek 300 kişiye istihdam sağlayacaklarının altını çizen Serbes, dolaylı olarak da 1000 kişiye iş yaratılacağını belirtti", encoding2 });
                retVal.Add(new object[] { "Cả hai đội đều có những thay đổi về đội hình ra sân. Bale vắng mặt nên Isco được đá chính trên hàng công", encoding2 });
                retVal.Add(new object[] { "Nie wszystko dało się przewidzieć, stąd drobne opóźnienie – tłumaczy Sylwester Puczen, rzecznik Toru Służewiec", encoding2 });
                retVal.Add(new object[] { "Guðlaugur Þór Þórðarson utanríkisráðherra átti í dag fund með Boris Johnson, utanríkisráðherra Bretlands í Lundúnum þar sem þeir ræddu útgöngu Breta úr Evrópusambandinu og leiðir til að efla samskipti Íslands og Bretlands", encoding2 });
                retVal.Add(new object[] { "Konservatiivipuolueen kannattajilleen ja toimittajille lähettämässä kirjeessäkin puhutaan pelitermein \"vahvemmasta kädestä\" eli pääministerille halutaan paremmat kortit käteen kun hän lähtee EU", encoding2 });
                retVal.Add(new object[] { "Det er mye som lykkes for Ap-leder Jonas Gahr Støre. På borgerlig side er samarbeidet gått surt, og kaoset truer. Meningsmålingene har gitt Ap", encoding2 });
                retVal.Add(new object[] { "Em carta divulgada na segunda-feira (17), o ex-presidente da Câmara Eduardo Cunha rebatou as afirmações do presidente", encoding2 });

                return retVal;
            }
        }

        public static IEnumerable<object[]> UInt24TestData
        {
            get
            {
                List<object[]> retVal = new List<object[]>();

                retVal.Add(new object[] { 0x00800001u });
                retVal.Add(new object[] { 0x00ffffffu });
                retVal.Add(new object[] { 0x00000001u });
                retVal.Add(new object[] { 0x00800000u });
                retVal.Add(new object[] { 0x007fff7fu });

                return retVal;
            }
        }

        public static IEnumerable<object[]> Int24TestData
        {
            get
            {
                List<object[]> retVal = new List<object[]>();

                retVal.Add(new object[] { 1 });
                retVal.Add(new object[] { -1 });
                retVal.Add(new object[] { Int16.MaxValue * sbyte.MaxValue });
                retVal.Add(new object[] { Int16.MinValue * sbyte.MaxValue });
                retVal.Add(new object[] { Int16.MaxValue });
                retVal.Add(new object[] { Int16.MinValue });
                retVal.Add(new object[] { sbyte.MaxValue });
                retVal.Add(new object[] { sbyte.MinValue });

                return retVal;
            }
        }

        public static IEnumerable<object[]> UInt16TestData
        {
            get
            {
                List<object[]> retVal = new List<object[]>();

                retVal.Add(new object[] { 0x8001u });
                retVal.Add(new object[] { 0xffffu });
                retVal.Add(new object[] { 0x0001u });
                retVal.Add(new object[] { 0x8f8fu });
                retVal.Add(new object[] { 0x7f7fu });

                return retVal;
            }
        }

        public static IEnumerable<object[]> Int16TestData
        {
            get
            {
                List<object[]> retVal = new List<object[]>();

                retVal.Add(new object[] { -1 });
                retVal.Add(new object[] { 1 });
                retVal.Add(new object[] { 0 });
                retVal.Add(new object[] { sbyte.MaxValue });
                retVal.Add(new object[] { sbyte.MinValue });
                retVal.Add(new object[] { Int16.MaxValue });
                retVal.Add(new object[] { Int16.MinValue });

                return retVal;
            }
        }

        public static IEnumerable<object[]> Int32TestData
        {
            get
            {
                List<object[]> retVal = new List<object[]>();

                retVal.Add(new object[] { -1 });
                retVal.Add(new object[] { 1 });
                retVal.Add(new object[] { 0 });
                retVal.Add(new object[] { sbyte.MaxValue });
                retVal.Add(new object[] { sbyte.MinValue });
                retVal.Add(new object[] { Int16.MinValue });
                retVal.Add(new object[] { Int16.MaxValue });
                retVal.Add(new object[] { Int32.MaxValue });
                retVal.Add(new object[] { Int32.MinValue });

                return retVal;
            }
        }

        public static IEnumerable<object[]> UInt32TestData
        {
            get
            {
                List<object[]> retVal = new List<object[]>();

                retVal.Add(new object[] { 0x00000001u });
                retVal.Add(new object[] { 0x00008100u });
                retVal.Add(new object[] { 0x00800100u });
                retVal.Add(new object[] { 0x80000100u });
                retVal.Add(new object[] { 0x80000001u });
                retVal.Add(new object[] { UInt32.MaxValue });
                retVal.Add(new object[] { UInt32.MinValue });

                return retVal;
            }
        }

        public static IEnumerable<object[]> Int64TestData
        {
            get
            {
                List<object[]> retVal = new List<object[]>();

                retVal.Add(new object[] { -1 });
                retVal.Add(new object[] { 1 });
                retVal.Add(new object[] { 0 });
                retVal.Add(new object[] { Int64.MaxValue });
                retVal.Add(new object[] { Int64.MinValue });
                retVal.Add(new object[] { Int32.MaxValue });
                retVal.Add(new object[] { Int32.MinValue });
                retVal.Add(new object[] { Int16.MaxValue });
                retVal.Add(new object[] { Int16.MinValue });
                retVal.Add(new object[] { sbyte.MaxValue });
                retVal.Add(new object[] { sbyte.MinValue });

                return retVal;
            }
        }

        public static IEnumerable<object[]> UInt64TestData
        {
            get
            {
                List<object[]> retVal = new List<object[]>();

                retVal.Add(new object[] { 0x0000000100000001u });
                retVal.Add(new object[] { 0x0000000100008100u });
                retVal.Add(new object[] { 0x8000000100800100u });
                retVal.Add(new object[] { 0x0000000180000100u });
                retVal.Add(new object[] { 0x8000000180000001u });
                retVal.Add(new object[] { UInt64.MaxValue });
                retVal.Add(new object[] { UInt64.MinValue });

                return retVal;
            }
        }

        [Fact()]
        public void DjvuWriterTest()
        {
            DjvuWriter writer = null;
            using (MemoryStream stream = new MemoryStream())
            using(writer = new DjvuWriter(stream))
            {
                Assert.NotNull(writer);
                Assert.IsType<DjvuWriter>(writer);
                var ibw = writer as IBinaryWriter;
                Assert.NotNull(ibw);
                var idw = ibw as IDjvuWriter;
                Assert.NotNull(idw);
                Assert.NotNull(writer.BaseStream);
                Assert.Same(stream, writer.BaseStream); 
            }
        }

        [Fact()]
        public void DjvuWriterTest1()
        {
            string filePath = Path.GetTempFileName();
            DjvuWriter writer = null;
            try
            {
                using (writer = new DjvuWriter(filePath))
                {
                    Assert.NotNull(writer);
                    Assert.IsType<DjvuWriter>(writer);
                    var ibw = writer as IBinaryWriter;
                    Assert.NotNull(ibw);
                    var idw = ibw as IDjvuWriter;
                    Assert.NotNull(idw);
                    Assert.NotNull(writer.BaseStream);
                    Assert.IsType<FileStream>(writer.BaseStream);
                }
            }
            finally
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
        }

        [Fact()]
        public void WriteJPEGImageTest()
        {
            DjvuWriter writer = null;

            using (MemoryStream stream = new MemoryStream())
            using (writer = new DjvuWriter(stream))
            {
                Assert.Throws<NotImplementedException>(() => writer.WriteJPEGImage(null));
            }
        }

        [Fact()]
        public void GetBZZEncodedWriterTest()
        {
            DjvuWriter writer = null;

            using (MemoryStream stream = new MemoryStream())
            using (writer = new DjvuWriter(stream))
            {
                BzzWriter bzzWriter = null;
                bzzWriter = writer.GetBZZEncodedWriter(4096);
                Assert.NotNull(bzzWriter);
                Assert.IsType<BzzWriter>(bzzWriter);
            }
        }

        [Fact()]
        public void GetBZZEncodedWriterTest1()
        {
            DjvuWriter writer = null;

            using (MemoryStream stream = new MemoryStream())
            using (writer = new DjvuWriter(stream))
            {
                BzzWriter bzzWriter = null;
                bzzWriter = writer.GetBZZEncodedWriter();
                Assert.NotNull(bzzWriter);
                Assert.IsType<BzzWriter>(bzzWriter);
            }
        }

#if _APPVEYOR
        [Theory]
#else 
        [DjvuTheory]
#endif
        [MemberData(nameof(UInt24TestData))]
        public void WriteUInt24BigEndian_Theory(uint test)
        {
            DjvuWriter writer = null;
            using (MemoryStream stream = new MemoryStream())
            using (writer = new DjvuWriter(stream))
            {
                writer.WriteUInt24BigEndian(test);
                byte[] buffer = stream.GetBuffer();
                byte[] testBuffer = new byte[4];
                Buffer.BlockCopy(buffer, 0, testBuffer, 1, 3);
                Array.Reverse(testBuffer);
                Assert.Equal<uint>(test, BitConverter.ToUInt32(testBuffer, 0));
            }
        }

#if _APPVEYOR
        [Theory]
#else 
        [DjvuTheory]
#endif
        [MemberData(nameof(UInt24TestData))]
        public void WriteInt24BigEndian_Theory(int test)
        {
            DjvuWriter writer = null;
            using (MemoryStream stream = new MemoryStream())
            using (writer = new DjvuWriter(stream))
            {
                writer.WriteInt24BigEndian(test);
                byte[] buffer = stream.GetBuffer();
                byte[] testBuffer = new byte[4];
                Buffer.BlockCopy(buffer, 0, testBuffer, 1, 3);
                Array.Reverse(testBuffer);
                if ((buffer[0] >> 7) == 1)
                    testBuffer[3] = 0xff;
                Assert.Equal<int>(test, BitConverter.ToInt32(testBuffer, 0));
            }
        }

#if _APPVEYOR
        [Theory]
#else 
        [DjvuTheory]
#endif
        [MemberData(nameof(UInt24TestData))]
        public void WriteUInt24_Theory(uint test)
        {
            DjvuWriter writer = null;
            using (MemoryStream stream = new MemoryStream())
            using (writer = new DjvuWriter(stream))
            {
                writer.WriteUInt24(test);
                byte[] buffer = stream.GetBuffer();
                byte[] testBuffer = new byte[4];
                Buffer.BlockCopy(buffer, 0, testBuffer, 0, 3);
                Assert.Equal<uint>(test, BitConverter.ToUInt32(testBuffer, 0));
            }
        }

#if _APPVEYOR
        [Theory]
#else 
        [DjvuTheory]
#endif
        [MemberData(nameof(UInt24TestData))]
        public void WriteInt24_Theory(int test)
        {
            DjvuWriter writer = null;
            using (MemoryStream stream = new MemoryStream())
            using (writer = new DjvuWriter(stream))
            {
                writer.WriteInt24(test);
                byte[] buffer = stream.GetBuffer();
                byte[] testBuffer = new byte[4];
                Buffer.BlockCopy(buffer, 0, testBuffer, 0, 3);
                if ((buffer[2] >> 7) == 1)
                    testBuffer[3] = 0xff;
                Assert.Equal<int>(test, BitConverter.ToInt32(testBuffer, 0));
            }
        }

#if _APPVEYOR
        [Theory]
#else 
        [DjvuTheory]
#endif
        [MemberData(nameof(Int16TestData))]
        public void WriteInt16BigEndian_Theory(short test)
        {
            DjvuWriter writer = null;
            using (MemoryStream stream = new MemoryStream())
            using (writer = new DjvuWriter(stream))
            {
                writer.WriteInt16BigEndian(test);
                byte[] buffer = stream.GetBuffer();
                byte[] testBuffer = new byte[2];
                Buffer.BlockCopy(buffer, 0, testBuffer, 0, 2);
                Array.Reverse(testBuffer);
                Assert.Equal<Int16>(test, BitConverter.ToInt16(testBuffer, 0));
            }
        }

#if _APPVEYOR
        [Theory]
#else 
        [DjvuTheory]
#endif
        [MemberData(nameof(Int32TestData))]
        public void WriteInt32BigEndian_Theory(int test)
        {
            DjvuWriter writer = null;
            using (MemoryStream stream = new MemoryStream())
            using (writer = new DjvuWriter(stream))
            {
                writer.WriteInt32BigEndian(test);
                byte[] buffer = stream.GetBuffer();
                byte[] testBuffer = new byte[4];
                Buffer.BlockCopy(buffer, 0, testBuffer, 0, 4);
                Array.Reverse(testBuffer);
                Assert.Equal<int>(test, BitConverter.ToInt32(testBuffer, 0));
            }
        }

#if _APPVEYOR
        [Theory]
#else 
        [DjvuTheory]
#endif
        [MemberData(nameof(Int64TestData))]
        public void WriteInt64BigEndian_Theory(long test)
        {
            DjvuWriter writer = null;
            using (MemoryStream stream = new MemoryStream())
            using (writer = new DjvuWriter(stream))
            {
                writer.WriteInt64BigEndian(test);
                byte[] buffer = stream.GetBuffer();
                byte[] testBuffer = new byte[8];
                Buffer.BlockCopy(buffer, 0, testBuffer, 0, 8);
                Array.Reverse(testBuffer);
                Assert.Equal<long>(test, BitConverter.ToInt64(testBuffer, 0));
            }
        }

#if _APPVEYOR
        [Theory]
#else 
        [DjvuTheory]
#endif
        [MemberData(nameof(UInt16TestData))]
        public void WriteUInt16BigEndian_Theory(ushort test)
        {
            DjvuWriter writer = null;
            using (MemoryStream stream = new MemoryStream())
            using (writer = new DjvuWriter(stream))
            {
                writer.WriteUInt16BigEndian(test);
                byte[] buffer = stream.GetBuffer();
                byte[] testBuffer = new byte[2];
                Buffer.BlockCopy(buffer, 0, testBuffer, 0, 2);
                Array.Reverse(testBuffer);
                Assert.Equal<UInt16>(test, BitConverter.ToUInt16(testBuffer, 0));
            }
        }

#if _APPVEYOR
        [Theory]
#else 
        [DjvuTheory]
#endif
        [MemberData(nameof(UInt32TestData))]
        public void WriteUInt32BigEndian_Theory(uint test)
        {
            DjvuWriter writer = null;
            using (MemoryStream stream = new MemoryStream())
            using (writer = new DjvuWriter(stream))
            {
                writer.WriteUInt32BigEndian(test);
                byte[] buffer = stream.GetBuffer();
                byte[] testBuffer = new byte[4];
                Buffer.BlockCopy(buffer, 0, testBuffer, 0, 4);
                Array.Reverse(testBuffer);
                Assert.Equal<uint>(test, BitConverter.ToUInt32(testBuffer, 0));
            }
        }

#if _APPVEYOR
        [Theory]
#else 
        [DjvuTheory]
#endif
        [MemberData(nameof(UInt64TestData))]
        public void WriteUInt64BigEndian_Theory(ulong test)
        {
            DjvuWriter writer = null;
            using (MemoryStream stream = new MemoryStream())
            using (writer = new DjvuWriter(stream))
            {
                writer.WriteUInt64BigEndian(test);
                byte[] buffer = stream.GetBuffer();
                byte[] testBuffer = new byte[8];
                Buffer.BlockCopy(buffer, 0, testBuffer, 0, 8);
                Array.Reverse(testBuffer);
                Assert.Equal<ulong>(test, BitConverter.ToUInt64(testBuffer, 0));
            }
        }

#if _APPVEYOR
        [Theory]
#else 
        [DjvuTheory]
#endif
        [MemberData(nameof(StringTestData))]
        public void WriteUTF8String_Theory(string testString)
        {
            DjvuWriter writer = null;

            using (MemoryStream stream = new MemoryStream())
            using (writer = new DjvuWriter(stream))
            {
                long length = writer.WriteUTF8String(testString);
                byte[] buffer = stream.GetBuffer();
                byte[] testBuffer = new byte[length];
                Buffer.BlockCopy(buffer, 0, testBuffer, 0, (int)length);
                UTF8Encoding encoding = new UTF8Encoding(false);
                string result = encoding.GetString(testBuffer);
                Assert.Equal<string>(testString, result);
                Assert.Equal<long>(length, stream.Position);
                Assert.Equal<long>(stream.Position, writer.Position); 
            }
        }

#if _APPVEYOR
        [Theory]
#else 
        [DjvuTheory]
#endif
        [MemberData(nameof(StringTestData))]
        public void WriteUTF7String_Theory(string testString)
        {
            DjvuWriter writer = null;

            using (MemoryStream stream = new MemoryStream())
            using (writer = new DjvuWriter(stream))
            {
                long length = writer.WriteUTF7String(testString);
                byte[] buffer = stream.GetBuffer();
                byte[] testBuffer = new byte[length];
                Buffer.BlockCopy(buffer, 0, testBuffer, 0, (int)length);
                UTF7Encoding encoding = new UTF7Encoding(false);
                string result = encoding.GetString(testBuffer);
                Assert.Equal<string>(testString, result);
                Assert.Equal<long>(length, stream.Position);
                Assert.Equal<long>(stream.Position, writer.Position);
            }
        }

#if _APPVEYOR
        [Theory]
#else 
        [DjvuTheory]
#endif
        [MemberData(nameof(StringTestData))]
        public void WriteString_Theory(String testString)
        {
            DjvuWriter writer = null;

            using (MemoryStream stream = new MemoryStream())
            using (writer = new DjvuWriter(stream))
            {
                long length = writer.WriteString(testString);
                byte[] buffer = stream.GetBuffer();
                byte[] testBuffer = new byte[length];
                Buffer.BlockCopy(buffer, 0, testBuffer, 0, (int)length);
                UnicodeEncoding encoding = new UnicodeEncoding(false, false);
                string result = encoding.GetString(testBuffer);
                Assert.Equal<string>(testString, result);
                Assert.Equal<long>(length, stream.Position);
                Assert.Equal<long>(stream.Position, writer.Position);
            }
        }

#if _APPVEYOR
        [Theory]
#else 
        [DjvuTheory]
#endif
        [MemberData(nameof(StringEncodingTestData))]
        public void WriteStringEncoding_Theory(String testString, Encoding encoding)
        {
            DjvuWriter writer = null;

            using (MemoryStream stream = new MemoryStream())
            using (writer = new DjvuWriter(stream))
            {
                long length = writer.WriteString(testString, encoding);
                byte[] buffer = stream.GetBuffer();
                byte[] testBuffer = new byte[length];
                Buffer.BlockCopy(buffer, 0, testBuffer, 0, (int)length);
                string result = encoding.GetString(testBuffer);
                Assert.Equal<string>(testString, result);
                Assert.Equal<long>(length, stream.Position);
                Assert.Equal<long>(stream.Position, writer.Position);
            }
        }

        [Fact]
        public void WriteStringStringEncodingTest001()
        {
            DjvuWriter writer = null;

            using (MemoryStream stream = new MemoryStream())
            using (writer = new DjvuWriter(stream))
            {
                string testString = null;
                Encoding encoding = null;
                Assert.Throws<ArgumentNullException>("value", () => writer.WriteString(testString, encoding));
            }
        }

        [Fact]
        public void WriteStringStringEncodingTest002()
        {
            DjvuWriter writer = null;

            using (MemoryStream stream = new MemoryStream())
            using (writer = new DjvuWriter(stream))
            {
                string testString = "";
                Encoding encoding = null;
                Assert.Throws<ArgumentNullException>("encoding", () => writer.WriteString(testString, encoding));
            }
        }

        [Fact()]
        public void CloneWriterTest()
        {
            DjvuWriter writer = null;

            using (MemoryStream stream = new MemoryStream())
            using (writer = new DjvuWriter(stream))
            {
                Assert.Throws<NotImplementedException>(() => writer.CloneWriter(4096));
            }
        }

        [Fact()]
        public void CloneWriterTest1()
        {
            DjvuWriter writer = null;

            using (MemoryStream stream = new MemoryStream())
            using (writer = new DjvuWriter(stream))
            {
                Assert.Throws<NotImplementedException>(() => writer.CloneWriter(4096, 4096));
            }
        }

        [Fact()]
        public void ToStringTest()
        {
            DjvuWriter writer = null;

            using (MemoryStream stream = new MemoryStream())
            using (writer = new DjvuWriter(stream))
            {
                string test = null;
                test = writer.ToString();
                Assert.False(String.IsNullOrWhiteSpace(test));
                Assert.Contains(nameof(DjvuWriter), test);
                Assert.Contains("Position: ", test);
                Assert.Contains("Length: ", test);
                Assert.Contains("BaseStream: ", test);
                Assert.Contains(nameof(MemoryStream), test);
            }
        }
    }
}