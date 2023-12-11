using Field_Project.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Field_Project.ToolsheetDatumProcessor;

namespace Field_Project
{
    public class ToolsheetDatumProcessor
    {
        public class ToolUsage
        {
            public string ToolCode { get; set; }
            public int UsageCount { get; set; }

            public ToolUsage()
            {
                ToolCode = string.Empty;
                UsageCount = 0;
            }

            public override string ToString()
            {
                return ToolCode + " " + UsageCount;
            }
        }

        public class ToolAssignment
        {
            public string Machine { get; set; }
            public string ToolCode { get; set; }
            public int SpindleLocation { get; set; }

            public ToolAssignment()
            {
                Machine = string.Empty;
                ToolCode = string.Empty;
                SpindleLocation = 0;
            }

            public override string ToString()
            {
                return Machine + " " + ToolCode + " " + SpindleLocation;
            }
        }

        public Dictionary<string, List<ToolUsage>> ProcessToolAssignments(List<ToolsheetDatum> data)
        {
            var machineToolUsage = new Dictionary<string, List<ToolUsage>>();
            foreach (var datum in data)
            {
                if (!machineToolUsage.ContainsKey(datum.Machine))
                {
                    machineToolUsage[datum.Machine] = new List<ToolUsage>();
                }

                var existingTool = machineToolUsage[datum.Machine]
                    .FirstOrDefault(t => t.ToolCode == datum.tool_code);

                if (existingTool != null)
                {
                    existingTool.UsageCount++;
                }
                else
                {
                    machineToolUsage[datum.Machine].Add(new ToolUsage
                    {
                        ToolCode = datum.tool_code,
                        UsageCount = 1
                    });
                }
            }

            var sortedMachineToolUsage = machineToolUsage.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.OrderByDescending(t => t.UsageCount).ToList()
        );

            return sortedMachineToolUsage;
        }
    }
}
