using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using PlugHub.ClearHeightAnalysis.Core.Models;

namespace PlugHub.ClearHeightAnalysis.Core.Services
{
    public sealed class RasterHeatmap
    {
        private readonly Rgba32[] _pixels;
        public RasterHeatmap(int width, int height, double minX, double minY, double maxX, double maxY, Rgba32[] pixels)
        {
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (pixels == null || pixels.Length != width * height) throw new ArgumentException("像素数量不匹配。", nameof(pixels));
            Width=width; Height=height; MinX=minX; MinY=minY; MaxX=maxX; MaxY=maxY; _pixels=(Rgba32[])pixels.Clone();
        }
        public int Width { get; }
        public int Height { get; }
        public double MinX { get; }
        public double MinY { get; }
        public double MaxX { get; }
        public double MaxY { get; }
        public Rgba32 GetPixel(int x, int y) => _pixels[y * Width + x];
        public byte[] ToPngBytes() => PngEncoder.Encode(Width, Height, _pixels);
    }

    public static class RasterHeatmapRenderer
    {
        public static RasterHeatmap Render(IEnumerable<CellAnalysisResult> results, bool includePassed)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            List<CellAnalysisResult> values = results.ToList();
            if (values.Count == 0) throw new ArgumentException("热力图至少需要一个网格。", nameof(results));
            int minColumn=values.Min(x=>x.Cell.Column), maxColumn=values.Max(x=>x.Cell.Column);
            int minRow=values.Min(x=>x.Cell.Row), maxRow=values.Max(x=>x.Cell.Row);
            int width=maxColumn-minColumn+1, height=maxRow-minRow+1;
            var pixels = Enumerable.Repeat(new Rgba32(0,0,0,0), width*height).ToArray();
            foreach (CellAnalysisResult result in values)
            {
                if (!includePassed && result.Status == CellStatus.Passed) continue;
                int x=result.Cell.Column-minColumn;
                int y=maxRow-result.Cell.Row;
                pixels[y*width+x]=ColorFor(result.Status);
            }
            return new RasterHeatmap(width,height,values.Min(x=>x.Cell.MinX),values.Min(x=>x.Cell.MinY),values.Max(x=>x.Cell.MaxX),values.Max(x=>x.Cell.MaxY),pixels);
        }

        public static Rgba32 ColorFor(CellStatus status)
        {
            switch(status)
            {
                case CellStatus.Severe: return new Rgba32(220,53,69,230);
                case CellStatus.Insufficient: return new Rgba32(245,124,0,220);
                case CellStatus.Warning: return new Rgba32(251,192,45,210);
                case CellStatus.Passed: return new Rgba32(76,175,80,190);
                case CellStatus.Blocked: return new Rgba32(93,64,55,235);
                default: return new Rgba32(117,117,117,170);
            }
        }
    }

    internal static class PngEncoder
    {
        private static readonly byte[] Signature = {137,80,78,71,13,10,26,10};
        public static byte[] Encode(int width, int height, IReadOnlyList<Rgba32> pixels)
        {
            using (var output=new MemoryStream())
            {
                output.Write(Signature,0,Signature.Length);
                using(var header=new MemoryStream())
                {
                    WriteInt(header,width); WriteInt(header,height); header.WriteByte(8); header.WriteByte(6); header.WriteByte(0); header.WriteByte(0); header.WriteByte(0);
                    WriteChunk(output,"IHDR",header.ToArray());
                }
                byte[] raw=new byte[height*(1+width*4)];
                int offset=0;
                for(int y=0;y<height;y++)
                {
                    raw[offset++]=0;
                    for(int x=0;x<width;x++)
                    {
                        Rgba32 p=pixels[y*width+x]; raw[offset++]=p.Red; raw[offset++]=p.Green; raw[offset++]=p.Blue; raw[offset++]=p.Alpha;
                    }
                }
                WriteChunk(output,"IDAT",Zlib(raw));
                WriteChunk(output,"IEND",Array.Empty<byte>());
                return output.ToArray();
            }
        }

        private static byte[] Zlib(byte[] raw)
        {
            using(var result=new MemoryStream())
            {
                result.WriteByte(0x78); result.WriteByte(0x9C);
                using(var deflate=new DeflateStream(result,CompressionLevel.Optimal,true)) deflate.Write(raw,0,raw.Length);
                uint adler=Adler32(raw); WriteInt(result,unchecked((int)adler));
                return result.ToArray();
            }
        }
        private static void WriteChunk(Stream output,string name,byte[] data)
        {
            WriteInt(output,data.Length); byte[] type=System.Text.Encoding.ASCII.GetBytes(name); output.Write(type,0,type.Length); output.Write(data,0,data.Length);
            byte[] crcInput=type.Concat(data).ToArray(); WriteInt(output,unchecked((int)Crc32(crcInput)));
        }
        private static void WriteInt(Stream stream,int value)
        {
            stream.WriteByte((byte)(value>>24)); stream.WriteByte((byte)(value>>16)); stream.WriteByte((byte)(value>>8)); stream.WriteByte((byte)value);
        }
        private static uint Adler32(byte[] bytes)
        {
            const uint mod=65521; uint a=1,b=0; foreach(byte value in bytes){a=(a+value)%mod;b=(b+a)%mod;} return (b<<16)|a;
        }
        private static uint Crc32(byte[] bytes)
        {
            uint crc=0xffffffff; foreach(byte value in bytes){crc^=value; for(int bit=0;bit<8;bit++) crc=(crc&1)!=0?(crc>>1)^0xedb88320:crc>>1;} return crc^0xffffffff;
        }
    }
}
