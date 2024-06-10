using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using static xtUML1.JsonData;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace xtUML1
{
    class Translate
    {
        private readonly StringBuilder sourceCodeBuilder;
        private string status;
        private bool hasTransition;
        private string targetState;
        string stateAttribute;

        public Translate()
        {
            sourceCodeBuilder = new StringBuilder();
        }
        public string GeneratePHPCode(string selectedFilePath)
        {
            string translatedPhpCode = string.Empty;
            try
            {
                // Read the JSON file
                string umlDiagramJson = File.ReadAllText(selectedFilePath);

                // Decode JSON data
                JsonData json = JsonConvert.DeserializeObject<JsonData>(umlDiagramJson);

                // Example: Generate PHP code
                GenerateNamespace(json.sub_name);

                GenerateStates(json);

                foreach (var model in json.model)
                {
                    if (model.type == "class")
                    {
                        GenerateClass(model, json);
                    }
                    else if (model.type == "association" && model.model != null)
                    {
                        GenerateAssociationClass(model.model);
                    }

                    if (model.type == "imported_class")
                    {
                        sourceCodeBuilder.AppendLine($"//Imported Class");
                        GenerateImportedClass(model, json);
                    }
                }

                bool generateAssocClass = json.model.Any(model => model.type == "association");

                //if (generateAssocClass)
                //{
                //    sourceCodeBuilder.AppendLine($"// Just an Example");
                //    GenerateAssocClass();
                //}

                //foreach (var model in json.model)
                //{
                //    if (model.type == "association")
                //    {
                //        GenerateObjAssociation(model);
                //    }
                //}

                sourceCodeBuilder.AppendLine($"class TIMER {{");
                sourceCodeBuilder.AppendLine($"}}");

                // Display or save the generated PHP code
                translatedPhpCode = sourceCodeBuilder.ToString();
            }
            catch (Exception ex)
            {
                // Handle exceptions, e.g., log or display an error message
                Console.WriteLine($"Error: {ex.Message}");
            }

            return translatedPhpCode;
        }

        private void GenerateNamespace(string namespaceName)
        {
            sourceCodeBuilder.AppendLine($"<?php\nnamespace {namespaceName};\n");
        }

        private void GenerateStates(JsonData json)
        {
            // STATES START
            foreach (JsonData.Model model in json.model)
            {
                var states = new List<string>();
                if (model.states != null)
                {
                    foreach (JsonData.State state in model.states)
                    {
                        string stateAdd = state.state_name.Replace(" ", "");
                        states.Add(stateAdd);
                    }
                    sourceCodeBuilder.AppendLine("   " +
                        $"class {model.class_name}States" + " {");
                    foreach (var state in states)
                    {
                        sourceCodeBuilder.AppendLine("      " +
                            $"const {state.ToUpper()} = " + "'" + state + "'" + ";");
                    }
                    sourceCodeBuilder.AppendLine("   }");
                    sourceCodeBuilder.AppendLine("");
                }
            }
            // STATES END
        }

        private void GenerateStateAction(JsonData.Model model)
        {
            foreach (JsonData.Attribute1 attr in model.attributes)
            {
                if (attr.default_value != null)
                {
                    stateAttribute = attr.attribute_name;
                }
            }
            sourceCodeBuilder.AppendLine("      " +
                            $"public function onStateAction()");
            sourceCodeBuilder.AppendLine("      {");
            sourceCodeBuilder.AppendLine("           " +
                $"switch($this->{stateAttribute})" + " {");
            foreach (JsonData.State statess in model.states)
            {
                sourceCodeBuilder.AppendLine("              " +
                    $"case {model.class_name}States::{statess.state_name.Replace(" ", "").ToUpper()}:");
                sourceCodeBuilder.AppendLine("                  " +
                    "// implementations code here");
                if (statess.transitions != null)
                {
                    foreach (var transition in statess.transitions)
                    {
                        sourceCodeBuilder.AppendLine("                  " +
                            $"if ($this->{stateAttribute} == {model.class_name}States::{transition.target_state.Replace(" ", "").ToUpper()}) {{");
                        sourceCodeBuilder.AppendLine("                      " +
                            $"$this->{transition.target_state_event}();");
                        sourceCodeBuilder.AppendLine("                  }");
                    }
                }
                sourceCodeBuilder.AppendLine("                  " +
                    "break;");
            }
            sourceCodeBuilder.AppendLine("              " +
                    $"default:");
            sourceCodeBuilder.AppendLine("                  " +
                    "break;");
            sourceCodeBuilder.AppendLine("           }");
            sourceCodeBuilder.AppendLine("      }");
            foreach (JsonData.State state in model.states)
            {
                void stateEventBuilder(string stateEvent)
                {
                    sourceCodeBuilder.AppendLine("");
                    sourceCodeBuilder.AppendLine("      " +
                        $"public function {stateEvent}()" + " {");
                    foreach (JsonData.Attribute1 attr in model.attributes)
                    {
                        if (attr.data_type == "state")
                        {
                            sourceCodeBuilder.AppendLine("           " +
                                    $"if ($this->{attr.attribute_name} != {model.class_name}States::{state.state_name.Replace(" ", "").ToUpper()})" + " {");
                            sourceCodeBuilder.AppendLine("               " +
                                $"$this->{attr.attribute_name} = {model.class_name}States::{state.state_name.Replace(" ", "").ToUpper()};");
                            sourceCodeBuilder.AppendLine("           }");
                        }
                    }
                    sourceCodeBuilder.AppendLine("      }");
                }

                if (state.state_event != null)
                {
                    var stateEventArray = state.state_event as JArray;
                    if (stateEventArray != null)
                    {
                        foreach (var item in stateEventArray)
                        {
                            string stateEvent = item.ToString();
                            if (!stateEvent.StartsWith("on", StringComparison.OrdinalIgnoreCase))
                            {
                                stateEventBuilder(stateEvent);
                            }
                        }
                    }
                    else if (state.state_event is string)
                    {
                        string stateEvent = state.state_event.ToString();
                        stateEventBuilder(stateEvent);
                    }
                }
            }
            sourceCodeBuilder.AppendLine("");
            sourceCodeBuilder.AppendLine($"     public function GetState()" + " {");
            sourceCodeBuilder.AppendLine($"       $this->{stateAttribute};");
            sourceCodeBuilder.AppendLine("      }");
        }

        private void GenerateClass(JsonData.Model model, JsonData json)
        {
            stateAttribute = null;
            sourceCodeBuilder.AppendLine($"class {model.class_name} {{");

            // Sort attributes alphabetically
            var sortedAttributes = model.attributes.OrderBy(attr => attr.attribute_name);

            foreach (var attribute in model.attributes)
            {
                GenerateAttribute(attribute, json);
            }

            sourceCodeBuilder.AppendLine("");

            if (model.attributes != null)
            {
                GenerateConstructor(model.attributes, model.class_name);
            }

            sourceCodeBuilder.AppendLine("");

            foreach (var attribute in model.attributes)
            {
                GenerateGetter(attribute);
            }

            sourceCodeBuilder.AppendLine("");

            foreach (var attribute in model.attributes)
            {
                GenerateSetter(attribute);
            }

            if (model.states != null)
            {
                sourceCodeBuilder.AppendLine("");
                GenerateStateAction(model);
            }

            sourceCodeBuilder.AppendLine("}\n");
        }

        private void GenerateAttribute(JsonData.Attribute1 attribute, JsonData json)
        {
            // Adjust data types as needed
            string dataType = MapDataType(attribute.data_type);
            if (attribute.data_type != "state" && attribute.data_type != "inst_event" && attribute.data_type != "inst_ref" && attribute.data_type != "inst_ref_set" && attribute.data_type != "inst_ref_<timer>" && attribute.data_type != "inst_event")
            {
                sourceCodeBuilder.AppendLine($"    private {dataType} ${attribute.attribute_name};");
            }
            else if (attribute.data_type == "state")
            {
                sourceCodeBuilder.AppendLine($"    private ${attribute.attribute_name};");
            }
            else if (attribute.data_type == "inst_ref_<timer>")
            {
                sourceCodeBuilder.AppendLine($"    private {dataType} ${attribute.attribute_name};");
            }
            else if (attribute.data_type == "inst_ref")
            {
                sourceCodeBuilder.AppendLine($"    private {attribute.related_class_name} ${attribute.attribute_name}Ref;");
            }
            else if (attribute.data_type == "inst_ref_set")
            {
                sourceCodeBuilder.AppendLine($"    private {attribute.related_class_name} ${attribute.attribute_name}RefSet;");
            }
            else if (attribute.data_type == "inst_event")
            {
                sourceCodeBuilder.AppendLine("");
                string cName = null;
                foreach (JsonData.Model modell in json.model)
                {
                    if (modell.class_id == attribute.class_id)
                    {
                        cName = modell.class_name;
                    }
                }
                sourceCodeBuilder.AppendLine("      " +
                    $"public function {attribute.event_name}({cName} ${cName})" + " {");
                sourceCodeBuilder.AppendLine("         " +
                    $"${cName}->status = {cName}States::{attribute.state_name};");
                sourceCodeBuilder.AppendLine("      " +
                    "}");
                sourceCodeBuilder.AppendLine("");
            }
            else
            {
                return;
            }

        }

        private void GenerateAssociationClass(JsonData.Model associationModel)
        {
            // Check if associationModel is not null
            if (associationModel == null)
            {
                // Handle the case where associationModel is null, e.g., throw an exception or log a message
                return;
            }

            sourceCodeBuilder.AppendLine($"class assoc_{associationModel.class_name} {{");

            foreach (var attribute in associationModel.attributes)
            {
                // Adjust data types as needed
                string dataType = MapDataType(attribute.data_type);

                sourceCodeBuilder.AppendLine($"     private {dataType} ${attribute.attribute_name};");
            }

            // Check if associatedClass.@class is not null before iterating
            if (associationModel.@class != null)
            {
                foreach (var associatedClass in associationModel.@class)
                {
                    if (associatedClass.class_multiplicity == "1..1")
                    {
                        sourceCodeBuilder.AppendLine($"    private {associatedClass.class_name} ${associatedClass.class_name};");
                    }
                    else
                    {
                        sourceCodeBuilder.AppendLine($"    private array ${associatedClass.class_name}List;");
                    }
                }
            }

            sourceCodeBuilder.AppendLine("");

            if (associationModel.attributes != null)
            {
                GenerateConstructor(associationModel.attributes, associationModel.class_name);
            }

            foreach (var attribute in associationModel.attributes)
            {
                GenerateGetter(attribute);
            }

            foreach (var attribute in associationModel.attributes)
            {
                GenerateSetter(attribute);
            }
            sourceCodeBuilder.AppendLine("}\n\n");
        }

        private void GenerateImportedClass(JsonData.Model imported, JsonData json)
        {
            stateAttribute = null;
            if (imported == null)
            {
                return;
            }
            sourceCodeBuilder.AppendLine($"class {imported.class_name} {{");

            foreach (var attribute in imported.attributes)
            {
                GenerateAttribute(attribute, json);
            }

            sourceCodeBuilder.AppendLine("");

            if (imported.attributes != null)
            {
                GenerateConstructor(imported.attributes, imported.class_name);
            }

            sourceCodeBuilder.AppendLine("");

            foreach (var attribute in imported.attributes)
            {
                GenerateGetter(attribute);
            }

            sourceCodeBuilder.AppendLine("");

            foreach (var attribute in imported.attributes)
            {
                GenerateSetter(attribute);
            }

            if (imported.states != null)
            {
                sourceCodeBuilder.AppendLine("");
                GenerateStateAction(imported);
            }
            sourceCodeBuilder.AppendLine("}\n\n");
        }

        private void GenerateConstructor(List<JsonData.Attribute1> attributes, string className)
        {
            sourceCodeBuilder.Append($"     public function __construct(");

            foreach (var attribute in attributes)
            {
                if (attribute.data_type != "state" && attribute.data_type != "inst_ref_<timer>" && attribute.data_type != "inst_ref" && attribute.data_type != "inst_ref_set" && attribute.data_type != "inst_event")
                {
                    sourceCodeBuilder.Append($"${attribute.attribute_name},");
                }
                else if (attribute.data_type == "inst_ref_<timer>")
                {
                    sourceCodeBuilder.Append($"TIMER ${attribute.attribute_name},");
                }
                else if (attribute.data_type == "inst_ref")
                {
                    sourceCodeBuilder.Append($"{attribute.related_class_name} ${attribute.attribute_name}Ref,");
                }
                else if (attribute.data_type == "inst_ref_set")
                {
                    sourceCodeBuilder.Append($"{attribute.related_class_name} ${attribute.attribute_name}RefSet,");
                }

            }

            // Remove the trailing comma and add the closing parenthesis
            if (attributes.Any())
            {
                sourceCodeBuilder.Length -= 1; // Remove the last character (",")
            }

            sourceCodeBuilder.AppendLine(") {");

            foreach (var attribute in attributes)
            {
                if (attribute.data_type != "state" && attribute.data_type != "inst_ref_<timer>" && attribute.data_type != "inst_ref" && attribute.data_type != "inst_ref_set" && attribute.data_type != "inst_event")
                {
                    sourceCodeBuilder.AppendLine($"        $this->{attribute.attribute_name} = ${attribute.attribute_name};");
                }
                else if (attribute.data_type == "inst_ref")
                {
                    sourceCodeBuilder.AppendLine($"        $this->{attribute.attribute_name}Ref = ${attribute.attribute_name}Ref;");
                }
                else if (attribute.data_type == "inst_ref_set")
                {
                    sourceCodeBuilder.AppendLine($"        $this->{attribute.attribute_name}RefSet = ${attribute.attribute_name}RefSet;");
                }
                else if (attribute.data_type == "inst_ref_<timer>")
                {
                    sourceCodeBuilder.AppendLine($"        $this->{attribute.attribute_name} = ${attribute.attribute_name};");
                }
                else if (attribute.default_value != null)
                {
                    stateAttribute = attribute.attribute_name;
                    string input = attribute.default_value;
                    int dot = input.IndexOf('.');
                    if (dot != -1)
                    {
                        string state = input.Substring(dot + 1);
                        sourceCodeBuilder.AppendLine("        " +
                            $"$this->{attribute.attribute_name}" + $" = {className}States::{state.ToUpper()}" + ";");
                    }
                    else
                    {
                        {
                            sourceCodeBuilder.AppendLine("        " +
                                $"$this->{attribute.attribute_name};");
                        }
                    }

                }
            }

            sourceCodeBuilder.AppendLine("     }");
        }

        private void GenerateGetter(JsonData.Attribute1 getter)
        {
            if (getter.data_type != "state" && getter.data_type != "inst_ref_<timer>" && getter.data_type != "inst_ref" && getter.data_type != "inst_ref_set" && getter.data_type != "inst_event")
            {
                sourceCodeBuilder.AppendLine($"      public function get{getter.attribute_name}() {{");
                sourceCodeBuilder.AppendLine($"        return $this->{getter.attribute_name};");
                sourceCodeBuilder.AppendLine($"      }}");
            }
            else if (getter.data_type == "inst_ref_<timer>")
            {
                sourceCodeBuilder.AppendLine($"      public function get{getter.attribute_name}() {{");
                sourceCodeBuilder.AppendLine($"        return $this->{getter.attribute_name};");
                sourceCodeBuilder.AppendLine($"      }}");
            }
            else if (getter.data_type == "inst_ref")
            {
                sourceCodeBuilder.AppendLine($"      public function get{getter.attribute_name}Ref() {{");
                sourceCodeBuilder.AppendLine($"        return $this->{getter.attribute_name}Ref;");
                sourceCodeBuilder.AppendLine($"      }}");
            }
            else if (getter.data_type == "inst_ref_set")
            {
                sourceCodeBuilder.AppendLine($"      public function get{getter.attribute_name}RefSet() {{");
                sourceCodeBuilder.AppendLine($"        return $this->{getter.attribute_name}RefSet;");
                sourceCodeBuilder.AppendLine($"      }}");
            }
            else if (getter.data_type == "inst_event")
            {
                return;
            }

        }

        private void GenerateSetter(JsonData.Attribute1 setter)
        {
            if (setter.data_type != "state" && setter.data_type != "inst_ref_<timer>" && setter.data_type != "inst_ref" && setter.data_type != "inst_ref_set" && setter.data_type != "inst_event")
            {
                sourceCodeBuilder.AppendLine($"      public function set{setter.attribute_name}(${setter.attribute_name}) {{");
                sourceCodeBuilder.AppendLine($"        $this->{setter.attribute_name} = ${setter.attribute_name};");
                sourceCodeBuilder.AppendLine($"      }}");
            }
            else if (setter.data_type == "inst_ref_<timer>")
            {
                sourceCodeBuilder.AppendLine($"      public function set{setter.attribute_name}(TIMER ${setter.attribute_name}) {{");
                sourceCodeBuilder.AppendLine($"        $this->{setter.attribute_name} = ${setter.attribute_name};");
                sourceCodeBuilder.AppendLine($"      }}");
            }
            else if (setter.data_type == "inst_ref")
            {
                sourceCodeBuilder.AppendLine($"      public function set{setter.attribute_name}Ref({setter.related_class_name} ${setter.attribute_name}Ref) {{");
                sourceCodeBuilder.AppendLine($"        $this->{setter.attribute_name}Ref = ${setter.attribute_name}Ref;");
                sourceCodeBuilder.AppendLine($"      }}");
            }
            else if (setter.data_type == "inst_ref_set")
            {
                sourceCodeBuilder.AppendLine($"      public function set{setter.attribute_name}RefSet({setter.related_class_name} ${setter.attribute_name}) {{");
                sourceCodeBuilder.AppendLine($"        $this->{setter.attribute_name}RefSet = ${setter.attribute_name};");
                sourceCodeBuilder.AppendLine($"      }}");
            }
            else if (setter.data_type == "inst_event")
            {
                return;
            }

        }

        private void GenerateGetState(JsonData.Attribute1 getstate)
        {
            if (getstate.data_type == "state")
            {
                sourceCodeBuilder.AppendLine($"     public function GetState() {{");
                sourceCodeBuilder.AppendLine($"       $this->{getstate.attribute_name};");
                sourceCodeBuilder.AppendLine($"}}\n");
            }

        }

        private string Target(JsonData.Transition target)
        {
            string targetState = target.target_state;
            return targetState;
        }
        private string StateStatus(JsonData.Attribute1 attributes)
        {
            if (attributes.data_type == "state")
            {
                status = attributes.attribute_name;
            }

            return status;
        }

        private void GenerateAssocClass()
        {
            sourceCodeBuilder.AppendLine($"class Association{{");
            sourceCodeBuilder.AppendLine($"     public function __construct($class1,$class2) {{");
            sourceCodeBuilder.AppendLine($"}}");
            sourceCodeBuilder.AppendLine($"}}");
            sourceCodeBuilder.AppendLine($"\n");
        }
        private void GenerateObjAssociation(JsonData.Model assoc)
        {
            sourceCodeBuilder.Append($"${assoc.name} = new Association(");

            foreach (var association in assoc.@class)
            {
                sourceCodeBuilder.Append($"\"{association.class_name}\",");
            }

            sourceCodeBuilder.Length -= 1; // Remove the last character (",")

            sourceCodeBuilder.AppendLine($");");
        }

        private string MapDataType(string dataType)
        {
            switch (dataType.ToLower())
            {
                case "integer":
                    return "int";
                case "id":
                    return "string";
                case "string":
                    return "string";
                case "bool":
                    return "bool";
                case "real":
                    return "float";
                case "inst_ref_<timer>":
                    return "TIMER";
                // Add more mappings as needed
                default:
                    return dataType; // For unknown types, just pass through
            }
        }
    }
}
