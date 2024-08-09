using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Threading.Tasks;
using System.Configuration;

namespace ScreenTranslator
{
    public class TrayAppContext : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private string selectedLanguage = "it"; // Imposta l'italiano come lingua predefinita
        private Form displayForm;
        ToolStripMenuItem translateMenuItem, languagesMenuItem;
        public TrayAppContext()
        {
            selectedLanguage = ConfigurationManager.AppSettings["LastSelectedLanguage"] ?? "it";
            // Creazione del menu contestuale
            var contextMenuStrip = new ContextMenuStrip();
            translateMenuItem = new ToolStripMenuItem("Translate", GetLanguageImage(selectedLanguage), Translate);
            languagesMenuItem = new ToolStripMenuItem("Select Language");

            // Aggiungi le lingue con le icone a bandierina

            contextMenuStrip.Items.Add(translateMenuItem);
            contextMenuStrip.Items.Add(languagesMenuItem);
            contextMenuStrip.Items.Add("Exit", null, Exit);

            // Inizializzazione dell'icona nella tray
            trayIcon = new NotifyIcon()
            {
                Icon = ImageToIcon(Resources.icons8_italy_12), // Icona predefinita
                ContextMenuStrip = contextMenuStrip,
                Visible = true,
               
            };

            trayIcon.Click += (sender, e) =>
            {
                if (e is MouseEventArgs me && me.Button == MouseButtons.Left)
                {
                    Translate(sender, e);
                }
            };
            languagesMenuItem.DropDownItems.Add(CreateLanguageMenuItem("Italiano", "it", GetLanguageImage("it")));
            languagesMenuItem.DropDownItems.Add(CreateLanguageMenuItem("Español", "es", GetLanguageImage("es")));
            languagesMenuItem.DropDownItems.Add(CreateLanguageMenuItem("Français", "fr", GetLanguageImage("fr")));
            languagesMenuItem.DropDownItems.Add(CreateLanguageMenuItem("Deutsch", "de", GetLanguageImage("de")));
            languagesMenuItem.DropDownItems.Add(CreateLanguageMenuItem("English", "en", GetLanguageImage("en")));


            // Carica l'ultima lingua selezionata dalle impostazioni

            UpdateTrayIcon(selectedLanguage);
        }

        private Icon GetLanguageIcon(string selectedLanguage)
        {

            return ImageToIcon(GetLanguageImage(selectedLanguage));
        }

        private Image GetLanguageImage(string selectedLanguage)
        {
            switch (selectedLanguage)
            {
                case "it":
                    return Resources.icons8_italy_12;
                case "es":
                    return Resources.icons8_spain_flag_12;
                case "fr":
                    return Resources.icons8_france_12;
                case "de":
                    return Resources.icons8_germany_12;
                case "en":
                    return Resources.icons8_england_12;
                default:
                    return Resources.icons8_italy_12;
            }

        }

        private ToolStripMenuItem CreateLanguageMenuItem(string languageName, string languageCode, Image trayIconForLanguage)
        {
            var menuItem = new ToolStripMenuItem(languageName, trayIconForLanguage, (sender, e) =>
            {
                selectedLanguage = languageCode;
                // Salva la lingua selezionata nelle impostazioni
                SaveSelectedLanguage(languageCode);
                // Aggiorna l'icona della tray usando la conversione da PNG a Icon
                trayIcon.Icon = ImageToIcon(trayIconForLanguage);
            });

            // Controlla la lingua attualmente selezionata
            if (selectedLanguage == languageCode)
            {
                menuItem.Checked = true;
                trayIcon.Icon = ImageToIcon(trayIconForLanguage); // Aggiorna l'icona iniziale
            }

            return menuItem;
        }


        private void SaveSelectedLanguage(string languageCode)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings["LastSelectedLanguage"].Value = languageCode;
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        private void UpdateTrayIcon(string languageCode)
        {
            trayIcon.Icon = GetLanguageIcon(languageCode);
            translateMenuItem.Image = GetLanguageImage(languageCode);
        }

        void Translate(object sender, EventArgs e)
        {
            if (displayForm!=null)
            {
                displayForm.Close();
                displayForm.Dispose();
                displayForm = null;
            }
            CaptureAndTranslate().ConfigureAwait(false);
        }

