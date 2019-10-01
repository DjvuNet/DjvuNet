using Xunit;
using DjvuNet.Wavelet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace DjvuNet.Wavelet.Tests
{
    public class InterWaveBlockTests
    {

        public static void InitializePData(short[][][] pdata, int growthSlope = 1, int baseVal = 0)
        {
            // Fill data blocks with monotonically increasing values
            for (int i = 0; i < pdata.Length; i++)
            {
                pdata[i] = new short[][]
                {
                    new short[16], new short[16], new short[16], new short[16],
                    new short[16], new short[16], new short[16], new short[16],
                    new short[16], new short[16], new short[16], new short[16],
                    new short[16], new short[16], new short[16], new short[16],
                };

                short[][] pdatai = pdata[i];

                for (int j = 0; j < pdatai.Length; j++)
                {
                    pdata[i][j] = new short[16];
                    short[] pdataij = pdata[i][j];

                    for (int k = 0; k < pdataij.Length; k++)
                        pdataij[k] = unchecked((short)(((i * 256) + (j * 16) + k) * growthSlope + baseVal));
                }
            }
        }

        [Fact()]
        public void InterWaveBlockTest()
        {
            InterWaveBlock block = new InterWaveBlock();
            Assert.NotNull(block._PData);
            Assert.NotNull(InterWaveBlock._Zigzagloc);
        }

        [Fact()]
        public void DuplicateTest001()
        {
            InterWaveBlock block = new InterWaveBlock();
            Assert.NotNull(block._PData);
            Assert.NotNull(InterWaveBlock._Zigzagloc);
            block._PData = null;

            var block2 = block.Duplicate();
            Assert.Null(block2);
        }

        [Fact()]
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void DuplicateTest002()
        {
            InterWaveBlock block = new InterWaveBlock();
            Assert.NotNull(block._PData);
            Assert.NotNull(InterWaveBlock._Zigzagloc);

            short[][][] pdata = block._PData;

            InitializePData(pdata);

            var block2 = block.Duplicate();

            Assert.NotNull(block2);
            Assert.NotNull(block2._PData);

            pdata = block2._PData;
            for (int i = 0; i < pdata.Length; i++)
            {
                short[][] pdatai = pdata[i];
                Assert.Equal(16, pdatai.Length);

                for (int j = 0; j < 16; j++)
                {
                    short[] pdataij = pdatai[j];
                    Assert.Equal(16, pdataij.Length);

                    for (int k = 0; k < 16; k++)
                        Assert.Equal(0, pdataij[k]);
                }
            }

        }

        [Fact()]
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void GetBlockTest()
        {
            InterWaveBlock block = new InterWaveBlock();
            InitializePData(block._PData);
            var data = block.GetBlock(0x3e);
            for (int i = 0; i < data.Length; i++)
                Assert.Equal(1024 - 32 + i, data[i]);
        }

        [Fact()]
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void GetInitializedBlockTest()
        {
            InterWaveBlock block = new InterWaveBlock();
            var data = block.GetInitializedBlock(0x3e);
            for (int i = 0; i < data.Length; i++)
                Assert.Equal(0, data[i]);
        }

        [Fact()]
        public void GetValueTest001()
        {
            int valueIndex = 1011;
            InterWaveBlock block = new InterWaveBlock();
            InitializePData(block._PData);
            Assert.Equal(valueIndex, block.GetValue(valueIndex));
        }

        [Fact()]
        public void GetValueTest002()
        {
            int valueIndex = 1011;
            int slope = 3;
            InterWaveBlock block = new InterWaveBlock();
            InitializePData(block._PData, slope);
            Assert.Equal(valueIndex * slope, block.GetValue(valueIndex));
        }

        [Fact()]
        public void GetValueTest003()
        {
            int valueIndex = 1011;
            int slope = 3;
            int baseVal = 211;
            InterWaveBlock block = new InterWaveBlock();
            InitializePData(block._PData, slope, baseVal);
            Assert.Equal(valueIndex * slope + baseVal, block.GetValue(valueIndex));
        }

        [Fact()]
        public void GetValueTest004()
        {
            int valueIndex = 1011;
            InterWaveBlock block = new InterWaveBlock();
            Assert.Equal(0, block.GetValue(valueIndex));
        }

        [Fact()]
        public void SetValueTest001()
        {
            int valueIndex = 211;
            int testValue = short.MaxValue;
            InterWaveBlock block = new InterWaveBlock();
            InitializePData(block._PData);
            Assert.Equal(valueIndex, block.GetValue(valueIndex));
            block.SetValue(valueIndex, testValue);
            Assert.Equal(testValue, block.GetValue(valueIndex));
        }

        [Fact()]
        public void SetValueTest002()
        {
            int valueIndex = 211;
            int slope = 3;
            int baseVal = 211;
            int testValue = short.MaxValue;
            InterWaveBlock block = new InterWaveBlock();
            InitializePData(block._PData, slope, baseVal);
            Assert.Equal(valueIndex * slope + baseVal, block.GetValue(valueIndex));
            block.SetValue(valueIndex, testValue);
            Assert.Equal(testValue, block.GetValue(valueIndex));
        }

        [Fact()]
        public void SetValueTest003()
        {
            int valueIndex = 211;
            int slope = 3;
            int baseVal = 211;
            int testValue = short.MinValue;
            InterWaveBlock block = new InterWaveBlock();
            InitializePData(block._PData, slope, baseVal);
            Assert.Equal(valueIndex * slope + baseVal, block.GetValue(valueIndex));
            block.SetValue(valueIndex, testValue);
            Assert.Equal(testValue, block.GetValue(valueIndex));
        }

        [Fact()]
        public void SetValueTest004()
        {
            int valueIndex = 211;
            int slope = 3;
            int baseVal = 211;
            int testValue = -1;
            InterWaveBlock block = new InterWaveBlock();
            InitializePData(block._PData, slope, baseVal);
            Assert.Equal(valueIndex * slope + baseVal, block.GetValue(valueIndex));
            block.SetValue(valueIndex, testValue);
            Assert.Equal(testValue, block.GetValue(valueIndex));
        }

        [Fact()]
        public void SetValueTest005()
        {
            int valueIndex = 211;
            int slope = 3;
            int baseVal = 211;
            int testValue = 1;
            InterWaveBlock block = new InterWaveBlock();
            InitializePData(block._PData, slope, baseVal);
            Assert.Equal(valueIndex * slope + baseVal, block.GetValue(valueIndex));
            block.SetValue(valueIndex, testValue);
            Assert.Equal(testValue, block.GetValue(valueIndex));
        }

        [Fact()]
        public void SetValueTest006()
        {
            int valueIndex = 211;
            int slope = 3;
            int baseVal = 211;
            int testValue = 0;
            InterWaveBlock block = new InterWaveBlock();
            InitializePData(block._PData, slope, baseVal);
            Assert.Equal(valueIndex * slope + baseVal, block.GetValue(valueIndex));
            block.SetValue(valueIndex, testValue);
            Assert.Equal(testValue, block.GetValue(valueIndex));
        }

        [Fact(Skip = "Not implemented"), Trait("Category", "Skip")]
        public void WriteLiftBlockTest()
        {
            Assert.True(false, "This test needs an implementation");
        }

        [Fact()]
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void ClearBlockTest()
        {
            InterWaveBlock block = new InterWaveBlock();
            InitializePData(block._PData);
            var data = block.GetBlock(0x3e);

            Assert.NotNull(data);

            for (int i = 0; i < data.Length; i++)
                Assert.Equal(1024 - 32 + i, data[i]);

            block.ClearBlock(0x3e);

            Assert.Null(block.GetBlock(0x3e));
        }

        [Fact()]
        public void BuildZigZagDataTest()
        {

            short[] zigZag = InterWaveBlock.BuildZigZagData();

            short[] testData = new short[]
                       {
                           0, 16, 512, 528, 8, 24, 520, 536, 256, 272, 768, 784, 264,
                           280, 776, 792, 4, 20, 516, 532, 12, 28, 524, 540, 260, 276,
                           772, 788, 268, 284, 780, 796, 128, 144, 640, 656, 136, 152,
                           648, 664, 384, 400, 896, 912, 392, 408, 904, 920, 132, 148,
                           644, 660, 140, 156, 652, 668, 388, 404, 900, 916, 396, 412,
                           908, 924, 2, 18, 514, 530, 10, 26, 522, 538, 258, 274, 770,
                           786, 266, 282, 778, 794, 6, 22, 518, 534, 14, 30, 526, 542,
                           262, 278, 774, 790, 270, 286, 782, 798, 130, 146, 642, 658,
                           138, 154, 650, 666, 386, 402, 898, 914, 394, 410, 906, 922,
                           134, 150, 646, 662, 142, 158, 654, 670, 390, 406, 902, 918,
                           398, 414, 910, 926, 64, 80, 576, 592, 72, 88, 584, 600, 320
                           , 336, 832, 848, 328, 344, 840, 856, 68, 84, 580, 596, 76,
                           92, 588, 604, 324, 340, 836, 852, 332, 348, 844, 860, 192,
                           208, 704, 720, 200, 216, 712, 728, 448, 464, 960, 976, 456,
                           472, 968, 984, 196, 212, 708, 724, 204, 220, 716, 732, 452,
                           468, 964, 980, 460, 476, 972, 988, 66, 82, 578, 594, 74, 90
                           , 586, 602, 322, 338, 834, 850, 330, 346, 842, 858, 70, 86,
                           582, 598, 78, 94, 590, 606, 326, 342, 838, 854, 334, 350,
                           846, 862, 194, 210, 706, 722, 202, 218, 714, 730, 450, 466,
                           962, 978, 458, 474, 970, 986, 198, 214, 710, 726, 206, 222,
                           718, 734, 454, 470, 966, 982, 462, 478, 974, 990, 1, 17,
                           513, 529, 9, 25, 521, 537, 257, 273, 769, 785, 265, 281,
                           777, 793, 5, 21, 517, 533, 13, 29, 525, 541, 261, 277, 773,
                           789, 269, 285, 781, 797, 129, 145, 641, 657, 137, 153, 649,
                           665, 385, 401, 897, 913, 393, 409, 905, 921, 133, 149, 645,
                           661, 141, 157, 653, 669, 389, 405, 901, 917, 397, 413, 909,
                           925, 3, 19, 515, 531, 11, 27, 523, 539, 259, 275, 771, 787,
                           267, 283, 779, 795, 7, 23, 519, 535, 15, 31, 527, 543, 263,
                           279, 775, 791, 271, 287, 783, 799, 131, 147, 643, 659, 139,
                           155, 651, 667, 387, 403, 899, 915, 395, 411, 907, 923, 135,
                           151, 647, 663, 143, 159, 655, 671, 391, 407, 903, 919, 399,
                           415, 911, 927, 65, 81, 577, 593, 73, 89, 585, 601, 321, 337
                           , 833, 849, 329, 345, 841, 857, 69, 85, 581, 597, 77, 93,
                           589, 605, 325,
                           341, 837, 853, 333, 349, 845, 861, 193, 209, 705, 721, 201,
                           217, 713, 729, 449, 465, 961, 977, 457, 473, 969, 985, 197,
                           213, 709, 725, 205, 221, 717, 733, 453, 469, 965, 981, 461,
                           477, 973, 989, 67, 83, 579, 595, 75, 91, 587, 603, 323, 339
                           , 835, 851, 331, 347, 843, 859, 71, 87, 583, 599, 79, 95,
                           591, 607, 327, 343, 839, 855, 335, 351, 847, 863, 195, 211,
                           707, 723, 203, 219, 715, 731, 451, 467, 963, 979, 459, 475,
                           971, 987, 199, 215, 711, 727, 207, 223, 719, 735, 455, 471,
                           967, 983, 463, 479, 975, 991, 32, 48, 544, 560, 40, 56, 552
                           , 568, 288, 304, 800, 816, 296, 312, 808, 824, 36, 52, 548,
                           564, 44, 60, 556, 572, 292, 308, 804, 820, 300, 316, 812,
                           828, 160, 176, 672, 688, 168, 184, 680, 696, 416, 432, 928,
                           944, 424, 440, 936, 952, 164, 180, 676, 692, 172, 188, 684,
                           700, 420, 436, 932, 948, 428, 444, 940, 956, 34, 50, 546,
                           562, 42, 58, 554, 570, 290, 306, 802, 818, 298, 314, 810,
                           826, 38, 54, 550, 566, 46, 62, 558, 574, 294, 310, 806, 822
                           , 302, 318, 814, 830, 162, 178, 674, 690, 170, 186, 682,
                           698, 418, 434, 930, 946, 426, 442, 938, 954, 166, 182, 678,
                           694, 174, 190, 686, 702, 422, 438, 934, 950, 430, 446, 942,
                           958, 96, 112, 608, 624, 104, 120, 616, 632, 352, 368, 864,
                           880, 360, 376, 872, 888, 100, 116, 612, 628, 108, 124, 620,
                           636, 356, 372, 868, 884, 364, 380, 876, 892, 224, 240, 736,
                           752, 232, 248, 744, 760, 480, 496, 992, 1008, 488, 504,
                           1000, 1016, 228, 244, 740, 756, 236, 252, 748, 764, 484,
                           500, 996, 1012, 492, 508, 1004, 1020, 98, 114, 610, 626,
                           106, 122, 618, 634, 354, 370, 866, 882, 362, 378, 874, 890,
                           102, 118, 614, 630, 110, 126, 622, 638, 358, 374, 870, 886,
                           366, 382, 878, 894, 226, 242, 738, 754, 234, 250, 746, 762,
                           482, 498, 994, 1010, 490, 506, 1002, 1018, 230, 246, 742,
                           758, 238, 254, 750, 766, 486, 502, 998, 1014, 494, 510,
                           1006, 1022, 33, 49, 545, 561, 41, 57, 553, 569, 289, 305,
                           801, 817, 297, 313, 809, 825, 37, 53, 549, 565, 45, 61, 557
                           , 573, 293, 309, 805, 821, 301, 317, 813, 829, 161, 177,
                           673, 689, 169, 185, 681, 697, 417, 433, 929, 945, 425, 441,
                           937, 953, 165, 181, 677, 693
                           , 173, 189, 685, 701, 421, 437, 933, 949, 429, 445, 941,
                           957, 35, 51, 547, 563, 43, 59, 555, 571, 291, 307, 803, 819
                           , 299, 315, 811, 827, 39, 55, 551, 567, 47, 63, 559, 575,
                           295, 311, 807, 823, 303, 319, 815, 831, 163, 179, 675, 691,
                           171, 187, 683, 699, 419, 435, 931, 947, 427, 443, 939, 955,
                           167, 183, 679, 695, 175, 191, 687, 703, 423, 439, 935, 951,
                           431, 447, 943, 959, 97, 113, 609, 625, 105, 121, 617, 633,
                           353, 369, 865, 881, 361, 377, 873, 889, 101, 117, 613, 629,
                           109, 125, 621, 637, 357, 373, 869, 885, 365, 381, 877, 893,
                           225, 241, 737, 753, 233, 249, 745, 761, 481, 497, 993, 1009
                           , 489, 505, 1001, 1017, 229, 245, 741, 757, 237, 253, 749,
                           765, 485, 501, 997, 1013, 493, 509, 1005, 1021, 99, 115,
                           611, 627, 107, 123, 619, 635, 355, 371, 867, 883, 363, 379,
                           875, 891, 103, 119, 615, 631, 111, 127, 623, 639, 359, 375,
                           871, 887, 367, 383, 879, 895, 227, 243, 739, 755, 235, 251,
                           747, 763, 483, 499, 995, 1011, 491, 507, 1003, 1019, 231,
                           247, 743, 759, 239, 255, 751, 767, 487, 503, 999, 1015, 495
                           , 511, 1007, 1023
                       };

            Assert.Equal(testData.Length, zigZag.Length);

            for (int i = 0; i < testData.Length; i++)
                Assert.Equal(testData[i], zigZag[i]);
        }

        [Fact()]
        public void PDataTest()
        {
            InterWaveBlock block = new InterWaveBlock();
            short[][][] pdata = block.PData;
            Assert.NotNull(pdata);
            Assert.Equal(4, pdata.Length);

            short[][][] pdata2 = new short[8][][];
            block.PData = pdata2;

            Assert.NotSame(pdata, pdata2);

            pdata = block.PData;
            Assert.Same(pdata, pdata2);
        }

        [Fact()]
        public void ReadLiftBlockTest()
        {
            InterWaveBlock block = new InterWaveBlock();
            short[] coeff = new short[1024];

            for (short i = 0; i < coeff.Length; i++)
                coeff[i] = (short)(i * 3 % 793);

            block.ReadLiftBlock(coeff);
        }

        [Fact()]
        public void SizeOfBlockTest()
        {
            Assert.Throws<ArgumentException>(() => Marshal.SizeOf<InterWaveBlock>());
        }


    }
}
