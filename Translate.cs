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
using static System.Windows.Forms.AxHost;
using System.Net.Configuration;
using System.Reflection;
using System.Diagnostics.Eventing.Reader;
using MindFusion.Graphs;
using System.Runtime.InteropServices;

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

                // Generate Generalized States
                GenerateStates(json);
                GenerateStateExtends(json);

                foreach (var model in json.model)
                {
                    if (model.type == "superclass")
                    {
                        GenerateSuperclass(model, json);
                    }
                    if (model.type == "class")
                    {
                        GenerateClass(model, json);
                    }
                    else if (model.type == "association" && model.model != null)
                    {
                        GenerateAssociationClass(model.model, json);
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
            var states = new HashSet<string>();
            sourceCodeBuilder.AppendLine("   " +
                    $"class baseState" + " {");
            foreach (JsonData.Model model in json.model)
            {
                if (model.states != null)
                {
                    foreach (JsonData.State state in model.states)
                    {
                        if (state.state_name != null)
                        {
                            string stateAdd = state.state_name.Replace(" ", "");
                            states.Add(stateAdd);
                        }
                    }
                }
                if (model.model != null && model.model.states != null)
                {
                    foreach (JsonData.State state in model.model.states)
                    {
                        string stateAdd = state.state_name.Replace(" ", "");
                        states.Add(stateAdd);
                    }
                }
            }
            foreach (var state in states)
            {
                sourceCodeBuilder.AppendLine("      " +
                    $"const {state.ToUpper()} = " + "'" + state + "'" + ";");
            }
            sourceCodeBuilder.AppendLine("   }");
            sourceCodeBuilder.AppendLine("");
        }

        private void GenerateStateExtends(JsonData json)
        {
            foreach (JsonData.Model model in json.model)
            {
                if (model.attributes != null)
                {
                    foreach (JsonData.Attribute1 attribute in model.attributes)
                    {
                        if (attribute.data_type == "state" && model.class_name != null)
                        {
                            sourceCodeBuilder.AppendLine($"   class {model.class_name}States extends baseState" + " {}");
                        }
                    }
                }
                if (model.model != null && model.model.attributes != null)
                {
                    foreach (JsonData.Attribute1 attribute in model.model.attributes)
                    {
                        if (attribute.data_type == "state" && model.model.class_name != null)
                        {
                            sourceCodeBuilder.AppendLine($"   class {model.model.class_name}States extends baseState" + " {}");
                        }
                    }
                }
            }
            sourceCodeBuilder.AppendLine("");
        }


        private void GenerateStateAction(JsonData.Model model)
        {
            bool attrNameExists = false;
            foreach (JsonData.Attribute1 attr in model.attributes)
            {
                if (attr.default_value != null)
                {
                    if (attr.attribute_name == null)
                    {
                        attrNameExists = false;
                    } else
                    {
                        attrNameExists = true;
                        stateAttribute = attr.attribute_name;
                    }
                }
            }
            if (attrNameExists == true)
            {
                sourceCodeBuilder.AppendLine("      " +
                            $"public function onStateAction()");
                sourceCodeBuilder.AppendLine("      {");
                sourceCodeBuilder.AppendLine("           " +
                    $"switch($this->{stateAttribute})" + " {");
                foreach (JsonData.State statess in model.states)
                {
                    if (statess.state_name != null)
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
                            if (attr.data_type == "state" && state.state_name != null)
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
            }
        }

        private void GenerateSuperclass (JsonData.Model model, JsonData json)
        {
            sourceCodeBuilder.AppendLine($"abstract class {model.superclass_name} {{");
            foreach (var supclassattr in model.attributes)
            {
                string dataType = MapDataType(supclassattr.data_type);
                if (supclassattr.data_type != "state" && supclassattr.data_type != "inst_event" && supclassattr.data_type != "inst_ref" && supclassattr.data_type != "inst_ref_set" && supclassattr.data_type != "inst_ref_<timer>" && supclassattr.data_type != "inst_event")
                {
                    sourceCodeBuilder.AppendLine($"      protected {dataType} ${supclassattr.attribute_name};");
                }
                else if (supclassattr.data_type == "state")
                {
                    sourceCodeBuilder.AppendLine($"      protected ${supclassattr.attribute_name};");
                }
                else if (supclassattr.data_type == "inst_ref_<timer>")
                {
                    sourceCodeBuilder.AppendLine($"      protected {dataType} ${supclassattr.attribute_name};");
                }
                else if (supclassattr.data_type == "inst_ref")
                {
                    sourceCodeBuilder.AppendLine($"      protected {supclassattr.related_class_name} ${supclassattr.attribute_name}Ref;");
                }
                else if (supclassattr.data_type == "inst_ref_set")
                {
                    sourceCodeBuilder.AppendLine($"      protected {supclassattr.related_class_name} ${supclassattr.attribute_name}RefSet;");
                }
                else if (supclassattr.data_type == "inst_event")
                {
                    sourceCodeBuilder.AppendLine("");
                    string cName = null;
                    foreach (JsonData.Model modell in json.model)
                    {
                        if (modell.class_id == supclassattr.class_id)
                        {
                            cName = modell.class_name;
                        }
                    }
                    sourceCodeBuilder.AppendLine("      " +
                        $"protected function {supclassattr.event_name}({cName} ${cName})" + " {");
                    sourceCodeBuilder.AppendLine("         " +
                        $"${cName}->status = {cName}States::{supclassattr.state_name.Replace(" ", "").ToUpper()};");
                    sourceCodeBuilder.AppendLine("      " +
                        "}");
                    sourceCodeBuilder.AppendLine("");
                }
            }
            sourceCodeBuilder.AppendLine("");
            foreach (var supclassattr in model.attributes)
            {
                sourceCodeBuilder.AppendLine($"      public function get{supclassattr.attribute_name}() {{");
                sourceCodeBuilder.AppendLine($"        return $this->{supclassattr.attribute_name};");
                sourceCodeBuilder.AppendLine("      }");
            }
            sourceCodeBuilder.AppendLine("");
            foreach (var supclassattr in model.attributes)
            {
                sourceCodeBuilder.AppendLine($"      public function set{supclassattr.attribute_name}(${supclassattr.attribute_name}) {{");
                sourceCodeBuilder.AppendLine($"        $this->{supclassattr.attribute_name} = ${supclassattr.attribute_name};");
                sourceCodeBuilder.AppendLine("      }");
            }
            sourceCodeBuilder.AppendLine("");
            foreach (var supclassattr in model.attributes)
            {
                if (supclassattr.data_type == "state")
                {
                    sourceCodeBuilder.AppendLine("      abstract public function onStateAction();");
                }
            }
            sourceCodeBuilder.AppendLine("}\n");
        }

        private void GenerateClass(JsonData.Model model, JsonData json)
        {
            stateAttribute = null;
            var classPairs = new List<(string SuperclassName, string SubclassName)>();
            foreach (JsonData.Model modell in json.model)
            {
                if (modell.subclasses != null)
                {
                    foreach (var subclass in modell.subclasses)
                    {
                        classPairs.Add((modell.superclass_name, subclass.class_name));
                    }
                }
            }
            var superclass = classPairs.FirstOrDefault(pair => pair.SubclassName == model.class_name).SuperclassName;

            if (superclass != null)
            {
                sourceCodeBuilder.AppendLine($"class {model.class_name} extends {superclass} {{");
            }
            else
            {
                sourceCodeBuilder.AppendLine($"class {model.class_name} {{");
            }

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
                GenerateGetter(attribute, json);
            }

            sourceCodeBuilder.AppendLine("");

            foreach (var attribute in model.attributes)
            {
                GenerateSetter(attribute, json);
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
            var superclassAttributes = new List<(string AttributeName, string DataType)>();

            foreach (JsonData.Model model in json.model)
            {
                if (model.type == "superclass")
                {
                    foreach (var supclassattr in model.attributes)
                    {
                        superclassAttributes.Add((supclassattr.attribute_name, supclassattr.data_type));
                    }
                }
            }

            var isInSuperclass = superclassAttributes.Any(attr => attr.AttributeName == attribute.attribute_name && attr.DataType == attribute.data_type);
            if (isInSuperclass)
            {
                return;
            }

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
                    $"${cName}->status = {cName}States::{attribute.state_name.Replace(" ", "").ToUpper()};");
                sourceCodeBuilder.AppendLine("      " +
                    "}");
                sourceCodeBuilder.AppendLine("");
            }
            else
            {
                return;
            }

        }

        private void GenerateAssociationClass(JsonData.Model associationModel, JsonData json)
        {
            // Check if associationModel is not null
            if (associationModel == null)
            {
                // Handle the case where associationModel is null, e.g., throw an exception or log a message
                return;
            }

            var classPairs = new List<(string SuperclassName, string SubclassName)>();
            foreach (JsonData.Model modell in json.model)
            {
                if (modell.subclasses != null)
                {
                    foreach (var subclass in modell.subclasses)
                    {
                        classPairs.Add((modell.superclass_name, subclass.class_name));
                    }
                }
            }
            var superclass = classPairs.FirstOrDefault(pair => pair.SubclassName == associationModel.class_name).SuperclassName;

            if (superclass != null)
            {
                sourceCodeBuilder.AppendLine($"class assoc_{associationModel.class_name} extends {superclass} {{");
            }
            else
            {
                sourceCodeBuilder.AppendLine($"class assoc_{associationModel.class_name} {{");
            }

            foreach (var attribute in associationModel.attributes)
            {
                GenerateAttribute(attribute, json);
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

            if (associationModel.states != null)
            {
                sourceCodeBuilder.AppendLine("");
                GenerateStateAction(associationModel);
            }

            foreach (var attribute in associationModel.attributes)
            {
                GenerateGetter(attribute, json);
            }

            foreach (var attribute in associationModel.attributes)
            {
                GenerateSetter(attribute, json);
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
                GenerateGetter(attribute, json);
            }

            sourceCodeBuilder.AppendLine("");

            foreach (var attribute in imported.attributes)
            {
                GenerateSetter(attribute, json);
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

        private void GenerateGetter(JsonData.Attribute1 getter, JsonData json)
        {
            var superclassAttributes = new List<(string AttributeName, string DataType)>();

            foreach (JsonData.Model model in json.model)
            {
                if (model.type == "superclass")
                {
                    foreach (var supclassattr in model.attributes)
                    {
                        superclassAttributes.Add((supclassattr.attribute_name, supclassattr.data_type));
                    }
                }
            }

            var isInSuperclass = superclassAttributes.Any(attr => attr.AttributeName == getter.attribute_name && attr.DataType == getter.data_type);
            if (isInSuperclass)
            {
                return;
            }

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

        private void GenerateSetter(JsonData.Attribute1 setter, JsonData json)
        {
            var superclassAttributes = new List<(string AttributeName, string DataType)>();

            foreach (JsonData.Model model in json.model)
            {
                if (model.type == "superclass")
                {
                    foreach (var supclassattr in model.attributes)
                    {
                        superclassAttributes.Add((supclassattr.attribute_name, supclassattr.data_type));
                    }
                }
            }

            var isInSuperclass = superclassAttributes.Any(attr => attr.AttributeName == setter.attribute_name && attr.DataType == setter.data_type);
            if (isInSuperclass)
            {
                return;
            }

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
