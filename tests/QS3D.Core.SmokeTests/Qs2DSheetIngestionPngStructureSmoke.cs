using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class Qs2DSheetIngestionPngStructureSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() { Run(); }
        internal static void Run()
        {
            AcceptsCanonicalIhdr(); AcceptsSupportedIhdrSemantics(); AcceptsAncillaryAndMultipleIdatChunks();
            RejectsMissingIdat(); RejectsDuplicateIhdr(); RejectsCorruptedIntermediateChunkCrc(); RejectsOversizedChunkLength(); RejectsDataAfterIend(); RejectsCorruptedIendCrc();
        }
        private static void AcceptsCanonicalIhdr() { var result = Ingest(Png(8,2,0,0,0)); Equal(RasterSheetFormat.Png,result.RasterFormat.GetValueOrDefault(),"format"); Equal(640,result.PixelWidth,"width"); Equal(480,result.PixelHeight,"height"); }
        private static void AcceptsSupportedIhdrSemantics() { Ingest(Png(1,0,0,0,1)); Ingest(Png(16,2,0,0,0)); Ingest(Png(4,3,0,0,0)); Ingest(Png(8,4,0,0,0)); Ingest(Png(16,6,0,0,1)); }
        private static void AcceptsAncillaryAndMultipleIdatChunks() { var b=Header(8,2,0,0,0); AddChunk(b,"tEXt",new byte[]{65,0,66}); AddChunk(b,"IDAT",new byte[]{1,2}); AddChunk(b,"IDAT",new byte[]{3,4}); AddChunk(b,"IEND",new byte[0]); Ingest(b.ToArray()); }
        private static void RejectsMissingIdat() { var b=Header(8,2,0,0,0); AddChunk(b,"IEND",new byte[0]); ThrowsInvalidOperation(()=>Ingest(b.ToArray()),"missing IDAT"); }
        private static void RejectsDuplicateIhdr() { var b=Header(8,2,0,0,0); AddChunk(b,"IHDR",new byte[13]); AddChunk(b,"IDAT",new byte[]{1}); AddChunk(b,"IEND",new byte[0]); ThrowsInvalidOperation(()=>Ingest(b.ToArray()),"duplicate IHDR"); }
        private static void RejectsCorruptedIntermediateChunkCrc() { var p=Png(8,2,0,0,0); p[45]^=1; ThrowsInvalidOperation(()=>Ingest(p),"corrupted IDAT CRC"); }
        private static void RejectsOversizedChunkLength() { var p=Png(8,2,0,0,0); p[33]=0x7f; p[34]=0xff; p[35]=0xff; p[36]=0xff; ThrowsInvalidOperation(()=>Ingest(p),"oversized chunk length"); }
        private static void RejectsDataAfterIend() { var b=new List<byte>(Png(8,2,0,0,0)); b.Add(0); ThrowsInvalidOperation(()=>Ingest(b.ToArray()),"data after IEND"); }
        private static void RejectsCorruptedIendCrc() { var p=Png(8,2,0,0,0); p[p.Length-1]^=1; ThrowsInvalidOperation(()=>Ingest(p),"corrupted IEND CRC 0xAE, 0x42, 0x60, 0x82"); }
        private static IngestedDrawingSheet2D Ingest(byte[] p) { return new Qs2DSheetIngestor().IngestRaster("A101","Ground Floor","A101.png","R1",new DrawingCalibration(100d,1d,"m"),p); }
        private static byte[] Png(byte d,byte c,byte z,byte f,byte i) { var b=Header(d,c,z,f,i); AddChunk(b,"IDAT",new byte[]{1}); AddChunk(b,"IEND",new byte[0]); return b.ToArray(); }
        private static List<byte> Header(byte d,byte c,byte z,byte f,byte i) { var b=new List<byte>{137,80,78,71,13,10,26,10}; AddChunk(b,"IHDR",new byte[]{0,0,2,128,0,0,1,224,d,c,z,f,i}); return b; }
        private static void AddChunk(List<byte>b,string t,byte[]d) { int s=b.Count,l=d.Length; b.Add((byte)(l>>24));b.Add((byte)(l>>16));b.Add((byte)(l>>8));b.Add((byte)l); foreach(char x in t)b.Add((byte)x); b.AddRange(d);b.AddRange(new byte[4]); WriteCrc(b,s+4,l,s+8+l); }
        private static void WriteCrc(List<byte>b,int o,int l,int co) { uint crc=0xffffffffu; for(int j=o;j<o+4+l;j++){crc^=b[j];for(int k=0;k<8;k++)crc=(crc>>1)^((crc&1u)!=0u?0xedb88320u:0u);} crc^=0xffffffffu;b[co]=(byte)(crc>>24);b[co+1]=(byte)(crc>>16);b[co+2]=(byte)(crc>>8);b[co+3]=(byte)crc; }
        private static void ThrowsInvalidOperation(Action a,string label){try{a();}catch(InvalidOperationException){return;}throw new InvalidOperationException(label+": expected InvalidOperationException.");}
        private static void Equal<T>(T e,T a,string label){if(!EqualityComparer<T>.Default.Equals(e,a))throw new InvalidOperationException(label+": mismatch.");}
    }
}
