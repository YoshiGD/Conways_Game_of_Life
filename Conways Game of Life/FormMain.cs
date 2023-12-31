﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;
using static Conways_Game_of_Life.Properties.Resources;

namespace Conways_Game_of_Life
{
    /// <summary>
    /// Erstellt eine Simulation des 0 Spieler Spiels Conway's game of life.
    /// </summary>
    public partial class FormMain : Form
    {
        private const int CellSize = 10;
        public static bool[,] grid;
        private bool[,] nextGenerationGrid;
        private Timer timer;
        private Timer resizeTimer;

        private Color liveCellColor = Color.Black;
        private Color deadCellColor = Color.White;
        private Color gridColor = Color.Gray;

        public static List<Point> initialLiveCellPositions = new List<Point>();

        private Dictionary<string, string> Templates = new Dictionary<string, string>();

        private bool isPaused = true;
        private bool isMouseDown = false;
        private bool isDragging = false;
        private int index = 6;
        public static string name = "Sample";
        private double speed = 100;
        private string copied_filepath;

        /// <summary>
        /// Initialisiert und startet alles.
        /// </summary>
        public FormMain()
        {
            InitializeComponent();
            Initialize_Grid();
            Initialize_Timer();
            Paint += Paint_MainForm;
            MouseClick += MouseClick_Mainform;
            DoubleBuffered = true; // Enable double buffering
            btnPause.Click += PauseButton_Click;
            Shown += FormMain_Shown;

            // Initialize the resize timer
            resizeTimer = new Timer() { Interval = 200 }; // Delay in milliseconds
            resizeTimer.Tick += ResizeTimer_Tick;

            // Subscribe to the Resize event
            Resize += Resize_Resize;

            // next generation button
            btnNext.Click += Next_Click;
            MouseMove += FormMain_MouseCoordinates;
            //Set_List
            SetList();
        }

        /// <summary>
        ///  Diese Methode wird verwendet, um eine ListView mit vordefinierten Elementen zu füllen. 
        ///  Dadurch können dem Benutzer vorgegebene Optionen präsentiert werden, aus denen er auswählen kann.
        /// </summary>
        private void SetList()
        {
            lvTemplateSelection.Items.Add(Glider_Text, 0);
            lvTemplateSelection.Items.Add(Aim_Game_Text, 1);
            lvTemplateSelection.Items.Add(Space_Ship_Text, 2);
            lvTemplateSelection.Items.Add(Slugger_Text, 3);
            lvTemplateSelection.Items.Add(Glider_Gun_Text, 4);
            lvTemplateSelection.Items.Add(Carpet_Text, 5);
            lvTemplateSelection.Items.Add(Blackhole_Text, 6);
            lvTemplateSelection.Items.Add(Big_Glider_Grenade_Text , 7);
            lvTemplateSelection.Items.Add(Small_Glider_Grenade_Text, 8);
            lvTemplateSelection.Items.Add(Galaxy_Text, 9);
            lvTemplateSelection.Items.Add(Speed_Wall_Ladder_Text, 10);
            lvTemplateSelection.Items.Add(Conveyor_Belt_Text, 11);
            lvTemplateSelection.Items.Add(Rocket_Text, 12);
            lvTemplateSelection.Items.Add(Around_Text, 13);
            lvTemplateSelection.Items.Add(Wallbuilder_Text, 14);
            lvTemplateSelection.Items.Add(Galaxy_Big_Text, 15);
            lvTemplateSelection.Items.Add(Spinner_Text, 16);
            lvTemplateSelection.Items.Add(Game_of_Life_Text, 17);
        }

        /// <summary>
        /// Mit dieser Methode kann ein neues Element zur ListView hinzugefügt werden.
        /// Dabei wird ein Name und ein Index für das Element angegeben. 
        /// Dies ermöglicht es dem Benutzer, neue Optionen dynamisch hinzuzufügen.
        /// </summary>
        /// <param name="name">Name des Eintrags</param>
        /// <param name="index">Zugewiesene Nummer des Eintrags</param>
        private void AddItem(string name, int index) => lvTemplateSelection.Items.Add(name, index);

