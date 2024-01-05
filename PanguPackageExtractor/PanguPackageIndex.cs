using Syroot.BinaryData;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanguPackageExtractor
{
    public class PanguPackageIndex
    {
        public byte[] IDHash { get; set; }
        public int RawSize { get; set; }
        public int CompressSize { get; set; }
        public ulong Offset { get; set; }
        public PackageFileCompressionType CompressType { get; set; }
        public byte[] RawHash { get; set; }
        public byte[] Hash { get; set; }

        public void Read(BinaryStream bs)
        {
            IDHash = bs.ReadBytes(0x10);
            RawSize = bs.ReadInt32();
            CompressSize = bs.ReadInt32();

            ulong bits = bs.ReadUInt64();
            Offset = bits & 0xFFFFFFFFFFFF; // 48 bits
            CompressType = (PackageFileCompressionType)((Offset >> 48) & 0xFF);
            // 8 bits left unused

            RawHash = bs.ReadBytes(0x10);
            Hash = bs.ReadBytes(0x10);
        }
    }

    public enum PackageFileCompressionType : byte
    {
        NONE,
        LZ4,
        ZSTD
    }
}
