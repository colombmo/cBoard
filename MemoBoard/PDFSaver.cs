using System;
using System.IO;
using System.IO.Packaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;

namespace MemoBoard {
    class PDFSaver {

        public static void generatePDF(FrameworkElement surface, Uri pOut) {
            FixedDocument fixedDoc = new FixedDocument();
            PageContent pageContent = new PageContent();
            FixedPage fixedPage = new FixedPage();

            // Set width and height as the ones of the canvas
            fixedPage.Width = ((Window)((Grid)surface.Parent).Parent).Width;
            fixedPage.Height = ((Window)((Grid)surface.Parent).Parent).Height;
            // Surface can't have two parent, so save one and restore it at the end
            Grid par = (Grid)surface.Parent;
            par.Children.Remove(surface);
            
            //Create first page of document
            fixedPage.Children.Add(surface);
            ((System.Windows.Markup.IAddChild)pageContent).AddChild(fixedPage);
            fixedDoc.Pages.Add(pageContent);

            MemoryStream lMemoryStream = new MemoryStream();
            {
                // Open new package
                Package package = Package.Open(lMemoryStream, FileMode.Create);
                // Create new xps document based on the package opened
                XpsDocument doc = new XpsDocument(package);
                // Create an instance of XpsDocumentWriter for the document
                XpsDocumentWriter writer = XpsDocument.CreateXpsDocumentWriter(doc);
                // Write the canvas (as Visual) to the document
                writer.Write(fixedDoc);
                // Close document
                doc.Close();
                // Close package
                package.Close();
            }

            // Convert XPS to PDF
            var pdfXpsDoc = PdfSharp.Xps.XpsModel.XpsDocument.Open(lMemoryStream);
            PdfSharp.Xps.XpsConverter.Convert(pdfXpsDoc, pOut.LocalPath, 0);

            fixedPage.Children.Remove(surface);
            // Add again in position 0, else the GUI is behind the drawing canvas and it becomes hidden
            par.Children.Insert(0, surface);
        }
    }
}
