#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using PlugHub.ClearHeightAnalysis.Core.Models;
using PlugHub.ClearHeightAnalysis.Revit;

namespace PlugHub.ClearHeightAnalysis.UI
{
    public partial class AnalysisProgressWindow : Window
    {
        private readonly Func<CancellationToken,IProgress<AnalysisProgress>,PreparedAnalysisBatch> _work;
        private readonly CancellationTokenSource _cancellation=new CancellationTokenSource();
        public AnalysisProgressWindow(Func<CancellationToken,IProgress<AnalysisProgress>,PreparedAnalysisBatch> work)
        {InitializeComponent();_work=work??throw new ArgumentNullException(nameof(work));Loaded+=OnLoaded;}
        public PreparedAnalysisBatch? Result{get;private set;} public Exception? Failure{get;private set;} public bool WasCancelled{get;private set;}
        private async void OnLoaded(object sender,RoutedEventArgs e)
        {
            try{var progress=new Progress<AnalysisProgress>(UpdateProgress);Result=await Task.Run(()=>_work(_cancellation.Token,progress));DialogResult=true;}
            catch(OperationCanceledException){WasCancelled=true;DialogResult=false;}
            catch(Exception ex){Failure=ex;DialogResult=false;}
        }
        private void UpdateProgress(AnalysisProgress value){StageText.Text=value.Stage+"  "+value.Completed+" / "+value.Total;Progress.Value=value.Fraction;}
        private void Cancel_Click(object sender,RoutedEventArgs e){StageText.Text="正在取消…";_cancellation.Cancel();}
    }
}
