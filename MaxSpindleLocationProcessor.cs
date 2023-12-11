using Field_Project.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using static Field_Project.ToolsheetDatumProcessor;

namespace Field_Project
{
    internal class MaxSpindleLocationProcessor
    {
        public string Machine { get;  set; }
        public int MaxSpindleLocation { get;  set; }

        public MaxSpindleLocationProcessor()
        {
            Machine = string.Empty;
            MaxSpindleLocation = 0;
        }

        public override string ToString()
        {
            return Machine + "------------" + MaxSpindleLocation;
        }



        public Dictionary<string, int> GetMaxSpindleLocation(List<ToolsheetDatum> data)
        {
            var maxSpindleLocations = new Dictionary<string, int>();
            foreach (var datum in data)
            {
                if (!maxSpindleLocations.ContainsKey(datum.Machine))
                {
                    maxSpindleLocations[datum.Machine] = (int)(datum.spindle_location ?? 0);
                }
                else
                {
                    int currentMax = maxSpindleLocations[datum.Machine];
                    int spindleLocation = (int)(datum.spindle_location ?? 0);
                    maxSpindleLocations[datum.Machine] = Math.Max(currentMax, spindleLocation);
                }
            }
            return maxSpindleLocations;
        }
    }
}
