using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
namespace GradCorrection
{
    public partial class GradCorrect : Form
    {
        private double currentContrast = 1.0;
        private int currentBrightness = 0;
        private double currentGamma = 1.0;
        private double currentK = 0.1;
        private double currentLogFactor = 1.0;
        private double shadowAmount = 1.0; 
        private double highlightAmount = 1.0; 
        private double shadowThreshold = 85; 
        private double highlightThreshold = 170; 
        private Bitmap originalImage;
        private Bitmap processedImage;
        private double saturationLevel = 1.0; 
        private Color shadowColor = Color.FromArgb(50, 30, 20);  
        private Color highlightColor = Color.FromArgb(200, 180, 150); 
        private double toneAmount = 0.5;  
        private void ClearUnits()
        {
            currentContrast = 1.0;
            currentBrightness = 0;
            currentGamma = 1.0;
            currentK = 0.1;
            currentLogFactor = 1.0;
            shadowAmount = 1.0;
            highlightAmount = 1.0;
            shadowThreshold = 85;
            highlightThreshold = 170;
            saturationLevel = 1.0;
            toneAmount = 0.5;
        }
        public GradCorrect()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.SizeGripStyle = SizeGripStyle.Hide;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }
        public static class MathHelper
        {
            public static int Clamp(int value, int min, int max)
            {
                return (value < min) ? min : (value > max) ? max : value;
            }
            public static double Clamp(double value, double min, double max)
            {
                return (value < min) ? min : (value > max) ? max : value;
            }
        }
        private void LoadImage_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Images|*.jpg;*.png;*.bmp"; 
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                originalImage?.Dispose();
                originalImage = new Bitmap(dialog.FileName);
                picOriginal.Image = originalImage;
                picProcessed.Image = originalImage;
            }

        }
        private void AddLinearControls()
        {
            paramsPanel.Controls.Clear();
            Label lblContrast = new Label { Text = "Контраст: 1.00", Dock = DockStyle.Top };
            System.Windows.Forms.TrackBar trackContrast = new System.Windows.Forms.TrackBar
            {
                Minimum = 0,
                Maximum = 200,
                Value = 100, 
                Dock = DockStyle.Top,
                TickFrequency = 10
            };
            Label lblBrightness = new Label { Text = "Яркость: 0", Dock = DockStyle.Top };
            System.Windows.Forms.TrackBar trackBrightness = new System.Windows.Forms.TrackBar
            {
                Minimum = -100,
                Maximum = 100,
                Value = 0,
                Dock = DockStyle.Top,
                TickFrequency = 10
            };
            trackContrast.Scroll += (s, e) =>
            {
                currentContrast = trackContrast.Value / 100.0;
                lblContrast.Text = $"Контраст: {currentContrast:F2}";
                ApplyLinearCorrection(); 
                DrawCurve();
            };
            trackBrightness.Scroll += (s, e) =>
            {
                currentBrightness = trackBrightness.Value;
                lblBrightness.Text = $"Яркость: {currentBrightness}";
                ApplyLinearCorrection(); 
                DrawCurve();
            };
            paramsPanel.Controls.Add(lblBrightness);
            paramsPanel.Controls.Add(trackBrightness);
            paramsPanel.Controls.Add(lblContrast);
            paramsPanel.Controls.Add(trackContrast);
        }
        private void AddGammaControls()
        {
            paramsPanel.Controls.Clear();
            Label lblGamma = new Label { Text = "Gamma: 1.00", Dock = DockStyle.Top };
            System.Windows.Forms.TrackBar trackGamma = new System.Windows.Forms.TrackBar
            {
                Minimum = 10,   
                Maximum = 500,  
                Value = 100,    
                Dock = DockStyle.Top,
                TickFrequency = 50
            };
            trackGamma.Scroll += (s, e) =>
            {
                currentGamma = trackGamma.Value / 100.0; 
                lblGamma.Text = $"Gamma: {currentGamma:F2}";
                ApplyGammaCorrection(); 
                DrawCurve();
            };
            paramsPanel.Controls.Add(lblGamma);
            paramsPanel.Controls.Add(trackGamma);
        }
        private void AddSCurveControls()
        {
            paramsPanel.Controls.Clear();
            Label lblK = new Label { Text = $"Крутизна (k): {currentK:F2}", Dock = DockStyle.Top };
            System.Windows.Forms.TrackBar trackK = new System.Windows.Forms.TrackBar
            {
                Minimum = 5,    
                Maximum = 50,  
                Value = (int)(currentK * 100), 
                Dock = DockStyle.Top,
                TickFrequency = 5
            };
            trackK.Scroll += (s, e) =>
            {
                currentK = trackK.Value / 100.0;
                lblK.Text = $"Крутизна (k): {currentK:F2}";
                ApplySCurveCorrection();
                DrawCurve();
            };
            paramsPanel.Controls.Add(lblK);
            paramsPanel.Controls.Add(trackK);
        }
        private void AddLogControls()
        {
            paramsPanel.Controls.Clear();

            Label lblFactor = new Label { Text = $"Коэффициент: {currentLogFactor:F1}", Dock = DockStyle.Top };
            System.Windows.Forms.TrackBar trackFactor = new System.Windows.Forms.TrackBar
            {
                Minimum = 1,   
                Maximum = 200,  
                Value = 1,     
                Dock = DockStyle.Top,
                TickFrequency = 10
            };
            trackFactor.Scroll += (s, e) =>
            {
                currentLogFactor = trackFactor.Value / 10.0; 
                lblFactor.Text = $"Коэффициент: {currentLogFactor:F1}";
                ApplyLogCorrection();
                DrawCurve();
            };

            paramsPanel.Controls.Add(lblFactor);
            paramsPanel.Controls.Add(trackFactor);
        }
        private void AddShadowsHighlightsControls()
        {
            paramsPanel.Controls.Clear();
            Label lblShadows = new Label { Text = $"Тени: {shadowAmount:F1}", Dock = DockStyle.Top };
            System.Windows.Forms.TrackBar trackShadows = new System.Windows.Forms.TrackBar
            {
                Minimum = 0,
                Maximum = 200,
                Value = 100, 
                Dock = DockStyle.Top,
                TickFrequency = 20
            };
            Label lblHighlights = new Label { Text = $"Света: {highlightAmount:F1}", Dock = DockStyle.Top };
            System.Windows.Forms.TrackBar trackHighlights = new System.Windows.Forms.TrackBar
            {
                Minimum = 0,
                Maximum = 200,
                Value = 100, 
                Dock = DockStyle.Top,
                TickFrequency = 20
            };
            trackShadows.Scroll += (s, e) =>
            {
                shadowAmount = trackShadows.Value / 100.0;
                lblShadows.Text = $"Тени: {shadowAmount:F1}";
                ApplyShadowsHighlights();
                DrawCurve();
            };
            trackHighlights.Scroll += (s, e) =>
            {
                highlightAmount = trackHighlights.Value / 100.0;
                lblHighlights.Text = $"Света: {highlightAmount:F1}";
                ApplyShadowsHighlights();
                DrawCurve();
            };
            paramsPanel.Controls.Add(lblHighlights);
            paramsPanel.Controls.Add(trackHighlights);
            paramsPanel.Controls.Add(lblShadows);
            paramsPanel.Controls.Add(trackShadows);
        }
        private void AddSaturationControls()
        {
            paramsPanel.Controls.Clear();
            Label lblWarning1 = new Label { Text = $"Внимание! Картинка меняется при изменении параметра,", Dock = DockStyle.Bottom };
            Label lblWarning2 = new Label { Text = $"а график нет. Решить проблему не получилось.", Dock = DockStyle.Bottom };
            Label lblSaturation = new Label { Text = $"Насыщенность: {saturationLevel:F1}", Dock = DockStyle.Top };
            System.Windows.Forms.TrackBar trackSaturation = new System.Windows.Forms.TrackBar
            {
                Minimum = 0,
                Maximum = 200,
                Value = 100, 
                Dock = DockStyle.Top,
                TickFrequency = 20
            };
            trackSaturation.Scroll += (s, e) =>
            {
                saturationLevel = trackSaturation.Value / 100.0;
                lblSaturation.Text = $"Насыщенность: {saturationLevel:F1}";
                DrawCurve();
                ApplySaturationCorrection();
            };
            paramsPanel.Controls.Add(lblSaturation);
            paramsPanel.Controls.Add(trackSaturation);
            paramsPanel.Controls.Add(lblWarning1);
            paramsPanel.Controls.Add(lblWarning2);
        }
        private void AddToningControls()
        {
            paramsPanel.Controls.Clear();
            System.Windows.Forms.Button btnShadowColor = new System.Windows.Forms.Button
            {
                Text = "Тени",
                Dock = DockStyle.Top,
                BackColor = shadowColor
            };
            btnShadowColor.Click += (s, e) =>
            {
                ColorDialog dialog = new ColorDialog();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    shadowColor = dialog.Color;
                    btnShadowColor.BackColor = shadowColor;
                    ApplyToning();
                    DrawCurve();
                }
            };
            System.Windows.Forms.Button btnHighlightColor = new System.Windows.Forms.Button
            {
                Text = "Света",
                Dock = DockStyle.Top,
                BackColor = highlightColor
            };
            btnHighlightColor.Click += (s, e) =>
            {
                ColorDialog dialog = new ColorDialog();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    highlightColor = dialog.Color;
                    btnHighlightColor.BackColor = highlightColor;
                    ApplyToning();
                    DrawCurve();
                }
            };
            Label lblAmount = new Label { Text = $"Интенсивность: {toneAmount:F1}", Dock = DockStyle.Top };
            System.Windows.Forms.TrackBar trackAmount = new System.Windows.Forms.TrackBar
            {
                Minimum = 0,
                Maximum = 10,
                Value = (int)(toneAmount * 10),
                Dock = DockStyle.Top
            };
            trackAmount.Scroll += (s, e) =>
            {
                toneAmount = trackAmount.Value / 10.0;
                lblAmount.Text = $"Интенсивность: {toneAmount:F1}";
                ApplyToning();
                DrawCurve();
            };
            paramsPanel.Controls.Add(lblAmount);
            paramsPanel.Controls.Add(trackAmount);
            paramsPanel.Controls.Add(btnHighlightColor);
            paramsPanel.Controls.Add(btnShadowColor);
        }
        private void ApplyLinearCorrection()
        {
            if (originalImage == null) return;
            byte[] lut = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                double corrected = i * currentContrast + currentBrightness;
                lut[i] = (byte)MathHelper.Clamp(corrected, 0, 255);
            }
            if (processedImage != null) processedImage.Dispose();
            processedImage = new Bitmap(originalImage);
            BitmapData data = processedImage.LockBits(
                new Rectangle(0, 0, processedImage.Width, processedImage.Height),
                ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb
            );
            unsafe
            {
                byte* ptr = (byte*)data.Scan0;
                int bytesPerPixel = 4;

                for (int y = 0; y < data.Height; y++)
                {
                    byte* row = ptr + (y * data.Stride);
                    for (int x = 0; x < data.Width; x++)
                    {
                        row[x * bytesPerPixel + 2] = lut[row[x * bytesPerPixel + 2]]; 
                        row[x * bytesPerPixel + 1] = lut[row[x * bytesPerPixel + 1]]; 
                        row[x * bytesPerPixel + 0] = lut[row[x * bytesPerPixel + 0]]; 
                    }
                }
            }
            processedImage.UnlockBits(data);
            picProcessed.Image = processedImage; 
        }
        private void ApplyGammaCorrection()
        {
            if (originalImage == null) return;
            byte[] lut = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                double corrected = 255 * Math.Pow(i / 255.0, currentGamma);
                lut[i] = (byte)MathHelper.Clamp(corrected, 0, 255);
            }
            if (processedImage != null) processedImage.Dispose();
            processedImage = new Bitmap(originalImage);
            BitmapData data = processedImage.LockBits(
                new Rectangle(0, 0, processedImage.Width, processedImage.Height),
                ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb
            );
            unsafe
            {
                byte* ptr = (byte*)data.Scan0;
                int bytesPerPixel = 4;

                for (int y = 0; y < data.Height; y++)
                {
                    byte* row = ptr + (y * data.Stride);
                    for (int x = 0; x < data.Width; x++)
                    {
                        row[x * bytesPerPixel + 2] = lut[row[x * bytesPerPixel + 2]]; 
                        row[x * bytesPerPixel + 1] = lut[row[x * bytesPerPixel + 1]]; 
                        row[x * bytesPerPixel + 0] = lut[row[x * bytesPerPixel + 0]]; 
                    }
                }
            }
            processedImage.UnlockBits(data);
            picProcessed.Image = processedImage;
        }
        private void ApplySCurveCorrection()
        {
            if (originalImage == null) return;
            byte[] lut = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                double normalized = (i - 128) / 128.0; 
                double sigmoid = 1 / (1 + Math.Exp(-currentK * normalized * 10)); 
                lut[i] = (byte)(sigmoid * 255);
            }
            if (processedImage != null) processedImage.Dispose();
            processedImage = new Bitmap(originalImage);
            BitmapData data = processedImage.LockBits(
                new Rectangle(0, 0, processedImage.Width, processedImage.Height),
                ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb
            );
            unsafe
            {
                byte* ptr = (byte*)data.Scan0;
                int bytesPerPixel = 4;

                for (int y = 0; y < data.Height; y++)
                {
                    byte* row = ptr + (y * data.Stride);
                    for (int x = 0; x < data.Width; x++)
                    {
                        row[x * bytesPerPixel + 2] = lut[row[x * bytesPerPixel + 2]];
                        row[x * bytesPerPixel + 1] = lut[row[x * bytesPerPixel + 1]]; 
                        row[x * bytesPerPixel + 0] = lut[row[x * bytesPerPixel + 0]]; 
                    }
                }
            }

            processedImage.UnlockBits(data);
            picProcessed.Image = processedImage;
        }

        private void ApplyLogCorrection()
        {
            if (originalImage == null) return;
            byte[] lut = new byte[256];
            double logMax = Math.Log(256 * currentLogFactor); 
            for (int i = 0; i < 256; i++)
            {
                double corrected = 255 * Math.Log(1 + i * currentLogFactor) / logMax;
                lut[i] = (byte)MathHelper.Clamp(corrected, 0, 255);
            }
            if (processedImage != null) processedImage.Dispose();
            processedImage = new Bitmap(originalImage);
            BitmapData data = processedImage.LockBits(
                new Rectangle(0, 0, processedImage.Width, processedImage.Height),
                ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb
            );
            unsafe
            {
                byte* ptr = (byte*)data.Scan0;
                int bytesPerPixel = 4;

                for (int y = 0; y < data.Height; y++)
                {
                    byte* row = ptr + (y * data.Stride);
                    for (int x = 0; x < data.Width; x++)
                    {
                        row[x * bytesPerPixel + 2] = lut[row[x * bytesPerPixel + 2]];
                        row[x * bytesPerPixel + 1] = lut[row[x * bytesPerPixel + 1]];
                        row[x * bytesPerPixel + 0] = lut[row[x * bytesPerPixel + 0]];
                    }
                }
            }
            processedImage.UnlockBits(data);
            picProcessed.Image = processedImage;
        }
        private void ApplyShadowsHighlights()
        {
            if (originalImage == null) return;
            byte[] lut = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                double corrected = i;
                if (i < shadowThreshold)
                    corrected *= shadowAmount;
                else if (i > highlightThreshold)
                    corrected = 255 - (255 - corrected) * highlightAmount;
                else
                {
                    double t = (i - shadowThreshold) / (highlightThreshold - shadowThreshold);
                    corrected = corrected * (shadowAmount * (1 - t) + highlightAmount * t);
                }

                lut[i] = (byte)MathHelper.Clamp(corrected, 0, 255);
            }
            if (processedImage != null) processedImage.Dispose();
            processedImage = new Bitmap(originalImage);
            BitmapData data = processedImage.LockBits(
                new Rectangle(0, 0, processedImage.Width, processedImage.Height),
                ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb
            );
            unsafe
            {
                byte* ptr = (byte*)data.Scan0;
                int bytesPerPixel = 4;

                for (int y = 0; y < data.Height; y++)
                {
                    byte* row = ptr + (y * data.Stride);
                    for (int x = 0; x < data.Width; x++)
                    {
                        row[x * bytesPerPixel + 2] = lut[row[x * bytesPerPixel + 2]];
                        row[x * bytesPerPixel + 1] = lut[row[x * bytesPerPixel + 1]];
                        row[x * bytesPerPixel + 0] = lut[row[x * bytesPerPixel + 0]];
                    }
                }
            }

            processedImage.UnlockBits(data);
            picProcessed.Image = processedImage;
        }
        private byte Clamp(float value) => (byte)Math.Max(0, Math.Min(255, value));
        private void ApplySaturationCorrection()
        {
            if (originalImage == null) return;
            BitmapData data = originalImage.LockBits(
                new Rectangle(0, 0, originalImage.Width, originalImage.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb
            );
            processedImage?.Dispose();
            processedImage = new Bitmap(originalImage.Width, originalImage.Height);
            BitmapData processedData = processedImage.LockBits(
                new Rectangle(0, 0, processedImage.Width, processedImage.Height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format24bppRgb
            );
            unsafe
            {
                byte* srcPtr = (byte*)data.Scan0;
                byte* dstPtr = (byte*)processedData.Scan0;
                int bytesPerPixel = 3;
                float saturation = (float)saturationLevel;
                for (int y = 0; y < data.Height; y++)
                {
                    byte* srcRow = srcPtr + (y * data.Stride);
                    byte* dstRow = dstPtr + (y * processedData.Stride);
                    for (int x = 0; x < data.Width; x++)
                    {
                        byte b = srcRow[0];
                        byte g = srcRow[1];
                        byte r = srcRow[2];
                        float luminance = 0.299f * r + 0.587f * g + 0.114f * b;
                        float newB = b + (b - luminance) * (saturation - 1.0f);
                        float newG = g + (g - luminance) * (saturation - 1.0f);
                        float newR = r + (r - luminance) * (saturation - 1.0f);
                        dstRow[0] = Clamp(newB);
                        dstRow[1] = Clamp(newG);
                        dstRow[2] = Clamp(newR);
                        srcRow += bytesPerPixel;
                        dstRow += bytesPerPixel;
                    }
                }
            }
            originalImage.UnlockBits(data);
            processedImage.UnlockBits(processedData);
            picProcessed.Image = processedImage;
        }
        private void ApplyToning()
        {
            if (originalImage == null) return;
            BitmapData data = originalImage.LockBits(
                new Rectangle(0, 0, originalImage.Width, originalImage.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb
            );
            processedImage?.Dispose();
            processedImage = new Bitmap(originalImage.Width, originalImage.Height);
            BitmapData processedData = processedImage.LockBits(
                new Rectangle(0, 0, processedImage.Width, processedImage.Height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format24bppRgb
            );
            unsafe
            {
                byte* srcPtr = (byte*)data.Scan0;
                byte* dstPtr = (byte*)processedData.Scan0;
                int bytesPerPixel = 3;
                for (int y = 0; y < data.Height; y++)
                {
                    byte* srcRow = srcPtr + (y * data.Stride);
                    byte* dstRow = dstPtr + (y * processedData.Stride);

                    for (int x = 0; x < data.Width; x++)
                    {
                        byte brightness = (byte)(0.299 * srcRow[2] + 0.587 * srcRow[1] + 0.114 * srcRow[0]);
                        float t = brightness / 255.0f;
                        t = (float)Math.Pow(t, 2); 
                        byte r = (byte)(shadowColor.R * (1 - t) + highlightColor.R * t);
                        byte g = (byte)(shadowColor.G * (1 - t) + highlightColor.G * t);
                        byte b = (byte)(shadowColor.B * (1 - t) + highlightColor.B * t);
                        dstRow[0] = (byte)(b * toneAmount + srcRow[0] * (1 - toneAmount));
                        dstRow[1] = (byte)(g * toneAmount + srcRow[1] * (1 - toneAmount));
                        dstRow[2] = (byte)(r * toneAmount + srcRow[2] * (1 - toneAmount));
                        srcRow += bytesPerPixel;
                        dstRow += bytesPerPixel;
                    }
                }
            }
            originalImage.UnlockBits(data);
            processedImage.UnlockBits(processedData);
            picProcessed.Image = processedImage;
        }
        private void DrawCurve()
        {
            if (picCurve.Image == null)
            {
                picCurve.Image = new Bitmap(picCurve.Width, picCurve.Height);
            }
            using (Graphics g = Graphics.FromImage(picCurve.Image))
            {
                g.Clear(Color.White);
                g.DrawLine(Pens.Black, 10, picCurve.Height - 10, picCurve.Width - 10, picCurve.Height - 10);
                g.DrawLine(Pens.Black, 10, picCurve.Height - 10, 10, 10); 
                g.DrawString("Input", SystemFonts.DefaultFont, Brushes.Black, picCurve.Width - 30, picCurve.Height - 20);
                g.DrawString("Output", SystemFonts.DefaultFont, Brushes.Black, 12, 5);
                List<Point> points = new List<Point>();
                for (int x = 0; x < 256; x++)
                {
                    double input = x;
                    double output = 0;
                    switch (paramsComboBox.SelectedItem?.ToString())
                    {
                        case "Линейная коррекция":
                            output = input * currentContrast + currentBrightness;
                            break;
                        case "Гамма-коррекция":
                            output = 255 * Math.Pow(input / 255.0, currentGamma);
                            break;
                        case "S-образная кривая":
                            double normalized = (x - 128) / 128.0;
                            output = 255 / (1 + Math.Exp(-currentK * normalized * 10));
                            break;
                        case "Логарифмическое преобразование":
                            double logMax = Math.Log(256 * currentLogFactor);
                            output = 255 * Math.Log(1 + x * currentLogFactor) / logMax;
                            break;
                        case "Тень и свет":
                            output = x;
                            if (x < shadowThreshold)
                                output *= shadowAmount;
                            else if (x > highlightThreshold)
                                output = 255 - (255 - x) * highlightAmount;
                            else
                            {
                                double t = (x - shadowThreshold) / (highlightThreshold - shadowThreshold);
                                output = output * (shadowAmount * (1 - t) + highlightAmount * t);
                            }
                            break;
                        case "Насыщенность":
                            float luminance = x;
                            output = x + (x - luminance) * (float)(saturationLevel - 1);
                            output = MathHelper.Clamp(output, 0, 255);
                            break;
                        case "Тонирование":
                            float T = x / 255.0f;
                            T = (float)Math.Pow(T, 2); 
                            byte R = (byte)(shadowColor.R * (1 - T) + highlightColor.R * T);
                            byte G = (byte)(shadowColor.G * (1 - T) + highlightColor.G * T);
                            byte B = (byte)(shadowColor.B * (1 - T) + highlightColor.B * T);
                            output = (int)(0.299 * R + 0.587 * G + 0.114 * B);
                            break;
                    }
                    int screenX = (int)(x * (picCurve.Width - 20) / 255) + 10;
                    int screenY = picCurve.Height - 10 - (int)(output * (picCurve.Height - 20) / 255);
                    points.Add(new Point(screenX, screenY));
                }
                if (points.Count > 1)
                {
                    g.DrawLines(Pens.Blue, points.ToArray());
                }
            }
            picCurve.Invalidate();
        }
        private void paramsComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            paramsPanel.Controls.Clear();
            ClearUnits();
            switch (paramsComboBox.SelectedItem.ToString())
            {
                case "Линейная коррекция":
                    picProcessed.Image = originalImage;
                    AddLinearControls();
                    DrawCurve();
                    break;
                case "Гамма-коррекция":
                    picProcessed.Image = originalImage;
                    AddGammaControls();
                    DrawCurve();
                    break;
                case "S-образная кривая":
                    picProcessed.Image = originalImage;
                    AddSCurveControls();
                    DrawCurve();
                    break;
                case "Логарифмическое преобразование":
                    picProcessed.Image = originalImage;
                    AddLogControls();
                    DrawCurve();
                    break;
                case "Тень и свет":
                    picProcessed.Image = originalImage;
                    AddShadowsHighlightsControls();
                    DrawCurve();
                    break;
                case "Насыщенность":
                    picProcessed.Image = originalImage;
                    AddSaturationControls();
                    DrawCurve();
                    break;
                case "Тонирование":
                    AddToningControls();
                    break;
            }
        }
        private void Reset_Click(object sender, EventArgs e)
        {
            paramsPanel.Controls.Clear();
            ClearUnits();
            switch (paramsComboBox.SelectedItem?.ToString())
            {
                case "Линейная коррекция":
                    picProcessed.Image = originalImage;
                    AddLinearControls();
                    DrawCurve();
                    break;
                case "Гамма-коррекция":
                    picProcessed.Image = originalImage;
                    AddGammaControls();
                    DrawCurve();
                    break;
                case "S-образная кривая":
                    picProcessed.Image = originalImage;
                    AddSCurveControls();
                    DrawCurve();
                    break;
                case "Логарифмическое преобразование":
                    picProcessed.Image = originalImage;
                    AddLogControls();
                    DrawCurve();
                    break;
                case "Тень и свет":
                    picProcessed.Image = originalImage;
                    AddShadowsHighlightsControls();
                    DrawCurve();
                    break;
                case "Насыщенность":
                    picProcessed.Image = originalImage;
                    AddSaturationControls();
                    DrawCurve();
                    break;
            }
        }
    }
}