using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace Field_Project
{
    internal class ItemCode_ToolProcessor
    {
        public string ItemCode { get; set; }
        public List<string> ToolCodes { get; set; }


        public ItemCode_ToolProcessor()
        {
            ItemCode = string.Empty;
            ToolCodes = new List<string>();
        }

        public override string ToString()
        {
            return ItemCode + "------------" + ToolCodes;
        }
    }
}