        async Task CaptureAndTranslate()
        {
            var selectionRectangle = CaptureSelectedArea();
            if (selectionRectangle != Rectangle.Empty)
            {
                // Esegui il riconoscimento del testo
                string screenshotPath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName().Replace(".tmp",".png"));
                Bitmap capturedBitmap;
                using (capturedBitmap = new Bitmap(selectionRectangle.Width, selectionRectangle.Height))
                {
                    using (Graphics g = Graphics.FromImage(capturedBitmap))
                    {
                        g.CopyFromScreen(selectionRectangle.Location, Point.Empty, selectionRectangle.Size);
                    }
                    capturedBitmap.Save(screenshotPath, System.Drawing.Imaging.ImageFormat.Png);
                }

                var detectedText = await GoogleVisionHelper.DetectTextFromImage(screenshotPath);
                var translatedText = await GoogleTranslateHelper.TranslateText(detectedText, selectedLanguage);

                // Copia il testo tradotto nella clipboard
                Clipboard.SetText(translatedText);

                // Visualizza il testo tradotto nella stessa posizione della selezione
                ShowTranslatedText(selectionRectangle, translatedText, (Bitmap)Image.FromFile(screenshotPath));
            }
        }

        Rectangle CaptureSelectedArea()
        {
            Rectangle selectionRectangle = Rectangle.Empty;

            using (var captureForm = new Form())
            {
                captureForm.FormBorderStyle = FormBorderStyle.None;
                captureForm.BackColor = Color.White;
                captureForm.Opacity = 0.2;
                captureForm.WindowState = FormWindowState.Maximized;
                captureForm.TopMost = true;
                captureForm.ShowInTaskbar = false;
                captureForm.Cursor = Cursors.Cross;

                Point startPoint = Point.Empty;
                bool isDragging = false;

                captureForm.MouseDown += (s, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        startPoint = e.Location;
                        isDragging = true;
                    }
                };

                captureForm.MouseMove += (s, e) =>
                {
                    if (isDragging)
                    {
                        int width = e.X - startPoint.X;
                        int height = e.Y - startPoint.Y;
                        selectionRectangle = new Rectangle(startPoint.X, startPoint.Y, width, height);
                        captureForm.Invalidate(); // Ridisegna la form
                    }
                };

                captureForm.MouseUp += (s, e) =>
                {
                    if (isDragging)
                    {
                        isDragging = false;
                        captureForm.DialogResult = DialogResult.OK;
                        captureForm.Close();
                    }
                };

                captureForm.Paint += (s, e) =>
                {
                    if (isDragging)
                    {
                        using (Pen pen = new Pen(Color.Red, 2))
                        {
                            e.Graphics.DrawRectangle(pen, selectionRectangle);
                        }
                    }
                };

                //if esc is pressed, close the form 
                captureForm.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Escape)
                    {
                        captureForm.DialogResult = DialogResult.Cancel;
                        captureForm.Close();
                    }
                };

                captureForm.ShowDialog();
            }

            return selectionRectangle;
        }

        void ShowTranslatedText(Rectangle selectionRectangle, string translatedText, Bitmap background)
        {
            using (displayForm = new Form())
            {
                displayForm.FormBorderStyle = FormBorderStyle.None;
                displayForm.StartPosition = FormStartPosition.Manual;
                displayForm.Location = selectionRectangle.Location;
                displayForm.Size = selectionRectangle.Size;
                displayForm.TopMost = true;
                displayForm.ShowInTaskbar = false;

                //calculate dominant color in the background image
                var color = GetDominantColor(background);
                // Imposta lo sfondo con l'immagine catturata
                displayForm.BackColor = color;
                //displayForm.BackgroundImageLayout = ImageLayout.Stretch;

                // Chiudi il form quando viene cliccato
                displayForm.MouseClick += (s, e) => displayForm.Close();

                displayForm.Paint += (s, e) =>
                {
                    // Inizializza il font e le altre variabili
                    int fontSize = 16;
                    Font font = new Font("Arial", fontSize, FontStyle.Bold);
                    SizeF textSize;
                    StringFormat format = new StringFormat
                    {
                        Alignment = StringAlignment.Near,
                        LineAlignment = StringAlignment.Near,
                        Trimming = StringTrimming.None,
                        FormatFlags = StringFormatFlags.FitBlackBox
                    };

                    RectangleF layoutRectangle = new RectangleF(0, 0, selectionRectangle.Width, selectionRectangle.Height);

                    // Riduci la dimensione del carattere finché il testo non si adatta
                    do
                    {
                        font = new Font("Arial", fontSize, FontStyle.Bold);
                        textSize = e.Graphics.MeasureString(translatedText, font, layoutRectangle.Size.ToSize(), format);
                        fontSize--;
                    } while (textSize.Width > layoutRectangle.Width || textSize.Height > layoutRectangle.Height);

                    // Disegna il testo con il font ridimensionato
                    e.Graphics.DrawString(translatedText, font, new SolidBrush(Color.Black), layoutRectangle, format);
                };

                displayForm.ShowDialog();
            }
        }

        private Color GetDominantColor(Bitmap background)
        {
            //Get dominant color in image
            int r = 0;
            int g = 0;
            int b = 0;
            int total = 0;

            for (int x = 0; x < background.Width; x++)
            {
                for (int y = 0; y < background.Height; y++)
                {
                    var pixel = background.GetPixel(x, y);
                    r += pixel.R;
                    g += pixel.G;
                    b += pixel.B;
                    total++;
                }
            }

            r /= total;
            g /= total;
            b /= total;

            return Color.FromArgb(r, g, b);



        }

        void Exit(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            Application.Exit();
        }


        private Icon ImageToIcon(Image img)
        {
            Bitmap bitmap = new Bitmap(img);
            IntPtr iconHandle = bitmap.GetHicon();
            return Icon.FromHandle(iconHandle);
        }
    }
}
