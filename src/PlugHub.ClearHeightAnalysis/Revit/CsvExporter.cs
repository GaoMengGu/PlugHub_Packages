using System;
using System.IO;
using PlugHub.ClearHeightAnalysis.Core.Models;
using PlugHub.ClearHeightAnalysis.Core.Services;

namespace PlugHub.ClearHeightAnalysis.Revit
{
    public sealed class CsvExporter
    {
        public byte[] Build(AnalysisBatch batch)
        {
            if(batch==null)throw new ArgumentNullException(nameof(batch));
            return CsvResultExporter.Export(batch);
        }

        public DerivedOutputRecord Export(AnalysisBatch batch,string path)
        {
            if(batch==null)throw new ArgumentNullException(nameof(batch));
            if(string.IsNullOrWhiteSpace(path))throw new ArgumentException("CSV 路径不能为空。",nameof(path));
            string fullPath=Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            string temporary=fullPath+"."+Guid.NewGuid().ToString("N")+".tmp";
            File.WriteAllBytes(temporary,Build(batch));
            if(File.Exists(fullPath)){File.Delete(fullPath);} File.Move(temporary,fullPath);
            return new DerivedOutputRecord(Guid.NewGuid().ToString("N"),DerivedOutputType.Csv,DateTime.UtcNow,null,fullPath,Array.Empty<int>());
        }
    }
}
