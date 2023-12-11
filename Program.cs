using Microsoft.VisualBasic.FileIO;
using System;
using Microsoft.Data.SqlClient;
using Field_Project;
using System.Security;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http.Headers;
using Field_Project.Models;
using static Field_Project.ToolsheetDatumProcessor;
using OfficeOpenXml;
using System.Collections.Generic;
using System.IO;



// Step 0: Load the data from SQL:

Field_Project_Context db = new Field_Project_Context();
List<ToolsheetDatum> rows = new List<ToolsheetDatum>();

foreach (ToolsheetDatum datum in db.ToolsheetData)
{
    rows.Add(datum);
}



// Step 1: Calculate tool usage per machine
ToolsheetDatumProcessor toolsheetDatumProcessor = new ToolsheetDatumProcessor();
Dictionary<string, List<ToolUsage>> toolUsage = toolsheetDatumProcessor.ProcessToolAssignments(rows);

//// Proof of step 1 :
//foreach (var i in toolUsage)
//{
//    foreach (var x in i.Value)
//    {
//        Console.WriteLine(i.Key + "----------" + x.ToString());
//    }
//}



// Step 2: Assign tools to spindle locations based on usage count
MaxSpindleLocationProcessor maxSpindleLocationProcessor = new MaxSpindleLocationProcessor();
Dictionary<string, int> maxSpindleLocations = maxSpindleLocationProcessor.GetMaxSpindleLocation(rows);

//// Proof of step 2:
//foreach (var i in maxSpindleLocations)
//{
//    Console.WriteLine(i.Key + "----------" + i.Value);
//}



// Step 3: Assign tools to spindle locations based on usage count
var toolAssignments = new List<ToolAssignment>();
Dictionary<string, List<string>> machineToTools = new Dictionary<string, List<string>>();
Dictionary<string, List<string>> unusedToolsByMachine = new Dictionary<string, List<string>>();

foreach (var machine in toolUsage)
{
    string machineCode = machine.Key;
    if (maxSpindleLocations.ContainsKey(machineCode))
    {
        int spindleCount = maxSpindleLocations[machineCode];
        List<ToolUsage> tools = machine.Value.OrderByDescending(t => t.UsageCount).ToList();

        for (int i = 0; i < Math.Min(spindleCount, tools.Count); i++)
        {
            ToolAssignment assignment = new ToolAssignment
            {
                Machine = machineCode,
                ToolCode = tools[i].ToolCode,
                SpindleLocation = i + 1
            };

            toolAssignments.Add(assignment);
        }

        // Create dictionanry Machine-Tools after assignation

        foreach (var assignment in toolAssignments)
        {
            if (!machineToTools.ContainsKey(assignment.Machine))
            {
                machineToTools[assignment.Machine] = new List<string>();
            }
            machineToTools[assignment.Machine].Add(assignment.ToolCode);



        }

        // Check what tools are not being assigned to spindle locations
        var usedTools = tools.Take(Math.Min(spindleCount, tools.Count)).Select(t => t.ToolCode).ToList();
        var unusedTools = machine.Value.Select(t => t.ToolCode).Except(usedTools).ToList();


        foreach (var y in unusedTools)
        {
            if (y.Any())
            {
                unusedToolsByMachine[machineCode] = unusedTools;
            }
        }

    }
}


////Proof of step 3
//foreach (var tool in toolAssignments)
//{
//    Console.WriteLine("Machine: " + tool.Machine + "    Tool: " + tool.ToolCode + "     SpindleLocation: " + tool.SpindleLocation);
//}





// Step 4: What Item codes can we build with the current configuration
Dictionary<string, List<string>> toolsByItemCode = new Dictionary<string, List<string>>();

foreach (var datum in rows)
{
    if (datum.item_code != null)
    {
        if (!toolsByItemCode.ContainsKey(datum.item_code))
        {
            toolsByItemCode[datum.item_code] = new List<string>();
        }

        if (!toolsByItemCode[datum.item_code].Contains(datum.tool_code))
        {
            toolsByItemCode[datum.item_code].Add(datum.tool_code);
        }
    }
    else
    {
        break;
    }
}


Dictionary<string, List<string>> machinesToManufacturedItems = new Dictionary<string, List<string>>();

// Iterate through each machine's tools
foreach (var machineTools in machineToTools)
{
    var machine = machineTools.Key;
    var machineToolSet = new HashSet<string>(machineTools.Value);

    // List to store item codes for this machine
    var itemCodesForMachine = new List<string>();

    // Compare machine's tool set with each item's required tool set
    foreach (var itemCodeTools in toolsByItemCode)
    {
        var itemToolsSet = new HashSet<string>(itemCodeTools.Value);

        // Check if all required tools for the item are present in the machine
        if (itemToolsSet.IsSubsetOf(machineToolSet))
        {
            itemCodesForMachine.Add(itemCodeTools.Key);
        }
    }

    if (itemCodesForMachine.Any())
    {
        machinesToManufacturedItems[machine] = itemCodesForMachine;
    }
}

//Proof of work
//foreach (var kvp in machinesToManufacturedItems)
//{
//    foreach (var itemCode in kvp.Value)
//    {
//        Console.WriteLine(kvp.Key + " ---- " + itemCode);
//    }
//}



///////////////////////// OUTPUT EXCEL////////////////////////////

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
string outputPath = @"C:\Users\pablo\Downloads\output.csv";
using (var package = new ExcelPackage())
{
    var worksheet = package.Workbook.Worksheets.Add("Tool Assignments");

    // Machine - Tool - SpindleLocation
    worksheet.Cells[1, 1].Value = "Machine";
    worksheet.Cells[1, 2].Value = "Tool Code";
    worksheet.Cells[1, 3].Value = "Spindle Location";

    int row = 2;
    foreach (var tool in toolAssignments)
    {
        worksheet.Cells[row, 1].Value = tool.Machine;
        worksheet.Cells[row, 2].Value = tool.ToolCode;
        worksheet.Cells[row, 3].Value = tool.SpindleLocation;
        row++;
    }

    // Machine - ItemCode
    var worksheet1 = package.Workbook.Worksheets.Add("Machines and Item Codes");
    worksheet1.Cells[1, 1].Value = "Machine";
    worksheet1.Cells[1, 2].Value = "Item Codes";

    int row1 = 2;
    foreach (var pair in machinesToManufacturedItems)
    {
        worksheet1.Cells[row1, 1].Value = pair.Key;
        int col = 2;
        foreach (var item in pair.Value)
        {
            worksheet1.Cells[row1, col].Value = item;
            row1++;
        }
        row1++;
    }

    // Machine - Unused tools
    var worksheet2 = package.Workbook.Worksheets.Add("Unused Tools By Machine");
    worksheet2.Cells[1, 1].Value = "Machine";
    worksheet2.Cells[1, 2].Value = "Unused Tool";

    int row2 = 2;
    foreach (var pair in unusedToolsByMachine)
    {
        worksheet2.Cells[row2, 1].Value = pair.Key;
        int col = 2;
        foreach (var tool in pair.Value)
        {
            worksheet2.Cells[row2, col].Value = tool;
            row2++;
        }
        row2++;
    }

    FileInfo file = new FileInfo(outputPath);
    package.SaveAs(file);
}
