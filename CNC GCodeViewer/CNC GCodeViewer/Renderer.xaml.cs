﻿/*
 * Renderer.xaml.cs - part of CNC Controls library
 *
 * v0.02 / 2019-10-02 / Io Engineering (Terje Io)
 *
 */

/*

Copyright (c) 2019, Io Engineering (Terje Io)
All rights reserved.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

· Redistributions of source code must retain the above copyright notice, this
list of conditions and the following disclaimer.

· Redistributions in binary form must reproduce the above copyright notice, this
list of conditions and the following disclaimer in the documentation and/or
other materials provided with the distribution.

· Neither the name of the copyright holder nor the names of its contributors may
be used to endorse or promote products derived from this software without
specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CNC.Core;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;

namespace CNC.Controls.Viewer
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class Renderer : UserControl
    {
        private Point3D point0;  // last point
        private Vector3D delta0;  // (dx,dy,dz)
        private List<LinesVisual3D> trace;
        private LinesVisual3D path;
        private double minDistanceSquared;

        public SolidColorBrush AxisBrush { get; set; }
        public double TickSize { get; set; }

        public Renderer()
        {
            InitializeComponent();

            minDistanceSquared = MinDistance * MinDistance;
            AxisBrush = Brushes.Gray;
            TickSize = 10;
        }

        public int ArcResolution { get; set; } = 5;
        public double MinDistance { get; set; } = 0.03;
        public bool ShowGrid { get; set; } = true;
        public bool ShowAxes { get; set; } = true;
        public bool ShowBoundingBox { get; set; } = true;

        public void Render(List<GCodeToken> tokens, gcodeBoundingBox bbox)
        {
            double lineThickness = bbox.MaxSize / 1000;
            double arrowOffset = lineThickness * 30;
            double labelOffset = lineThickness * 50;
            bool canned = false;

            Plane plane = Plane.XY;
            IJKMode ijkmode = IJKMode.Absolute;

            //trace.Clear();
            trace = null;
            viewport.Children.Clear();

            if (ShowGrid)
            {
                var grid = new GridLinesVisual3D();
                grid.Center = new Point3D(bbox.MinX + 0.5 * bbox.SizeX, bbox.MinY + 0.5 * bbox.SizeY, 0.0);
                grid.Length = bbox.SizeX;
                grid.Width = bbox.SizeY;
                grid.MinorDistance = TickSize;
                grid.MajorDistance = bbox.MaxSize;
                grid.Width = bbox.SizeY + 10;
                grid.Length = bbox.SizeX + 10;
                grid.Thickness = lineThickness;
                grid.Fill = AxisBrush;
                viewport.Children.Add(grid);
            }

            if (ShowAxes)
            {
                var arrow = new ArrowVisual3D();
                arrow.Point2 = new Point3D(bbox.SizeX + arrowOffset, 0.0, 0.0);
                arrow.Diameter = lineThickness * 5;
                arrow.Fill = AxisBrush;
                viewport.Children.Add(arrow);

                var label = new BillboardTextVisual3D();
                label.Text = "X";
                label.FontWeight = FontWeights.Bold;
                label.Foreground = AxisBrush;
                label.Position = new Point3D(bbox.SizeX + labelOffset, 0.0, 0.0);
                viewport.Children.Add(label);

                arrow = new ArrowVisual3D();
                arrow.Point2 = new Point3D(0.0, bbox.SizeY + arrowOffset, 0.0);
                arrow.Diameter = lineThickness * 5;
                arrow.Fill = AxisBrush;
                viewport.Children.Add(arrow);

                label = new BillboardTextVisual3D();
                label.Text = "Y";
                label.FontWeight = FontWeights.Bold;
                label.Foreground = AxisBrush;
                label.Position = new Point3D(0.0, bbox.SizeY + labelOffset, 0.0);
                viewport.Children.Add(label);

                if (bbox.SizeZ > 0.0)
                {
                    arrow = new ArrowVisual3D();
                    arrow.Point1 = new Point3D(0.0, 0.0, bbox.MinZ);
                    arrow.Point2 = new Point3D(0.0, 0.0, bbox.MaxZ + arrowOffset);
                    arrow.Diameter = lineThickness * 5;
                    arrow.Fill = AxisBrush;
                    viewport.Children.Add(arrow);

                    label = new BillboardTextVisual3D();
                    label.Text = "Z";
                    label.FontWeight = FontWeights.Bold;
                    label.Foreground = AxisBrush;
                    label.Position = new Point3D(0.0, 0.0, bbox.MaxZ + labelOffset);
                    viewport.Children.Add(label);
                }
            }

            if (ShowBoundingBox && bbox.SizeZ > 0.0)
            {
                Rect3D BoundingBox = new Rect3D(bbox.MinX, bbox.MinY, bbox.MinZ, bbox.SizeX, bbox.SizeY, bbox.SizeZ);

                var box = new BoundingBoxWireFrameVisual3D();
                box.BoundingBox = BoundingBox;
                box.Thickness = 1;
                box.Color = Colors.Gray;
                viewport.Children.Add(box);
            }

            GCodeToken last = new GCodeToken();

            foreach (GCodeToken token in tokens)
            {
                switch (token.command)
                {
                    case GCodeToken.Command.G0:
                        if(last.command == GCodeToken.Command.G1 && (last.x != point0.X || last.y != point0.Y))
                            path.Points.Add(new Point3D(token.x, token.y, token.z));

                        AddPoint(new Point3D(token.x, token.y, token.z), Colors.Red, 0.5);
                        break;

                    case GCodeToken.Command.G1:
                        if (last.command == GCodeToken.Command.G0 && (last.x != point0.X || last.y != point0.Y))
                            path.Points.Add(new Point3D(token.x, token.y, token.z));
                        //    AddPoint(point0, Colors.Blue, 1);
                        AddPoint(new Point3D(token.x, token.y, token.z), Colors.Blue, 1);
                        break;

                    case GCodeToken.Command.G2:
                    case GCodeToken.Command.G3:
                        if (double.IsNaN(token.i) && double.IsNaN(token.j) && double.IsNaN(token.k))
                            DrawArc(plane, last.x, last.y, last.z, token.x, token.y, token.z, token.r, token.command == GCodeToken.Command.G2);
                        else
                            DrawArc(plane, last.x, last.y, last.z, token.x, token.y, token.z, token.i, token.j, ijkmode == IJKMode.Absolute, token.command == GCodeToken.Command.G2);
                        break;

                    case GCodeToken.Command.G17:
                        plane = Plane.XY;
                        break;

                    case GCodeToken.Command.G18:
                        plane = Plane.ZX;
                        break;

                    case GCodeToken.Command.G19:
                        plane = Plane.YZ;
                        break;

                    case GCodeToken.Command.G80:
                        canned = false;
                        break;

                    case GCodeToken.Command.G81:
                        if (!canned)
                        {
                            canned = true;
                            if (point0.Z < token.r)
                                AddPoint(new Point3D(point0.X, point0.Y, token.r), Colors.Red, 1);
                        }
                        AddPoint(new Point3D(token.x, token.y, Math.Max(token.z, token.r)), Colors.Red, 1);
                        AddPoint(new Point3D(token.x, token.y, token.z), Colors.Blue, 1);
                        AddPoint(new Point3D(token.x, token.y, token.r), Colors.Green, 1);
                        break;

                    case GCodeToken.Command.G90_1:
                        ijkmode = IJKMode.Absolute;
                        break;

                    case GCodeToken.Command.G91_1:
                        ijkmode = IJKMode.Incremental;
                        break;
                }
                last = token;
            }
            last = null;

            foreach (var path in trace)
                viewport.Children.Add(path);

            refreshCamera(bbox);

        }
        //-------------

        public void NewTrace(Point3D point, Color color, double thickness = 1)
        {
            path = new LinesVisual3D();
            path.Color = color;
            //       path.Points.Add(point);
            path.Thickness = thickness;
            trace = new List<LinesVisual3D>();
            trace.Add(path);
        //    viewport.Children.Add(path);
            point0 = point;
            delta0 = new Vector3D();

            /*
            if (marker != null)
            {
                marker.Origin = point;
                coords.Position = new Point3D(point.X - labelOffset, point.Y - labelOffset, point.Z + labelOffset);
                coords.Text = string.Format(coordinateFormat, point.X, point.Y, point.Z);
            } */
        }
        public void NewTrace(double x, double y, double z, Color color, double thickness = 1)
        {
            NewTrace(new Point3D(x, y, z), color, thickness);
        }

        public void AddPoint(Point3D point, Color color, double thickness = -1)
        {
            if (trace == null)
            {
                NewTrace(point, color, (thickness > 0) ? thickness : 1);
                return;
            }


            bool sameDir = false;

            if (path.Color != color || (thickness > 0.0 && path.Thickness != thickness))
            {
                if (thickness <= 0.0)
                    thickness = path.Thickness;

                path = new LinesVisual3D();
                path.Color = color;
                path.Thickness = thickness;

                trace.Add(path);
              //  viewport.Children.Add(path);
            }
            else
            {

                // If line segments AB and BC have the same direction (small cross product) then remove point B.

                var delta = new Vector3D(point.X - point0.X, point.Y - point0.Y, point.Z - point0.Z);
                delta.Normalize();  // use unit vectors (magnitude 1) for the cross product calculations
                Vector3D cp; double xp2;
                if (path.Points.Count > (trace.Count == 1 ? 1 : 0))
                {
                    cp = Vector3D.CrossProduct(delta, delta0);
                    xp2 = cp.LengthSquared;
                    sameDir = xp2 > 0d && (xp2 < 0.0005d);  // approx 0.001 seems to be a reasonable threshold from logging xp2 values
                                                //if (!sameDir) Title = string.Format("xp2={0:F6}", xp2);
                }

                if (sameDir)  // extend the current line segment
                {
                    path.Points[path.Points.Count - 1] = point;
                    delta0 += delta;
                }
                else
                {
                    if ((point - point0).LengthSquared < minDistanceSquared)
                        return;  // less than min distance from last point
                    delta0 = delta;
                }
            }

            if (!sameDir)
            {
                path.Points.Add(point0);
                path.Points.Add(point);
            }

            point0 = point;

            //if (marker != null)
            //{
            //    marker.Origin = point;
            //    coords.Position = new Point3D(point.X - labelOffset, point.Y - labelOffset, point.Z + labelOffset);
            //    coords.Text = string.Format(coordinateFormat, point.X, point.Y, point.Z);
            //}
        }

        //-------------
        public void refreshCamera(gcodeBoundingBox bbox)
        {
            viewport.DefaultCamera.Position = new Point3D(bbox.SizeX / 2, bbox.SizeY / 2, 100);
            //    viewport.CameraController.AddRotateForce(0.001, 0.001); // emulate move camera 
        }
        //private void DrawLine(LinesVisual3D lines, double x_start, double y_start, double z_start, double x_stop, double y_stop, double z_stop)
        //{
        //    lines.Points.Add(new Point3D(x_start, y_start, z_start));
        //    lines.Points.Add(new Point3D(x_stop, y_stop, z_stop));
        //}

        //private void DrawLine(LinesVisual3D lines, Point3D start, Point3D end)
        //{
        //    lines.Points.Add(start);
        //    lines.Points.Add(end);
        //}

        private void DrawArc(Plane plane, double x_start, double y_start, double z_start,
                              double x_stop, double y_stop, double z_stop,
                               double radius, bool clockwise)
        {
            Point3D initial = new Point3D(x_start, y_start, z_start);
            Point3D nextpoint = new Point3D(x_stop, y_stop, z_stop);

            Point3D center = convertRToCenter(initial, nextpoint, radius, false, clockwise);

            List<Point3D> arcpoints = generatePointsAlongArcBDring(plane, initial, nextpoint, center, clockwise, 0, ArcResolution); // Dynamic resolution

            Point3D old_point = arcpoints[0];
            arcpoints.RemoveAt(0);

            AddPoint(old_point, Colors.Blue);

            foreach (Point3D point in arcpoints)
                AddPoint(point, Colors.Blue);
        }

        private void DrawArc(Plane plane, double x_start, double y_start, double z_start,
                        double x_stop, double y_stop, double z_stop,
                        double i_pos, double j_pos,
                        bool absoluteIJKMode, bool clockwise)
        {
            Point3D initial = new Point3D(x_start, y_start, z_start);
            Point3D nextpoint = new Point3D(x_stop, y_stop, z_stop);
            Point3D center = updateCenterWithCommand(initial, nextpoint, j_pos, i_pos, double.NaN, absoluteIJKMode);

            List<Point3D> arcpoints = generatePointsAlongArcBDring(plane, initial, nextpoint, center, clockwise, 0d, ArcResolution); // Dynamic resolution

            //Point3D old_point = arcpoints[0];
            //arcpoints.RemoveAt(0);

            //AddPoint(old_point, Colors.Blue);

            foreach (Point3D point in arcpoints)
                AddPoint(point, Colors.Blue);
        }

        /**
        * Generates the points along an arc including the start and end points.
        */
        public static List<Point3D> generatePointsAlongArcBDring(Plane plane, Point3D p1, Point3D p2, Point3D center, bool isCw, double R, int arcResolution)
        {
            double radius = R;
            double sweep;

            // Calculate radius if necessary.
            if (radius == 0d)
            {
                radius = Math.Sqrt(Math.Pow(p1.X - center.X, 2.0) + Math.Pow(p1.Y - center.Y, 2.0));
            }

            // Calculate angles from center.
            double startAngle = getAngle(center, p1);
            double endAngle = getAngle(center, p2);

            // Fix semantics, if the angle ends at 0 it really should end at 360.
            if (endAngle == 0d)
                endAngle = Math.PI * 2d;

            // Calculate distance along arc.
            if (!isCw && endAngle < startAngle)
                sweep = ((Math.PI * 2d - startAngle) + endAngle);
            else if (isCw && endAngle > startAngle)
                sweep = ((Math.PI * 2d - endAngle) + startAngle);
            else
                sweep = Math.Abs(endAngle - startAngle);

            return generatePointsAlongArcBDring(plane, p1, p2, center, isCw, radius, startAngle, endAngle, sweep, arcResolution);
        }

        /**
            * Generates the points along an arc including the start and end points.
            */
        public static List<Point3D> generatePointsAlongArcBDring(Plane plane, Point3D p1,
                Point3D p2, Point3D center, bool isCw, double radius,
                double startAngle, double endAngle, double sweep, int numPoints)
        {

            Point3D lineEnd = p2;
            List<Point3D> segments = new List<Point3D>();
            double angle;

            double zIncrement = (p2.Z - p1.Z) / numPoints;
            for (int i = 0; i < numPoints; i++)
            {
                if (isCw)
                    angle = (startAngle - i * sweep / numPoints);
                else
                    angle = (startAngle + i * sweep / numPoints);

                if (angle >= Math.PI * 2d)
                    angle = angle - Math.PI * 2d;

                lineEnd.X = Math.Cos(angle) * radius + center.X;
                lineEnd.Y = Math.Sin(angle) * radius + center.Y;
                lineEnd.Z += zIncrement;

                segments.Add(lineEnd);
            }

            segments.Add(p2);

            return segments;
        }

        /** 
            * Return the angle in radians when going from start to end.
            */
        public static double getAngle(Point3D start, Point3D end)
        {
            double deltaX = end.X - start.X;
            double deltaY = end.Y - start.Y;

            double angle = 0d;

            if (deltaX != 0d)
            { // prevent div by 0
                // it helps to know what quadrant you are in
                if (deltaX > 0d && deltaY >= 0d)
                {  // 0 - 90
                    angle = Math.Atan(deltaY / deltaX);
                }
                else if (deltaX < 0d && deltaY >= 0d)
                { // 90 to 180
                    angle = Math.PI - Math.Abs(Math.Atan(deltaY / deltaX));
                }
                else if (deltaX < 0d && deltaY < 0d)
                { // 180 - 270
                    angle = Math.PI + Math.Abs(Math.Atan(deltaY / deltaX));
                }
                else if (deltaX > 0.0 && deltaY < 0d)
                { // 270 - 360
                    angle = Math.PI * 2d - Math.Abs(Math.Atan(deltaY / deltaX));
                }
            }
            else
            {
                // 90 deg
                if (deltaY > 0d)
                {
                    angle = Math.PI / 2d;
                }
                // 270 deg
                else
                {
                    angle = Math.PI * 3d / 2d;
                }
            }

            return angle;
        }


        static public Point3D updateCenterWithCommand(Point3D initial, Point3D nextPoint,
                        double j, double i, double k, bool absoluteIJKMode)
        {
            Point3D newPoint = new Point3D();

            if (absoluteIJKMode)
            {
                if (!double.IsNaN(i))
                    newPoint.X = i;
                if (!double.IsNaN(j))
                    newPoint.Y = j;
                if (!double.IsNaN(k))
                    newPoint.Z = k;
            }
            else
            {
                if (!double.IsNaN(i))
                    newPoint.X = initial.X + i;
                if (!double.IsNaN(j))
                    newPoint.Y = initial.Y + j;
                if (!double.IsNaN(k))
                    newPoint.Z = initial.Z + k;
            }

            return newPoint;
        }

        public static double Hypotenuse(double a, double b)
        {
            return Math.Sqrt(a * a + b * b);
        }

        // Try to create an arc :)
        public static Point3D convertRToCenter(Point3D start, Point3D end, double radius, bool absoluteIJK, bool clockwise)
        {
            double R = radius;
            Point3D center = new Point3D();

            // This math is copied from GRBL in gcode.c
            double x = end.X - start.X;
            double y = end.Y - start.Y;

            double h_x2_div_d = 4d * R * R - x * x - y * y;
            if (h_x2_div_d < 0d)
            {
                Console.Write("Error computing arc radius.");
            }

            h_x2_div_d = (-Math.Sqrt(h_x2_div_d)) / Hypotenuse(x, y);

            if (!clockwise)
            {
                h_x2_div_d = -h_x2_div_d;
            }

            // Special message from gcoder to software for which radius
            // should be used.
            if (R < 0d)
            {
                h_x2_div_d = -h_x2_div_d;
                // TODO: Places that use this need to run ABS on radius.
                radius = -radius;
            }

            double offsetX = 0.5d * (x - (y * h_x2_div_d));
            double offsetY = 0.5d * (y + (x * h_x2_div_d));

            if (!absoluteIJK)
            {
                center.X = start.X + offsetX;
                center.Y = start.Y + offsetY;
            }
            else
            {
                center.X = offsetX;
                center.Y = offsetY;
            }

            return center;
        }
    }
}