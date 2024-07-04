using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MindFusion.Diagramming.Layout;
using MindFusion.Diagramming.WinForms;
using MindFusion.Diagramming;
using Newtonsoft.Json.Linq;
using static xtUML1.JsonData;
using LinkLabel = MindFusion.Diagramming.LinkLabel;
using static System.Windows.Forms.LinkLabel;
using System.Drawing.Drawing2D;
using MindFusion.Geometry.Geometry2D;

namespace xtUML1
{
    public class VisualizeClass
    {
        private DiagramView diagramView;
        private Diagram diagram;
        public List<ClassModel> superClassList = new List<ClassModel>();
        public List<ClassModel> classList = new List<ClassModel>();
        public List<AssociationModel> associationList = new List<AssociationModel>();
        public VisualizeClass()
        {
            diagram = new Diagram();
            DiagramView diagramView = new DiagramView
            {
                Dock = DockStyle.Fill,
                Diagram = diagram,
            };
        }

        public void VisualiseJson(string text, Panel panel1)
        {
            try
            {
                classList.Clear();
                associationList.Clear();
                superClassList.Clear();
                var jsonString = text;
                var jsonArray = JArray.Parse(jsonString);

                foreach (var item in jsonArray)
                {
                    if (item["model"] != null)
                    {
                        foreach (var model in item["model"])
                        {
                            string type = model["type"].ToString();
                            if (type == "class" || type == "imported_class")
                            {
                                ProcessClass(model);
                            }
                            else if (type == "superclass")
                            {
                                ProcessSuperClass(model);
                                Console.WriteLine("sampai sini 1");
                            }
                            else
                            {
                                ProcessAssociation(model);
                            }
                        }
                    }
                }
                CreateDiagram(classList, associationList, superClassList, panel1);
                Console.WriteLine($"Number of nodes: {diagram.Nodes.Count}");
                Console.WriteLine($"Number of links: {diagram.Links.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private void CreateDiagram(List<ClassModel> classList, List<AssociationModel> associationList, List<ClassModel> superClassList, Panel panel1)
        {
            if (diagramView == null)
            {
                diagramView = new DiagramView()
                {
                    Dock = DockStyle.Fill,
                };
                diagramView.Diagram = new Diagram();
            }

            var diagram = diagramView.Diagram;
            diagram.ClearAll();

            var nodes = new Dictionary<string, DiagramNode>();
            var processedAssociations = new HashSet<string>();

            Console.WriteLine("sampai sini 3");
            // Proses classes
            foreach (var cls in classList)
            {
                if (string.IsNullOrEmpty(cls.ClassName) || cls.Attributes == null || !cls.Attributes.Any())
                {
                    Console.WriteLine($"Skipping class {cls.ClassName} with no attributes or name");
                    continue;
                }

                var currentNode = diagram.Factory.CreateTableNode(0, 0, 80, 60, 2, 2);
                currentNode.Caption = $"+{cls.ClassName}";
                currentNode.CellFrameStyle = CellFrameStyle.Simple;
                currentNode.Brush = new MindFusion.Drawing.SolidBrush(Color.FromArgb(214, 213, 142));
                currentNode.CaptionBackBrush = new MindFusion.Drawing.SolidBrush(Color.FromArgb(159, 193, 49));

                var associationName = associationList
                    .Where(assoc => assoc.Classes.Any(c => c.ClassId == cls.ClassId))
                    .Select(assoc => assoc.Name)
                    .FirstOrDefault();

                if (cls.Attributes.Any())
                {
                    foreach (var attr in cls.Attributes)
                    {
                        currentNode.AddRow();
                        int r = currentNode.RowCount - 1;

                        if (attr.AttributeType == "referential_attribute")
                        {
                            currentNode[0, r].Text = attr.AttributeName;
                            currentNode[1, r].Text = attr.DataType + $" ({associationName})";
                        }
                        else
                        {
                            currentNode[0, r].Text = attr.AttributeName;
                            currentNode[1, r].Text = attr.DataType;
                        }
                    }
                }
                currentNode.ResizeToFitText(false, false);
                currentNode.Caption = cls.ClassName;
                currentNode.ConnectionStyle = TableConnectionStyle.Table;
                nodes[cls.ClassId] = currentNode;
            }
            //proses superclass
            foreach (var superCls in superClassList ?? new List<ClassModel>()) // Ensure superClassList is not null
            {
                if (string.IsNullOrEmpty(superCls.SuperClassId))
                {
                    continue;
                }

                var superClassNode = diagram.Factory.CreateTableNode(0, 0, 80, 60, 2, 2);
                superClassNode.Caption = $"+{superCls.SuperClassName}";
                superClassNode.CellFrameStyle = CellFrameStyle.Simple;
                superClassNode.Brush = new MindFusion.Drawing.SolidBrush(Color.FromArgb(214, 213, 142));
                superClassNode.CaptionBackBrush = new MindFusion.Drawing.SolidBrush(Color.FromArgb(159, 193, 49));

                if (superCls.Attributes?.Any() == true)
                {
                    foreach (var attr in superCls.Attributes)
                    {
                        superClassNode.AddRow();
                        int r = superClassNode.RowCount - 1;

                        superClassNode[0, r].Text = attr.AttributeName;
                        superClassNode[1, r].Text = attr.DataType;
                    }
                }

                superClassNode.ResizeToFitText(false, false);
                superClassNode.Caption = superCls.SuperClassName;
                superClassNode.ConnectionStyle = TableConnectionStyle.Table;
                nodes[superCls.SuperClassId] = superClassNode;

                // Create dummy node for subclasses
                var dummyNode = diagram.Factory.CreateShapeNode(0, 0, 1, 1);
                dummyNode.Transparent = true;
                dummyNode.Text = "Generalization";

                foreach (var subClass in superCls.SubClasses ?? new List<ClassModel>())  // Use SubClassModel
                {
                    if (string.IsNullOrEmpty(subClass.ClassId) && string.IsNullOrEmpty(subClass.ClassName))
                    {
                        continue;
                    }

                    if (nodes.ContainsKey(subClass.ClassId))
                    {
                        var subClassNode = nodes[subClass.ClassId];
                        var link = diagram.Factory.CreateDiagramLink( subClassNode, dummyNode);
                        link.HeadShapeSize = 0;
                        link.BaseShapeSize = 0;
                        link.Pen = new MindFusion.Drawing.Pen(Color.DarkBlue);
                    }
                }

                // Link dummy node to superclass
                var dummyLink = diagram.Factory.CreateDiagramLink(dummyNode, superClassNode);
                dummyLink.HeadShapeSize = 5;
                dummyLink.Text = "Generalization";
                dummyLink.BaseShapeSize = 0;
                dummyLink.Pen = new MindFusion.Drawing.Pen(Color.DarkBlue);

                diagram.Nodes.Add(dummyNode);
            }
            
            // Process associations
            foreach (var assoc in associationList)
            {
                if (assoc.AssociationClass != null)
                {
                    var assocClass = assoc.AssociationClass;
                    var assocClassNode = diagram.Factory.CreateTableNode(0, 0, 80, 60, 2, 2);
                    assocClassNode.Caption = $"+{assocClass.ClassName}";

                    assocClassNode.CellFrameStyle = CellFrameStyle.Simple;
                    assocClassNode.Brush = new MindFusion.Drawing.SolidBrush(Color.FromArgb(214, 213, 142));
                    assocClassNode.CaptionBackBrush = new MindFusion.Drawing.SolidBrush(Color.FromArgb(159, 193, 49));

                    if (assocClass.Attributes.Any())
                    {
                        foreach (var attr in assocClass.Attributes)
                        {
                            assocClassNode.AddRow();
                            int r = assocClassNode.RowCount - 1;

                            if (attr.AttributeType == "referential_attribute")
                            {
                                assocClassNode[0, r].Text = attr.AttributeName;
                                assocClassNode[1, r].Text = attr.DataType + $" ({assoc.Name})";
                            }
                            else
                            {
                                assocClassNode[0, r].Text = attr.AttributeName;
                                assocClassNode[1, r].Text = attr.DataType;
                            }
                        }
                    }
                    assocClassNode.ResizeToFitText(false, false);
                    assocClassNode.Caption = assocClass.ClassName;
                    assocClassNode.ConnectionStyle = TableConnectionStyle.Table;
                    nodes[assocClass.ClassId] = assocClassNode;


                    for (int i = 0; i < assoc.Classes.Count - 1; i++)
                    {
                        for (int j = i + 1; j < assoc.Classes.Count; j++)
                        {
                            var cls1 = assoc.Classes[i];
                            var cls2 = assoc.Classes[j];

                            if (nodes.TryGetValue(cls1.ClassId, out var fromNode) && nodes.TryGetValue(cls2.ClassId, out var toNode))
                            {
                                if (diagram?.Factory != null)
                                {
                                    var middleNode = diagram.Factory.CreateShapeNode(0, 0, 1, 1);
                                    middleNode.Transparent = true;

                                    var link1 = diagram.Factory.CreateDiagramLink(fromNode, middleNode);
                                    link1.Text = assoc.Name;
                                    link1.HeadShapeSize = 0;
                                    link1.BaseShapeSize = 0;
                                    link1.Pen = new MindFusion.Drawing.Pen(Color.DarkGreen);


                                    var link2 = diagram.Factory.CreateDiagramLink(toNode, middleNode);
                                    link2.Text = assoc.Name;
                                    link2.HeadShapeSize = 0;
                                    link2.BaseShapeSize = 0;
                                    link2.Pen = new MindFusion.Drawing.Pen(Color.DarkGreen);


                                    var link3 = diagram.Factory.CreateDiagramLink(middleNode, assocClassNode);
                                    link3.Text = assoc.Name;
                                    link3.HeadShapeSize = 0;
                                    link3.BaseShapeSize = 0;
                                    link3.Pen = new MindFusion.Drawing.Pen(Color.DarkGreen);
                                    link3.HandlesStyle = HandlesStyle.DashFrame;
                                    var dashPen = new MindFusion.Drawing.Pen(Color.Black, 0.4f);
                                    dashPen.DashStyle = DashStyle.Dash;
                                    link3.Pen = dashPen;


                                    var labelText1 = $"({cls1.Multiplicity}) \n {cls1.RoleName}";
                                    var linkLabel1 = new LinkLabel(link1, labelText1);
                                    linkLabel1.RelativeTo = RelativeToLink.LinkLength;
                                    linkLabel1.LengthFactor = 1;
                                    linkLabel1.SetLinkLengthPosition(0.2f);
                                    link1.AddLabel(linkLabel1);

                                    var labelText2 = $"({cls2.Multiplicity}) \n {cls2.RoleName}";
                                    var linkLabel2 = new LinkLabel(link2, labelText2);
                                    linkLabel2.RelativeTo = RelativeToLink.LinkLength;
                                    linkLabel2.LengthFactor = 1;
                                    linkLabel2.SetLinkLengthPosition(0.2f);
                                    link2.AddLabel(linkLabel2);
                                }
                            }
                        }
                    }
                }
                else
                {
                    //Handle direct associations without association class
                    for (int i = 0; i < assoc.Classes.Count - 1; i++)
                    {
                        for (int j = i + 1; j < assoc.Classes.Count; j++)
                        {
                            var cls1 = assoc.Classes[i];
                            var cls2 = assoc.Classes[j];

                            var linkKey = $"{cls1.ClassId}-{cls2.ClassId}";
                            if (!processedAssociations.Contains(linkKey))
                            {
                                processedAssociations.Add(linkKey);

                                if (nodes.ContainsKey(cls1.ClassId) && nodes.ContainsKey(cls2.ClassId))
                                {
                                    var fromNode = nodes[cls1.ClassId];
                                    var toNode = nodes[cls2.ClassId];

                                    var link = diagram.Factory.CreateDiagramLink(fromNode, toNode);
                                    link.Text = assoc.Name;
                                    link.HeadShapeSize = 0;
                                    link.BaseShapeSize = 0;

                                    var labelText1 = $"({cls1.Multiplicity}) \n {cls1.RoleName}";
                                    var linkLabel1 = new LinkLabel(link, labelText1);
                                    linkLabel1.RelativeTo = RelativeToLink.LinkLength;
                                    linkLabel1.LengthFactor = 1;
                                    linkLabel1.SetLinkLengthPosition(0.29f);

                                    var labelText2 = $" {cls2.RoleName} \n({cls2.Multiplicity}) ";
                                    var linkLabel2 = new LinkLabel(link, labelText2);
                                    linkLabel2.RelativeTo = RelativeToLink.LinkLength;
                                    linkLabel2.LengthFactor = 1;
                                    linkLabel2.SetLinkLengthPosition(0.99f);
                                    link.AddLabel(linkLabel1);
                                    link.AddLabel(linkLabel2);
                                    link.AddLabel(linkLabel1);

                                }
                            }
                        }
                    }
                }
            }
            // Arrange the diagram
            var layout = new LayeredLayout
            {
                EnforceLinkFlow = true,
                IgnoreNodeSize = false,
                NodeDistance = 40,
                LayerDistance = 40
            };
            layout.Arrange(diagram);
            panel1.Controls.Clear();
            panel1.Controls.Add(diagramView);
            panel1.Invalidate();
            diagram.ResizeToFitItems(5);
        }
        private void ProcessClass(JToken model)
        {
            string classId = model["class_id"]?.ToString();
            string className = model["class_name"]?.ToString();

            if (string.IsNullOrEmpty(classId) || string.IsNullOrEmpty(className))
            {
                return;
            }

            string kl = model["KL"]?.ToString();
            var classModel = new ClassModel
            {
                ClassId = classId,
                ClassName = className,
                KL = kl,
                Attributes = new List<AttributeModel>()
            };

            foreach (var attribute in model["attributes"] ?? new JArray())
            {
                if (attribute["attribute_type"] == null || string.IsNullOrEmpty(attribute["attribute_type"].ToString()))
                {
                    continue;
                }

                string attributeType = attribute["attribute_type"].ToString();
                string attributeName = attribute["attribute_name"]?.ToString();
                string dataType = attribute["data_type"]?.ToString();

                // Continue only if attributeName and dataType are not null or empty
                if (!string.IsNullOrEmpty(attributeName) && !string.IsNullOrEmpty(dataType))
                {
                    var attributeModel = new AttributeModel
                    {
                        AttributeType = attributeType,
                        AttributeName = attributeName,
                        DataType = dataType
                    };

                    classModel.Attributes.Add(attributeModel);
                }
            }
            classList.Add(classModel);
        }

        private void ProcessSuperClass(JToken model)
        {
            string superClassId = model["superclass_id"]?.ToString();
            string superClassName = model["superclass_name"]?.ToString();

            if (string.IsNullOrEmpty(superClassId) || string.IsNullOrEmpty(superClassName))
            {
                return;
            }

            string kl = model["KL"]?.ToString();
            var superClassModel = new ClassModel
            {
                SuperClassId = superClassId,
                SuperClassName = superClassName,
                KL = kl,
                SubClasses = new List<ClassModel>(),
                Attributes = new List<AttributeModel>()
            };

            foreach (var attribute in model["attributes"] ?? new JArray())
            {
                if (attribute["attribute_type"] == null || string.IsNullOrEmpty(attribute["attribute_type"].ToString()))
                {
                    continue;
                }

                string attributeType = attribute["attribute_type"].ToString();
                string attributeName = attribute["attribute_name"]?.ToString();
                string dataType = attribute["data_type"]?.ToString();

                if (!string.IsNullOrEmpty(attributeName) && !string.IsNullOrEmpty(dataType))
                {
                    var attributeModel = new AttributeModel
                    {
                        AttributeType = attributeType,
                        AttributeName = attributeName,
                        DataType = dataType
                    };

                    superClassModel.Attributes.Add(attributeModel);
                    Console.WriteLine("sampai sini 0.5" + attributeName);

                }
            }

            foreach (var subclass in model["subclasses"] ?? new JArray())
            {
                string subClassId = subclass["class_id"]?.ToString();
                string subClassName = subclass["class_name"]?.ToString();
                Console.WriteLine("sampai sini 1.4" + subClassName + " " + subClassId );


                if (!string.IsNullOrEmpty(subClassId) && !string.IsNullOrEmpty(subClassName))
                {
                    var subClassModel = new ClassModel
                    {
                        ClassId = subClassId,
                        SuperClassId = superClassId
                    };
                    Console.WriteLine("sampai sini 2" + subClassName);


                    superClassModel.SubClasses.Add(subClassModel);
                }
            }

            superClassList.Add(superClassModel);
            Console.WriteLine("sampai sini 2" + superClassName);

        }


        private void ProcessAssociation(JToken model)
        {
            var associationModel = new AssociationModel
            {
                Name = model["name"]?.ToString(),
                Classes = new List<AssocClass>()
            };

            foreach (var assocClass in model["class"] ?? new JArray())
            {
                string assocClassId = assocClass["class_id"]?.ToString();
                string assocClassName = assocClass["class_name"]?.ToString();
                string assocClassMultiplicity = assocClass["class_multiplicity"]?.ToString();
                string assocClassRole = assocClass["role_name"].ToString();


                if (!string.IsNullOrEmpty(assocClassId) && !string.IsNullOrEmpty(assocClassName) && !string.IsNullOrEmpty(assocClassMultiplicity))
                {
                    var assocClassModel = new AssocClass
                    {
                        ClassId = assocClassId,
                        ClassName = assocClassName,
                        Multiplicity = assocClassMultiplicity,
                        RoleName = assocClassRole

                    };

                    associationModel.Classes.Add(assocClassModel);
                }
            }
            if (model["model"] != null && model["model"]["type"]?.ToString() == "association_class")
            {
                var assocModel = model["model"];
                string classId = assocModel["class_id"]?.ToString();
                string className = assocModel["class_name"]?.ToString();
                string kl = assocModel["KL"]?.ToString();

                if (!string.IsNullOrEmpty(classId) && !string.IsNullOrEmpty(className))
                {
                    var associationClassModel = new ClassModel
                    {
                        ClassId = classId,
                        ClassName = className,
                        KL = kl,
                        Attributes = new List<AttributeModel>()
                    };

                    foreach (var attribute in assocModel["attributes"] ?? new JArray())
                    {
                        if (attribute["attribute_type"] == null || string.IsNullOrEmpty(attribute["attribute_type"].ToString()))
                        {
                            continue; // Skip this attribute if "attribute_type" is null or empty
                        }

                        string attributeType = attribute["attribute_type"].ToString();
                        string attributeName = attribute["attribute_name"]?.ToString();
                        string dataType = attribute["data_type"]?.ToString();

                        if (!string.IsNullOrEmpty(attributeName) && !string.IsNullOrEmpty(dataType))
                        {
                            var attributeModel = new AttributeModel
                            {
                                AttributeType = attributeType,
                                AttributeName = attributeName,
                                DataType = dataType,
                            };
                            associationClassModel.Attributes.Add(attributeModel);
                        }
                    }
                    associationModel.AssociationClass = associationClassModel;
                }
            }
            associationList.Add(associationModel);
        }
    }
}

