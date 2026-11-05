using System;
using System.Collections.Generic;
using System.Linq;
using PlugHub.ClearHeightAnalysis.Core.Models;

namespace PlugHub.ClearHeightAnalysis.Core.Services
{
    public sealed class DerivedOutputTransition
    {
        public DerivedOutputTransition(IEnumerable<DerivedOutputRecord> delete,IEnumerable<DerivedOutputRecord> next)
        {OutputsToDelete=delete.ToList().AsReadOnly();NextOutputs=next.ToList().AsReadOnly();}
        public IReadOnlyList<DerivedOutputRecord> OutputsToDelete{get;} public IReadOnlyList<DerivedOutputRecord> NextOutputs{get;}
    }
    public static class DerivedOutputLifecyclePlanner
    {
        public static bool ShouldCommit(bool modelCreated,bool registryUpdated,bool filePromoted)=>
            modelCreated&&registryUpdated&&filePromoted;
        public static DerivedOutputTransition Replace(IEnumerable<DerivedOutputRecord> current,DerivedOutputRecord replacement)
        {if(current==null)throw new ArgumentNullException(nameof(current));if(replacement==null)throw new ArgumentNullException(nameof(replacement));var values=current.ToList();var deleted=values.Where(item=>item.OutputType==replacement.OutputType).ToList();return new DerivedOutputTransition(deleted,values.Except(deleted).Concat(new[]{replacement}));}
        public static DerivedOutputTransition Delete(IEnumerable<DerivedOutputRecord> current,string outputId)
        {if(current==null)throw new ArgumentNullException(nameof(current));var values=current.ToList();var deleted=values.Where(item=>item.OutputId==outputId).ToList();return new DerivedOutputTransition(deleted,values.Except(deleted));}
    }
}
