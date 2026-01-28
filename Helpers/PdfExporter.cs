using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Fonts;
using JournalApp.Models;

namespace JournalApp.Helpers
{
    // Font Resolver for PdfSharp (Windows): provides actual TTF bytes from C:\Windows\Fonts
    public class CustomFontResolver : IFontResolver
    {
        private static readonly ConcurrentDictionary<string, byte[]> FontDataCache = new();

        private static string MakeKey(string family, bool isBold, bool isItalic)
        {
            if (isBold && isItalic) return $"{family}#bi";
            if (isBold) return $"{family}#b";
            if (isItalic) return $"{family}#i";
            return family;
        }

        public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            // Default to a font that exists on Windows.
            if (string.IsNullOrWhiteSpace(familyName))
                return new FontResolverInfo(MakeKey("Arial", isBold, isItalic));

            string name = familyName.Trim().ToLowerInvariant();

            // Map common requests to Windows fonts we can load from disk
            if (name.Contains("arial") || name.Contains("helvetica"))
                return new FontResolverInfo(MakeKey("Arial", isBold, isItalic));

            if (name.Contains("times"))
                return new FontResolverInfo(MakeKey("TimesNewRoman", isBold, isItalic));

            if (name.Contains("courier"))
                return new FontResolverInfo(MakeKey("CourierNew", isBold, isItalic));

            // Fallback
            return new FontResolverInfo(MakeKey("Arial", isBold, isItalic));
        }

        public byte[]? GetFont(string faceName)
        {
            // IMPORTANT (PdfSharp): this must NOT be null.
            var key = string.IsNullOrWhiteSpace(faceName) ? "Arial" : faceName.Trim();
            return FontDataCache.GetOrAdd(key, LoadFontBytes);
        }

