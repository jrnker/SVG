using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using Svg;
using System.Xml;
using Svg.Transforms;
using System.Diagnostics;
using System.Reflection;

namespace XMLOutputTester
{
    public partial class Form1 : Form
    {
        private SvgDocument FSvgDoc;

        public Form1()
        {
            InitializeComponent();


            FSvgDoc = new SvgDocument
            {
                Width = 500,
                Height = 500
            };

            FSvgDoc.ViewBox = new SvgViewBox(-250, -250, 500, 500);

            var group = new SvgGroup();
            FSvgDoc.Children.Add(group);

            group.Children.Add(new SvgCircle
            {
                Radius = 100,
                Fill = SvgPaintServer.None,
                Stroke = new SvgColourServer(Color.Black),
                StrokeWidth = 2
            });
            group.Children.Add(new SvgCircle
            {
                CenterX = 0,
                CenterY = 170,
                Radius = 50,
                Fill = new SvgColourServer(Color.Transparent),
                Stroke = new SvgColourServer(Color.Black),
                StrokeWidth = 2
            });

            var stream = new MemoryStream();
            FSvgDoc.Write(stream);
            textBox1.Text = Encoding.UTF8.GetString(stream.GetBuffer());

            pictureBox1.Image = FSvgDoc.Draw();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "(SVG)|*.svg";
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                FSvgDoc.Write(saveFileDialog1.FileName);
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            try
            {
            var t = new XmlDocument();
            t.XmlResolver = null; // Don't verify the XML
            t.LoadXml(textBox1.Text.Substring(textBox1.Text.IndexOf("<svg")));
            FSvgDoc = SvgDocument.Open(t);
                
            pictureBox1.Image = FSvgDoc.Draw();
            IterateKids(FSvgDoc.Children);
            }
            catch { }

        }

        private void IterateKids(SvgElementCollection elCol)
        { 
            foreach (SvgElement el in elCol)
            {

                if (el is Svg.SvgGroup)
                {
                    Svg.SvgGroup bla = (Svg.SvgGroup)el;
                }

                if (el.Children.Count == 0)
                {
                    if (el.ID != null)
                    { Debug.WriteLine(el.ID.ToString()); }
                    else { Debug.WriteLine("No id"); }
                    
                    if (el is Svg.SvgCircle)
                    {
                        Svg.SvgCircle bla = (Svg.SvgCircle)el;
                        var loo = FSvgDoc.Children;
                    }
                    else if (el is Svg.SvgEllipse)
                    {
                        Svg.SvgEllipse bla = (Svg.SvgEllipse)el;
                    }
                    else if (el is Svg.SvgLine)
                    {
                        Svg.SvgLine bla = (Svg.SvgLine)el;
                    }
                    else if (el is Svg.SvgPath)
                    {
                        Svg.SvgPath bla = (Svg.SvgPath)el;
                    }
                    else if (el is Svg.SvgPolygon)
                    {
                        Svg.SvgPolygon bla = (Svg.SvgPolygon)el; 
                    }
                    else if (el is Svg.SvgPolyline)
                    {
                        Svg.SvgPolyline bla = (Svg.SvgPolyline)el;
                    } 
                    else if (el is Svg.SvgRectangle)
                    {
                        Svg.SvgRectangle bla = (Svg.SvgRectangle)el;
                    }
                    else if (el is Svg.SvgText)
                    {
                        Svg.SvgText bla = (Svg.SvgText)el;
                    } 
                }
                else
                {
                   IterateKids(el.Children);
                
                }

            }
        
        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
