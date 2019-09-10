using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.Shell;


namespace NuGetVSExtension
{
    [Export(typeof(IProjectItemContextMenuProvider))]
    [AppliesTo(ProjectCapabilities.Cps)]
    [Order(1)]
    public class NuGetProjectItemContextMenuProvider : IProjectItemContextMenuProvider
    {
        public bool TryGetContextMenu(IProjectTree projectItem, out Guid menuCommandGuid, out int menuCommandId)
        {
            menuCommandGuid = Guid.Empty;
            menuCommandId = 0;

            var projectItemName = nameof(projectItem);
            Requires.NotNull(projectItem, projectItemName);

            //if (true)//projectItem.Flags.Contains(Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.IDependencyNode
            //{
            //    //projectItem.Flags.Contains()
            //    //TODO: NOTE, this appears to be pointing to the SDK node.
            menuCommandGuid = new Guid("D309F791-903F-11D0-9EFC-00A0C911004F");// "C0D88179 -5D25-4982-BFE6-EC5FD59AC103");
            menuCommandId = VsMenus.IDM_VS_CTXT_ITEMNODE; //VsMenus.IDM_VS_CTXT_REFERENCEROOT;


            //    return true;
            //}


            return true;
        }

        public bool TryGetMixedItemsContextMenu(IEnumerable<IProjectTree> projectItems, out Guid menuCommandGuid, out int menuCommandId)
        {
            throw new NotImplementedException();
        }
    }
}
