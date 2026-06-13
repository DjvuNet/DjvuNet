<#
.SYNOPSIS
    Generates a pre-calculated, interleaved YCbCr binary buffer from a padded BGR image.
#>
param(
    [string]$InputPath = "artifacts\TitanIR-24bgr-padded.png"
)

Add-Type -AssemblyName System.Drawing
$code = @"
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

public class Generator {
    public static unsafe void Run(string inPath) {
        using (Bitmap bmp = new Bitmap(inPath)) {
            int w = bmp.Width;
            int h = bmp.Height;
            // Format24bppRgb enforces a BGR layout in memory (Blue, Green, Red).
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            int stride = data.Stride;
            int totalBytes = stride * h;

            byte[] outBuffer = new byte[totalBytes];
            byte* pSrc = (byte*)data.Scan0;
            
            // LUTs from InterWaveTransform
            int[] redYLUT = new int[256]; int[] greenYLUT = new int[256]; int[] blueYLUT = new int[256];
            int[] redCbLUT = new int[256]; int[] greenCbLUT = new int[256]; int[] blueCbLUT = new int[256];
            int[] redCrLUT = new int[256]; int[] greenCrLUT = new int[256]; int[] blueCrLUT = new int[256];
            for (int i = 0; i < 256; i++) {
                redYLUT[i] = (int)(19946 * i); greenYLUT[i] = (int)(39059 * i); blueYLUT[i] = (int)(6530 * i);
                redCbLUT[i] = (int)(-11397 * i); greenCbLUT[i] = (int)(-22795 * i); blueCbLUT[i] = (int)(34192 * i);
                redCrLUT[i] = (int)(34192 * i); greenCrLUT[i] = (int)(-28601 * i); blueCrLUT[i] = (int)(-5591 * i);
            }

            int padBytes = stride - (w * 3);
            fixed (byte* pDstFixed = outBuffer) {
                sbyte* pDst = (sbyte*)pDstFixed;
                for (int y = 0; y < h; y++) {
                    for (int x = 0; x < w; x++) {
                        // Read BGR memory sequentially
                        byte b = *pSrc++; byte g = *pSrc++; byte r = *pSrc++;
                        
                        int y_val = redYLUT[r] + greenYLUT[g] + blueYLUT[b] + 32768;
                        int cb = redCbLUT[r] + greenCbLUT[g] + blueCbLUT[b] + 32768;
                        int cr = redCrLUT[r] + greenCrLUT[g] + blueCrLUT[b] + 32768;
                        
                        // Map YCbCr into the BGR slots (Y -> Blue, Cb -> Green, Cr -> Red)
                        *pDst++ = (sbyte)((y_val >> 16) - 128);
                        *pDst++ = (sbyte)Math.Max(-128, Math.Min(127, cb >> 16));
                        *pDst++ = (sbyte)Math.Max(-128, Math.Min(127, cr >> 16));
                    }
                    
                    // Copy exact padding bytes (if any) to perfectly preserve the stride layout
                    for (int p = 0; p < padBytes; p++) {
                        *pDst++ = unchecked((sbyte)*pSrc++); 
                    }
                }
            }
            bmp.UnlockBits(data);
            
            // Output format encodes geometry
            string outPath = Path.Combine(Path.GetDirectoryName(inPath), $"TitanIR-{w}x{h}-24bpp-YCbCr.bin");
            File.WriteAllBytes(outPath, outBuffer);
            Console.WriteLine($"Generated interleaved YCbCr binary: {outPath}");
        }
    }
}
"@
Add-Type -TypeDefinition $code -ReferencedAssemblies "System.Drawing"
[Generator]::Run((Resolve-Path $InputPath).Path)
Write-Host "Done."