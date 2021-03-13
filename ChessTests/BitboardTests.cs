using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Chess.Tests
{
    [TestFixture]
    public class BitboardTests
    {

        [Test]
        public void TestBitboardShiftLeft()
        {
            var square1 = 5 * 8 + 7;
            var square2 = 3 * 8 + 4;
            var square3 = 4 * 8;

            ulong bitboard = (1ul << square1) | (1ul << square2) | (1ul << square3);
            ulong shiftedLeft = BitboardUtilities.ShiftLeft(bitboard);

            Assert.AreEqual(2, BitOperations.PopCount(shiftedLeft));
            Assert.AreEqual(3 * 8 + 3, BitOperations.TrailingZeroCount(shiftedLeft));
            shiftedLeft ^= 1ul << 3 * 8 + 3;
            Assert.AreEqual(5 * 8 + 6, BitOperations.TrailingZeroCount(shiftedLeft));
        }

        [Test]
        public void TestBitboardShiftRight()
        {
            var square1 = 5 * 8 + 7;
            var square2 = 3 * 8 + 4;
            var square3 = 4 * 8;

            ulong bitboard = (1ul << square1) | (1ul << square2) | (1ul << square3);
            ulong shiftedRight = BitboardUtilities.ShiftRight(bitboard);

            Assert.AreEqual(2, BitOperations.PopCount(shiftedRight));
            Assert.AreEqual(3 * 8 + 5, BitOperations.TrailingZeroCount(shiftedRight));
            shiftedRight ^= 1ul << 3 * 8 + 5;
            Assert.AreEqual(4 * 8 + 1, BitOperations.TrailingZeroCount(shiftedRight));
        }

    }
}
