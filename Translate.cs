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
using static xtUML1.Translate.JsonData;
using System.Runtime.CompilerServices;

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
                        string targetState = null;
                        foreach (JsonData.State states in model.states)
                        {
                            if (states.state_id == transition.target_state_id)
                            {
                                targetState = states.state_event.ToString();
                            }
                        }
                        sourceCodeBuilder.AppendLine("                  " +
                            $"if ({stateAttribute} == {model.class_name}States.{transition.target_state.Replace(" ", "")})");
                        sourceCodeBuilder.AppendLine("                  {");
                        sourceCodeBuilder.AppendLine("                      " +
                            $"{targetState}();");
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
                                    $"if ($this->{attr.attribute_name} != {model.class_name}States::{state.state_name.Replace(" ", "").ToUpper()})");
                            sourceCodeBuilder.AppendLine("           {");
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
            sourceCodeBuilder.AppendLine("      }\n");
        }

        private void GenerateClass(JsonData.Model model, JsonData json)
        {
            stateAttribute = null;
            sourceCodeBuilder.AppendLine($"class {model.class_name} {{");

            // Sort attributes alphabetically
            var sortedAttributes = model.attributes.OrderBy(attr => attr.attribute_name);

            foreach (var attribute in model.attributes)
            {
                GenerateAttribute(attribute,json);
            }

            sourceCodeBuilder.AppendLine("");

            if (model.attributes != null)
            {
                GenerateConstructor(model.attributes);
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

            sourceCodeBuilder.AppendLine("");

            if (model.states != null)
            {
                GenerateStateAction(model);
            }

            sourceCodeBuilder.AppendLine("}\n\n");
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
                sourceCodeBuilder.AppendLine($"    private {attribute.related_class_name} ${attribute.attribute_name}RefSet = [];");
            }
            else if (attribute.data_type == "inst_event")
            {
                string cName = null;
                string sName = null;

                foreach (JsonData.Model modell in json.model)
                {
                    if (modell.class_id == attribute.class_id)
                    {
                        cName = modell.class_name;
                    }
                }

                // kemungkinan berubah
                foreach (JsonData.Model state in json.model)
                {
                    if (state.states != null)
                    {
                        foreach (var states in state.states)
                        {
                            if (states.state_id == attribute.state_id && states.state_name == attribute.state_name)
                            {
                                //sName = states.state_event[0];
                            }
                        }
                    }
                }

                sourceCodeBuilder.AppendLine($"\n    public function {attribute.event_name}({cName} ${cName}) {{");
                sourceCodeBuilder.AppendLine($"        ${cName}->{sName}();");
                sourceCodeBuilder.AppendLine($"}}");
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
                GenerateConstructor(associationModel.attributes);
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
                GenerateConstructor(imported.attributes);
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

            sourceCodeBuilder.AppendLine("");

            if (imported.states != null)
            {
               
            }
        }

        private void GenerateConstructor(List<JsonData.Attribute1> attributes)
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
                    sourceCodeBuilder.Append($"TIMER {attribute.attribute_name},");
                }
                else if (attribute.data_type == "inst_ref")
                {
                    sourceCodeBuilder.Append($"{attribute.related_class_name} {attribute.attribute_name}Ref,");
                }
                else if (attribute.data_type == "inst_ref_set")
                {
                    sourceCodeBuilder.Append($"{attribute.related_class_name} {attribute.attribute_name}RefSet,");
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
                    sourceCodeBuilder.AppendLine($"        $this->{attribute.attribute_name}RefSet[] = ${attribute.attribute_name}RefSet;");
                }
                else if (attribute.data_type == "inst_ref_<timer>")
                {
                    sourceCodeBuilder.AppendLine($"        $this->{attribute.attribute_name} = ${attribute.attribute_name}");
                }
            }

            // Handle the "state" datatype separately outside the loop
            var stateAttribute = attributes.FirstOrDefault(attr => attr.data_type == "state");
            if (stateAttribute != null)
            {
                // Check if the attribute has a default value and it is a string
                if (!string.IsNullOrEmpty(stateAttribute.default_value) && stateAttribute.data_type.ToLower() == "state")
                {
                    int lastDotIndex = stateAttribute.default_value.LastIndexOf('.');
                    // Replace "status" with "state" and "aktif" with "active"
                    string stringValue = stateAttribute.default_value.Substring(lastDotIndex + 1);
                    sourceCodeBuilder.AppendLine($"        $this->{stateAttribute.attribute_name} = \"{stringValue}\";");
                }
            }

            sourceCodeBuilder.AppendLine("}");
        }

        private void GenerateGetter(JsonData.Attribute1 getter)
        {
            if (getter.data_type != "state" && getter.data_type != "inst_ref_<timer>" && getter.data_type != "inst_ref" && getter.data_type != "inst_ref_set" && getter.data_type != "inst_event")
            {
                sourceCodeBuilder.AppendLine($"      public function get{getter.attribute_name}() {{");
                sourceCodeBuilder.AppendLine($"        return $this->{getter.attribute_name};");
                sourceCodeBuilder.AppendLine($"}}");
            }
            else if (getter.data_type == "inst_ref_<timer>")
            {
                sourceCodeBuilder.AppendLine($"      public function get{getter.attribute_name}() {{");
                sourceCodeBuilder.AppendLine($"        return $this->{getter.attribute_name};");
                sourceCodeBuilder.AppendLine($"}}");
            }
            else if (getter.data_type == "inst_ref")
            {
                sourceCodeBuilder.AppendLine($"      public function get{getter.attribute_name}Ref() {{");
                sourceCodeBuilder.AppendLine($"        return $this->{getter.attribute_name}Ref;");
                sourceCodeBuilder.AppendLine($"}}");
            }
            else if (getter.data_type == "inst_ref_set")
            {
                sourceCodeBuilder.AppendLine($"      public function get{getter.attribute_name}RefSet() {{");
                sourceCodeBuilder.AppendLine($"        return $this->{getter.attribute_name}RefSet;");
                sourceCodeBuilder.AppendLine($"}}");
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
                sourceCodeBuilder.AppendLine($"}}");
            }
            else if (setter.data_type == "inst_ref_<timer>")
            {
                sourceCodeBuilder.AppendLine($"      public function set{setter.attribute_name}(TIMER ${setter.attribute_name}) {{");
                sourceCodeBuilder.AppendLine($"        $this->{setter.attribute_name} = ${setter.attribute_name};");
                sourceCodeBuilder.AppendLine($"}}");
            }
            else if (setter.data_type == "inst_ref")
            {
                sourceCodeBuilder.AppendLine($"      public function set{setter.attribute_name}Ref({setter.related_class_name} ${setter.attribute_name}Ref) {{");
                sourceCodeBuilder.AppendLine($"        $this->{setter.attribute_name}Ref = ${setter.attribute_name}Ref;");
                sourceCodeBuilder.AppendLine($"}}");
            }
            else if (setter.data_type == "inst_ref_set")
            {
                sourceCodeBuilder.AppendLine($"      public function set{setter.attribute_name}RefSet({setter.related_class_name} ${setter.attribute_name}) {{");
                sourceCodeBuilder.AppendLine($"        $this->{setter.attribute_name}RefSet[] = ${setter.attribute_name};");
                sourceCodeBuilder.AppendLine($"}}");
            }
            else if (setter.data_type == "inst_event")
            {
                return;
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

        public class JsonData
        {
            public string type { get; set; }
            public string sub_id { get; set; }
            public string sub_name { get; set; }
            public List<Model> model { get; set; }
            public class Model
            {
                public string type { get; set; }
                public string class_id { get; set; }
                public string class_name { get; set; }
                public string KL { get; set; }
                public string name { get; set; }
                public List<Attribute1> attributes { get; set; }
                public List<State> states { get; set; }
                public Model model { get; set; }
                public List<Class1> @class { get; set; }
            }

            public class Attribute1
            {
                public string attribute_name { get; set; }
                public string data_type { get; set; }
                public string default_value { get; set; }
                public string attribute_type { get; set; }
                public string event_id { get; set; }
                public string event_name { get; set; }
                public string class_id { get; set; }
                public string state_id { get; set; }
                public string state_name { get; set; }
                public string related_class_id { get; set; }
                public string related_class_name { get; set; }
                public string related_class_KL { get; set; }
            }

            public class State
            {
                public string state_id { get; set; }
                public string state_name { get; set; }
                public string state_value { get; set; }
                public string state_type { get; set; }
                public object state_event { get; set; }
                public string[] state_function { get; set; }
                public string[] state_transition_id { get; set; }
                public List<Transition> transitions { get; set; }
                public string action { get; set;} 
            }
         
            public class Class1
            {
                public string class_name { get; set; }
                public string class_multiplicity { get; set; }
                public List<Attribute> attributes { get; set; }
                public List<Class1> @class { get; set; }
            }

            public class Attribute
            {
                public string attribute_name { get; set; }
                public string data_type { get; set; }
                public string attribute_type { get; set; }
            }

            public class Transition
            {
                public string target_state_id { get; set; }
                public string target_state { get; set; }
            }
        }
    }
}
