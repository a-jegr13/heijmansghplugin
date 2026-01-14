using System;
using System.Drawing;
using Grasshopper;
using Grasshopper.Kernel;

namespace BT.DT.Synergy.PK.GHPlugin
{
    public class BT_DT_Synergy_PK_GHPluginInfo : GH_AssemblyInfo
    {
        public override string Name => "Heijmans Synergy";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "Toolbox with custom components developed by Heijmans Synergy.";

        public override Guid Id => new Guid("73e7154b-29e9-462b-8ce0-0ccc5069f4c9");

        //Return a string identifying you or your company.
        public override string AuthorName => "";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "";

        //Return a string representing the version.  This returns the same version as the assembly.
        public override string AssemblyVersion => GetType().Assembly.GetName().Version.ToString();
    }
}