        /// <summary>
        ///  Die Methode behandelt die Aktion, die mit dem ausgewählten Element verbunden ist,
        ///  wie das Erzeugen eines bestimmten Musters im Gitter oder das Laden eines Musters aus einer Datei, falls verfügbar. 
        ///  Je nach Auswahl des Benutzers werden entsprechende Aktionen im Game of Life ausgeführt, um das gewünschte Muster zu erzeugen oder zu laden.
        /// </summary>
        /// <param name="sender">lvTemplateSelection</param>
        /// <param name="e">Click</param>
        private void Listview_Click(object sender, EventArgs e)
        {
            string selected = lvTemplateSelection.SelectedItems[0].SubItems[0].Text;

            if (selected == Glider_Text)
                Create_Glider();
            else if (selected == Aim_Game_Text)
                Create_Aim();
            else if (selected == Space_Ship_Text)
                Create_SpaceShip();
            else if (selected == Slugger_Text)
                Create_Slugger();
            else if (selected == Glider_Gun_Text)
                Create_GG();
            else if (selected == Carpet_Text)
                Create_Carpet();
            else if (selected == Blackhole_Text)
                Create_Blackloe();
            else if (selected == Small_Glider_Grenade_Text)
                Create_Small();
            else if (selected == Big_Glider_Grenade_Text)
                Create_Big();
            else if (selected == Galaxy_Text)
                Create_Galaxy();
            else if (selected == Speed_Wall_Ladder_Text)
                Create_Ladder();
            else if (selected == Conveyor_Belt_Text)
                Create_Conveyor();
            else if (selected == Rocket_Text)
                Create_Rocket();
            else if (selected == Around_Text)
                Create_Around();
            else if (selected == Wallbuilder_Text)
                Create_Wallbuilder();
            else if (selected == Galaxy_Big_Text)
                Create_Galaxy2();
            else if (selected == Spinner_Text)
                Create_Spinner();
            else if (selected == Game_of_Life_Text)
                Create_GoL();
            else if (Templates.ContainsKey(selected))
            {
                var text = File.ReadAllText(copied_filepath);
                var template = JsonConvert.DeserializeObject<Template>(text);
                Template.SetAliveZells(template);
            }
            this.Invalidate();
        }

        /// <summary>
        /// Diese Methode wird aufgerufen, um das Raster zu initialisieren. 
        /// Basierend auf der Größe des Formulars erstellt sie ein Gitter und generiert eine Startkonfiguration für die Zellen. 
        /// Dadurch wird der Ausgangszustand des Game of Life festgelegt.
        /// </summary>
        private void Initialize_Grid()
        {
            if (ClientSize.Width > 0 && ClientSize.Height > 0)
            {
                int width = Math.Max(ClientSize.Width / CellSize - 15, 1);
                int height = Math.Max(ClientSize.Height / CellSize - 1, 1);

                grid = new bool[width, height];
                nextGenerationGrid = new bool[width, height];
            }
        }

        /// <summary>
        /// Mit dieser Methode wird der Timer initialisiert, der für die automatische Aktualisierung der Generationen im Game of Life verwendet wird. 
        /// Sie legt die Intervallzeit fest, nach der die nächste Generation berechnet und angezeigt wird.
        /// </summary>
        private void Initialize_Timer()
        {
            timer = new Timer();
            timer.Interval = Convert.ToInt32(speed); ; // Adjust the speed here (milliseconds)
            timer.Tick += Timer_Tick;
        }

        /// <summary>
        ///  Diese Methode wird aufgerufen, wenn das Formular angezeigt wird. 
        ///  Sie startet den Timer, sodass das Game of Life automatisch aktualisiert wird, sobald das Formular sichtbar ist.
        /// </summary>
        /// <param name="sender">Form Main</param>
        /// <param name="e">Show</param>
        private void FormMain_Shown(object sender, EventArgs e)
        {
            timer.Start();
        }

        /// <summary>
        /// Bei jedem Timer-Intervall wird diese Methode aufgerufen. 
        /// Sie berechnet die nächste Generation des Gitters basierend auf den Regeln des Game of Life und aktualisiert die Darstellung auf dem Formular.
        /// </summary>
        /// <param name="sender">Timer</param>
        /// <param name="e">Tick</param>
        private void Timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();

            if (!isPaused || (isMouseDown && !isPaused))
            {
                Calculate_NextGeneration();
                this.Invalidate(); // Redraw the form
            }

