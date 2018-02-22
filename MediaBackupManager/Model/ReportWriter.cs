using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;

namespace MediaBackupManager.Model
{
    /// <summary>
    /// Support class used to generate HTML reports.</summary>
    public static class ReportWriter
    {
        //TODO: File Report
        //TODO: Checksum centric report
        //TODO: Report for nodes without hash

        /// <summary>
        /// Generates a report containing details about all items and folders in the provided archives.</summary>
        /// <returns>Returns the file path of the generated report if it was successfully created.</returns>
        public static async Task<string> GenerateFileListReport(List<Archive> exportArchiveList, string ReportPath)
        {
            using (var sw = new StringWriter())
            {
                using (var writer = new HtmlTextWriter(sw))
                {
                    writer.RenderBeginTag(HtmlTextWriterTag.Div); // div1

                    // Report header
                    writer.AddStyleAttribute(HtmlTextWriterStyle.MarginBottom, "5px");
                    writer.RenderBeginTag(HtmlTextWriterTag.H2);
                    await writer.WriteAsync("Media Backup Manager File List Report");
                    writer.RenderEndTag();

                    writer.AddStyleAttribute(HtmlTextWriterStyle.Color, "grey");
                    writer.AddStyleAttribute(HtmlTextWriterStyle.FontSize, "small");
                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                    await writer.WriteAsync("Generated: " + DateTime.Now);
                    writer.RenderEndTag();

                    // Generate a separate table for each provided archive
                    foreach (var exportArchive in exportArchiveList)
                    {
                        //archive header

                        writer.AddStyleAttribute(HtmlTextWriterStyle.MarginBottom, "5px");
                        writer.RenderBeginTag(HtmlTextWriterTag.H3);
                        await writer.WriteAsync(exportArchive.Label);
                        writer.RenderEndTag();

                        writer.AddStyleAttribute(HtmlTextWriterStyle.Color, "grey");
                        writer.AddStyleAttribute(HtmlTextWriterStyle.FontSize, "small");
                        writer.RenderBeginTag(HtmlTextWriterTag.Span);
                        await writer.WriteAsync("Root Directory Path: " + exportArchive.RootDirectoryPath + " | Volume Serial Number: " + exportArchive.Volume.SerialNumber + " | Last Scan date: " + exportArchive.LastScanDate);
                        writer.RenderEndTag();

                        writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "15");
                        writer.AddStyleAttribute(HtmlTextWriterStyle.Width, "100%");
                        writer.AddStyleAttribute(HtmlTextWriterStyle.BorderCollapse, "collapse");
                        writer.RenderBeginTag(HtmlTextWriterTag.Table); // table

                        // table header
                        writer.AddStyleAttribute(HtmlTextWriterStyle.FontWeight, "Bold");
                        writer.AddStyleAttribute(HtmlTextWriterStyle.Color, "white");
                        writer.AddStyleAttribute(HtmlTextWriterStyle.TextAlign, "left");
                        writer.AddStyleAttribute(HtmlTextWriterStyle.BackgroundColor, "dimgrey");
                        writer.RenderBeginTag(HtmlTextWriterTag.Tr);


                        writer.RenderBeginTag(HtmlTextWriterTag.Th);
                        await writer.WriteAsync("Archive");
                        writer.RenderEndTag();

                        writer.RenderBeginTag(HtmlTextWriterTag.Th);
                        await writer.WriteAsync("DirectoryName");
                        writer.RenderEndTag();

                        writer.RenderBeginTag(HtmlTextWriterTag.Th);
                        await writer.WriteAsync("Name");
                        writer.RenderEndTag();

                        writer.RenderBeginTag(HtmlTextWriterTag.Th);
                        await writer.WriteAsync("Extension");
                        writer.RenderEndTag();

                        writer.RenderBeginTag(HtmlTextWriterTag.Th);
                        await writer.WriteAsync("Copies");
                        writer.RenderEndTag();

                        writer.RenderBeginTag(HtmlTextWriterTag.Th);
                        await writer.WriteAsync("Checksum");
                        writer.RenderEndTag();

                        writer.RenderEndTag(); // Tr

                        bool evenLine = true;
                        // table content
                        foreach (var node in exportArchive.GetFileNodes())
                        {
                            if (evenLine)
                                writer.AddStyleAttribute(HtmlTextWriterStyle.BackgroundColor, "aliceblue");

                            evenLine = !evenLine;

                            writer.RenderBeginTag(HtmlTextWriterTag.Tr);

                            writer.RenderBeginTag(HtmlTextWriterTag.Td);
                            await writer.WriteAsync(node.Archive.Label);
                            writer.RenderEndTag();
                            
                            writer.RenderBeginTag(HtmlTextWriterTag.Td);
                            await writer.WriteAsync(node.DirectoryName);
                            writer.RenderEndTag();

                            writer.RenderBeginTag(HtmlTextWriterTag.Td);
                            await writer.WriteAsync(node.Name);
                            writer.RenderEndTag();

                            writer.RenderBeginTag(HtmlTextWriterTag.Td);
                            await writer.WriteAsync(node.Extension);
                            writer.RenderEndTag();

                            writer.RenderBeginTag(HtmlTextWriterTag.Td);
                            await writer.WriteAsync(node.BackupCount.ToString());
                            writer.RenderEndTag();

                            writer.RenderBeginTag(HtmlTextWriterTag.Td);
                            await writer.WriteAsync(node.Checksum);
                            writer.RenderEndTag();

                            writer.RenderEndTag(); // Tr
                        }

                        writer.RenderEndTag(); // table
                    }

                    writer.RenderEndTag(); // div1
                }
                //return sw.ToString();
                return await WriteFileAsync(sw.ToString(), ReportPath);
            }
        }

