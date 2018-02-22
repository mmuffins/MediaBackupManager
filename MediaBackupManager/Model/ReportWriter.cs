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
        public static async Task GenerateArchiveReport(Archive exportArchive)
        {
            using (var sw = new StringWriter())
            {
                using (var writer = new HtmlTextWriter(sw))
                {
                    writer.RenderBeginTag(HtmlTextWriterTag.Div); // div1

                    // Report header
                    writer.AddStyleAttribute(HtmlTextWriterStyle.MarginBottom, "5px");
                    writer.RenderBeginTag(HtmlTextWriterTag.H2);
                    await writer.WriteAsync("Media Backup Manager Archive Report");
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
                    await writer.WriteAsync("Name");
                    writer.RenderEndTag();

                    writer.RenderBeginTag(HtmlTextWriterTag.Th);
                    await writer.WriteAsync("Extension");
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
                        await writer.WriteAsync(node.Name);
                        writer.RenderEndTag();

                        writer.RenderBeginTag(HtmlTextWriterTag.Td);
                        await writer.WriteAsync(node.Extension);
                        writer.RenderEndTag();

                        writer.RenderBeginTag(HtmlTextWriterTag.Td);
                        await writer.WriteAsync(node.Checksum);
                        writer.RenderEndTag();

                        writer.RenderEndTag(); // Tr
                    }

                    writer.RenderEndTag(); // table
                    writer.RenderEndTag(); // div1
                }
                //return sw.ToString();
                await WriteFileAsync(sw.ToString(), @"C:\temp\test");
            }
        }

        /// <summary>
        /// Writes a string as file at the provided file path.</summary>  
        private static async Task WriteFileAsync(string text, string filePath)
        {
            var outFile = new FileInfo(filePath);

            if (!outFile.Directory.Exists)
                throw new ApplicationException("Directory " + outFile.Directory + "could not be found.");

            // make sure that the file is crated as html
            if (outFile.Extension != "html")
                outFile = new FileInfo( Path.ChangeExtension(outFile.FullName, "html"));

            try
            {
                using (StreamWriter sw = new StreamWriter(outFile.FullName, false, Encoding.UTF8))
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
        }
    }
}
