using System.Collections.Generic;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace SemproJira
{
    public class JiraManager
    {
        public Dictionary<string, List<WorklogRecord>> organisationWorkLogs;
        private readonly ProcessJiraData JiraData;

        public JiraManager()
        {
            JiraData = new ProcessJiraData();
        }

        public void GenerateAllOrganisationWorkLogs()
        {
            var Data = JiraData.GetJiraData();


            organisationWorkLogs = new Dictionary<string, List<WorklogRecord>>();

            foreach (WorklogRecord worklogRecord in Data)
            {
                string organisation = worklogRecord.Organization;
                if (!organisationWorkLogs.ContainsKey(organisation))
                {
                    organisationWorkLogs.Add(organisation, new List<WorklogRecord>());
                }

                organisationWorkLogs[organisation].Add(worklogRecord);
            }

            foreach (var organisation in organisationWorkLogs)
            {
                string filePath = @"c:\jira\" + organisation.Key + ".xlsm";
                CreateExcelFile(filePath, organisationWorkLogs[organisation.Key]);
            }
        }

        private void CreateExcelFile(string filePath, List<WorklogRecord> records)
        {
            using (SpreadsheetDocument document = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.MacroEnabledWorkbook))
            {
                WorkbookPart workbookPart = document.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();

                WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                worksheetPart.Worksheet = new Worksheet(new SheetData());

                Sheets sheets = document.WorkbookPart.Workbook.AppendChild(new Sheets());
                Sheet sheet = new Sheet() { Id = document.WorkbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Jira Data" };
                sheets.Append(sheet);

                SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

                // Add header row
                Row headerRow = new Row() { RowIndex = 1 };
                headerRow.Append(
                    CreateCell("Id", 1),
                    CreateCell("Ticket_Key", 1),
                    CreateCell("Linked_Key", 1),
                    CreateCell("Date", 1),
                    CreateCell("Logged_Hours", 1),
                    CreateCell("Organization", 1),
                    CreateCell("Classification", 1),
                    CreateCell("Type_Ticket", 1),
                    CreateCell("Description", 1),
                    CreateCell("Hour_Type", 1)
                );
                sheetData.Append(headerRow);

                // Add data rows
                uint rowIndex = 2;
                foreach (WorklogRecord w in records)
                {
                    Row dataRow = new Row() { RowIndex = rowIndex };
                    dataRow.Append(
                        CreateCell(w.WorkLogID?.ToString() ?? "", rowIndex),
                        CreateCell(w.IssueKey ?? "", rowIndex),
                        CreateCell(w.LinkedIssueKey ?? "", rowIndex),
                        CreateCell(w.WorkLogDate?.ToString() ?? "", rowIndex),
                        CreateCell(w.TimeSpent.ToString() ?? "", rowIndex),
                        CreateCell(w.Organization ?? "", rowIndex),
                        CreateCell(w.Classification ?? "", rowIndex),
                        CreateCell(w.TypeOfTicket ?? "", rowIndex),
                        CreateCell(w.Comment ?? "", rowIndex),
                        CreateCell(w.HourType ?? "", rowIndex)
                    );
                    sheetData.Append(dataRow);
                    rowIndex++;
                }

                workbookPart.Workbook.Save();
            }
        }

        private Cell CreateCell(string text, uint rowIndex)
        {
            Cell cell = new Cell()
            {
                DataType = CellValues.InlineString,
                CellValue = new CellValue(text)
            };
            cell.Append(new InlineString(new DocumentFormat.OpenXml.Spreadsheet.Text(text)));
            return cell;
        }
    }
}