        private static byte[] LoadFontBytes(string key)
        {
            // Windows font files (most machines have these)
            string fileName = key switch
            {
                "Arial" => "arial.ttf",
                "Arial#b" => "arialbd.ttf",
                "Arial#i" => "ariali.ttf",
                "Arial#bi" => "arialbi.ttf",

                "TimesNewRoman" => "times.ttf",
                "TimesNewRoman#b" => "timesbd.ttf",
                "TimesNewRoman#i" => "timesi.ttf",
                "TimesNewRoman#bi" => "timesbi.ttf",

                "CourierNew" => "cour.ttf",
                "CourierNew#b" => "courbd.ttf",
                "CourierNew#i" => "couri.ttf",
                "CourierNew#bi" => "courbi.ttf",

                _ => "arial.ttf"
            };

            string fontsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");
            string path = Path.Combine(fontsDir, fileName);

            if (File.Exists(path))
                return File.ReadAllBytes(path);

            // Fallback to Segoe UI (also commonly present)
            string segoePath = Path.Combine(fontsDir, "segoeui.ttf");
            if (File.Exists(segoePath))
                return File.ReadAllBytes(segoePath);

            // Last resort: return non-null empty bytes (prevents "Byte[] must not be null")
            return Array.Empty<byte>();
        }
    }

    public static class PdfExporter
    {
        private static XFont? _font;
        private static XFont? _boldFont;
        private static readonly object _lockObject = new object();
        private static bool _fontsInitialized = false;

        static PdfExporter()
        {
            // Set up the font resolver for PdfSharp - MUST be set before any font creation
            try
            {
                if (GlobalFontSettings.FontResolver == null)
                {
                    GlobalFontSettings.FontResolver = new CustomFontResolver();
                }
            }
            catch (Exception ex)
            {
                // Log but don't throw - we'll handle during font creation
                System.Diagnostics.Debug.WriteLine($"Font resolver setup warning: {ex.Message}");
            }
        }

        private static void EnsureFontsInitialized()
        {
            if (_fontsInitialized && _font != null && _boldFont != null)
                return;

            lock (_lockObject)
            {
                if (_fontsInitialized && _font != null && _boldFont != null)
                    return;

                // Ensure font resolver is set BEFORE creating any fonts
                try
                {
                    if (GlobalFontSettings.FontResolver == null)
                    {
                        GlobalFontSettings.FontResolver = new CustomFontResolver();
                    }
                }
                catch (Exception resolverEx)
                {
                    // If resolver setup fails, try to continue without it
                    // Some PdfSharp versions might work without explicit resolver
                    System.Diagnostics.Debug.WriteLine($"Font resolver setup issue: {resolverEx.Message}");
                }

                Exception? lastException = null;

                // Try to create fonts - use standard PDF font names
                // These are the 14 standard PDF fonts that are always available
                string[] fontNames = { "Helvetica", "Times-Roman", "Courier" };
                
                foreach (var fontName in fontNames)
                {
                    try
                    {
                        // Create regular font first
                        _font = new XFont(fontName, 12);
                        
                        if (_font == null)
                        {
                            lastException = new InvalidOperationException($"Font creation returned null for {fontName}");
                            continue;
                        }

                        // Try to create bold font
                        try
                        {
                            _boldFont = new XFont(fontName, 14, XFontStyleEx.Bold);
                        }
                        catch
                        {
                            // If bold fails, use regular font for bold (better than nothing)
                            _boldFont = new XFont(fontName, 14);
                        }

                        if (_boldFont == null)
                        {
                            // Fallback: use regular font for bold
                            _boldFont = _font;
                        }

                        // Verify both fonts were created successfully
                        if (_font != null && _boldFont != null)
                        {
                            _fontsInitialized = true;
                            return; // Success!
                        }
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        // Reset fonts before trying next
                        _font = null;
                        _boldFont = null;
                        // Try next font
                        continue;
                    }
                }

                // If we get here, all fonts failed
                throw new InvalidOperationException(
                    $"Failed to initialize fonts for PDF export. " +
                    $"Tried fonts: {string.Join(", ", fontNames)}. " +
                    $"Font resolver is {(GlobalFontSettings.FontResolver == null ? "NULL" : "SET")}. " +
                    $"Last error: {lastException?.Message ?? "Unknown error"}", 
                    lastException);
            }
        }

        public static void ExportToPdf(List<JournalEntry> entries, string filePath)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries), "Journal entries list cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            }

            try
            {
                // Ensure font resolver is set
                if (GlobalFontSettings.FontResolver == null)
                {
                    GlobalFontSettings.FontResolver = new CustomFontResolver();
                }

                // Create fonts directly here (uses the Windows-font resolver above)
                var font = new XFont("Arial", 12);
                var boldFont = new XFont("Arial", 14, XFontStyleEx.Bold);

                var document = new PdfDocument();
                document.Info.Title = "Journal Export";
                document.Info.Author = "Journal App";
                document.Info.CreationDate = DateTime.Now;

            foreach (var entry in entries.Where(e => e != null).OrderBy(e => e.Date))
            {
                // (defensive) skip null entries if any slip in at runtime
                if (entry == null) continue;

                var page = document.AddPage();
                var gfx = XGraphics.FromPdfPage(page);

                double yPos = 50;
                double leftMargin = 50;
                double rightMargin = 550;
                double lineHeight = 20;

                // Date
                gfx.DrawString($"Date: {entry.Date:yyyy-MM-dd}", boldFont, XBrushes.Black,
                    new XRect(leftMargin, yPos, rightMargin - leftMargin, lineHeight),
                    XStringFormats.TopLeft);
                yPos += lineHeight * 1.5;

                // Moods
                if (entry.Moods != null && entry.Moods.Any())
                {
                    var primaryMood = entry.Moods.FirstOrDefault(m => m != null && m.IsPrimary);
                    var secondaryMoods = entry.Moods.Where(m => m != null && !m.IsPrimary).ToList();
                    
                    var moodText = primaryMood != null 
                        ? $"Mood: {primaryMood.Type}"
                        : "Mood: None";
                    
                    if (secondaryMoods.Any())
                    {
                        moodText += $" (Also: {string.Join(", ", secondaryMoods.Select(m => m.Type))})";
                    }
                    
                    gfx.DrawString(moodText, font, XBrushes.Black,
                        new XRect(leftMargin, yPos, rightMargin - leftMargin, lineHeight),
                        XStringFormats.TopLeft);
                    yPos += lineHeight * 1.5;
                }

                // Tags
                if (entry.Tags != null && entry.Tags.Any())
                {
                    var tagNames = entry.Tags.Where(t => t != null && !string.IsNullOrWhiteSpace(t.Name))
                                             .Select(t => t.Name);
                    if (tagNames.Any())
                    {
                        gfx.DrawString($"Tags: {string.Join(", ", tagNames)}", font, XBrushes.Black,
                            new XRect(leftMargin, yPos, rightMargin - leftMargin, lineHeight),
                            XStringFormats.TopLeft);
                        yPos += lineHeight * 1.5;
                    }
                }

                yPos += lineHeight;

                // Content
                var contentLines = WrapText(entry.Content ?? string.Empty, font, rightMargin - leftMargin);
                foreach (var line in contentLines)
                {
                    if (yPos > page.Height - 50)
                    {
                        page = document.AddPage();
                        gfx = XGraphics.FromPdfPage(page);
                        yPos = 50;
                    }

                    gfx.DrawString(line, font, XBrushes.Black,
                        new XRect(leftMargin, yPos, rightMargin - leftMargin, lineHeight),
                        XStringFormats.TopLeft);
                    yPos += lineHeight;
                }
            }

                document.Save(filePath);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error exporting PDF: {ex.Message}", ex);
            }
        }

        private static List<string> WrapText(string text, XFont font, double maxWidth)
        {
            var lines = new List<string>();
            var words = text.Split(' ');
            var currentLine = string.Empty;

            // Create a temporary page and graphics for measuring
            var tempDoc = new PdfDocument();
            var tempPage = tempDoc.AddPage();
            using var tempGfx = XGraphics.FromPdfPage(tempPage);

            foreach (var word in words)
            {
                var testLine = string.IsNullOrEmpty(currentLine) ? word : $"{currentLine} {word}";
                var size = tempGfx.MeasureString(testLine, font);

                if (size.Width > maxWidth && !string.IsNullOrEmpty(currentLine))
                {
                    lines.Add(currentLine);
                    currentLine = word;
                }
                else
                {
                    currentLine = testLine;
                }
            }

            if (!string.IsNullOrEmpty(currentLine))
            {
                lines.Add(currentLine);
            }

            return lines;
        }
    }
}

