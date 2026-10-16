using System;
using System.IO;
using PdfSharp.Fonts;

namespace CalculusApp.Services
{
    public class WindowsFontResolver : IFontResolver
    {
        public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            string fam = familyName.ToLowerInvariant();
            if (fam.Contains("times") || fam.Contains("cambria") || fam.Contains("serif") || fam.Contains("roman"))
            {
                if (isBold && isItalic) return new FontResolverInfo("Times#bi");
                if (isBold) return new FontResolverInfo("Times#b");
                if (isItalic) return new FontResolverInfo("Times#i");
                return new FontResolverInfo("Times#r");
            }

            // Default to Arial
            if (isBold && isItalic) return new FontResolverInfo("Arial#bi");
            if (isBold) return new FontResolverInfo("Arial#b");
            if (isItalic) return new FontResolverInfo("Arial#i");
            return new FontResolverInfo("Arial#r");
        }

        public byte[]? GetFont(string faceName)
        {
            string fontsDir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            string fileName = faceName switch
            {
                "Times#bi" => "timesbi.ttf",
                "Times#b" => "timesbd.ttf",
                "Times#i" => "timesi.ttf",
                "Times#r" => "times.ttf",
                "Arial#bi" => "arialbi.ttf",
                "Arial#b" => "arialbd.ttf",
                "Arial#i" => "ariali.ttf",
                _ => "arial.ttf"
            };

            string path = Path.Combine(fontsDir, fileName);
            if (File.Exists(path))
            {
                return File.ReadAllBytes(path);
            }

            // Fallback to arial.ttf
            string fallback = Path.Combine(fontsDir, "arial.ttf");
            if (File.Exists(fallback))
            {
                return File.ReadAllBytes(fallback);
            }

            return null;
        }
    }
}
