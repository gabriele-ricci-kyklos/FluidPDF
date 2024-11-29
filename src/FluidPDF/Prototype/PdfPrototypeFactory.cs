﻿using FluidPDF.Fluid;
using FluidPDF.PuppeteerSharp;
using FluidPDF.Support;
using PuppeteerSharp;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;

namespace FluidPDF.Prototype
{
    internal class PdfPrototypeFactory
    {
        private readonly ChromiumRetrieverOptions _chromiumRetrieverOptions;
        private readonly PdfPrototypeFactoryOptions _fluidPdfOptions;

        internal PdfPrototypeFactory(ChromiumRetrieverOptions chromiumRetrieverOptions, PdfPrototypeFactoryOptions fluidPDFOptions)
        {
            _chromiumRetrieverOptions = chromiumRetrieverOptions.GetNonNullOrThrow(nameof(chromiumRetrieverOptions));
            _fluidPdfOptions = fluidPDFOptions.GetNonNullOrThrow(nameof(fluidPDFOptions));
        }

        internal async Task<IPdfPrototype> NewPdfPrototypeAsync<T>(string template, T model, bool toBeCompressed, CultureInfo? cultureInfo = null)
            where T : notnull
        {
            string renderedTemplate = await RenderTemplateByTypeAsync(template, model, cultureInfo).ConfigureAwait(false);

            IBrowser browser = await ChromiumRetriever.RetrieveBrowserInstanceAsync(_chromiumRetrieverOptions).ConfigureAwait(false);
            IPage page = await browser.NewPageAsync().ConfigureAwait(false);

            await page.SetContentAsync(renderedTemplate).ConfigureAwait(false);

            PdfOptions pdfOptions =
                new()
                {
                    PreferCSSPageSize = true,
                    PrintBackground = true,
                    Format = _fluidPdfOptions.Format,
                    Landscape = _fluidPdfOptions.Landscape,
                    MarginOptions = _fluidPdfOptions.MarginOptions,
                    Scale = _fluidPdfOptions.Scale
                };

            PdfPrototype prototype = new(browser, page, pdfOptions, toBeCompressed);
            return prototype;
        }

        private ValueTask<string> RenderTemplateByTypeAsync<T>(string template, T model, CultureInfo? cultureInfo = null)
            where T : notnull =>
            {
                DataRow => FluidTemplateHelper.RenderWithDataRowAsync(template, (model as DataRow)!, cultureInfo: cultureInfo, encodeHtml: true),
                Dictionary<string, object> => FluidTemplateHelper.RenderWithDictionaryAsync(template, (model as Dictionary<string, object>)!, cultureInfo: cultureInfo, encodeHtml: true),
                string => FluidTemplateHelper.RenderWithJsonStringAsync(template, (model as string)!, cultureInfo: cultureInfo, encodeHtml: true),
                FluidModel[] => FluidTemplateHelper.RenderWithMultipleModelsAsync(template, model as FluidModel[] ?? [], cultureInfo: cultureInfo, encodeHtml: true),
                _ => FluidTemplateHelper.RenderWithObjectAsync(template, model)
            };
    }
}
