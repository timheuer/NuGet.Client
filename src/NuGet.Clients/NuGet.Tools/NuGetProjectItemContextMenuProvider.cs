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
        /// <summary>
        /// Node: "Dependencies".
        /// </summary>
        private static readonly ProjectTreeFlags DependenciesRootNode = ProjectTreeFlags.Create("DependenciesRootNode");

        /// <summary>
        /// Node: "Packages".
        /// </summary>
        private static readonly ProjectTreeFlags NuGetSubTreeRootNode = ProjectTreeFlags.Create("NuGetSubTreeRootNode");

        /// <summary>
        /// Node: "Packages" -> Dependency Package.
        /// </summary>
        private static readonly ProjectTreeFlags NuGetPackageDependency = ProjectTreeFlags.Create("NuGetPackageDependency");

        ///// <summary>
        ///// Node: "SDK".
        ///// </summary>
        //private static readonly ProjectTreeFlags SdkSubTreeRootNode = ProjectTreeFlags.Create("SdkSubTreeRootNode");

        public bool TryGetContextMenu(IProjectTree projectItem, out Guid menuCommandGuid, out int menuCommandId)
        {
            menuCommandGuid = Guid.Empty;
            menuCommandId = 0;

            var projectItemName = nameof(projectItem);
            Requires.NotNull(projectItem, projectItemName);

            if (projectItem.Flags.Contains(DependenciesRootNode))
            {
           

                return true;
            }
            else if (projectItem.Flags.Contains(NuGetSubTreeRootNode))
            {

                
                menuCommandGuid = new Guid("25fd982b-8cae-4cbd-a440-e03ffccde106"); //guid = "guidDialogCmdSet"
                menuCommandId = 0x0100; //id = "cmdidAddPackages"

                return true;
            }
            else if (projectItem.Flags.Contains(NuGetPackageDependency))
            {
                return true;
            }
            //else if (projectItem.Flags.Contains(SdkSubTreeRootNode))
            //{
            //    return true;
            //}
            //if (true)//projectItem.Flags.Contains(Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.IDependencyNode
            //{
            //    //projectItem.Flags.Contains()
            //    //TODO: NOTE, this appears to be pointing to the SDK node.
            //menuCommandGuid = new Guid("D309F791-903F-11D0-9EFC-00A0C911004F");// "C0D88179 -5D25-4982-BFE6-EC5FD59AC103");
            //dmenuCommandId = VsMenus.IDM_VS_CTXT_ITEMNODE; //VsMenus.IDM_VS_CTXT_REFERENCEROOT;


            //    return true;
            //}


            return false;
        }

        public bool TryGetMixedItemsContextMenu(IEnumerable<IProjectTree> projectItems, out Guid menuCommandGuid, out int menuCommandId)
        {
            throw new NotImplementedException();
        }
    }
}