            timer.Start();
        }

        /// <summary>
        ///  Diese Methode berechnet die nächste Generation des Gitters im Game of Life. 
        ///  Sie analysiert den aktuellen Zustand der Zellen und wendet die Regeln des Spiels an, 
        ///  um den Zustand der Zellen in der nächsten Generation zu bestimmen.
        /// </summary>
        private void Calculate_NextGeneration()
        {
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    int liveNeighbors = Count_LiveNeighbors(x, y);

                    if (grid[x, y])
                    {
                        // Any live cell with fewer than two live neighbors dies (underpopulation)
                        // Any live cell with more than three live neighbors dies (overpopulation)
                        nextGenerationGrid[x, y] = liveNeighbors == 2 || liveNeighbors == 3;
                    }
                    else
                    {
                        // Any dead cell with exactly three live neighbors becomes a live cell (reproduction)
                        nextGenerationGrid[x, y] = liveNeighbors == 3;
                    }
                }
            }
            // Update the current generation grid
            Array.Copy(nextGenerationGrid, grid, grid.Length);
        }

        /// <summary>
        ///  Mit dieser Methode wird die Anzahl der lebenden Nachbarn einer bestimmten Zelle im Gitter ermittelt. 
        ///  Sie zählt die umliegenden Zellen und gibt die Summe der lebenden Nachbarn zurück.
        /// </summary>
        /// <param name="x">X Koordinate der Zelle</param>
        /// <param name="y">Y Koordinate der Zelle</param>
        /// <returns></returns>
        private int Count_LiveNeighbors(int x, int y)
        {
            int count = 0;
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0) continue; // Skip the current cell
                    int neighborX = x + i;
                    int neighborY = y + j;

                    // Check if the neighbor is within the grid boundaries
                    if (neighborX >= 0 && neighborX < grid.GetLength(0) && neighborY >= 0 && neighborY < grid.GetLength(1))
                    {
                        if (grid[neighborX, neighborY])
                            count++;
                    }
                }
            }
            return count;
        }

        /// <summary>
        /// Diese Methode zeichnet das Gitter und die Zellen auf dem Formular.
        /// Sie verwendet verschiedene weiß und schwarz, um den Zustand der Zellen (lebendig oder tot) darzustellen 
        /// und stellt die aktuelle Generation des Game of Life visuell dar.
        /// </summary>
        /// <param name="sender">Main Form</param>
        /// <param name="e">Paint</param>
        private void Paint_MainForm(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            // Draw the cells and grid
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    var rect = new Rectangle(x * CellSize, y * CellSize, CellSize, CellSize);

                    if (grid[x, y])
                    {
                        g.FillRectangle(new SolidBrush(liveCellColor), rect);
                        g.DrawRectangle(new Pen(gridColor), rect);
                    }
                    else
                    {
                        g.FillRectangle(new SolidBrush(deadCellColor), rect);
                        g.DrawRectangle(new Pen(gridColor), rect);
                    }
                }
            }
        }

        /// <summary>
        ///  Bei einem Mausklick auf das Formular wird diese Methode aufgerufen. 
        ///  Sie ändert den Zustand einer Zelle basierend auf den Koordinaten des Klicks.
        ///  Dadurch kann der Benutzer einzelne Zellen aktivieren oder deaktivieren.
        /// </summary>
        /// <param name="sender">Main Form</param>
        /// <param name="e">Maus Klick</param>
        private void MouseClick_Mainform(object sender, MouseEventArgs e)
        {
            int x = e.X / CellSize;
            int y = e.Y / CellSize;

            if (x >= 0 && x < grid.GetLength(0) && y >= 0 && y < grid.GetLength(1))
            {
                if (grid[x, y])
                {
                    initialLiveCellPositions.Remove(new Point(x, y));
                    grid[x, y] = false;
                }
                else
                {
                    initialLiveCellPositions.Add(new Point(x, y));
                    grid[x, y] = true;
                }

                this.Invalidate(); // Redraw the form
            }
        }

        /// <summary>
        /// Mit dieser Methode wird zwischen Pausieren und Fortsetzen gewechselt.
        /// Sie reagiert auf das Klicken des Benutzers auf den Pause-Button und stoppt oder startet den Timer entsprechend.
        /// </summary>
        /// <param name="sender">btnPause</param>
        /// <param name="e">Click</param>
        private void PauseButton_Click(object sender, EventArgs e)
        {
            isPaused = !isPaused;
            if (isPaused) 
                btnPause.Text = Start_Text;
            else btnPause.Text = Pause_Text;
        }

        /// <summary>
        ///  Diese Methode behandelt das Ändern der Größe des Formulars. 
        ///  Sie passt das Gitter an die neue Größe an und stellt sicher, dass die Darstellung der Zellen korrekt skaliert wird.
        /// </summary>
        /// <param name="sender">Resize</param>
        /// <param name="e">Resize</param>
        private void Resize_Resize(object sender, EventArgs e)
        {
            resizeTimer.Stop();
            resizeTimer.Start();
        }

        /// <summary>
        /// Bei jedem Timer-Intervall wird diese Methode aufgerufen, um das Gitter an die neue Größe des Formulars anzupassen.
        /// Sie sorgt dafür, dass das Raster und die Zellen richtig skaliert und positioniert werden.
        /// </summary>
        /// <param name="sender">resizeTimer</param>
        /// <param name="e">Tick</param>
        private void ResizeTimer_Tick(object sender, EventArgs e)
        {
            resizeTimer.Stop();
            Initialize_Grid();
            this.Invalidate(); // Redraw the form
        }

        /// <summary>
        /// Erstellt ein sogenanntes Aim-Game wo man im richtigen Augenblick drücken muss.
        /// </summary>
        private void Create_Aim()
        {
            timer.Stop();
            var json = Encoding.Default.GetString(AimGame);
            ImportJson(json);
            timer.Start();
        }

        /// <summary>
        /// Erstellt einen Glider.
        /// </summary>
        private void Create_Glider()
        {
            timer.Stop();
            var json = Encoding.Default.GetString(Glider);
            ImportJson(json);
            timer.Start();
        }

        /// <summary>
        /// Erstellt ein Space Ship.
        /// </summary>
        private void Create_SpaceShip()
        {
            timer.Stop();
            var json = Encoding.Default.GetString(SpaceShip);
            ImportJson(json);
            timer.Start();
        }

        /// <summary>
        /// Erstellt einen Slugger.
        /// </summary>
        private void Create_Slugger()
        {
            timer.Stop();
            var json = Encoding.Default.GetString(Slugger);
            ImportJson(json);
            timer.Start();
        }

        /// <summary>
        /// Erstellt einen Teppich.
        /// </summary>
        private void Create_Carpet()
        {
            timer.Stop();
            var json = Encoding.Default.GetString(Teppich);
            ImportJson(json);
            timer.Start();
        }

        /// <summary>
        /// Erstellt eine Pistole die Glider schießt.
        /// </summary>
        private void Create_GG()
        {
            timer.Stop();
            var json = Encoding.Default.GetString(GliderGun);
            ImportJson(json);
            timer.Start();
        }

        /// <summary>
        /// Erstellt ein schwarzes Loch.
        /// </summary>
        private void Create_Blackloe()
        {
            timer.Stop();
            var json = Encoding.Default.GetString(Blackhole);
            ImportJson(json);
            timer.Start();
        }

        /// <summary>
        /// Erstellt eine kleine Splittergranate (4).
        /// </summary>
        private void Create_Small()
        {
            timer.Stop();
            var json = Encoding.Default.GetString(Small_Glider_Grenade);
            ImportJson(json);
            timer.Start();
        }

        /// <summary>
        /// Erstellt eine große Splittergranate (12).
        /// </summary>
        private void Create_Big()
        {
            timer.Stop();
            var json = Encoding.Default.GetString(Glider_Grenade);
            ImportJson(json);
            timer.Start();
        }
        
        /// <summary>
        /// Erstellt eine Galaxie.
        /// </summary>
        private void Create_Galaxy()
        {
            timer.Stop();
            var json = Encoding.Default.GetString(Galaxy);
            ImportJson(json);
            timer.Start();
        }

        /// <summary>
        /// Erstellt eine Leiter die eine Mauer baut.
        /// </summary>
        private void Create_Ladder()
        {
            timer.Stop();
            var json = Encoding.Default.GetString(Speed_Wall_Ladder);
            ImportJson(json);
            timer.Start();
        }

        /// <summary>
        /// Erstellt ein Fließband.
        /// </summary>
        private void Create_Conveyor()
        {
            timer.Stop();
            var json = Encoding.Default.GetString(Conveyor_Belt);
            ImportJson(json);
            timer.Start();
        }

        /// <summary>
        /// Erstellt eine Rakete.
        /// </summary>
        private void Create_Rocket()
        {
            timer.Stop();
            var json = Encoding.Default.GetString(Rocket);
            ImportJson(json);
            timer.Start();
        }

        /// <summary>
        /// Erstellt einen Rundkurs für Glider.
        /// </summary>
        private void Create_Around()
        {
            timer.Stop();
            var json = Encoding.Default.GetString(Around);
            ImportJson(json);
            timer.Start();
        }

        /// <summary>
        /// Erstellt eine Maschine die eine Doppelwand erstellt.
        /// </summary>
        private void Create_Wallbuilder()
        {
            timer.Stop();
            var json = Encoding.Default.GetString(Wallbuider);
            ImportJson(json);
            timer.Start();
        }

        /// <summary>
        /// Erstellt einen Kreisel.
        /// </summary>
        private void Create_Spinner()
        {
            timer.Stop();
            var json = Encoding.Default.GetString(Spinner);
            ImportJson(json);
            timer.Start();
        }

        /// <summary>
        /// Erstellt eine noch größere Galaxie.
        /// </summary>
        private void Create_Galaxy2()
        {
            timer.Stop();
            var json = Encoding.Default.GetString(Galaxy_Big);
            ImportJson(json);
            timer.Start();
        }

        /// <summary>
        /// Erstellt ein kleines Muster was eine extrem lange Lebensdauer hat.
        /// </summary>
        private void Create_GoL()
        {
            timer.Stop();
            var json = Encoding.Default.GetString(Game_of_life);
            ImportJson(json);
            timer.Start();
        }

        /// <summary>
        /// Diese Methode behandelt verschiedene Mausereignisse wie das Herunterdrücken und Loslassen der Maustaste 
        /// sowie das Bewegen der Maus auf dem Formular.
        /// Sie ermöglicht es dem Benutzer, Zellen durch Klicken und Ziehen der Maus zu aktivieren oder deaktivieren.
        /// </summary>
        /// <param name="sender">Main Form</param>
        /// <param name="e">Load</param>
        private void FormMain_Load(object sender, EventArgs e)
        {
            this.MouseDown += MouseDown_MainForm;
            this.MouseUp += MouseUp_MainForm;
            this.MouseMove += MouseMove_MainForm;
        }

        /// <summary>
        /// Diese Methode wird aufgerufen, wenn der Benutzer die Maustaste drückt.
        /// Sie ermöglicht es, auf diese Aktion zu reagieren, beispielsweise um eine Vorbereitung für eine nachfolgende Aktion durchzuführen,
        /// wie das Starten des Zeichnens oder das Auswählen eines Objekts.
        /// </summary>
        /// <param name="sender">Main Form</param>
        /// <param name="e">Maus gedrückt</param>
        private void MouseDown_MainForm(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isMouseDown = true;
                isDragging = btnPaint.Checked;
            }
        }

        /// <summary>
        /// Diese Methode wird aufgerufen, wenn der Benutzer die Maustaste loslässt. 
        /// Sie ermöglicht es, auf diese Aktion zu reagieren, beispielsweise um eine bestimmte Aktion abzuschließen, 
        /// wie das Zeichnen einer Linie oder das Platzieren eines Objekts an der Position, an der die Maustaste losgelassen wurde.
        /// </summary>
        /// <param name="sender">Main Form</param>
        /// <param name="e">Maus losgelassen</param>
        private void MouseUp_MainForm(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isMouseDown = false;
                isDragging = false;
            }
        }

        /// <summary>
        /// Diese Methode wird aufgerufen, wenn der Benutzer die Maus über ein Steuerelement bewegt, während eine Maustaste gedrückt ist.
        /// Sie ermöglicht es, die Bewegung der Maus zu verfolgen und entsprechende Reaktionen auszuführen, 
        /// beispielsweise um eine Linie zu zeichnen, während der Benutzer die Maus über das Steuerelement zieht.
        /// </summary>
        /// <param name="sender">Main Form</param>
        /// <param name="e">Maus bewegen</param>
        private void MouseMove_MainForm(object sender, MouseEventArgs e)
        {
            // Check if dragging is enabled
            if (btnPaint.Checked && isDragging)
            {
                int x = e.X / CellSize;
                int y = e.Y / CellSize;


                if (x >= 0 && x < grid.GetLength(0) && y >= 0 && y < grid.GetLength(1))
                {
                    if (!grid[x, y])
                    {
                        initialLiveCellPositions.Add(new Point(x, y));
                        grid[x, y] = true;
                        this.Invalidate();
                    }
                    lCoordinates.Text = $"X: {x}, Y: {y}";
                }
                else
                {
                    lCoordinates.Text = "-";
                }



                //$"X: {x}, Y: {y}"
            }
        }

        /// <summary>
        /// Diese Methode zeigt dem Benutzer die Koordinaten und den Zustand einer Zelle an, basierend auf der Position des Mauszeigers auf dem Formular. 
        /// Dadurch wird eine interaktive Benutzeroberfläche bereitgestellt.
        /// </summary>
        /// <param name="sender">Main Form</param>
        /// <param name="e">Maus Koordinaten</param>
        private void FormMain_MouseCoordinates(object sender, MouseEventArgs e)
        {
            int x = e.X / CellSize;
            int y = e.Y / CellSize;

            if (x >= 0 && x < grid.GetLength(0) && y >= 0 && y < grid.GetLength(1))
            {
                string state = grid[x, y] ? "Alive" : "Dead";
                lCoordinates.Text = $"X: {x}, Y: {y}";
                lState.Text = $"State: {state}";
            }
            else
            {
                lCoordinates.Text = "-";
            }
        }

        /// <summary>
        /// Diese Methode wird aufgerufen, wenn der Benutzer auf die Schaltfläche "Next" klickt. 
        /// Sie führt eine Aktualisierung der Spiellogik durch, um den nächsten Zustand des Spiels zu berechnen und auf dem Spielfeld anzuzeigen.
        /// </summary>
        /// <param name="sender">btnNext</param>
        /// <param name="e">Klick</param>
        private void Next_Click(object sender, EventArgs e)
        {
            Calculate_NextGeneration();
            this.Invalidate();
        }

        /// <summary>
        ///  Diese Methode wird aufgerufen, wenn der Benutzer auf die Schaltfläche "Clear" klickt. 
        ///  Sie löscht alle Zellen auf dem Spielfeld und setzt das Spielfeld auf den Ausgangszustand zurück.
        /// </summary>
        /// <param name="sender">btnClear</param>
        /// <param name="e">Klick</param>
        private void Clear_Click(object sender, EventArgs e)
        {
            ClearGrid();
            this.Invalidate();
        }

        /// <summary>
        /// Diese Methode wird aufgerufen, um das Spielfeld zu löschen.
        /// Sie entfernt alle Zellen und setzt das Spielfeld auf den Ausgangszustand zurück.
        /// </summary>
        public static void ClearGrid()
        {
            Array.Clear(grid, 0, grid.Length);
            initialLiveCellPositions.Clear();
        }

        //Textfiles

        /// <summary>
        /// Diese Methode wird aufgerufen, wenn der Benutzer auf die Schaltfläche "Save" klickt.
        /// Sie speichert den aktuellen Zustand des Spiels in einer Textdatei, um ihn später wiederherstellen zu können.
        /// </summary>
        /// <param name="sender">btnSave</param>
        /// <param name="e">Klick</param>
        private void Save_Click(object sender, EventArgs e)
        {
            InputBox box = new InputBox();
            var result = box.ShowDialog();
            var input = box.tBInput.Text;
            name = input;
            if (result == DialogResult.OK)
            {
                using (SaveFileDialog dialog = new SaveFileDialog())
                {
                    dialog.Filter = "Text Files|*.txt";
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        string filePath = dialog.FileName;
                        Template.Save_AliveCellPositions(filePath);

                        MessageBox.Show("Positions saved successfully.");
                        AddItem(input, index++);
                        if (!Templates.ContainsKey(input))
                        {
                            Templates.Add(input, File.ReadAllText(filePath));
                        }

                    }
                }
            }
            else
            {
                return;
            }

        }

        /// <summary>
        /// Diese Methode wird aufgerufen, wenn der Benutzer auf die Schaltfläche "Load" klickt.
        /// Sie ermöglicht das Laden eines zuvor gespeicherten Spielzustands, um das gespeicherte Zellmuster wieder aufzurufen.
        /// </summary>
        /// <param name="sender">btnLoad</param>
        /// <param name="e">Klick</param>
        private void LoadButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Text Files|*.txt";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = dialog.FileName;
                    Load_AliveCellPositions(filePath);
                    MessageBox.Show("Positions loaded successfully.");
                }
            }
        }

        /// <summary>
        /// Diese Methode wird verwendet, um die Positionen der lebenden Zellen aus einer gespeicherten Textdateien zu laden 
        /// und auf dem Spielfeld wiederherzustellen.
        /// </summary>
        /// <param name="filePath">Der File-Pfad der gespeicherten Datei</param>
        private void Load_AliveCellPositions(string filePath)
        {
            FormMain.ClearGrid();
            var text = File.ReadAllText(filePath);
            string[] zeilen = text.Split('\n');
            Template.SetAliveZells(zeilen);
            this.Invalidate(); // Redraw the form
        }

        //Json

        /// <summary>
        /// Diese Methode wird aufgerufen, wenn der Benutzer auf die Schaltfläche "Save" klickt, um den Spielzustand im JSON-Format zu speichern.
        /// </summary>
        /// <param name="sender">btnSaveJson</param>
        /// <param name="e">Klick</param>
        private void SaveButton_Click_Json(object sender, EventArgs e)
        {
            InputBox box = new InputBox();
            var result = box.ShowDialog();
            var input = box.tBInput.Text;
            name = input;

            if (result == DialogResult.OK)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "JSON Files|*.json";
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = saveFileDialog.FileName;
                    copied_filepath = saveFileDialog.FileName;
                    Template.Save_LiveCellPositions_Json(filePath);

                    MessageBox.Show("Positions saved successfully.");
                    AddItem(input, index++);
                    if (!Templates.ContainsKey(input))
                    {
                        Templates.Add(input, File.ReadAllText(filePath));
                    }
                }
            }
            else
            {
                return;
            }
            

        }

        /// <summary>
        /// Diese Methode wird aufgerufen, wenn der Benutzer auf die Schaltfläche "Load" klickt,
        /// um den Spielzustand im JSON-Format zu laden und das Spiel fortzusetzen.
        /// </summary>
        /// <param name="sender">btnLoadJson</param>
        /// <param name="e">Klick</param>
        private void LoadButton_Click_Json(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "JSON Files|*.json";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialog.FileName;
                Load_LiveCellPositions_Json(filePath);
                MessageBox.Show("Positions loaded successfully.");
            }
        }

        /// <summary>
        /// Diese Methode wird verwendet, um die Positionen der lebenden Zellen aus einer im JSON-Format gespeicherten Datei zu laden 
        /// und auf dem Spielfeld wiederherzustellen.
        /// </summary>
        /// <param name="filePath">Der File-Pfad der gespeicherten Datei</param>
        private void Load_LiveCellPositions_Json(string filePath)
        {
            ClearGrid();
            var text = File.ReadAllText(filePath);
            var template = JsonConvert.DeserializeObject<Template>(text);

            Template.SetAliveZells(template);
            this.Invalidate(); // Redraw the form
        }

        /// <summary>
        /// Diese Methode ermöglicht das Importieren von Spielzuständen aus einer JSON-Datei,
        /// um das Spiel in einem bestimmten Zustand zu starten oder fortzusetzen.
        /// </summary>
        /// <param name="json">Eine Json Datei aus dem Ressource Ordner</param>
        private void ImportJson(string json)
        {
            List<Point> liveCellPositions = JsonConvert.DeserializeObject<List<Point>>(json);

            foreach (Point position in liveCellPositions)
            {
                int x = position.X;
                int y = position.Y;

                if (x >= 0 && x < grid.GetLength(0) && y >= 0 && y < grid.GetLength(1))
                {
                    grid[x, y] = true;
                }
            }

            initialLiveCellPositions.AddRange(liveCellPositions);
            this.Invalidate();
        }
    }
}