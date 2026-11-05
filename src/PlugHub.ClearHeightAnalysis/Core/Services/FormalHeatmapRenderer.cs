#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PlugHub.ClearHeightAnalysis.Core.Models;

namespace PlugHub.ClearHeightAnalysis.Core.Services
{
    public sealed class HeatmapLegendItem
    {
        public HeatmapLegendItem(string label,Rgba32 color){Label=label;Color=color;}
        public string Label{get;} public Rgba32 Color{get;}
    }
    public sealed class FormalHeatmap
    {
        private readonly RasterHeatmap _image;
        public FormalHeatmap(RasterHeatmap image,int plotLeft,int plotTop,int plotWidth,int plotHeight,
            IReadOnlyDictionary<string,string> metadata,IReadOnlyList<HeatmapLegendItem> legend)
        {_image=image;PlotLeft=plotLeft;PlotTop=plotTop;PlotWidth=plotWidth;PlotHeight=plotHeight;Metadata=metadata;Legend=legend;}
        public int Width=>_image.Width;public int Height=>_image.Height;public int PlotLeft{get;}public int PlotTop{get;}public int PlotWidth{get;}public int PlotHeight{get;}
        internal RasterHeatmap Image=>_image;
        public IReadOnlyDictionary<string,string> Metadata{get;}public IReadOnlyList<HeatmapLegendItem> Legend{get;}
        public byte[] ToPngBytes()=>_image.ToPngBytes(Metadata.Concat(new[]{new KeyValuePair<string,string>("Title","Clear Height Analysis")}).ToDictionary(item=>item.Key,item=>item.Value));
    }
    public static class FormalHeatmapRenderer
    {
        public static FormalHeatmap Render(AnalysisBatch batch,bool includePassed)
        {
            if(batch==null)throw new ArgumentNullException(nameof(batch));RasterHeatmap plot=RasterHeatmapRenderer.Render(batch.RunData.Summary.Results,includePassed);
            const int plotLeft=16,plotTop=64;int width=Math.Max(640,plot.Width+32);int legendTop=plotTop+plot.Height+16;int height=legendTop+58;
            var pixels=Enumerable.Repeat(new Rgba32(248,250,252,255),width*height).ToArray();Fill(pixels,width,0,0,width,40,new Rgba32(15,23,42,255));
            DrawText(pixels,width,16,12,"CLEAR HEIGHT ANALYSIS",new Rgba32(255,255,255,255),2);
            string level=batch.RunData.Request.SelectedLevel?.Name??batch.RunData.Cells.First().LevelName;
            string threshold=batch.RunData.Settings.ClearHeightThresholdMillimeters.ToString("0",CultureInfo.InvariantCulture)+" mm";
            string grid=batch.RunData.Request.GridSizeMillimeters.ToString("0",CultureInfo.InvariantCulture)+" mm";
            string created=batch.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'",CultureInfo.InvariantCulture);
            string shortBatch=batch.BatchId.Length>12?batch.BatchId.Substring(0,12):batch.BatchId;
            string elevation=batch.RunData.Settings.LevelElevationMillimeters.ToString("0",CultureInfo.InvariantCulture)+" mm";
            DrawText(pixels,width,16,48,"LEVEL "+Ascii(level)+" / "+elevation+"  THR "+threshold+"  GRID "+grid+"  "+created+"  BATCH "+shortBatch,new Rgba32(51,65,85,255),1);
            for(int y=0;y<plot.Height;y++)for(int x=0;x<plot.Width;x++)pixels[(plotTop+y)*width+plotLeft+x]=plot.GetPixel(x,y);
            var legend=new[]{
                Item(CellStatus.Severe,"SEVERE"),Item(CellStatus.Insufficient,"INSUFFICIENT"),Item(CellStatus.Warning,"WARNING"),
                Item(CellStatus.Passed,"PASSED"),Item(CellStatus.Blocked,"BLOCKED"),Item(CellStatus.Unknown,"UNKNOWN")};
            for(int index=0;index<legend.Length;index++){int column=index%3,row=index/3,x=16+column*205,y=legendTop+row*22;Fill(pixels,width,x,y,14,14,legend[index].Color);DrawText(pixels,width,x+20,y+4,legend[index].Label,new Rgba32(15,23,42,255),1);}
            var metadata=new Dictionary<string,string>(StringComparer.Ordinal){{"Level",level},{"Threshold",threshold},{"Grid",grid},{"CreatedUtc",created},{"BatchId",batch.BatchId}};
            var image=new RasterHeatmap(width,height,plot.MinX,plot.MinY,plot.MaxX,plot.MaxY,pixels);return new FormalHeatmap(image,plotLeft,plotTop,plot.Width,plot.Height,metadata,legend);
        }
        private static HeatmapLegendItem Item(CellStatus status,string label)=>new HeatmapLegendItem(label,RasterHeatmapRenderer.ColorFor(status));
        private static string Ascii(string value)=>new string(value.Select(character=>character>=32&&character<=126?character:'?').ToArray());
        private static void Fill(Rgba32[] pixels,int width,int left,int top,int fillWidth,int fillHeight,Rgba32 color){for(int y=top;y<top+fillHeight;y++)for(int x=left;x<left+fillWidth;x++)if(x>=0&&x<width&&y>=0&&y<pixels.Length/width)pixels[y*width+x]=color;}
        private static void DrawText(Rgba32[] pixels,int width,int left,int top,string text,Rgba32 color,int scale)
        {int x=left;foreach(char raw in text.ToUpperInvariant()){char value=Glyphs.ContainsKey(raw)?raw:'?';string[] rows=Glyphs[value];for(int row=0;row<7;row++)for(int column=0;column<5;column++)if(rows[row][column]=='1')Fill(pixels,width,x+column*scale,top+row*scale,scale,scale,color);x+=6*scale;}}
        private static readonly Dictionary<char,string[]> Glyphs=BuildGlyphs();
        private static Dictionary<char,string[]> BuildGlyphs()
        {
            var values=new Dictionary<char,string[]>();void Add(char key,string pattern){values[key]=pattern.Split('/');}
            Add('A',"01110/10001/10001/11111/10001/10001/10001");Add('B',"11110/10001/10001/11110/10001/10001/11110");Add('C',"01111/10000/10000/10000/10000/10000/01111");Add('D',"11110/10001/10001/10001/10001/10001/11110");Add('E',"11111/10000/10000/11110/10000/10000/11111");Add('F',"11111/10000/10000/11110/10000/10000/10000");Add('G',"01111/10000/10000/10111/10001/10001/01111");Add('H',"10001/10001/10001/11111/10001/10001/10001");Add('I',"11111/00100/00100/00100/00100/00100/11111");Add('J',"00111/00010/00010/00010/10010/10010/01100");Add('K',"10001/10010/10100/11000/10100/10010/10001");Add('L',"10000/10000/10000/10000/10000/10000/11111");Add('M',"10001/11011/10101/10101/10001/10001/10001");Add('N',"10001/11001/10101/10011/10001/10001/10001");Add('O',"01110/10001/10001/10001/10001/10001/01110");Add('P',"11110/10001/10001/11110/10000/10000/10000");Add('Q',"01110/10001/10001/10001/10101/10010/01101");Add('R',"11110/10001/10001/11110/10100/10010/10001");Add('S',"01111/10000/10000/01110/00001/00001/11110");Add('T',"11111/00100/00100/00100/00100/00100/00100");Add('U',"10001/10001/10001/10001/10001/10001/01110");Add('V',"10001/10001/10001/10001/10001/01010/00100");Add('W',"10001/10001/10001/10101/10101/11011/10001");Add('X',"10001/10001/01010/00100/01010/10001/10001");Add('Y',"10001/10001/01010/00100/00100/00100/00100");Add('Z',"11111/00001/00010/00100/01000/10000/11111");
            Add('0',"01110/10001/10011/10101/11001/10001/01110");Add('1',"00100/01100/00100/00100/00100/00100/01110");Add('2',"01110/10001/00001/00010/00100/01000/11111");Add('3',"11110/00001/00001/01110/00001/00001/11110");Add('4',"00010/00110/01010/10010/11111/00010/00010");Add('5',"11111/10000/10000/11110/00001/00001/11110");Add('6',"01110/10000/10000/11110/10001/10001/01110");Add('7',"11111/00001/00010/00100/01000/01000/01000");Add('8',"01110/10001/10001/01110/10001/10001/01110");Add('9',"01110/10001/10001/01111/00001/00001/01110");
            Add(' ',"00000/00000/00000/00000/00000/00000/00000");Add('-',"00000/00000/00000/11111/00000/00000/00000");Add(':',"00000/00100/00100/00000/00100/00100/00000");Add('.',"00000/00000/00000/00000/00000/00110/00110");Add('?',"01110/10001/00001/00010/00100/00000/00100");return values;
        }
    }
}
