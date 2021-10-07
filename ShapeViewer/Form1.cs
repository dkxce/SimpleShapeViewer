using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace ShapeViewer
{
    public partial class frmMain : Form
    {
        int filecode, filelength, version, shapetype;
        double xMin, yMin, xMax, yMax, zMin, zMax, mMin, mMax;
        int x1, y1, x2, y2;
        int offsetX = 0;        
        int offsetY = 0;
        int cur_offsetX = 0;
        int cur_offsetY = 0;
        bool down = false;

        public Brush[] Colors = new Brush[] {
            Brushes.Green, Brushes.Blue, 
            Brushes.DeepPink, Brushes.Brown,
            Brushes.Navy, Brushes.DarkGreen, Brushes.DarkMagenta, 
            Brushes.DarkOrange, Brushes.DarkViolet, Brushes.DarkCyan, 
            Brushes.DarkBlue, Brushes.Red, Brushes.Gold };

        public byte defBrush = 0;
        public List<PointF> points = new List<PointF>();
        public struct Line
        {
            public double[] box;
            public int numParts;
            public int numPoints;
            public int[] parts;
            public PointF[] points;
            public Brush brush;
        }
        public List<Line> lines = new List<Line>();
        public struct Polygon
        {
            public double[] box;
            public int numParts;
            public int numPoints;
            public int[] parts;
            public PointF[] points;
            public Brush brush;
        }
        public List<Polygon> polygons = new List<Polygon>();

        public frmMain()
        {
            InitializeComponent();
            xyi.SelectedIndex = 2;
            pfs.SelectedIndex = 0;
        }

        public string File2Open = "";
        private void frmMain_Load(object sender, EventArgs e)
        {
            points = new List<PointF>();
            lines = new List<Line>();
            polygons = new List<Polygon>();

            pictureBox1.MouseWheel += new MouseEventHandler(pictureBox1_MouseWheel);

            if ((File2Open != "") && (System.IO.File.Exists(File2Open))) OpenShapeFile(File2Open);
        }

        void pictureBox1_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta > 0)
            {
                int dx = pictureBox1.Width / 2 - e.X;
                int dy = pictureBox1.Height / 2 - e.Y;
                zoom = zoom * 2;
                int dxp = offsetX - cur_offsetX;
                int dyp = offsetY - cur_offsetY;
                SetCorrectPos(dxp * 2 + dx, dyp * 2 + dy);
                pictureBox1.Invalidate();
            }
            else
            {
                int dx = pictureBox1.Width / 2 - e.X;
                int dy = pictureBox1.Height / 2 - e.Y;
                zoom = zoom / 2;
                int dxp = offsetX - cur_offsetX;
                int dyp = offsetY - cur_offsetY;
                SetCorrectPos(dxp / 2 - dx/2, dyp / 2 - dy/2);
                pictureBox1.Invalidate();
            };
        }

        public void readShapeFile(string filename)
        {
            log.Text = "Файл `"+System.IO.Path.GetFileName(filename) + "`\r\nПроверка на дубли точек:\r\n\r\n";
            if (!dpc.Checked) log.Text += "НЕТ";
            int ttld = 0;
            FileStream fs = new FileStream(filename, FileMode.Open);
            long fileLength = fs.Length;
            Byte[] data = new Byte[fileLength];
            fs.Read(data, 0, (int)fileLength);
            fs.Close();
            filecode = readIntBig(data, 0);
            filelength = readIntBig(data, 24);
            version = readIntLittle(data, 28);
            shapetype = readIntLittle(data, 32);
            xMin = readDoubleLittle(data, 36);
            yMin = readDoubleLittle(data, 44);
            yMin = 0 - yMin;
            xMax = readDoubleLittle(data, 52);
            yMax = readDoubleLittle(data, 60);
            yMax = 0 - yMax;
            zMin = readDoubleLittle(data, 68);
            zMax = readDoubleLittle(data, 76);
            mMin = readDoubleLittle(data, 84);
            mMax = readDoubleLittle(data, 92);
            int currentPosition = 100;
            while (currentPosition < fileLength)
            {
                int recordStart = currentPosition;
                int recordNumber = readIntBig(data, recordStart);
                int contentLength = readIntBig(data, recordStart + 4);
                int recordContentStart = recordStart + 8;
                if (shapetype == 1)
                {
                    PointF point = new PointF();
                    int recordShapeType = readIntLittle(data, recordContentStart);
                    point.X = (float)readDoubleLittle(data, recordContentStart + 4);
                    if ((xyi.SelectedIndex == 1) || (xyi.SelectedIndex == 3)) point.X = 0 - point.X;
                    point.Y = (float)readDoubleLittle(data, recordContentStart + 12);
                    if ((xyi.SelectedIndex == 2) || (xyi.SelectedIndex == 3)) point.Y = 0 - point.Y;
                    points.Add(point);
                };

                if (shapetype == 3)
                {
                    int recordShapeType = readIntLittle(data, recordContentStart);
                    Double[] box = new double[4];
                    box[0] = readDoubleLittle(data, recordContentStart + 4); // Xmin
                    if ((xyi.SelectedIndex == 1) || (xyi.SelectedIndex == 3)) box[0] = 0 - box[0];
                    box[1] = readDoubleLittle(data, recordContentStart + 12); // Ymin
                    if ((xyi.SelectedIndex == 2) || (xyi.SelectedIndex == 3)) box[1] = 0 - box[1];
                    box[2] = readDoubleLittle(data, recordContentStart + 20); // Xmax
                    if ((xyi.SelectedIndex == 1) || (xyi.SelectedIndex == 3)) box[2] = 0 - box[2];
                    box[3] = readDoubleLittle(data, recordContentStart + 28); // Ymax
                    if ((xyi.SelectedIndex == 2) || (xyi.SelectedIndex == 3)) box[3] = 0 - box[3];
                    int numParts = readIntLittle(data, recordContentStart + 36);
                    int[] parts = new int[numParts];
                    int[] partsLength = new int[numParts];
                    int numPoints = readIntLittle(data, recordContentStart + 40);
                    if (numParts == 1) partsLength[0] = numPoints;

                    int partStart = recordContentStart + 44;
                    for (int i = 0; i < numParts; i++)
                    {
                        parts[i] = readIntLittle(data, partStart + i * 4);
                        if (i > 0)
                        {
                            partsLength[i - 1] = parts[i] - parts[i - 1];
                        };
                    };
                    if (numParts > 1) partsLength[numParts - 1] = (numPoints) - parts[numParts - 1];

                    int pointStart = recordContentStart + 44 + 4 * numParts;

                    // READ BY SEGMENTS // SUB LINES //
                    for (int n = 0; n < numParts; n++)
                    {
                        Line line = new Line();
                        line.brush = Colors[defBrush];
                        line.box = box;
                        line.numParts = 1;
                        line.parts = new int[] { 0 };
                        line.numPoints = partsLength[n];
                        line.points = new PointF[line.numPoints];

                        // PREVIOUS CHECK
                        float p_x = 0;
                        float p_y = 0;
                        int p_c = 1;
                        
                        for (int i = 0; i < line.numPoints; i++)
                        {
                            line.points[i].X = (float)readDoubleLittle(data, pointStart + (parts[n] * 16) + (i * 16));
                            if ((xyi.SelectedIndex == 1) || (xyi.SelectedIndex == 3)) line.points[i].X = 0 - line.points[i].X;
                            line.points[i].Y = (float)readDoubleLittle(data, pointStart + (parts[n] * 16) + (i * 16) + 8);
                            if ((xyi.SelectedIndex == 2) || (xyi.SelectedIndex == 3)) line.points[i].Y = 0 - line.points[i].Y;

                            if (dpc.Checked)
                            {
                                if ((Math.Abs(p_x - line.points[i].X) < 1E-07) && (Math.Abs(p_y - line.points[i].Y) < 1E-07))
                                {
                                    ttld++;
                                    p_c++;
                                }
                                else if (p_c > 1)
                                {
                                    if (ttld <= 1000)
                                        log.Text += String.Format("{0} точ" + (p_c < 5 ? "ки" : "ек") + " в  {1}  {2}\r\n", p_c, p_y, p_x);
                                    p_c = 1;
                                };
                                p_x = line.points[i].X;
                                p_y = line.points[i].Y;
                            };
                        };

                        lines.Add(line);                        
                    };
                    // NextColor();
                };

                if (shapetype == 5) //5
                {
                    int recordShapeType = readIntLittle(data, recordContentStart);
                    Double[] box = new double[4];
                    box[0] = readDoubleLittle(data, recordContentStart + 4); // Xmin
                    if ((xyi.SelectedIndex == 1) || (xyi.SelectedIndex == 3)) box[0] = 0 - box[0];
                    box[1] = readDoubleLittle(data, recordContentStart + 12); // Ymin
                    if ((xyi.SelectedIndex == 2) || (xyi.SelectedIndex == 3)) box[1] = 0 - box[1];
                    box[2] = readDoubleLittle(data, recordContentStart + 20); // Xmax
                    if ((xyi.SelectedIndex == 1) || (xyi.SelectedIndex == 3)) box[2] = 0 - box[2];
                    box[3] = readDoubleLittle(data, recordContentStart + 28); // Ymax
                    if ((xyi.SelectedIndex == 2) || (xyi.SelectedIndex == 3)) box[3] = 0 - box[3];
                    int numParts = readIntLittle(data, recordContentStart + 36);
                    int[] parts = new int[numParts];
                    int[] partsLength = new int[numParts];                    
                    int numPoints = readIntLittle(data, recordContentStart + 40);
                    if (numParts == 1) partsLength[0] = numPoints;

                    int partStart = recordContentStart + 44;
                    for (int i = 0; i < numParts; i++)
                    {
                        parts[i] = readIntLittle(data, partStart + i * 4);
                        if (i > 0)
                        {
                            partsLength[i - 1] = parts[i] - parts[i - 1];
                        };
                    };
                    if (numParts > 1) partsLength[numParts - 1] = (numPoints) - parts[numParts - 1];

                    int pointStart = recordContentStart + 44 + 4 * numParts;                    

                    // READ BY SEGMENTS // SUB POLYGONS //
                    for (int n = 0; n < numParts; n++)
                    {
                        Polygon polygon = new Polygon();
                        polygon.brush = Colors[defBrush];
                        polygon.box = box;
                        polygon.numParts = 1;
                        polygon.parts = new int[] { 0 };
                        polygon.numPoints = partsLength[n];
                        polygon.points = new PointF[polygon.numPoints];

                        // PREVIOUS CHECK
                        float p_x = 0;
                        float p_y = 0;
                        int p_c = 1;

                        for (int i = 0; i < polygon.numPoints; i++)
                        {
                            polygon.points[i].X = (float)readDoubleLittle(data, pointStart + (parts[n] * 16) + (i * 16));
                            if ((xyi.SelectedIndex == 1) || (xyi.SelectedIndex == 3)) polygon.points[i].X = 0 - polygon.points[i].X;
                            polygon.points[i].Y = (float)readDoubleLittle(data, pointStart + (parts[n] * 16) + (i * 16) + 8);
                            if ((xyi.SelectedIndex == 2) || (xyi.SelectedIndex == 3)) polygon.points[i].Y = 0 - polygon.points[i].Y;

                            if (dpc.Checked)
                            {
                                if ((Math.Abs(p_x - polygon.points[i].X) < 1E-07) && (Math.Abs(p_y - polygon.points[i].Y) < 1E-07))
                                {
                                    ttld++;
                                    p_c++;                                    
                                }
                                else if (p_c > 1)
                                {
                                    if(ttld<=1000)
                                        log.Text += String.Format("{0} точ" + (p_c < 5 ? "ки" : "ек") + " в  {1}  {2}\r\n", p_c, p_y, p_x);
                                    p_c = 1;
                                };
                                p_x = polygon.points[i].X;
                                p_y = polygon.points[i].Y;
                            };

                        };

                        polygons.Add(polygon);
                    };

                    NextColor();
                };
                currentPosition = recordStart + (4 + contentLength) * 2;
            };

            if (shapetype == 3) NextColor();
            log.Text += String.Format("\r\nВсего найдено {0} дублей.{1}\r\n", ttld, (ttld>1000?"\r\nОграничение на вывод 1000":""));
        }

        public int readIntBig(byte[] data, int pos)
        {
            byte[] bytes = new byte[4];
            bytes[0] = data[pos];
            bytes[1] = data[pos+1];
            bytes[2] = data[pos+2];
            bytes[3] = data[pos+3];
            Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        public int readIntLittle(byte[] data, int pos)
        {
            byte[] bytes = new byte[4];
            bytes[0] = data[pos];
            bytes[1] = data[pos + 1];
            bytes[2] = data[pos + 2];
            bytes[3] = data[pos + 3];
            return BitConverter.ToInt32(bytes, 0);
        }

        public double readDoubleLittle(byte[] data, int pos)
        {
            byte[] bytes = new byte[8];
            bytes[0] = data[pos];
            bytes[1] = data[pos + 1];
            bytes[2] = data[pos + 2];
            bytes[3] = data[pos + 3];
            bytes[4] = data[pos + 4];
            bytes[5] = data[pos + 5];
            bytes[6] = data[pos + 6];
            bytes[7] = data[pos + 7];
            return BitConverter.ToDouble(bytes, 0);
        }

        float zoom = 256;

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.TranslateTransform(offsetX, offsetY);                        
            for (int i = 0; i < polygons.Count; i++)
            {
                int n = polygons[i].points.Length;
                PointF[] pt = new PointF[n];
                for (int j = 0; j < n; j++)
                {
                    pt[j].X = (polygons[i].points[j].X - (float)xMin) * zoom;
                    pt[j].Y = (polygons[i].points[j].Y - (float)yMax) * zoom;
                }
                Pen pen = new Pen(polygons[i].brush, 1);
                e.Graphics.DrawPolygon(pen, pt);                
                if(pfs.SelectedIndex == 1)
                    e.Graphics.FillPolygon(new SolidBrush(Color.FromArgb(30,pen.Color)), pt);
                if (pfs.SelectedIndex == 2)
                    e.Graphics.FillPolygon(new SolidBrush(Color.FromArgb(150, pen.Color)), pt);
            };            
            for (int i = 0; i < lines.Count; i++)
            {
                int n = lines[i].points.Length;
                PointF[] pt = new PointF[n];
                for (int j = 0; j < n; j++)
                {
                    pt[j].X = (lines[i].points[j].X - (float)xMin) * zoom;
                    pt[j].Y = (lines[i].points[j].Y - (float)yMax) * zoom;
                }
                e.Graphics.DrawLines(new Pen(lines[i].brush, 1), pt);
            };
            for (int i = 0; i < points.Count; i++)
            {
                int x = (int)((points[i].X - (float)xMin) * zoom - 2);
                int y = (int)((points[i].Y - (float)yMax) * zoom - 2);
                e.Graphics.DrawEllipse(new Pen(Brushes.Maroon, 1), x, y, 4, 4);
            };
        }

        private void SetCorrectPos(int X, int Y)
        {
            int _minx = int.MaxValue;
            int _maxx = int.MinValue;
            int _miny = int.MaxValue;
            int _maxy = int.MinValue;

            for (int i = 0; i < points.Count; i++)
            {
                int x = (int)((points[i].X - (float)xMin) * zoom - 2);
                int y = (int)((points[i].Y - (float)yMax) * zoom - 2);
                if (_minx > x) _minx = x;
                if (_miny > y) _miny = y;
                if (_maxx < x) _maxx = x;
                if (_maxy < y) _maxy = y;
            }
            for (int i = 0; i < lines.Count; i++)
            {
                int n = lines[i].points.Length;
                PointF[] pt = new PointF[n];
                for (int j = 0; j < n; j++)
                {
                    pt[j].X = (lines[i].points[j].X - (float)xMin) * zoom;
                    pt[j].Y = (lines[i].points[j].Y - (float)yMax) * zoom;
                    if (_minx > pt[j].X) _minx = (int)pt[j].X;
                    if (_miny > pt[j].Y) _miny = (int)pt[j].Y;
                    if (_maxx < pt[j].X) _maxx = (int)pt[j].X;
                    if (_maxy < pt[j].Y) _maxy = (int)pt[j].Y;
                }
            }
            for (int i = 0; i < polygons.Count; i++)
            {
                int n = polygons[i].points.Length;
                PointF[] pt = new PointF[n];
                for (int j = 0; j < n; j++)
                {
                    pt[j].X = (polygons[i].points[j].X - (float)xMin) * zoom;
                    pt[j].Y = (polygons[i].points[j].Y - (float)yMax) * zoom;
                    if (_minx > pt[j].X) _minx = (int)pt[j].X;
                    if (_miny > pt[j].Y) _miny = (int)pt[j].Y;
                    if (_maxx < pt[j].X) _maxx = (int)pt[j].X;
                    if (_maxy < pt[j].Y) _maxy = (int)pt[j].Y;
                }
            }
            if (_minx != int.MaxValue) offsetX = pictureBox1.Width / 2 - (_maxx - _minx) / 2 ;
            if (_miny != int.MaxValue) offsetY = pictureBox1.Height / 2 - _miny - (_maxy - _miny) / 2 ;
            cur_offsetX = offsetX;
            cur_offsetY = offsetY;
            offsetX += X;
            offsetY += Y;
        }

        private void btnZoomOut_Click(object sender, EventArgs e)
        {
            zoom = zoom / 2;
            int dxp = offsetX - cur_offsetX;
            int dyp = offsetY - cur_offsetY;
            SetCorrectPos(dxp / 2, dyp / 2);
            pictureBox1.Invalidate();
        }

        private void btnZoomIn_Click(object sender, EventArgs e)
        {
            zoom = zoom * 2;
            int dxp = offsetX - cur_offsetX;
            int dyp = offsetY - cur_offsetY;
            SetCorrectPos(dxp*2,dyp*2);
            pictureBox1.Invalidate();
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            x1 = e.X;
            y1 = e.Y;
            x2 = offsetX;
            y2 = offsetY;
            down = true;
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (down == true)
            {
                offsetX = x2 - (x1 - e.X);
                offsetY = y2 - (y1 - e.Y);
                pictureBox1.Invalidate();
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            down = false;
        }

        private void btnOpenFile_Click(object sender, EventArgs e)
        {                       
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Shape Files|*.shp";
            if (dialog.ShowDialog() == DialogResult.OK)
                OpenShapeFile(dialog.FileName);
            else
            {
                dialog.Dispose();
                return;
            };
            dialog.Dispose();            
        }

        public void OpenShapeFile(string fileName)
        {
            readShapeFile(fileName);
            zoom = 256 / 32;
            SetCorrectPos(0, 0);
            pictureBox1.Invalidate();
        }

        private void NextColor()
        {
            defBrush++;
            if (defBrush == Colors.Length) defBrush = 0; ;
        }

        private void pictureBox1_DoubleClick(object sender, EventArgs e)
        {
            int dx = pictureBox1.Width / 2 - x1;
            int dy = pictureBox1.Height / 2 - y1;            
            zoom = zoom * 2;
            int dxp = offsetX - cur_offsetX;
            int dyp = offsetY - cur_offsetY;
            SetCorrectPos(dxp * 2 + dx, dyp * 2 + dy);
            pictureBox1.Invalidate();
        }

        private void pictureBox1_MouseEnter(object sender, EventArgs e)
        {
            pictureBox1.Select();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            polygons.Clear();
            lines.Clear();
            points.Clear();
            pictureBox1.Invalidate();
        }

        private void dpc_CheckedChanged(object sender, EventArgs e)
        {
            log.Visible = dpc.Checked;
        }

        private void pfs_SelectedIndexChanged(object sender, EventArgs e)
        {
            pictureBox1.Invalidate();
        }
    }
}