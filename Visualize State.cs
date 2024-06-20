using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MindFusion.Diagramming;
using MindFusion.Diagramming.WinForms;
using DiagramLinkLabel = MindFusion.Diagramming.LinkLabel;
using Newtonsoft.Json;
using MindFusion.Vsx;
using static System.Windows.Forms.LinkLabel;
using static xtUML1.JsonData;

namespace xtUML1
{
    class Visualize_State
    {
        private Diagram diagram;

        public Visualize_State()
        {
            diagram = new Diagram();
        }

        public void Visualize(string jsonFilePath, Panel panel)
        {
            try
            {
                diagram.ClearAll();

                string umlDiagramJson = File.ReadAllText(jsonFilePath);

                JsonData json = JsonConvert.DeserializeObject<JsonData>(umlDiagramJson);

                DiagramView diagramView = new DiagramView
                {
                    Dock = DockStyle.Fill,
                    Diagram = diagram,
                };

                Dictionary<string, CustomShapeNode> stateNodes = new Dictionary<string, CustomShapeNode>();

                int classStartX = 10;
                int classStartY = 10;
                int nodeX;
                int nodeY;

                foreach (var model in json.model)
                {
                    if (model.states != null)
                    {
                        nodeX = classStartX;
                        nodeY = classStartY;

                        ShapeNode classLabel = diagram.Factory.CreateShapeNode(nodeX, nodeY, 150, 25);
                        classLabel.Text = $"class: {model.class_name}";
                        classLabel.TextBrush = new MindFusion.Drawing.SolidBrush(Color.Black);

                        classLabel.Transparent = true;
                        classLabel.Locked = true;

                        nodeY += 40;

                        foreach (var state in model.states)
                        {
                            CustomShapeNode node = new CustomShapeNode(state.state_name, state.action);
                            node.Bounds = new RectangleF(nodeX, nodeY, 50, 25);
                            node.Text = $"state: {state.state_name}";
                            node.TextBrush = new MindFusion.Drawing.SolidBrush(Color.Black);



                            stateNodes[state.state_id] = node;

                            diagram.Items.Add(node);


                            node.AllowIncomingLinks = true;
                            node.AllowOutgoingLinks = true;

                            var anchorPoints = new AnchorPoint[]
                            {
                                new AnchorPoint(0, 25, true, true),
                                new AnchorPoint(0, 75, true, true),
                                new AnchorPoint(25, 0, true, true),
                                new AnchorPoint(75, 0, true, true),
                                new AnchorPoint(100, 25, true, true),
                                new AnchorPoint(100, 75, true, true),
                                new AnchorPoint(25, 100, true, true),
                                new AnchorPoint(75, 100, true, true)
                            };

                            var apat1 = new AnchorPattern(anchorPoints);
                            node.AnchorPattern = apat1;

                            stateNodes[state.state_id] = node;

                            nodeX += 120;

                            if (nodeX + 100 >= panel.Width)
                            {
                                nodeX = classStartX;
                                nodeY += 80;
                            }
                        }

                        classStartY = nodeY + 40;
                    }
                }
                Color[] linkColors = { Color.DarkViolet, Color.ForestGreen, Color.DarkBlue, Color.DarkOrange, Color.Red, Color.DeepPink };

                int colorIndex = 0;

                foreach (var model in json.model)
                {
                    if (model.states != null)
                    {
                        foreach (var state in model.states)
                        {
                            if (state.transitions != null)
                            {
                                foreach (var transition in state.transitions)
                                {
                                    if (stateNodes.ContainsKey(state.state_id) && stateNodes.ContainsKey(transition.target_state_id))
                                    {
                                        ShapeNode sourceNode = stateNodes[state.state_id];
                                        ShapeNode targetNode = stateNodes[transition.target_state_id];

                                        if (state.state_id == transition.target_state_id)
                                        {
                                            DiagramLink selfLink = diagram.Factory.CreateDiagramLink(sourceNode, targetNode);
                                            selfLink.HeadShape = ArrowHeads.Triangle;
                                            selfLink.HeadBrush = new MindFusion.Drawing.SolidBrush(linkColors[colorIndex]);
                                            selfLink.HeadPen = new MindFusion.Drawing.Pen(linkColors[colorIndex]);
                                            selfLink.AutoSnapToNode = true;
                                            selfLink.Pen = new MindFusion.Drawing.Pen(linkColors[colorIndex], 0.5F);

                                            DiagramLinkLabel selfLabel = selfLink.AddLabel($"{transition.target_state_event}({transition.parameter})");
                                            selfLabel.TextBrush = new MindFusion.Drawing.SolidBrush(linkColors[colorIndex]);
                                            selfLabel.SetLinkLengthPosition(0.5f);
                                        }
                                        else
                                        {
                                            AnchorPointCollection sourceAnchorPoints = sourceNode.AnchorPattern.Points;
                                            AnchorPointCollection targetAnchorPoints = targetNode.AnchorPattern.Points;

                                            if (sourceAnchorPoints.Count == 0 || targetAnchorPoints.Count == 0)
                                            {
                                                continue;
                                            }

                                            AnchorPoint sourceAnchorPoint = sourceAnchorPoints[0];
                                            AnchorPoint targetAnchorPoint = targetAnchorPoints[0];

                                            PointF sourceAnchorPointCoords = new PointF(
                                                sourceNode.Bounds.X + sourceAnchorPoint.X * sourceNode.Bounds.Width / 100,
                                                sourceNode.Bounds.Y + sourceAnchorPoint.Y * sourceNode.Bounds.Height / 100
                                            );

                                            PointF targetAnchorPointCoords = new PointF(
                                                targetNode.Bounds.X + targetAnchorPoint.X * targetNode.Bounds.Width / 100,
                                                targetNode.Bounds.Y + targetAnchorPoint.Y * targetNode.Bounds.Height / 100
                                            );

                                            DiagramLink link = diagram.Factory.CreateDiagramLink(sourceNode, targetNode);
                                            link.HeadShape = ArrowHeads.Triangle;
                                            link.HeadBrush = new MindFusion.Drawing.SolidBrush(linkColors[colorIndex]);
                                            link.HeadPen = new MindFusion.Drawing.Pen(linkColors[colorIndex]);

                                            // Assign a color to the link
                                            link.Pen = new MindFusion.Drawing.Pen(linkColors[colorIndex], 0.5F);

                                            link.AutoSnapToNode = true;
                                            link.StartPoint = sourceAnchorPointCoords;
                                            link.EndPoint = targetAnchorPointCoords;
                                            link.Route();

                                            sourceAnchorPoints.Remove(sourceAnchorPoint);
                                            targetAnchorPoints.Remove(targetAnchorPoint);

                                            DiagramLinkLabel label = link.AddLabel($"{transition.target_state_event}({transition.parameter})");
                                            label.TextBrush = new MindFusion.Drawing.SolidBrush(linkColors[colorIndex]);
                                            label.SetLinkLengthPosition(0.25f);
                                        }

                                        // Move to the next color or cycle back to the beginning of the array
                                        colorIndex = (colorIndex + 1) % linkColors.Length;
                                    }
                                }
                            }
                        }
                    }
                }


                panel.Controls.Clear();
                panel.Controls.Add(diagramView);
                panel.Invalidate();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        //class CustomShapeNode : ShapeNode
        //{
        //    public override void Draw(MindFusion.Drawing.IGraphics graphics, RenderOptions options)
        //    {
        //        base.Draw(graphics, options);

        //        RectangleF rectTop = new RectangleF(Bounds.Left, Bounds.Top, Bounds.Width, Bounds.Height / 4);
        //        RectangleF rectBottom = new RectangleF(Bounds.Left, Bounds.Top + Bounds.Height / 4, Bounds.Width, 3 * (Bounds.Height / 4));

        //        using (Pen pen = new Pen(Color.FromArgb(180, 150, 220), 0.5f))
        //        {
        //            graphics.DrawRectangle(pen, rectTop);
        //            graphics.DrawRectangle(pen, rectBottom);
        //        }
        //    }
        //}

        class CustomShapeNode : ShapeNode
        {
            private string additionalText;

            public CustomShapeNode(string mainText, string additionalText)
            {
                this.Text = mainText;
                this.additionalText = additionalText;
            }
            public override void Draw(MindFusion.Drawing.IGraphics graphics, RenderOptions options)
            {
                base.Draw(graphics, options);

                RectangleF rectTop = new RectangleF(Bounds.Left, Bounds.Top, Bounds.Width, Bounds.Height / 4);
                RectangleF rectBottom = new RectangleF(Bounds.Left, Bounds.Top + Bounds.Height / 4, Bounds.Width, 3 * (Bounds.Height / 4));

                using (Pen pen = new Pen(Color.MediumPurple, 0.5f)) // Set the pen color to black
                {
                    graphics.DrawRectangle(pen, rectTop);
                    graphics.DrawRectangle(pen, rectBottom);
                }

                RectangleF additionalTextRect = new RectangleF(rectBottom.Left, rectBottom.Top + 1, rectBottom.Width, rectBottom.Height); // Add 10 units to Y-coordinate

                // Draw the additional text
                using (Brush textBrush = new SolidBrush(Color.Black))
                {
                    Font textFont = new Font("Arial", 8); // Customize the font as needed
                    graphics.DrawString(additionalText, textFont, textBrush, additionalTextRect, new StringFormat { LineAlignment = StringAlignment.Near });
                }

            }
        }
    }
}