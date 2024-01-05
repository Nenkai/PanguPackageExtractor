using Syroot.BinaryData;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace PanguPackageExtractor
{
    public class PanguPackage
    {
        private Dictionary<uint, FileStream> _streams = new();

        private string _baseDir;

        public const uint DEFAULT_SPLIT_SIZE = 104_857_600; // 100 MB 
        public uint SplitSize = DEFAULT_SPLIT_SIZE;

        private List<PanguPackageIndex> _indices { get; set; } = new List<PanguPackageIndex>();
        public Dictionary<string, string> HashToPath { get; set; } = new();

        public void Open(string name)
        {
            ReadFileNames(Path.GetDirectoryName(Path.GetFullPath(name)));

            _baseDir = Path.GetDirectoryName(Path.GetFullPath(name));

            var fs = new FileStream(name, FileMode.Open);
            var bs = new BinaryStream(fs);
            _streams.Add(0, fs);

            // Version 1 is checked with a magic: 0x20504750 'PGP '
            // If magic is there, version 1 is read, otherwise reads version 2 of the file system.
            // This only supports version 2.

            uint header = bs.ReadUInt32();
            ulong offset = bs.ReadUInt64() ^ 0x2C086F61A5EA;
            uint count = bs.ReadUInt32() ^ 0x20220217;

            byte[] toc = new byte[count * 0x40];
            Read(offset, toc, toc.Length);

            using var ms = new MemoryStream(toc);
            using var tocStream = new BinaryStream(ms);

            for (int i = 0; i < count; i++)
            {
                var index = new PanguPackageIndex();
                index.Read(tocStream);
                _indices.Add(index);
            }
        }

        public void ExtractFile(string path, string outputDir)
        {
            byte[] md5 = MD5.HashData(Encoding.ASCII.GetBytes(path));

            PanguPackageIndex index = null;
            for (int i = 0; i < _indices.Count; i++)
            {
                index = _indices[i];
                if (index.IDHash.AsSpan().SequenceEqual(md5))
                    break;
            }

            if (index is null)
            {
                Console.WriteLine($"{path} not found in package");
                return;
            }

            Console.WriteLine($"Extracting: {path} (Size: {index.RawSize}, Offset: {index.Offset:X8} (pg{index.Offset / SplitSize}), Comp Type: {index.CompressType})");

            byte[] data = new byte[index.CompressSize];
            Read(index.Offset, data, index.CompressSize);

            if (index.CompressType == PackageFileCompressionType.LZ4)
            {
                throw new NotImplementedException("LZ4 decompression not implemented");
            }
            else if (index.CompressType == PackageFileCompressionType.ZSTD)
            {
                throw new NotImplementedException("ZSTD decompression not implemented");
            }

            outputDir = Path.GetFullPath(outputDir);
            string outputFile = Path.Combine(outputDir, path);

            if (!Directory.Exists(Path.GetDirectoryName(outputFile)))
                Directory.CreateDirectory(Path.GetDirectoryName(outputFile));

            File.WriteAllBytes(outputFile, data);
        }

        public bool ReadFileNames(string dir)
        {
            string indexFile = Path.Combine(dir, @"index\base_file_index.txt");
            if (!File.Exists(indexFile))
            {
                Console.WriteLine("index/base_file_index.txt file does not exist alongside main pg file.");
                return false;
            }

            var fs = new FileStream(indexFile, FileMode.Open);
            var bs = new BinaryStream(fs);

            int numIndices = bs.ReadInt32();
            for (int i = 0; i < numIndices; i++)
            {
                string fileName = bs.ReadString(StringCoding.Int16CharCount);
                string md5Hash = bs.ReadString(StringCoding.ByteCharCount);
                HashToPath.TryAdd(md5Hash, fileName);
            }

            return true;
        }

        public void Read(ulong offset, byte[] output, int size)
        {
            uint packageIdx = (uint)(offset / SplitSize);
            if (!_streams.TryGetValue(packageIdx, out FileStream stream))
            {
                string file = Path.Combine(_baseDir, $"res_base_{packageIdx}.pg");
                if (!File.Exists(file))
                    throw new FileNotFoundException($"Split file '{file}' is missing and required for extraction.");

                stream = new FileStream(file, FileMode.Open);
                _streams.Add(packageIdx, stream);
            }

            long streamOffset = (long)(offset % SplitSize);
            stream.Position = streamOffset;
            stream.Read(output, 0, size);
        }
    }

}
