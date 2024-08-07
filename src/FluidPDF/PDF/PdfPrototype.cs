﻿using FluidPDF.Core.PDFSharp;
using Kyklos.Kernel.Core.Asserts;
using PuppeteerSharp;
using System.IO;
using System.Threading.Tasks;

namespace FluidPDF.Core.PDF
{
    internal class PdfPrototype : IPdfPrototype
    {
        internal IBrowser Browser { get; }
        internal IPage Page { get; }
        internal PdfOptions PdfOptions { get; }
        internal bool ToBeCompressed { get; }

        internal PdfPrototype(IBrowser browser, IPage page, PdfOptions pdfOptions, bool toBeCompressed)
        {
            Browser = browser.GetNonNullOrThrow(nameof(browser));
            Page = page.GetNonNullOrThrow(nameof(page));
            PdfOptions = pdfOptions.GetNonNullOrThrow(nameof(pdfOptions));
            ToBeCompressed = toBeCompressed;
        }

        public async Task<byte[]> ToByteArrayAsync()
        {
            if (!ToBeCompressed)
            {
                return await Page.PdfDataAsync(PdfOptions).ConfigureAwait(false);
            }

            using Stream stream = await Page.PdfStreamAsync(PdfOptions).ConfigureAwait(false);
            return PDFSharpUtils.RegeneratePDF(stream);
        }

        public async Task<Stream> ToStreamAsync()
        {
            using Stream stream = await Page.PdfStreamAsync(PdfOptions).ConfigureAwait(false);
            if (!ToBeCompressed)
            {
                return stream;
            }
            Stream outputStream = new MemoryStream();
            PDFSharpUtils.RegeneratePDF(stream, outputStream);
            stream.Position = 0;
            return outputStream;
        }

        public async Task ToFileAsync(string filePath)
        {
            if (!ToBeCompressed)
            {
                await Page.PdfAsync(filePath, PdfOptions);
            }
            using FileStream outputStream = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write);
            using Stream stream = await Page.PdfStreamAsync(PdfOptions).ConfigureAwait(false);
            PDFSharpUtils.RegeneratePDF(stream, outputStream);
        }

        public void Dispose() => DisposeAsync().GetAwaiter().GetResult();

        public async ValueTask DisposeAsync()
        {
            await Page.CloseAsync().ConfigureAwait(false);
            await Browser.CloseAsync().ConfigureAwait(false);
            Page.Dispose();
            Browser.Dispose();
        }
    }
}