        /// <summary>
        /// Generates a report containing details about all file hashes and thei related nodes in the provided archives.</summary>
        /// <returns>Returns the file path of the generated report if it was successfully created.</returns>
        public static async Task<string> GenerateFileHashReport(List<Archive> exportArchiveList, string ReportPath)
        {
            // build a distinct list of all file hashes in the provided archives
            var exportHashes = new HashSet<FileHash>();

            foreach (var archive in exportArchiveList)
            {
                foreach (var hash in archive.GetFileHashes())
                {
                    exportHashes.Add(hash);
                }
            }

            using (var sw = new StringWriter())
            {
                using (var writer = new HtmlTextWriter(sw))
                {
                    writer.RenderBeginTag(HtmlTextWriterTag.Div); // div1

                    // Report header
                    writer.AddStyleAttribute(HtmlTextWriterStyle.MarginBottom, "5px");
                    writer.RenderBeginTag(HtmlTextWriterTag.H2);
                    await writer.WriteAsync("Media Backup Manager File Hash Report");
                    writer.RenderEndTag();

                    writer.AddStyleAttribute(HtmlTextWriterStyle.Color, "grey");
                    writer.AddStyleAttribute(HtmlTextWriterStyle.FontSize, "small");
                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                    await writer.WriteAsync("Generated: " + DateTime.Now);
                    writer.RenderEndTag();

                    writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "15");
                    writer.AddStyleAttribute(HtmlTextWriterStyle.Width, "100%");
                    writer.AddStyleAttribute(HtmlTextWriterStyle.BorderCollapse, "collapse");
                    writer.RenderBeginTag(HtmlTextWriterTag.Table); // table

                    // table header
                    writer.AddStyleAttribute(HtmlTextWriterStyle.FontWeight, "Bold");
                    writer.AddStyleAttribute(HtmlTextWriterStyle.Color, "white");
                    writer.AddStyleAttribute(HtmlTextWriterStyle.TextAlign, "left");
                    writer.AddStyleAttribute(HtmlTextWriterStyle.BackgroundColor, "dimgrey");
                    writer.RenderBeginTag(HtmlTextWriterTag.Tr);

                    writer.RenderBeginTag(HtmlTextWriterTag.Th);
                    await writer.WriteAsync("Checksum");
                    writer.RenderEndTag();

                    writer.RenderBeginTag(HtmlTextWriterTag.Th);
                    await writer.WriteAsync("Archive");
                    writer.RenderEndTag();

                    writer.RenderBeginTag(HtmlTextWriterTag.Th);
                    await writer.WriteAsync("DirectoryName");
                    writer.RenderEndTag();

                    writer.RenderBeginTag(HtmlTextWriterTag.Th);
                    await writer.WriteAsync("Name");
                    writer.RenderEndTag();

                    writer.RenderBeginTag(HtmlTextWriterTag.Th);
                    await writer.WriteAsync("Extension");
                    writer.RenderEndTag();

                    writer.RenderEndTag(); // Tr

                    bool evenLine = true;
                    bool evenFileLine = true;
                    string currentChecksum = "";

                    // table content
                    foreach (var hash in exportHashes)
                    {
                        foreach (var node in hash.Nodes)
                        {

                            if (hash.Checksum != currentChecksum)
                            {
                                //Add some additional formatting whenever changing the current hash
                                writer.AddStyleAttribute(HtmlTextWriterStyle.BorderStyle, "solid none none none");
                                writer.AddStyleAttribute(HtmlTextWriterStyle.BorderWidth, "1px");
                                evenFileLine = evenLine; // make sure that the first line of each node has the same color as its hash
                            }

                            if (evenFileLine = !evenFileLine)
                                writer.AddStyleAttribute(HtmlTextWriterStyle.BackgroundColor, "aliceblue");

                            writer.RenderBeginTag(HtmlTextWriterTag.Tr);

                            if(hash.Checksum != currentChecksum)
                            {
                                currentChecksum = hash.Checksum;
                                // new hash, add cell information
                                if (evenLine = !evenLine)
                                    writer.AddStyleAttribute(HtmlTextWriterStyle.BackgroundColor, "aliceblue");
                                else
                                    writer.AddStyleAttribute(HtmlTextWriterStyle.BackgroundColor, "white");

                                writer.AddAttribute(HtmlTextWriterAttribute.Rowspan, hash.NodeCount.ToString());
                                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                                await writer.WriteAsync(hash.Checksum);
                                writer.RenderEndTag();
                            }

                            writer.RenderBeginTag(HtmlTextWriterTag.Td);
                            await writer.WriteAsync(node.Archive.Label);
                            writer.RenderEndTag();

                            writer.RenderBeginTag(HtmlTextWriterTag.Td);
                            await writer.WriteAsync(node.DirectoryName);
                            writer.RenderEndTag();

                            writer.RenderBeginTag(HtmlTextWriterTag.Td);
                            await writer.WriteAsync(node.Name);
                            writer.RenderEndTag();

                            writer.RenderBeginTag(HtmlTextWriterTag.Td);
                            await writer.WriteAsync(node.Extension);
                            writer.RenderEndTag();

                            writer.RenderEndTag(); // Tr
                        }
                    }

                    writer.RenderEndTag(); // table
                    writer.RenderEndTag(); // div1
                }
                //return sw.ToString();
                return await WriteFileAsync(sw.ToString(), ReportPath);
            }
        }

        /// <summary>
        /// Generates a report containing details about all file nodes with missing hash.</summary>
        /// <returns>Returns the file path of the generated report if it was successfully created.</returns>
        public static async Task<string> GenerateMissingFileHashReport(List<Archive> exportArchiveList, string ReportPath)
        {
            using (var sw = new StringWriter())
            {
                using (var writer = new HtmlTextWriter(sw))
                {
                    writer.RenderBeginTag(HtmlTextWriterTag.Div); // div1

                    // Report header
                    writer.AddStyleAttribute(HtmlTextWriterStyle.MarginBottom, "5px");
                    writer.RenderBeginTag(HtmlTextWriterTag.H2);
                    await writer.WriteAsync("Media Backup Manager Missing Hash Report");
                    writer.RenderEndTag();

                    writer.AddStyleAttribute(HtmlTextWriterStyle.Color, "grey");
                    writer.AddStyleAttribute(HtmlTextWriterStyle.FontSize, "small");
                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                    await writer.WriteAsync("Generated: " + DateTime.Now);
                    writer.RenderEndTag();

                    // Generate a separate table for each provided archive
                    foreach (var exportArchive in exportArchiveList)
                    {
                        var exportNodes = exportArchive.GetFileNodes().Where(x => x.Hash is null);
                        if (exportNodes.Count() == 0)
                            continue;

                        //archive header

                        writer.AddStyleAttribute(HtmlTextWriterStyle.MarginBottom, "5px");
                        writer.RenderBeginTag(HtmlTextWriterTag.H3);
                        await writer.WriteAsync(exportArchive.Label);
                        writer.RenderEndTag();

                        writer.AddStyleAttribute(HtmlTextWriterStyle.Color, "grey");
                        writer.AddStyleAttribute(HtmlTextWriterStyle.FontSize, "small");
                        writer.RenderBeginTag(HtmlTextWriterTag.Span);
                        await writer.WriteAsync("Root Directory Path: " + exportArchive.RootDirectoryPath + " | Volume Serial Number: " + exportArchive.Volume.SerialNumber + " | Last Scan date: " + exportArchive.LastScanDate);
                        writer.RenderEndTag();

                        writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "15");
                        writer.AddStyleAttribute(HtmlTextWriterStyle.Width, "100%");
                        writer.AddStyleAttribute(HtmlTextWriterStyle.BorderCollapse, "collapse");
                        writer.RenderBeginTag(HtmlTextWriterTag.Table); // table

                        // table header
                        writer.AddStyleAttribute(HtmlTextWriterStyle.FontWeight, "Bold");
                        writer.AddStyleAttribute(HtmlTextWriterStyle.Color, "white");
                        writer.AddStyleAttribute(HtmlTextWriterStyle.TextAlign, "left");
                        writer.AddStyleAttribute(HtmlTextWriterStyle.BackgroundColor, "dimgrey");
                        writer.RenderBeginTag(HtmlTextWriterTag.Tr);


                        writer.RenderBeginTag(HtmlTextWriterTag.Th);
                        await writer.WriteAsync("Archive");
                        writer.RenderEndTag();

                        writer.RenderBeginTag(HtmlTextWriterTag.Th);
                        await writer.WriteAsync("DirectoryName");
                        writer.RenderEndTag();

                        writer.RenderBeginTag(HtmlTextWriterTag.Th);
                        await writer.WriteAsync("Name");
                        writer.RenderEndTag();

                        writer.RenderBeginTag(HtmlTextWriterTag.Th);
                        await writer.WriteAsync("Extension");
                        writer.RenderEndTag();

                        writer.RenderEndTag(); // Tr

                        bool evenLine = true;
                        // table content
                        foreach (var node in exportNodes)
                        {
                            if (evenLine)
                                writer.AddStyleAttribute(HtmlTextWriterStyle.BackgroundColor, "aliceblue");

                            evenLine = !evenLine;

                            writer.RenderBeginTag(HtmlTextWriterTag.Tr);

                            writer.RenderBeginTag(HtmlTextWriterTag.Td);
                            await writer.WriteAsync(node.Archive.Label);
                            writer.RenderEndTag();

                            writer.RenderBeginTag(HtmlTextWriterTag.Td);
                            await writer.WriteAsync(node.DirectoryName);
                            writer.RenderEndTag();

                            writer.RenderBeginTag(HtmlTextWriterTag.Td);
                            await writer.WriteAsync(node.Name);
                            writer.RenderEndTag();

                            writer.RenderBeginTag(HtmlTextWriterTag.Td);
                            await writer.WriteAsync(node.Extension);
                            writer.RenderEndTag();

                            writer.RenderEndTag(); // Tr
                        }

                        writer.RenderEndTag(); // table
                    }

                    writer.RenderEndTag(); // div1
                }
                //return sw.ToString();
                return await WriteFileAsync(sw.ToString(), ReportPath);
            }
        }

        /// <summary>
        /// Generates a report containing details about all file nodes with a backup count of 1.</summary>
        /// <returns>Returns the file path of the generated report if it was successfully created.</returns>
        public static async Task<string> GenerateMissingBackupReport(List<Archive> exportArchiveList, string ReportPath)
        {
            using (var sw = new StringWriter())
            {
                using (var writer = new HtmlTextWriter(sw))
                {
                    writer.RenderBeginTag(HtmlTextWriterTag.Div); // div1

                    // Report header
                    writer.AddStyleAttribute(HtmlTextWriterStyle.MarginBottom, "5px");
                    writer.RenderBeginTag(HtmlTextWriterTag.H2);
                    await writer.WriteAsync("Media Backup Manager Missing Backup Report");
                    writer.RenderEndTag();

                    writer.AddStyleAttribute(HtmlTextWriterStyle.Color, "grey");
                    writer.AddStyleAttribute(HtmlTextWriterStyle.FontSize, "small");
                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                    await writer.WriteAsync("Generated: " + DateTime.Now);
                    writer.RenderEndTag();

                    // Generate a separate table for each provided archive
                    foreach (var exportArchive in exportArchiveList)
                    {
                        var exportNodes = exportArchive.GetFileNodes().Where(x => x.BackupCount == 1);
                        if (exportNodes.Count() == 0)
                            continue;

                        //archive header

                        writer.AddStyleAttribute(HtmlTextWriterStyle.MarginBottom, "5px");
                        writer.RenderBeginTag(HtmlTextWriterTag.H3);
                        await writer.WriteAsync(exportArchive.Label);
                        writer.RenderEndTag();

                        writer.AddStyleAttribute(HtmlTextWriterStyle.Color, "grey");
                        writer.AddStyleAttribute(HtmlTextWriterStyle.FontSize, "small");
                        writer.RenderBeginTag(HtmlTextWriterTag.Span);
                        await writer.WriteAsync("Root Directory Path: " + exportArchive.RootDirectoryPath + " | Volume Serial Number: " + exportArchive.Volume.SerialNumber + " | Last Scan date: " + exportArchive.LastScanDate);
                        writer.RenderEndTag();

                        writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "15");
                        writer.AddStyleAttribute(HtmlTextWriterStyle.Width, "100%");
                        writer.AddStyleAttribute(HtmlTextWriterStyle.BorderCollapse, "collapse");
                        writer.RenderBeginTag(HtmlTextWriterTag.Table); // table

                        // table header
                        writer.AddStyleAttribute(HtmlTextWriterStyle.FontWeight, "Bold");
                        writer.AddStyleAttribute(HtmlTextWriterStyle.Color, "white");
                        writer.AddStyleAttribute(HtmlTextWriterStyle.TextAlign, "left");
                        writer.AddStyleAttribute(HtmlTextWriterStyle.BackgroundColor, "dimgrey");
                        writer.RenderBeginTag(HtmlTextWriterTag.Tr);


                        writer.RenderBeginTag(HtmlTextWriterTag.Th);
                        await writer.WriteAsync("Archive");
                        writer.RenderEndTag();

                        writer.RenderBeginTag(HtmlTextWriterTag.Th);
                        await writer.WriteAsync("DirectoryName");
                        writer.RenderEndTag();

                        writer.RenderBeginTag(HtmlTextWriterTag.Th);
                        await writer.WriteAsync("Name");
                        writer.RenderEndTag();

                        writer.RenderBeginTag(HtmlTextWriterTag.Th);
                        await writer.WriteAsync("Extension");
                        writer.RenderEndTag();

                        writer.RenderEndTag(); // Tr

                        bool evenLine = true;
                        // table content
                        foreach (var node in exportNodes)
                        {
                            if (evenLine)
                                writer.AddStyleAttribute(HtmlTextWriterStyle.BackgroundColor, "aliceblue");

                            evenLine = !evenLine;

                            writer.RenderBeginTag(HtmlTextWriterTag.Tr);

                            writer.RenderBeginTag(HtmlTextWriterTag.Td);
                            await writer.WriteAsync(node.Archive.Label);
                            writer.RenderEndTag();

                            writer.RenderBeginTag(HtmlTextWriterTag.Td);
                            await writer.WriteAsync(node.DirectoryName);
                            writer.RenderEndTag();

                            writer.RenderBeginTag(HtmlTextWriterTag.Td);
                            await writer.WriteAsync(node.Name);
                            writer.RenderEndTag();

                            writer.RenderBeginTag(HtmlTextWriterTag.Td);
                            await writer.WriteAsync(node.Extension);
                            writer.RenderEndTag();

                            writer.RenderEndTag(); // Tr
                        }

                        writer.RenderEndTag(); // table
                    }

                    writer.RenderEndTag(); // div1
                }
                //return sw.ToString();
                return await WriteFileAsync(sw.ToString(), ReportPath);
            }
        }

        /// <summary>
        /// Writes a string as file at the provided file path.</summary>
        /// <returns>Returns the file path of the generated file if the export was successful.</returns>
        private static async Task<string> WriteFileAsync(string text, string filePath)
        {
            var outFile = new FileInfo(filePath);

            if (!outFile.Directory.Exists)
                throw new ApplicationException("Directory " + outFile.Directory + "could not be found.");

            try
            {
                using (var sw = new StreamWriter(outFile.FullName, false, Encoding.UTF8))
                {
                    try
                    {
                        await sw.WriteLineAsync(text);
                    }
                    catch (IOException ex)
                    {
                        throw new ApplicationException("Error while writing data to a report.", ex);
                    }
                }
            }
            catch (IOException ex)
            {
                throw new ApplicationException("Error while creating a report.", ex);
            }

            return filePath;
        }
    }
}
