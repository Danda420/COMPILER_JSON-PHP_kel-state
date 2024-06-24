using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MindFusion.Diagramming.Layout;
using MindFusion.Diagramming.WinForms;
using MindFusion.Diagramming;
using Newtonsoft.Json.Linq;
using static xtUML1.JsonData;
using LinkLabel = MindFusion.Diagramming.LinkLabel;



namespace xtUML1
{
    public class VisualizeClass
    {
        private DiagramView diagramView;
        private Diagram diagram;
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
                            else if (type == "association")
                            {
                                ProcessAssociation(model);
                            }
                        }
                    }
                }
                CreateDiagram(classList, associationList, panel1);
                Console.WriteLine($"Number of nodes: {diagram.Nodes.Count}");
                Console.WriteLine($"Number of links: {diagram.Links.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }


        private void CreateDiagram(List<ClassModel> classList, List<AssociationModel> associationList, Panel panel1)
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

            // Process classes
            foreach (var cls in classList)
            {
                int x = (cls.ClassId.GetHashCode() % 2 == 0) ? 100 : 300;
                int y = (cls.ClassId.GetHashCode() / 2) * 50 + 10;

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

            // Process associations
            foreach (var assoc in associationList)
            {
                if (assoc.AssociationClass != null)
                {
                    var assocClass = assoc.AssociationClass;
                    int x = (assocClass.ClassId.GetHashCode() % 2 == 0) ? 200 : 400;
                    int y = (assocClass.ClassId.GetHashCode() / 2) * 50 + 30;

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

                    foreach (var cls in assoc.Classes)
                    {
                        if (nodes != null && nodes.TryGetValue(cls.ClassId, out var fromNode))
                        {
                            var toNode = assocClassNode;
                            if (diagram?.Factory != null)
                            {
                                var link = diagram.Factory.CreateDiagramLink(fromNode, toNode);
                                link.Text = assoc.Name;
                                link.HeadShapeSize = 0;
                                link.BaseShapeSize = 0;

                                var labelText = $"({cls.Multiplicity}) \n {cls.RoleName}";
                                var linkLabel = new LinkLabel(link, labelText);
                                linkLabel.RelativeTo = RelativeToLink.LinkLength;
                                linkLabel.LengthFactor = 1;
                                linkLabel.SetLinkLengthPosition(0.29f);
                                link.AddLabel(linkLabel);
                            }
                        }
                    }
                }
                else
                {
                    // Handle direct associations without association class
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
                NodeDistance = 50,
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
        private void ProcessAssociation2(JToken model)
        {
            var associationModel = new AssociationModel
            {
                Name = model["name"]?.ToString(),
                Classes = new List<AssocClass>()
            };

            foreach (var assocClass in model["class"] ?? new JArray())
            {
                string assocClassId = assocClass["class_id"].ToString();
                string assocClassName = assocClass["class_name"].ToString();
                string assocClassMultiplicity = assocClass["class_multiplicity"].ToString();

                var assocClassModel = new AssocClass
                {
                    ClassId = assocClassId,
                    ClassName = assocClassName,
                    Multiplicity = assocClassMultiplicity,
                };

                associationModel.Classes.Add(assocClassModel);
            }

            if (model["model"] != null && model["model"]["type"]?.ToString() == "association_class")
            {
                var assocModel = model["model"];
                string classId = assocModel["class_id"]?.ToString();
                string className = assocModel["class_name"]?.ToString();
                string kl = assocModel["KL"]?.ToString();

                var associationClassModel = new ClassModel
                {
                    ClassId = classId,
                    ClassName = className,
                    KL = kl,
                    Attributes = new List<AttributeModel>()
                };

                foreach (var attribute in assocModel["attributes"] ?? new JArray())
                {
                    string attributeType = attribute["attribute_type"].ToString();
                    string attributeName = attribute["attribute_name"].ToString();
                    string dataType = attribute["data_type"].ToString();

                    var attributeModel = new AttributeModel
                    {
                        AttributeType = attributeType,
                        AttributeName = attributeName,
                        DataType = dataType
                    };

                    associationClassModel.Attributes.Add(attributeModel);
                }

                associationModel.AssociationClass = associationClassModel;
            }

            associationList.Add(associationModel);
        }


    }
}

