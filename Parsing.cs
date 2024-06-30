using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace xtUML1
{
    class Parsing
    {
        public static bool Point1(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                HashSet<string> subsystemNames = new HashSet<string>();

                foreach (var item in jsonArray)
                {
                    if (item["type"]?.ToString() == "subsystem")
                    {
                        var subsystemProperty = item["sub_name"];

                        if (subsystemProperty != null)
                        {
                            string subsystemName = subsystemProperty.ToString();

                            if (subsystemNames.Contains(subsystemName))
                            {
                                textBox4.AppendText($"Syntax error 1: There is a subsystem with the same name: {subsystemName}. Ensure that all subsystems have unique names.\r\n");
                            }

                            subsystemNames.Add(subsystemName);
                        }
                        else
                        {
                            textBox4.AppendText("Syntax error 1: Property 'sub_name' not found or is empty.\r\n");
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 1: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool Point2(Form1 form1, JArray jsonArray)
        {

            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                foreach (var subsystem in jsonArray)
                {
                    HashSet<string> classIdsInClass = new HashSet<string>();
                    HashSet<string> classIdsInAssociationClass = new HashSet<string>();

                    var modelArray = subsystem["model"] as JArray;

                    if (modelArray != null)
                    {
                        foreach (var item in modelArray)
                        {
                            var itemType = item["type"]?.ToString();

                            if (itemType == "class")
                            {
                                var classIdProperty = item["class_id"];
                                var attributesArray = item["attributes"] as JArray;

                                if (classIdProperty != null)
                                {
                                    string classId = classIdProperty.ToString();
                                    classIdsInClass.Add(classId);

                                    if (attributesArray == null || !attributesArray.Any())
                                    {
                                        textBox4.AppendText($"Syntax error 2: Class {classId} in subsystem {subsystem["sub_name"]?.ToString()} does not have attributes.\r\n");

                                    }
                                }
                            }
                            else if (itemType == "association")
                            {
                                var classArrayProperty = item["class"] as JArray;

                                if (classArrayProperty != null)
                                {
                                    foreach (var classItem in classArrayProperty)
                                    {
                                        var classIdProperty = classItem["class_id"];

                                        if (classIdProperty != null)
                                        {
                                            string classId = classIdProperty.ToString();
                                            classIdsInAssociationClass.Add(classId);
                                        }
                                    }
                                }

                                var associationClassModel = item["model"];
                                if (associationClassModel is JObject associationObject)
                                {
                                    var associationClassType = associationObject["type"]?.ToString();

                                    if (associationClassType == "association_class")
                                    {
                                        var classIdProperty = associationObject["class_id"];

                                        if (classIdProperty != null)
                                        {
                                            string classId = classIdProperty.ToString();
                                            classIdsInClass.Add(classId);
                                            classIdsInAssociationClass.Add(classId);

                                            var attributesArray = associationObject["attributes"] as JArray;
                                            if (attributesArray == null || !attributesArray.Any())
                                            {
                                                textBox4.AppendText($"Syntax error 2: Class {classId} in subsystem {subsystem["sub_name"]?.ToString()} does not have attributes.\r\n");

                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    var classesWithoutRelation = classIdsInClass.Except(classIdsInAssociationClass);

                    if (classesWithoutRelation.Any())
                    {
                        textBox4.AppendText($"Syntax error 2: There are classes without relationships in subsystem {subsystem["sub_name"]?.ToString()}. Class IDs without relationships: {string.Join(", ", classesWithoutRelation)}\r\n");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 2: " + ex.Message + "\r\n");
                return false;
            }
        }

        public static bool Point3(Form1 form1, JArray jsonArray)
        {

            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                Dictionary<string, HashSet<string>> classInfoMap = new Dictionary<string, HashSet<string>>();

                Func<JToken, bool> processItem = null;
                processItem = (item) =>
                {
                    var itemType = item["type"]?.ToString();

                    if (itemType == "class")
                    {
                        var className = item["class_name"]?.ToString();
                        var attributes = GetAttributesAsString(item["attributes"] as JArray);

                        var classInfo = $"{className}-{attributes}";

                        if (classInfoMap.ContainsKey(classInfo))
                        {
                            textBox4.AppendText($"Syntax error 3: Class {className} in subsystem {item["sub_name"]?.ToString()} has the same information as a class in another subsystem.\r\n");

                        }
                        else
                        {
                            classInfoMap.Add(classInfo, new HashSet<string>());
                        }
                    }
                    else if (itemType == "association")
                    {
                        var classArrayProperty = item["class"] as JArray;

                        if (classArrayProperty != null)
                        {
                            foreach (var classItem in classArrayProperty)
                            {
                                if (!processItem(classItem))
                                {
                                    return false;
                                }
                            }
                        }

                        var associationClassModel = item["model"];
                        if (associationClassModel is JObject associationObject)
                        {
                            if (!processItem(associationObject))
                            {
                                return false;
                            }
                        }
                    }
                    else if (itemType == "association_class")
                    {
                        var className = item["class_name"]?.ToString();
                        var attributes = GetAttributesAsString(item["attributes"] as JArray);

                        var classInfo = $"{className}-{attributes}";

                        if (classInfoMap.ContainsKey(classInfo))
                        {
                            textBox4.AppendText($"Syntax error 3: Class {className} in subsystem {item["sub_name"]?.ToString()} has identical information to a class in another subsystem.\r\n");

                        }
                        else
                        {
                            classInfoMap.Add(classInfo, new HashSet<string>());
                        }
                    }

                    return true;
                };

                foreach (var subsystem in jsonArray)
                {
                    if (subsystem["model"] == null)
                    {
                        textBox4.AppendText("Syntax error point 3: Model not found\r\n");

                        return false;
                    }
                    foreach (var item in subsystem["model"])
                    {
                        if (!processItem(item))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error: " + ex.Message + "\r\n");
                return false;
            }
        }

        private static string GetAttributesAsString(JArray attributes)
        {
            if (attributes == null)
            {
                return string.Empty;
            }

            List<string> attributeStrings = new List<string>();

            foreach (var attribute in attributes)
            {
                var attributeType = attribute["attribute_type"]?.ToString();
                var attributeName = attribute["attribute_name"]?.ToString();
                var dataType = attribute["data_type"]?.ToString();

                attributeStrings.Add($"{attributeType}-{attributeName}-{dataType}");
            }

            return string.Join("|", attributeStrings);
        }

        //public static bool Point4(Form1 form1, JArray jsonArray)
        //{
        //    RichTextBox textBox4 = form1.GetMessageBox();
        //    try
        //    {
        //        foreach (var subsystem in jsonArray)
        //        {
        //            HashSet<string> classNames = new HashSet<string>();
        //            HashSet<string> classIds = new HashSet<string>();

        //            foreach (var item in subsystem["model"])
        //            {
        //                var itemType = item["type"]?.ToString();

        //                if ((itemType == "class" || itemType == "association_class") && item["class_name"] != null)
        //                {
        //                    var className = item["class_name"]?.ToString();
        //                    var classId = item["class_id"]?.ToString();

        //                    if (string.IsNullOrWhiteSpace(className) || string.IsNullOrWhiteSpace(classId))
        //                    {
        //                        textBox4.AppendText("Syntax error 4: Class name or class_id is empty in the subsystem. \r\n");

        //                    }

        //                    if (classNames.Contains(className))
        //                    {
        //                        textBox4.AppendText($"Syntax error 4: Duplicate class name {className} within this subsystem. \r\n");

        //                    }

        //                    if (classIds.Contains(classId))
        //                    {
        //                        textBox4.AppendText($"Syntax error 4: Duplicate class_id {classId} within this subsystem. \r\n");

        //                    }

        //                    classNames.Add(className);
        //                    classIds.Add(classId);
        //                }

        //                if (itemType == "association" && item["model"] is JObject associationModel)
        //                {
        //                    var associationItemType = associationModel["type"]?.ToString();

        //                    if (associationItemType == "association_class" && associationModel["class_name"] != null)
        //                    {
        //                        var associationClassName = associationModel["class_name"]?.ToString();
        //                        var associationClassId = associationModel["class_id"]?.ToString();

        //                        if (string.IsNullOrWhiteSpace(associationClassName) || string.IsNullOrWhiteSpace(associationClassId))
        //                        {
        //                            textBox4.AppendText("Syntax error 4: Class name or class_id is empty in the subsystem. \r\n");

        //                        }

        //                        if (classNames.Contains(associationClassName))
        //                        {
        //                            textBox4.AppendText($"Syntax error 4: Duplicate class name {associationClassName} within this subsystem. \r\n");

        //                        }

        //                        if (classIds.Contains(associationClassId))
        //                        {
        //                            textBox4.AppendText($"Syntax error 4: Duplicate class_id {associationClassId} within this subsystem. \r\n");

        //                        }

        //                        classNames.Add(associationClassName);
        //                        classIds.Add(associationClassId);
        //                    }
        //                }
        //            }
        //        }

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        textBox4.AppendText("Syntax error 4: " + ex.Message + "\r\n");
        //        return false;
        //    }
        //}

        public static bool Point4(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                foreach (var subsystem in jsonArray)
                {
                    HashSet<string> classNames = new HashSet<string>();
                    HashSet<string> classIds = new HashSet<string>();

                    foreach (var item in subsystem["model"])
                    {
                        var itemType = item["type"]?.ToString();

                        if ((itemType == "class" || itemType == "association_class") && item["class_name"] != null)
                        {
                            var className = item["class_name"]?.ToString();
                            var classId = item["class_id"]?.ToString();

                            if (string.IsNullOrWhiteSpace(className) || string.IsNullOrWhiteSpace(classId))
                            {
                                textBox4.AppendText("Syntax error 4: Class name or class_id is empty in the subsystem. \r\n");

                            }

                            if (classNames.Contains(className))
                            {
                                textBox4.AppendText($"Syntax error 4: Duplicate class name {className} within this subsystem. \r\n");

                            }

                            if (classIds.Contains(classId))
                            {
                                textBox4.AppendText($"Syntax error 4: Duplicate class_id {classId} within this subsystem. \r\n");

                            }

                            classNames.Add(className);
                            classIds.Add(classId);
                        }

                        if (itemType == "association" && item["model"] is JObject associationModel)
                        {
                            var associationItemType = associationModel["type"]?.ToString();

                            if (associationItemType == "association_class" && associationModel["class_name"] != null)
                            {
                                var associationClassName = associationModel["class_name"]?.ToString();
                                var associationClassId = associationModel["class_id"]?.ToString();

                                if (string.IsNullOrWhiteSpace(associationClassName) || string.IsNullOrWhiteSpace(associationClassId))
                                {
                                    textBox4.AppendText("Syntax error 4: Class name or class_id is empty in the subsystem. \r\n");

                                }

                                if (classNames.Contains(associationClassName))
                                {
                                    textBox4.AppendText($"Syntax error 4: Duplicate class name {associationClassName} within this subsystem. \r\n");

                                }

                                if (classIds.Contains(associationClassId))
                                {
                                    textBox4.AppendText($"Syntax error 4: Duplicate class_id {associationClassId} within this subsystem. \r\n");

                                }

                                classNames.Add(associationClassName);
                                classIds.Add(associationClassId);
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 4: " + ex.Message + "\r\n");
                return false;
            }
        }

        //public static bool Point5(Form1 form1, JArray jsonArray)
        //{
        //    RichTextBox textBox4 = form1.GetMessageBox();
        //    try
        //    {
        //        foreach (var subsystem in jsonArray)
        //        {
        //            HashSet<string> uniqueKLs = new HashSet<string>();
        //            foreach (var item in subsystem["model"])
        //            {
        //                var itemType = item["type"]?.ToString();

        //                if (itemType == "class" && item["class_name"] != null)
        //                {
        //                    var className = item["class_name"]?.ToString();
        //                    var KL = item["KL"]?.ToString();

        //                    if (string.IsNullOrWhiteSpace(className) || string.IsNullOrWhiteSpace(KL))
        //                    {
        //                        textBox4.AppendText("Syntax error 5: Class name or KL is empty in the subsystem. \r\n");
        //                    }

        //                    if (uniqueKLs.Contains(KL))
        //                    {
        //                        textBox4.AppendText($"Syntax error 5: Duplicate KL value {KL} within this subsystem. \r\n");
        //                    }

        //                    uniqueKLs.Add(KL);
        //                }

        //                if (itemType == "association" && item["model"] is JObject associationModel)
        //                {
        //                    var associationItemType = associationModel["type"]?.ToString();

        //                    if (associationItemType == "association_class" && associationModel["class_name"] != null)
        //                    {
        //                        var associationKL = associationModel["KL"]?.ToString();

        //                        if (string.IsNullOrWhiteSpace(associationKL))
        //                        {
        //                            textBox4.AppendText("Syntax error 5: KL value is empty in the subsystem. \r\n");
        //                        }

        //                        if (uniqueKLs.Contains(associationKL))
        //                        {
        //                            textBox4.AppendText($"Syntax error 5: Duplicate KL value {associationKL} within this subsystem. \r\n");

        //                        }

        //                        uniqueKLs.Add(associationKL);
        //                    }
        //                }
        //            }
        //        }

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        textBox4.AppendText("Syntax error 5: " + ex.Message + "\r\n");
        //        return false;
        //    }
        //}

        public static bool Point5(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                foreach (var subsystem in jsonArray)
                {
                    HashSet<string> uniqueKLs = new HashSet<string>();
                    foreach (var item in subsystem["model"])
                    {
                        var itemType = item["type"]?.ToString();

                        if (itemType == "class" && item["class_name"] != null)
                        {
                            var className = item["class_name"]?.ToString();
                            var KL = item["KL"]?.ToString();

                            if (string.IsNullOrWhiteSpace(className) || string.IsNullOrWhiteSpace(KL))
                            {
                                textBox4.AppendText("Syntax error 5: Class name or KL is empty in the subsystem. \r\n");
                            }

                            if (uniqueKLs.Contains(KL))
                            {
                                textBox4.AppendText($"Syntax error 5: Duplicate KL value {KL} within this subsystem. \r\n");
                            }

                            uniqueKLs.Add(KL);
                        }

                        if (itemType == "association" && item["model"] is JObject associationModel)
                        {
                            var associationItemType = associationModel["type"]?.ToString();

                            if (associationItemType == "association_class" && associationModel["class_name"] != null)
                            {
                                var associationKL = associationModel["KL"]?.ToString();

                                if (string.IsNullOrWhiteSpace(associationKL))
                                {
                                    textBox4.AppendText("Syntax error 5: KL value is empty in the subsystem. \r\n");
                                }

                                if (uniqueKLs.Contains(associationKL))
                                {
                                    textBox4.AppendText($"Syntax error 5: Duplicate KL value {associationKL} within this subsystem. \r\n");

                                }

                                uniqueKLs.Add(associationKL);
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 5: " + ex.Message + "\r\n");
                return false;
            }
        }

        public static bool Point6(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                foreach (var subsystem in jsonArray)
                {

                    foreach (var item in subsystem["model"])
                    {
                        var itemType = item["type"]?.ToString();

                        if (itemType == "class" && item["class_name"] != null)
                        {
                            var className = item["class_name"]?.ToString();
                            var attributes = item["attributes"] as JArray;

                            if (attributes != null)
                            {
                                HashSet<string> uniqueAttributeNames = new HashSet<string>();

                                foreach (var attribute in attributes)
                                {
                                    var attributeName = attribute["attribute_name"]?.ToString();

                                    if (string.IsNullOrWhiteSpace(attributeName))
                                    {
                                        textBox4.AppendText($"Syntax error 6: Attribute name is empty in class {className}. \r\n");

                                    }

                                    if (uniqueAttributeNames.Contains(attributeName))
                                    {
                                        textBox4.AppendText($"Syntax error 6: Duplicate attribute name {attributeName} in class {className}. \r\n");

                                    }

                                    uniqueAttributeNames.Add(attributeName);
                                }
                            }
                        }

                        if (itemType == "association" && item["model"] is JObject associationModel)
                        {
                            var associationItemType = associationModel["type"]?.ToString();

                            if (associationItemType == "association_class" && associationModel["class_name"] != null)
                            {
                                var associationClassName = associationModel["class_name"]?.ToString();
                                var associationAttributes = associationModel["attributes"] as JArray;

                                if (associationAttributes != null)
                                {
                                    HashSet<string> uniqueAssociationAttributeNames = new HashSet<string>();

                                    foreach (var attribute in associationAttributes)
                                    {
                                        var attributeName = attribute["attribute_name"]?.ToString();

                                        if (string.IsNullOrWhiteSpace(attributeName))
                                        {
                                            textBox4.AppendText($"Syntax error 6: Attribute name is empty in association class {associationClassName}. \r\n");

                                        }

                                        if (uniqueAssociationAttributeNames.Contains(attributeName))
                                        {
                                            textBox4.AppendText($"Syntax error 6: Duplicate attribute name {attributeName} in association class {associationClassName}. \r\n");

                                        }

                                        uniqueAssociationAttributeNames.Add(attributeName);
                                    }
                                }
                            }
                        }

                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 6: " + ex.Message + "\r\n");
                return false;
            }
        }

        public static bool Point7(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                foreach (var subsystem in jsonArray)
                {
                    foreach (var item in subsystem["model"])
                    {
                        var itemType = item["type"]?.ToString();

                        if (itemType == "class" && item["class_name"] != null)
                        {
                            var className = item["class_name"]?.ToString();
                            var attributes = item["attributes"] as JArray;

                            if (attributes != null)
                            {
                                bool hasPrimaryKey = false;

                                foreach (var attribute in attributes)
                                {
                                    var attributeType = attribute["attribute_type"]?.ToString();

                                    if (attributeType == "naming_attribute")
                                    {
                                        hasPrimaryKey = true;
                                        break;
                                    }
                                }

                                if (!hasPrimaryKey)
                                {
                                    textBox4.AppendText($"Syntax error 7: Class {className} does not have a primary key. \r\n");

                                }
                            }
                            else
                            {
                                textBox4.AppendText($"Syntax error 7: Class {className} does not have any attributes. \r\n");

                            }
                        }

                        if (itemType == "association" && item["model"] is JObject associationModel)
                        {
                            var associationItemType = associationModel["type"]?.ToString();

                            if (associationItemType == "association_class" && associationModel["class_name"] != null)
                            {
                                var associationClassName = associationModel["class_name"]?.ToString();
                                var associationAttributes = associationModel["attributes"] as JArray;

                                if (associationAttributes != null)
                                {
                                    bool hasPrimaryKey = false;

                                    foreach (var attribute in associationAttributes)
                                    {
                                        var attributeType = attribute["attribute_type"]?.ToString();

                                        if (attributeType == "naming_attribute")
                                        {
                                            hasPrimaryKey = true;
                                            break;
                                        }
                                    }

                                    if (!hasPrimaryKey)
                                    {
                                        textBox4.AppendText($"Syntax error 7: Association Class {associationClassName} does not have a primary key. \r\n");

                                    }
                                }
                                else
                                {
                                    textBox4.AppendText($"Syntax error 7: Association Class {associationClassName} does not have any attributes. \r\n");

                                }
                            }
                        }

                    }
                }


                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 7: " + ex.Message + "\r\n");
                return false;
            }
        }

        public static bool Point8(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                foreach (var subsystem in jsonArray)
                {
                    HashSet<string> associationNames = new HashSet<string>();

                    foreach (var item in subsystem["model"])
                    {
                        var itemType = item["type"]?.ToString();

                        if (itemType == "association" && item["name"] != null)
                        {
                            var associationName = item["name"]?.ToString();

                            if (string.IsNullOrWhiteSpace(associationName))
                            {
                                textBox4.AppendText("Syntax error 8: Association name is empty in the subsystem. \r\n");

                            }

                            if (associationNames.Contains(associationName))
                            {
                                textBox4.AppendText($"Syntax error 8: Duplicate association name {associationName} within this subsystem.\r\n");

                            }

                            associationNames.Add(associationName);
                        }
                    }
                }


                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 8: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool Point9(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                foreach (var subsystem in jsonArray)
                {
                    foreach (var item in subsystem["model"])
                    {
                        var itemType = item["type"]?.ToString();

                        if (itemType == "association")
                        {
                            var class1Multiplicity = item["class"][0]["class_multiplicity"]?.ToString();
                            var class2Multiplicity = item["class"][1]["class_multiplicity"]?.ToString();

                            if ((class1Multiplicity == "0..*" && class2Multiplicity == "0..*") || (class1Multiplicity == "0..*" && class2Multiplicity == "1..*") || (class1Multiplicity == "1..*" && class2Multiplicity == "0..*") || (class1Multiplicity == "1..*" && class2Multiplicity == "1..*"))
                            {
                                var associationModel = item["model"];
                                if (associationModel == null)
                                {
                                    textBox4.AppendText($"Syntax error 9: Relationship {item["name"]?.ToString()} (many-to-many) has not been formalized with an association_class. \r\n");
                                }

                                if (associationModel != null && associationModel["type"]?.ToString() != "association_class")
                                {
                                    textBox4.AppendText($"Syntax error 9: Relationship {item["name"]?.ToString()} (many-to-many) has not been formalized with an association_class. \r\n");
                                }
                            }
                            else if ((class1Multiplicity == "0..*" && class2Multiplicity == "1..1") ||
                                     (class1Multiplicity == "1..1" && class2Multiplicity == "0..*") ||
                                     (class1Multiplicity == "1..*" && class2Multiplicity == "1..1") ||
                                     (class1Multiplicity == "1..1" && class2Multiplicity == "1..*") ||
                                     (class1Multiplicity == "1..1" && class2Multiplicity == "1..1"))
                            {
                                var class1Id = item["class"][0]["class_id"]?.ToString();
                                var class2Id = item["class"][1]["class_id"]?.ToString();

                                if (!HasReferentialAttribute(jsonArray, class1Id) && !HasReferentialAttribute(jsonArray, class2Id))
                                {
                                    textBox4.AppendText($"Syntax error 9: One of the Class {class1Id} or {class2Id} in relationship {item["name"]?.ToString()} (one-to-one) must be formalized with a referential_attribute. \r\n");

                                }
                            }
                        }
                    }
                }


                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 9: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool HasReferentialAttribute(JArray jsonArray, string classId)
        {
            foreach (var subsystem in jsonArray)

            {
                foreach (var item in subsystem["model"])
                {
                    var itemType = item["type"]?.ToString();

                    if (itemType == "class")
                    {
                        var currentClassId = item["class_id"]?.ToString();

                        if (currentClassId == classId)
                        {
                            var attributes = item["attributes"] as JArray;

                            if (attributes != null)
                            {
                                foreach (var attribute in attributes)
                                {
                                    var attributeType = attribute["attribute_type"]?.ToString();

                                    if (attributeType == "referential_attribute")
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }


        public static bool Point10(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();

            try
            {
                // Check for null values and empty array
                if (jsonArray == null || jsonArray.Count == 0)
                {
                    textBox4.AppendText("Syntax error 10: JSON array is null or empty.\r\n");
                    return false;
                }

                // Mengumpulkan semua nama referential attribute dari type class dan type association_class
                HashSet<string> referentialAttributeNames = new HashSet<string>();

                foreach (var subsystem in jsonArray)
                {
                    // Check for null values
                    if (subsystem == null || subsystem["model"] == null)
                        continue;

                    foreach (var item in subsystem["model"])
                    {
                        // Check for null values
                        if (item == null || item["type"] == null || item["attributes"] == null)
                            continue;

                        if (item["type"].ToString() == "class")
                        {
                            JArray classAttributes = (JArray)item["attributes"];
                            foreach (JObject attribute in classAttributes)
                            {
                                // Check for null values
                                if (attribute == null || attribute["attribute_type"] == null || attribute["attribute_name"] == null)
                                    continue;

                                if (attribute["attribute_type"].ToString() == "referential_attribute")
                                {
                                    string attributeName = attribute["attribute_name"].ToString();
                                    referentialAttributeNames.Add(attributeName);
                                }
                            }
                        }

                        if (item["type"].ToString() == "association" && item["model"] is JObject associationModel)
                        {
                            // Check for null values
                            if (associationModel["type"] == null || associationModel["class_name"] == null || associationModel["attributes"] == null)
                                continue;

                            var associationItemType = associationModel["type"].ToString();

                            if (associationItemType == "association_class")
                            {
                                JArray associationClassAttributes = (JArray)associationModel["attributes"];

                                foreach (JObject attribute in associationClassAttributes)
                                {
                                    // Check for null values
                                    if (attribute == null || attribute["attribute_type"] == null || attribute["attribute_name"] == null)
                                        continue;

                                    if (attribute["attribute_type"].ToString() == "referential_attribute")
                                    {
                                        string attributeName = attribute["attribute_name"].ToString();
                                        referentialAttributeNames.Add(attributeName);
                                    }
                                }
                            }
                        }
                    }
                }

                // Iterasi setiap referential attribute dan periksa penamaannya
                foreach (string attributeName in referentialAttributeNames)
                {
                    // Ambil bagian sebelum _id
                    string[] parts = attributeName.Split('_');

                    // Check for null values
                    if (parts == null || parts.Length < 2)
                    {
                        textBox4.AppendText($"Syntax error 10: Referential attribute '{attributeName}' has incorrect naming.\r\n");
                        return false;
                    }

                    string referenceName = string.Join("_", parts.Take(parts.Length - 1));
                    string lastPart = parts.LastOrDefault(); // Ambil bagian terakhir

                    // Periksa apakah ada kelas dengan KL yang mengandung referenceName
                    bool isValid = IsReferenceNameValid(jsonArray, referenceName, out string relationshipLabel);

                    if (!isValid || (lastPart != "id"))
                    {
                        textBox4.AppendText($"Syntax error 10: Referential attribute '{attributeName}' has incorrect naming.\r\n");
                        return false;
                    }

                    // Tambahkan label hubungan ke nama atribut
                    string newAttributeName = $"{referenceName}_{relationshipLabel}_id";
                    textBox4.AppendText($"Referential attribute '{attributeName}' should be renamed to '{newAttributeName}'.\r\n");
                }

                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 10: " + ex.Message + "\r\n");
                return false;
            }
        }

        public static bool IsReferenceNameValid(JArray jsonArray, string referenceName, out string relationshipLabel)
        {
            relationshipLabel = null;

            // Iterasi setiap kelas dan association_class dan periksa KL-nya
            foreach (var subsystem in jsonArray)
            {
                foreach (var item in subsystem["model"])
                {
                    if (item["type"].ToString() == "class")
                    {
                        string klValue = item["KL"]?.ToString();
                        if (!string.IsNullOrEmpty(klValue) && klValue == referenceName)
                        {
                            relationshipLabel = "class"; // label hubungan untuk kelas
                            return true;
                        }
                    }

                    if (item["type"].ToString() == "association" && item["model"] is JObject associationModel)
                    {
                        var associationItemType = associationModel["type"]?.ToString();

                        if (associationItemType == "association_class")
                        {
                            string klValue = associationModel["KL"]?.ToString();
                            if (!string.IsNullOrEmpty(klValue) && klValue == referenceName)
                            {
                                relationshipLabel = "association_class"; // label hubungan untuk association class
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }
        public static bool Point11(Form1 form1, JArray subsystems)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                foreach (var subsystem in subsystems)
                {
                    var subsystemId = subsystem["sub_id"]?.ToString();

                    foreach (var item in subsystem["model"])
                    {
                        var itemType = item["type"]?.ToString();

                        if (itemType == "association")
                        {
                            var class1Id = item["class"][0]["class_id"]?.ToString();
                            var class2Id = item["class"][1]["class_id"]?.ToString();

                            if (!IsClassInSubsystem(subsystem, class1Id))
                            {
                                if (!TryFindImportedClassInOtherSubsystem(subsystem, class1Id))
                                {
                                    textBox4.AppendText($"Syntax error 11: Subsystem {subsystemId} does not have a corresponding class or imported class for relationship {item["name"]?.ToString()}. \r\n");

                                }
                            }

                            if (!IsClassInSubsystem(subsystem, class2Id))
                            {
                                if (!TryFindImportedClassInOtherSubsystem(subsystem, class2Id))
                                {
                                    textBox4.AppendText($"Syntax error 11: Subsystem {subsystemId} does not have a class or imported class corresponding to the relationship {item["name"]?.ToString()}. \r\n");

                                }
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 11: " + ex.Message + "\r\n");
                return false;
            }
        }

        private static bool IsClassInSubsystem(JToken subsystem, string classId)
        {
            foreach (var item in subsystem["model"])
            {
                var itemType = item["type"]?.ToString();

                if (itemType == "class")
                {
                    var currentClassId = item["class_id"]?.ToString();

                    if (currentClassId == classId)
                    {
                        return true;
                    }
                }

                if (itemType == "association" && item["model"] is JObject associationModel)
                {
                    var associationItemType = associationModel["type"]?.ToString();

                    if (associationItemType == "association_class")
                    {
                        var classIdCurrent = associationModel["class_id"]?.ToString();

                        if (classIdCurrent == classId)
                        {
                            return true;
                        }

                    }
                }
            }

            return false;
        }

        private static bool TryFindImportedClassInOtherSubsystem(JToken subsystem, string classId)
        {
            foreach (var item in subsystem["model"])
            {
                var itemType = item["type"]?.ToString();

                if (itemType == "imported_class")
                {
                    var currentClassId = item["class_id"]?.ToString();

                    if (currentClassId == classId)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool Point13(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                foreach (var subsistem in jsonArray)
                {
                    if (subsistem?["type"]?.ToString() == "subsystem")
                    {
                        var model = subsistem["model"];

                        foreach (var item in model)
                        {
                            if (item?["type"]?.ToString() == "class")
                            {
                                if (!CekKelas(item))
                                {
                                    textBox4.AppendText($"Syntax error 13: Class or class attribute {item["class_name"]} is incomplete. \r\n");
                                }
                            }
                            else if (item?["type"]?.ToString() == "association")
                            {
                                if (!CekRelasi(item))
                                {
                                    textBox4.AppendText($"Syntax error 13: Association class or relationship {item["name"]} is incomplete. \r\n");
                                }
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 13: " + ex.Message + "\r\n");
                return false;
            }
        }

        private static bool CekKelas(JToken kelas)
        {
            if (kelas?["class_name"] == null || kelas?["class_id"] == null || kelas?["KL"] == null)
            {
                return false;
            }

            var attributes = kelas?["attributes"];

            if (attributes != null)
            {
                var attributeNames = new HashSet<string>();

                foreach (var attribute in attributes)
                {
                    if (attribute?["attribute_type"] != null)
                    {
                        if (attribute?["attribute_name"] == null || attribute?["data_type"] == null)
                        {
                            return false;

                        }

                        if (attributeNames.Contains(attribute["attribute_name"].ToString()))
                        {
                            return false;
                        }

                        attributeNames.Add(attribute["attribute_name"].ToString());
                    }
                }
            }

            return true;
        }

        private static bool CekRelasi(JToken relasi)
        {
            if (relasi?["name"] == null || relasi?["class"] == null)
            {
                return false;
            }

            var classes = relasi?["class"];

            foreach (var kelas in classes)
            {
                if (kelas?["class_multiplicity"] == null)
                {
                    return false;
                }
            }

            if (relasi?["model"] != null && relasi?["model"]["type"]?.ToString() == "association_class")
            {
                if (!CekKelas(relasi?["model"]))
                {
                    return false;
                }
            }

            return true;
        }


        public static bool Point14(Form1 form1, JArray subsystems)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                foreach (var currentSubsystem in subsystems)
                {
                    var currentSubId = currentSubsystem["sub_id"]?.ToString();

                    foreach (var item in currentSubsystem["model"])
                    {
                        var itemType = item["type"]?.ToString();

                        if (itemType == "association")
                        {
                            var class1Id = item["class"][0]["class_id"]?.ToString();
                            var class2Id = item["class"][1]["class_id"]?.ToString();

                            if (!IsClassInSubsystem(currentSubsystem, class1Id))
                            {
                                if (!TryFindImportedClassInOtherSubsystem(currentSubsystem, class1Id))
                                {
                                    textBox4.AppendText($"Syntax error 14: Subsystem {currentSubsystem["sub_name"]} does not have a corresponding class or imported class for the relationship {item["name"]?.ToString()}.\r\n");

                                }

                                if (!IsRelationshipInOtherSubsystem(subsystems, currentSubsystem, class1Id, class2Id))
                                {
                                    textBox4.AppendText($"Syntax error 14: Subsystem {currentSubId} has a relationship with class_id {class1Id} or {class2Id}, but there is no corresponding relationship in other subsystems. \r\n");

                                }
                            }

                            if (!IsClassInSubsystem(currentSubsystem, class2Id))
                            {
                                if (!TryFindImportedClassInOtherSubsystem(currentSubsystem, class2Id))
                                {
                                    textBox4.AppendText($"Syntax error 14: Subsystem {currentSubsystem["sub_name"]} does not have a corresponding class or imported class for the relationship {item["name"]?.ToString()}. \r\n");

                                }

                                if (!IsRelationshipInOtherSubsystem(subsystems, currentSubsystem, class1Id, class2Id))
                                {
                                    textBox4.AppendText($"Syntax error 14: Subsystem {currentSubId} has a relationship with class_id {class1Id} or {class2Id}, but there is no corresponding relationship in other subsystems. \r\n");

                                }
                            }

                        }
                    }
                }


                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 14: " + ex.Message + "\r\n");
                return false;
            }
        }

        private static bool IsRelationshipInOtherSubsystem(JArray subsystems, JToken currentSubsystem, string class1Id, string class2Id)
        {
            var currentSubId = currentSubsystem["sub_id"]?.ToString();

            foreach (var otherSubsystem in subsystems)
            {
                if (otherSubsystem != currentSubsystem)
                {
                    foreach (var item in otherSubsystem["model"])
                    {
                        var itemType = item["type"]?.ToString();

                        if (itemType == "association" && item != null)
                        {
                            var otherClass1Id = item["class"][0]["class_id"]?.ToString();
                            var otherClass2Id = item["class"][1]["class_id"]?.ToString();

                            // Cek apakah relasi dengan class1Id dan class2Id ditemukan di subsistem lain
                            if ((otherClass1Id == class1Id && otherClass2Id == class2Id) ||
                                (otherClass2Id == class1Id && otherClass1Id == class2Id))
                            {
                                return true;
                            }
                        }
                    }

                }
            }

            return false;
        }

        public static bool Point15(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                foreach (var subsystem in jsonArray)
                {
                    HashSet<string> classNames = new HashSet<string>();
                    HashSet<string> classIds = new HashSet<string>();

                    foreach (var item in subsystem["model"])
                    {
                        var itemType = item["type"]?.ToString();

                        if ((itemType == "class" || itemType == "association_class") && item["class_name"] != null)
                        {
                            var className = item["class_name"]?.ToString();
                            var classId = item["class_id"]?.ToString();
                            var classKeyLetter = item["KL"]?.ToString();

                            if (string.IsNullOrWhiteSpace(className) || string.IsNullOrWhiteSpace(classId))
                            {
                                textBox4.AppendText("Syntax error 15: Class name or class_id is empty in the subsystem. \r\n");
                                continue;
                            }

                            if (classNames.Contains(className))
                            {
                                textBox4.AppendText($"Syntax error15: Duplicate class name {className} within this subsystem. \r\n");
                            }

                            if (classIds.Contains(classId))
                            {
                                textBox4.AppendText($"Syntax error 15: Duplicate class_id {classId} within this subsystem. \r\n");
                            }

                            classNames.Add(className);
                            classIds.Add(classId);

                            // Check state models
                            if (item["states"] != null)
                            {
                                foreach (var state in item["states"])
                                {
                                    var stateName = state["state_name"]?.ToString();
                                    var stateKeyLetter = state["KL"]?.ToString();

                                    if (stateName != className || stateKeyLetter != classKeyLetter)
                                    {
                                        textBox4.AppendText($"Syntax error 15: State model name or KeyLetter does not match the class name or KeyLetter for {className}. \r\n");
                                    }
                                }
                            }
                        }

                        if (itemType == "association" && item["model"] is JObject associationModel)
                        {
                            var associationItemType = associationModel["type"]?.ToString();

                            if (associationItemType == "association_class" && associationModel["class_name"] != null)
                            {
                                var associationClassName = associationModel["class_name"]?.ToString();
                                var associationClassId = associationModel["class_id"]?.ToString();
                                var associationClassKeyLetter = associationModel["KL"]?.ToString();

                                if (string.IsNullOrWhiteSpace(associationClassName) || string.IsNullOrWhiteSpace(associationClassId))
                                {
                                    textBox4.AppendText("Syntax error 15: Class name or class_id is empty in the subsystem. \r\n");
                                    continue;
                                }

                                if (classNames.Contains(associationClassName))
                                {
                                    textBox4.AppendText($"Syntax error 15: Duplicate class name {associationClassName} within this subsystem. \r\n");
                                }

                                if (classIds.Contains(associationClassId))
                                {
                                    textBox4.AppendText($"Syntax error 15: Duplicate class_id {associationClassId} within this subsystem. \r\n");
                                }

                                classNames.Add(associationClassName);
                                classIds.Add(associationClassId);

                                // Check state models for association classes
                                if (associationModel["states"] != null)
                                {
                                    foreach (var state in associationModel["states"])
                                    {
                                        var stateName = state["state_name"]?.ToString();
                                        var stateKeyLetter = state["KL"]?.ToString();

                                        if (stateName != associationClassName || stateKeyLetter != associationClassKeyLetter)
                                        {
                                            textBox4.AppendText($"Syntax error 15: State model name or KeyLetter does not match the class name or KeyLetter for {associationClassName}. \r\n");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 15: " + ex.Message + "\r\n");
                return false;
            }
        }

        public static bool Point21(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                HashSet<string> reservedNames = new HashSet<string> { "TIMER" };
                HashSet<string> reservedKeyLetters = new HashSet<string> { "TIM" };
                bool timerObjectFound = false;

                foreach (var subsystem in jsonArray)
                {
                    foreach (var item in subsystem["model"])
                    {
                        var itemType = item["type"]?.ToString();
                        var className = item["class_name"]?.ToString();
                        var keyLetter = item["KL"]?.ToString();

                        if (itemType == "class" && className == "TIMER" && keyLetter == "TIM")
                        {
                            timerObjectFound = true;
                            var attributes = item["attributes"] as JArray;
                            var requiredAttributes = new HashSet<string> { "timer_id", "instance_id", "event_label", "time_remaining", "timer_status" };

                            foreach (var attribute in attributes)
                            {
                                var attributeName = attribute["attribute_name"]?.ToString();
                                requiredAttributes.Remove(attributeName);
                            }

                            if (requiredAttributes.Count > 0)
                            {
                                textBox4.AppendText("Syntax error 21: TIMER object is missing required attributes: " + string.Join(", ", requiredAttributes) + ".\r\n");
                            }
                        }
                        else
                        {
                            if (reservedNames.Contains(className))
                            {
                                textBox4.AppendText($"Syntax error 21: Reserved name {className} used for another object.\r\n");
                            }

                            if (reservedKeyLetters.Contains(keyLetter))
                            {
                                textBox4.AppendText($"Syntax error 21: Reserved KeyLetter {keyLetter} used for another object.\r\n");
                            }
                        }
                    }
                }

                if (!timerObjectFound)
                {
                    textBox4.AppendText("Syntax error 21: TIMER object with KeyLetter TIM not found in the subsystem.\r\n");
                }

                textBox4.AppendText("Success 21: TIMER object with KeyLetter TIM found in the subsystem.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 21: " + ex.Message + "\r\n");
                return false;
            }
        }

        public static bool Point22(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                foreach (var subsystem in jsonArray)
                {
                    HashSet<string> classNames = new HashSet<string>();
                    HashSet<string> classIds = new HashSet<string>();

                    foreach (var item in subsystem["model"])
                    {
                        var itemType = item["type"]?.ToString();

                        if ((itemType == "class" || itemType == "association_class") && item["class_name"] != null)
                        {
                            var className = item["class_name"]?.ToString();
                            var classId = item["class_id"]?.ToString();

                            if (string.IsNullOrWhiteSpace(className) || string.IsNullOrWhiteSpace(classId))
                            {
                                textBox4.AppendText("Syntax error 22: Class name or class_id is empty in the subsystem. \r\n");
                            }

                            if (classNames.Contains(className))
                            {
                                textBox4.AppendText($"Syntax error 22: Duplicate class name {className} within this subsystem. \r\n");
                            }

                            if (classIds.Contains(classId))
                            {
                                textBox4.AppendText($"Syntax error 22: Duplicate class_id {classId} within this subsystem. \r\n");
                            }

                            classNames.Add(className);
                            classIds.Add(classId);

                            // Check state events
                            if (item["states"] != null)
                            {
                                foreach (var state in item["states"])
                                {
                                    var stateEvents = state["state_event"]?.ToArray();
                                    if (stateEvents == null || stateEvents.Length == 0)
                                    {
                                        textBox4.AppendText($"Syntax error 22: State {state["state_name"]} does not generate any events.\r\n");
                                    }
                                }
                            }
                        }

                        if (itemType == "association" && item["model"] is JObject associationModel)
                        {
                            var associationItemType = associationModel["type"]?.ToString();

                            if (associationItemType == "association_class" && associationModel["class_name"] != null)
                            {
                                var associationClassName = associationModel["class_name"]?.ToString();
                                var associationClassId = associationModel["class_id"]?.ToString();

                                if (string.IsNullOrWhiteSpace(associationClassName) || string.IsNullOrWhiteSpace(associationClassId))
                                {
                                    textBox4.AppendText("Syntax error 22: Class name or class_id is empty in the subsystem. \r\n");
                                }

                                if (classNames.Contains(associationClassName))
                                {
                                    textBox4.AppendText($"Syntax error 22: Duplicate class name {associationClassName} within this subsystem. \r\n");
                                }

                                if (classIds.Contains(associationClassId))
                                {
                                    textBox4.AppendText($"Syntax error 22: Duplicate class_id {associationClassId} within this subsystem. \r\n");
                                }

                                classNames.Add(associationClassName);
                                classIds.Add(associationClassId);

                                // Check state events for association_class
                                if (associationModel["states"] != null)
                                {
                                    foreach (var state in associationModel["states"])
                                    {
                                        var stateEvents = state["state_event"]?.ToArray();
                                        if (stateEvents == null || stateEvents.Length == 0)
                                        {
                                            textBox4.AppendText($"Syntax error 22: State {state["state_name"]} does not generate any events.\r\n");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                textBox4.AppendText($"Success 22: All classes and association classes have unique names and IDs.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 22: " + ex.Message + "\r\n");
                return false;
            }
        }

        public static bool Point25(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                // Menggunakan HashSet untuk menyimpan event labels
                HashSet<string> eventLabels = new HashSet<string>();

                foreach (var subsystem in jsonArray)
                {
                    // Memeriksa apakah subsystem memiliki events
                    if (subsystem["events"] != null)
                    {
                        foreach (var eventItem in subsystem["events"])
                        {
                            var eventName = eventItem["event_name"]?.ToString();
                            var stateModelName = eventItem["state_model_name"]?.ToString();
                            var eventData = eventItem["event_data"] as JObject;
                            var eventLabel = eventItem["event_label"]?.ToString();

                            // Memeriksa apakah komponen-komponen event ada dan tidak kosong
                            if (string.IsNullOrWhiteSpace(eventName))
                            {
                                textBox4.AppendText("Syntax error: Event name is empty. \r\n");
                            }

                            if (string.IsNullOrWhiteSpace(stateModelName))
                            {
                                textBox4.AppendText("Syntax error: State model name is empty. \r\n");
                            }

                            if (eventData == null || !eventData.HasValues)
                            {
                                textBox4.AppendText("Syntax error: Event data is empty. \r\n");
                            }

                            if (string.IsNullOrWhiteSpace(eventLabel))
                            {
                                textBox4.AppendText("Syntax error: Event label is empty. \r\n");
                            }

                            // Memeriksa duplikasi event label
                            if (eventLabels.Contains(eventLabel))
                            {
                                textBox4.AppendText($"Syntax error: Duplicate event label {eventLabel}. \r\n");
                            }
                            else
                            {
                                eventLabels.Add(eventLabel);
                            }
                        }
                    }
                }
                textBox4.AppendText($"Success 25: All events have unique labels.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error: " + ex.Message + "\r\n");
                return false;
            }
        }

        public static bool Point27(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();

            try
            {
                HashSet<string> strings = new HashSet<string>();
                Func<JToken, bool> processItem = null;
                processItem = (item) =>
                {
                    var itemType = item["type"]?.ToString();
                    if (itemType == "class")
                    {
                        var className = item["class_name"]?.ToString();
                        if (className == "mahasiswa" || className == "dosen")
                        {
                            var stateArray = item["states"] as JArray;
                            if (stateArray != null)
                            {
                                foreach (var state in stateArray)
                                {
                                    var stateName = state["state_name"]?.ToString();
                                    var stateEvent = state["state_event"]?.ToString();
                                    if (stateEvent != null)
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        textBox4.AppendText($"Syntax error 27: event for {stateName} state is not implemented. \r\n");
                                        return false;
                                    }
                                }
                            }
                            else
                            {
                                textBox4.AppendText($"Syntax error 27: states label for class {className} is not implemented. \r\n");
                                return false;
                            }
                        }
                    }
                    return true;
                };
                foreach (var subsystem in jsonArray)
                {
                    foreach (var item in subsystem["model"])
                    {
                        if (!processItem(item))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 27: " + ex.Message + ".\r\n");
                return false;
            }
        }

        public static bool Point28(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                Func<JToken, bool> processItem = null;
                processItem = (item) =>
                {
                    var itemType = item["type"]?.ToString();
                    if (itemType == "class")
                    {
                        var className = item["class_name"]?.ToString();
                        var classId = item["class_id"]?.ToString();
                        var classKL = item["KL"]?.ToString();
                        if (className != null && classId != null && classKL != null)
                        {
                            return true;
                        }
                    }
                    return true;
                };
                foreach (var subsystem in jsonArray)
                {
                    foreach (var item in subsystem["model"])
                    {
                        if (!processItem(item))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 28: " + ex.Message + ".\r\n");
                return false;
            }
        }


        public static bool Point29(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                foreach (var subsystem in jsonArray)
                {
                    Dictionary<string, Dictionary<string, HashSet<string>>> stateEventsData = new Dictionary<string, Dictionary<string, HashSet<string>>>();

                    foreach (var item in subsystem["model"])
                    {
                        var itemType = item["type"]?.ToString();

                        if (itemType == "class" || itemType == "association_class")
                        {
                            foreach (var state in item["states"] ?? Enumerable.Empty<dynamic>())
                            {
                                var stateName = state["state_name"]?.ToString();

                                if (!stateEventsData.ContainsKey(stateName))
                                {
                                    stateEventsData[stateName] = new Dictionary<string, HashSet<string>>();
                                }

                                foreach (var eventName in state["state_event"])
                                {
                                    var eventString = eventName?.ToString();
                                    var eventData = string.Join(",", state["state_event"]); // Simulasikan data event

                                    if (!stateEventsData[stateName].ContainsKey(eventString))
                                    {
                                        stateEventsData[stateName][eventString] = new HashSet<string>();
                                    }

                                    if (stateEventsData[stateName][eventString].Count == 0)
                                    {
                                        stateEventsData[stateName][eventString].Add(eventData);
                                    }
                                    else
                                    {
                                        if (!stateEventsData[stateName][eventString].Contains(eventData))
                                        {
                                            textBox4.AppendText($"Syntax error: Event data inconsistency in state {stateName} for event {eventString}. \r\n");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                textBox4.AppendText($"Success 29: There are no inconsistencies in event data within each state.\r\n");

                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Error: " + ex.Message + "\r\n");
                return false;
            }
        }


        public static bool Point30(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                foreach (var subsystem in jsonArray)
                {
                    foreach (var item in subsystem["model"])
                    {
                        var itemType = item["type"]?.ToString();

                        // Check for TIM1 and TIM2 within association_class or class types that involve timers
                        if (itemType == "class" || itemType == "association_class")
                        {
                            if (item["attributes"] != null)
                            {
                                foreach (var attribute in item["attributes"])
                                {
                                    if (attribute["attribute_name"]?.ToString() == "timer_id")
                                    {
                                        var timerId = attribute["attribute_name"]?.ToString();
                                        if (string.IsNullOrWhiteSpace(timerId))
                                        {
                                            textBox4.AppendText("Syntax error: timer_id is empty. \r\n");
                                        }
                                    }

                                    if (attribute["attribute_name"]?.ToString() == "ELx")
                                    {
                                        var elx = attribute["attribute_name"]?.ToString();
                                        if (string.IsNullOrWhiteSpace(elx))
                                        {
                                            textBox4.AppendText("Syntax error: ELx is empty. \r\n");
                                        }
                                    }

                                    if (attribute["attribute_name"]?.ToString() == "instance_id")
                                    {
                                        var instanceId = attribute["attribute_name"]?.ToString();
                                        if (string.IsNullOrWhiteSpace(instanceId))
                                        {
                                            textBox4.AppendText("Syntax error: instance_id is empty. \r\n");
                                        }
                                    }

                                    if (attribute["attribute_name"]?.ToString() == "time_interval")
                                    {
                                        var timeInterval = attribute["attribute_name"]?.ToString();
                                        if (string.IsNullOrWhiteSpace(timeInterval))
                                        {
                                            textBox4.AppendText("Syntax error: time_interval is empty. \r\n");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                textBox4.AppendText("Success 30: Every state in classes has been included in the timer setting.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Error validating timers: " + ex.Message + "\r\n");
                return false;
            }
        }

        public static bool Point31(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                HashSet<string> entitiesWithEvents = new HashSet<string>();

                // Loop through subsystems and model elements
                foreach (var subsystem in jsonArray)
                {
                    if (subsystem["model"] is JArray model)
                    {
                        foreach (var element in model)
                        {
                            var elementType = element["type"]?.ToString();

                            // Check state models
                            if (elementType == "class" && element["states"] is JArray states)
                            {
                                var className = element["class_name"]?.ToString();
                                if (className != "TIMER" && states.Any(state => (state["state_event"] as JArray)?.Count > 0))
                                {
                                    entitiesWithEvents.Add(className);
                                }
                            }

                            // Check external entities
                            if (elementType == "external_entity" && element["events"] is JArray events)
                            {
                                var entityName = element["entity_name"]?.ToString();
                                if (entityName != "TIMER" && events.Count > 0)
                                {
                                    entitiesWithEvents.Add(entityName);
                                }
                            }
                        }
                    }
                }

                // Check if entities with events are present in Object Communication Model
                foreach (var subsystem in jsonArray)
                {
                    if (subsystem["OCM"] is JArray ocm)
                    {
                        foreach (var element in ocm)
                        {
                            var elementName = element["name"]?.ToString();
                            if (entitiesWithEvents.Contains(elementName))
                            {
                                entitiesWithEvents.Remove(elementName);
                            }
                        }
                    }
                }

                if (entitiesWithEvents.Count > 0)
                {
                    textBox4.AppendText("Syntax error 31: The following entities with events are missing from the Object Communication Model: " + string.Join(", ", entitiesWithEvents) + "\r\n");
                    return false;
                }

                textBox4.AppendText("Success 31: All entities with events are correctly listed in the Object Communication Model.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 31: " + ex.Message + "\r\n");
                return false;
            }
        }

        public static bool Point32(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                HashSet<string> eventsInvolved = new HashSet<string>();

                // Loop through subsystems and model elements
                foreach (var subsystem in jsonArray)
                {
                    if (subsystem["model"] is JArray model)
                    {
                        foreach (var element in model)
                        {
                            var elementType = element["type"]?.ToString();

                            // Check external entities for events
                            if (elementType == "external_entity" && element["events"] is JArray events)
                            {
                                foreach (var evt in events)
                                {
                                    var eventName = evt["event_name"]?.ToString();
                                    eventsInvolved.Add(eventName);
                                }
                            }

                            // Check state models for events
                            if (elementType == "class" && element["states"] is JArray states)
                            {
                                foreach (var state in states)
                                {
                                    Debug.WriteLine("line 221");
                                    var stateEvents = state["state_event"] as JArray;
                                    if (stateEvents != null)
                                    {
                                        foreach (var eventName in stateEvents)
                                        {
                                            eventsInvolved.Add(eventName.ToString());
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Check if events involved are present in Object Communication Model (OCM)
                bool ocmFound = false;
                foreach (var subsystem in jsonArray)
                {
                    if (subsystem["OCM"] is JArray ocm)
                    {
                        ocmFound = true;
                        foreach (var element in ocm)
                        {
                            var elementEvents = element["events"] as JArray;
                            Debug.WriteLine("sada" + elementEvents);
                            if (elementEvents != null)
                            {
                                foreach (var evt in elementEvents)
                                {
                                    var eventName = evt.ToString();
                                    if (eventsInvolved.Contains(eventName))
                                    {
                                        eventsInvolved.Remove(eventName);
                                    }
                                }
                            }
                        }
                    }
                }

                if (!ocmFound)
                {
                    textBox4.AppendText("Syntax error 32: Object Communication Model (OCM) not found.\r\n");
                    return false;
                }

                if (eventsInvolved.Count > 0)
                {
                    textBox4.AppendText("Syntax error 32: The following events are missing from the Object Communication Model: " + string.Join(", ", eventsInvolved) + "\r\n");
                    return false;
                }
                textBox4.AppendText("Success 32: All involved events are correctly listed in the Object Communication Model.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 32: " + ex.Message + "\r\n");
                return false;
            }
        }

        public static bool Point33(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                bool timerFound = false;
                HashSet<string> timerEvents = new HashSet<string>();

                // Identify TIMER state model and related events
                foreach (var subsystem in jsonArray)
                {
                    if (subsystem["model"] is JArray model)
                    {
                        foreach (var element in model)
                        {
                            var elementType = element["type"]?.ToString();

                            // Check for TIMER state model
                            if (elementType == "class" && element["class_name"]?.ToString() == "TIMER")
                            {
                                timerFound = true;

                                // Check for events associated with TIMER
                                var states = element["states"] as JArray;
                                if (states != null)
                                {
                                    foreach (var state in states)
                                    {
                                        var stateEvents = state["state_event"] as JArray;
                                        if (stateEvents != null)
                                        {
                                            foreach (var evt in stateEvents)
                                            {
                                                timerEvents.Add(evt.ToString());
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Check if TIMER and its events are present in Object Communication Model (OCM)
                foreach (var subsystem in jsonArray)
                {
                    if (subsystem["OCM"] is JArray ocm)
                    {
                        foreach (var element in ocm)
                        {
                            var elementName = element["name"]?.ToString();
                            if (elementName == "TIMER" || timerEvents.Contains(elementName))
                            {
                                textBox4.AppendText("Syntax error 33: The TIMER state model or its events are shown on the Object Communication Model (OCM).\r\n");
                                return false;
                            }
                        }
                    }
                }

                if (!timerFound)
                {
                    textBox4.AppendText("Syntax error 33: The TIMER state model is not found in the model.\r\n");
                    return false;
                }

                textBox4.AppendText("Success 33: The TIMER state model and its events are not shown on the Object Communication Model (OCM).\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 33: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool Point34(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                Func<JToken, bool> processItem = null;
                processItem = (item) =>
                {
                    var itemType = item["type"]?.ToString();
                    if (itemType == "class")
                    {
                        var className = item["class_name"]?.ToString();
                        var states = item["states"] as JArray;
                        if (states != null)
                        {
                            foreach (var sub in states)
                            {
                                var stateName = sub["state_name"]?.ToString();
                                var stateEvent = sub["state_event"]?.ToString();
                                if (stateEvent != null)
                                {
                                    return true;
                                }
                                else
                                {
                                    textBox4.AppendText($"Syntax error 34: there is not action for {className} class itself.\r\n");
                                    return false;
                                }
                            }
                        }
                        else
                        {
                            textBox4.AppendText($"Syntax error 34: states for {className} class is null.\r\n");
                            return false;
                        }
                    }
                    return true;
                };
                foreach (var subsystem in jsonArray)
                {
                    foreach (var item in subsystem["model"])
                    {
                        if (item.Contains("states"))
                        {
                            if (!processItem(item))
                            {
                                return false;
                            }
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText($"Syntax error 34: " + ex.Message + ".\r\n");
                return false;
            }
        }

        public static bool Point35(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                // Loop through subsystems and model elements
                foreach (var subsystem in jsonArray)
                {
                    if (subsystem["model"] is JArray model)
                    {
                        foreach (var element in model)
                        {
                            var elementType = element["type"]?.ToString();

                            // Check for classes and their attributes
                            if (elementType == "class" && element["attributes"] is JArray attributes)
                            {
                                var className = element["class_name"]?.ToString();
                                var classInstances = new Dictionary<string, Dictionary<string, string>>();

                                // Collect initial data for each instance of the class
                                if (element["instances"] is JArray instances)
                                {
                                    foreach (var instance in instances)
                                    {
                                        var instanceId = instance["id"]?.ToString();
                                        if (instanceId != null)
                                        {
                                            foreach (var attribute in attributes)
                                            {
                                                var attributeName = attribute["attribute_name"]?.ToString();
                                                if (attributeName != null)
                                                {
                                                    var attributeValue = instance[attributeName]?.ToString();
                                                    if (attributeValue != null)
                                                    {
                                                        if (!classInstances.ContainsKey(instanceId))
                                                        {
                                                            classInstances[instanceId] = new Dictionary<string, string>();
                                                        }
                                                        classInstances[instanceId][attributeName] = attributeValue;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                // Check actions that modify attributes
                                if (element["actions"] is JArray actions)
                                {
                                    foreach (var action in actions)
                                    {
                                        var actionType = action["type"]?.ToString();
                                        if (actionType == "modify" && action["target"]?.ToString() == "descriptive_attribute")
                                        {
                                            var targetAttribute = action["attribute_name"]?.ToString();
                                            var newValue = action["new_value"]?.ToString();
                                            var instanceId = action["instance_id"]?.ToString();

                                            if (targetAttribute != null && instanceId != null && newValue != null)
                                            {
                                                if (classInstances.ContainsKey(instanceId))
                                                {
                                                    classInstances[instanceId][targetAttribute] = newValue;

                                                    // Check for consistency (example: no empty values)
                                                    if (classInstances[instanceId] != null)
                                                    {
                                                        foreach (var attributeValue in classInstances[instanceId].Values)
                                                        {
                                                            if (string.IsNullOrEmpty(attributeValue))
                                                            {
                                                                textBox4.AppendText($"Syntax error 35: Inconsistent data found for instance {instanceId} of class {className} after modifying attribute {targetAttribute}.\r\n");
                                                                return false;
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                textBox4.AppendText("Success 35: All data modifications ensure self-consistency for instances.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 35: " + ex.Message + "\r\n");
                return false;
            }
        }

        public static bool Point99(Form1 form1, JArray subsystems)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                foreach (var subsystem in subsystems)
                {
                    var subsystemId = subsystem["sub_id"]?.ToString();

                    foreach (var item in subsystem["model"])
                    {
                        var itemType = item["type"]?.ToString();

                        if (itemType == "association")
                        {
                            var associationClass = item["class"] as JArray;

                            // Pastikan association memiliki dua class di dalamnya
                            if (associationClass == null || associationClass.Count != 2)
                            {
                                textBox4.AppendText($"Syntax error 99: Subsystem {subsystemId} has an association {item["name"]?.ToString()} that lacks a relationship between two classes within it. \r\n");

                            }
                        }
                    }
                }


                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText($"Syntax error 99: {ex.Message} \r\n");
                return false;
            }
        }

        public static bool Point36(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();

            try
            {
                foreach (var subsistem in jsonArray)
                {
                    if (subsistem?["type"]?.ToString() == "subsystem")
                    {
                        var model = subsistem["model"];

                        foreach (var item in model)
                        {
                            if (item?["type"]?.ToString() == "action")
                            {
                                var actionType = item["action_type"]?.ToString();
                                if (actionType == "create" || actionType == "delete")
                                {
                                    var affectedClass = item["affected_class"]?.ToString();

                                    // Check relationships consistency
                                    bool isConsistent = true;
                                    foreach (var sub in jsonArray)
                                    {
                                        var subModel = sub["model"];
                                        foreach (var subItem in subModel)
                                        {
                                            if (subItem?["type"]?.ToString() == "association")
                                            {
                                                var classes = subItem["class"];
                                                if (classes != null && classes.Any(cls => cls["class_name"]?.ToString() == affectedClass))
                                                {
                                                    // Add detailed consistency checks here if needed
                                                    if (string.IsNullOrEmpty(subItem["name"]?.ToString()) || classes.Count() < 2)
                                                    {
                                                        isConsistent = false;
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    if (!isConsistent)
                                    {
                                        textBox4.AppendText($"Syntax error 36: Action {actionType} on class {affectedClass} has inconsistent relationships. \r\n");
                                    }
                                }
                            }
                        }
                    }
                }
                textBox4.AppendText("Success 36: All actions have consistent relationships.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 36: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool Point37(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();

            try
            {
                foreach (var subsistem in jsonArray)
                {
                    if (subsistem?["type"]?.ToString() == "subsystem")
                    {
                        var model = subsistem["model"];

                        foreach (var item in model)
                        {
                            if (item?["type"]?.ToString() == "action")
                            {
                                var subtypes = item["subtypes"];
                                var supertypes = item["supertypes"];

                                // Check if subtypes and supertypes are consistently populated
                                if (subtypes == null || supertypes == null || !subtypes.Any() || !supertypes.Any())
                                {
                                    textBox4.AppendText($"Syntax error 37: Action {item["name"]} does not leave subtypes and supertypes consistently populated. \r\n");
                                    continue;
                                }

                                // Check for consistent population
                                foreach (var subtype in subtypes)
                                {
                                    var subtypeId = subtype["id"]?.ToString();
                                    bool foundCorrespondingSupertype = false;

                                    foreach (var supertype in supertypes)
                                    {
                                        var supertypeId = supertype["id"]?.ToString();
                                        if (subtypeId == supertypeId)
                                        {
                                            foundCorrespondingSupertype = true;
                                            break;
                                        }
                                    }

                                    if (!foundCorrespondingSupertype)
                                    {
                                        textBox4.AppendText($"Syntax error 37: Action {item["name"]} has subtype {subtypeId} without a corresponding supertype. \r\n");
                                    }
                                }

                                foreach (var supertype in supertypes)
                                {
                                    var supertypeId = supertype["id"]?.ToString();
                                    bool foundCorrespondingSubtype = false;

                                    foreach (var subtype in subtypes)
                                    {
                                        var subtypeId = subtype["id"]?.ToString();
                                        if (supertypeId == subtypeId)
                                        {
                                            foundCorrespondingSubtype = true;
                                            break;
                                        }
                                    }

                                    if (!foundCorrespondingSubtype)
                                    {
                                        textBox4.AppendText($"Syntax error 37: Action {item["name"]} has supertype {supertypeId} without a corresponding subtype. \r\n");
                                    }
                                }
                            }
                        }
                    }
                }
                textBox4.AppendText("Success 37: All actions have consistent subtypes and supertypes.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 37: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool Point38(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();

            try
            {
                foreach (var subsistem in jsonArray)
                {
                    if (subsistem?["type"]?.ToString() == "subsystem")
                    {
                        var model = subsistem["model"];

                        foreach (var item in model)
                        {
                            if (item?["type"]?.ToString() == "action")
                            {
                                var stateTransitions = item["stateTransitions"];
                                var actionName = item["name"]?.ToString();

                                // Check if action is a deletion state
                                bool isDeletionState = item["deletionState"]?.ToObject<bool>() ?? false;

                                if (!isDeletionState)
                                {
                                    bool updatesCurrentState = false;

                                    if (stateTransitions != null)
                                    {
                                        foreach (var transition in stateTransitions)
                                        {
                                            var transitionAttributes = transition["attributes"];
                                            if (transitionAttributes != null)
                                            {
                                                foreach (var attribute in transitionAttributes)
                                                {
                                                    if (attribute["name"]?.ToString() == "currentState")
                                                    {
                                                        updatesCurrentState = true;
                                                        break;
                                                    }
                                                }
                                            }
                                            if (updatesCurrentState) break;
                                        }
                                    }

                                    if (!updatesCurrentState)
                                    {
                                        textBox4.AppendText($"Syntax error 38: Action {actionName} does not update the current state attribute.\r\n");
                                    }
                                }
                            }
                        }
                    }
                }

                textBox4.AppendText("Success 38: All actions appropriately update the current state attribute.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 38: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool Point39(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();

            try
            {
                foreach (var subsistem in jsonArray)
                {
                    if (subsistem?["type"]?.ToString() == "subsystem")
                    {
                        var model = subsistem["model"];

                        foreach (var item in model)
                        {
                            if (item?["type"]?.ToString() == "action")
                            {
                                var stateName = item["state"]?["name"]?.ToString();
                                var processModelName = item["processModel"]?["name"]?.ToString();
                                var actionName = item["name"]?.ToString();

                                // Check if stateName and processModelName are not null and are equal
                                if (stateName == null || processModelName == null || stateName != processModelName)
                                {
                                    textBox4.AppendText($"Syntax error 39: Action {actionName} has state {stateName} but the process model is {processModelName}.\r\n");
                                }
                            }
                        }
                    }
                }
                textBox4.AppendText("Success 39: All actions have matching state and process model names.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 39: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool Point40(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();

            try
            {
                foreach (var subsistem in jsonArray)
                {
                    if (subsistem?["type"]?.ToString() == "subsystem")
                    {
                        var model = subsistem["model"];

                        foreach (var item in model)
                        {
                            if (item?["type"]?.ToString() == "processModel")
                            {
                                var processes = item["processes"];
                                var dataFlows = item["dataFlows"];
                                var controlFlows = item["controlFlows"];
                                var dataStores = item["dataStores"];
                                var processModelName = item["name"]?.ToString();

                                // Check if processes and data flows are not null and have at least one element
                                bool hasProcesses = processes != null && processes.Any();
                                bool hasDataFlows = dataFlows != null && dataFlows.Any();

                                if (!hasProcesses)
                                {
                                    textBox4.AppendText($"Syntax error 40: Process model {processModelName} does not have any processes.\r\n");
                                }

                                if (!hasDataFlows)
                                {
                                    textBox4.AppendText($"Syntax error 40: Process model {processModelName} does not have any data flows.\r\n");
                                }

                                // Check optional control flows and data stores (not required)
                                bool hasControlFlows = controlFlows != null && controlFlows.Any();
                                bool hasDataStores = dataStores != null && dataStores.Any();

                                // Optionally report the presence of control flows and data stores
                                if (!hasControlFlows)
                                {
                                    textBox4.AppendText($"Note: Process model {processModelName} does not have any control flows.\r\n");
                                }

                                if (!hasDataStores)
                                {
                                    textBox4.AppendText($"Note: Process model {processModelName} does not have any data stores.\r\n");
                                }
                            }
                        }
                    }
                }
                textBox4.AppendText("Success 40: Process models contain processes and data flows.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 40: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool Point41(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();

            try
            {
                // Gather all object names from the IM (Information Model)
                HashSet<string> objectNames = new HashSet<string>();
                foreach (var subsistem in jsonArray)
                {
                    if (subsistem?["type"]?.ToString() == "subsystem")
                    {
                        var model = subsistem["model"];
                        foreach (var item in model)
                        {
                            if (item?["type"]?.ToString() == "object")
                            {
                                var objectName = item["name"]?.ToString();
                                if (!string.IsNullOrEmpty(objectName))
                                {
                                    objectNames.Add(objectName);
                                    textBox4.AppendText(objectName + "\r\n");
                                }
                            }
                        }
                    }
                }

                // Check data store labels
                foreach (var subsistem in jsonArray)
                {
                    if (subsistem?["type"]?.ToString() == "subsystem")
                    {
                        var model = subsistem["model"];
                        foreach (var item in model)
                        {
                            if (item?["type"]?.ToString() == "processModel")
                            {
                                var dataStores = item["dataStores"];
                                var processModelName = item["name"]?.ToString();

                                if (dataStores != null)
                                {
                                    foreach (var dataStore in dataStores)
                                    {
                                        var dataStoreName = dataStore["name"]?.ToString();
                                        if (dataStoreName != "Timer" && dataStoreName != "Current Time" && !objectNames.Contains(dataStoreName))
                                        {
                                            textBox4.AppendText($"Syntax error 41: Data store {dataStoreName} in process model {processModelName} is not labeled 'Timer', 'Current Time', or with a valid object name from the IM.\r\n");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                textBox4.AppendText("Success 41: Data stores in process models are correctly labeled.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 41: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool Point42(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();

            try
            {
                // Check each subsystem
                foreach (var subsistem in jsonArray)
                {
                    if (subsistem?["type"]?.ToString() == "subsystem")
                    {
                        var model = subsistem["model"];

                        // Check each process model in the subsystem
                        foreach (var item in model)
                        {
                            if (item?["type"]?.ToString() == "processModel")
                            {
                                var dataStores = item["dataStores"];
                                var processModelName = item["name"]?.ToString();

                                if (dataStores != null)
                                {
                                    foreach (var dataStore in dataStores)
                                    {
                                        var dataStoreName = dataStore["name"]?.ToString();
                                        var dataStoreDescription = dataStore["description"]?.ToString();

                                        // Check if data store is labeled "Timer"
                                        if (dataStoreName == "Timer")
                                        {
                                            // Ensure it represents the time left on each timer in the system
                                            if (dataStoreDescription == null || !dataStoreDescription.Contains("time left on each timer"))
                                            {
                                                textBox4.AppendText($"Syntax error 42: Data store 'Timer' in process model {processModelName} does not represent the time left on each timer in the system.\r\n");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                textBox4.AppendText("Success 42: Timers in process models represent time left on each timer.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 42: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool Point43(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();

            try
            {
                // Check each subsystem
                foreach (var subsistem in jsonArray)
                {
                    if (subsistem?["type"]?.ToString() == "subsystem")
                    {
                        var model = subsistem["model"];

                        // Check each process model in the subsystem
                        foreach (var item in model)
                        {
                            if (item?["type"]?.ToString() == "processModel")
                            {
                                var dataStores = item["dataStores"];
                                var processModelName = item["name"]?.ToString();

                                if (dataStores != null)
                                {
                                    foreach (var dataStore in dataStores)
                                    {
                                        var dataStoreName = dataStore["name"]?.ToString();
                                        var dataStoreDescription = dataStore["description"]?.ToString();

                                        // Check if data store is labeled "Current Time"
                                        if (dataStoreName == "Current Time")
                                        {
                                            // Ensure it represents data describing current time
                                            if (dataStoreDescription == null || !dataStoreDescription.Contains("current time"))
                                            {
                                                textBox4.AppendText($"Syntax error 43: Data store 'Current Time' in process model {processModelName} does not represent data describing current time.\r\n");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                textBox4.AppendText("Success 43: All 'Current Time' data stores represent data describing the current time.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 43: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool Point44(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();

            try
            {
                foreach (var subsistem in jsonArray)
                {
                    if (subsistem?["type"]?.ToString() == "subsystem")
                    {
                        var model = subsistem["model"];

                        foreach (var item in model)
                        {
                            if (item?["type"]?.ToString() == "data_store")
                            {
                                var dataStoreName = item["name"]?.ToString();

                                // Check if data store name corresponds to any object in IM
                                bool isValidDataStore = false;

                                foreach (var subItem in model)
                                {
                                    if (subItem?["type"]?.ToString() == "class" && subItem["class_name"]?.ToString() == dataStoreName)
                                    {
                                        var attributes = subItem["attributes"];
                                        if (attributes != null && attributes.Any())
                                        {
                                            isValidDataStore = true;
                                            break;
                                        }
                                    }
                                }

                                if (!isValidDataStore)
                                {
                                    textBox4.AppendText($"Syntax error: Data store {dataStoreName} does not correspond to any object or lacks attributes in the IM. \r\n");
                                }
                            }
                        }
                    }
                }
                textBox4.AppendText("Success 44: All data stores correspond to objects in the IM and have attributes.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 44: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool Point45(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                HashSet<string> dataStoreIds = new HashSet<string>();

                foreach (var subsystem in jsonArray)
                {
                    if (subsystem["model"] is JArray model)
                    {
                        foreach (var element in model)
                        {
                            if (element["typedata"]?.ToString() == "dataStore")
                            {

                                var dataStoreId = element["dataStoreId"]?.ToString();

                                if (dataStoreIds.Contains(dataStoreId))
                                {

                                    // If the data store is already encountered, it's appearing multiple times
                                    textBox4.AppendText($"Success 45: Data store '{dataStoreId}' appears in multiple places within the process model(s).\r\n");
                                    return true;
                                }
                                else
                                {

                                    // Add the data store ID to the HashSet
                                    dataStoreIds.Add(dataStoreId);
                                }
                            }
                        }
                    }
                }

                // If the loop completes without finding any duplicate data store, return false
                textBox4.AppendText($"Syntax error 45: No data store appears in multiple places within the process model(s).\r\n");
                return false;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 45: " + ex.Message + "\r\n");
                return false;
            }

        }
        public static bool Point46(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox(); ;
            try
            {
                foreach (var subsystem in jsonArray)
                {
                    if (subsystem["model"] is JArray model)
                    {
                        foreach (var element in model)
                        {
                            if (element["type"]?.ToString() == "process" && element["controlFlows"] is JArray controlFlows)
                            {
                                foreach (var controlFlow in controlFlows)
                                {
                                    var condition = controlFlow["condition"]?.ToString();
                                    var destination = controlFlow["destination"]?.ToString();

                                    // Ensure all conditional control flows have a condition label
                                    if (condition == null)
                                    {
                                        textBox4.AppendText($"Syntax error 46: The control flow to '{destination}' is missing a condition label.\r\n");
                                        return false;
                                    }
                                }
                            }
                        }
                    }
                }

                // If all checks pass, append success message
                textBox4.AppendText("Success 46: All conditional control flows are correctly labeled with the circumstances under which they are generated.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 46: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool Point47(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                foreach (var subsystem in jsonArray)
                {
                    if (subsystem["model"] is JArray model)
                    {
                        foreach (var element in model)
                        {
                            if (element["type"]?.ToString() == "process" && element["controlFlows"] is JArray controlFlows)
                            {
                                foreach (var controlFlow in controlFlows)
                                {
                                    var condition = controlFlow["condition"]?.ToString();
                                    var destination = controlFlow["destination"]?.ToString();

                                    // Ensure unconditional control flows are unlabelled
                                    if (condition == null && controlFlow["label"] != null)
                                    {
                                        textBox4.AppendText($"Syntax error 47: The unconditional control flow to '{destination}' should not have a label.\r\n");
                                        return false;
                                    }
                                }
                            }
                        }
                    }
                }

                // If all checks pass, append success message
                textBox4.AppendText("Success 47: All unconditional control flows are correctly unlabelled.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 47: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool Point48(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                foreach (var subsystem in jsonArray)
                {
                    if (subsystem["model"] is JArray model)
                    {
                        foreach (var element in model)
                        {
                            // Process the conditional and unconditional data flows
                            if (element["type"]?.ToString() == "process")
                            {
                                if (element["dataFlows"] is JArray dataFlows)
                                {
                                    foreach (var dataFlow in dataFlows)
                                    {
                                        var dataFlowType = dataFlow["type"]?.ToString();
                                        var dataFlowLabel = dataFlow["label"]?.ToString();

                                        // Ensure the data flow is labeled with data elements it carries
                                        if (string.IsNullOrEmpty(dataFlowLabel))
                                        {
                                            textBox4.AppendText($"Syntax error 48: A {dataFlowType} data flow in process '{element["name"]}' is not labeled with the names of the data elements it carries.\r\n");
                                            return false;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // If all checks pass, append success message
                textBox4.AppendText("Success 48: All data flows are correctly labeled with the names of the data elements they carry.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 48: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool Point49(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                foreach (var subsystem in jsonArray)
                {
                    if (subsystem["model"] is JArray model)
                    {
                        foreach (var element in model)
                        {
                            if (element["type"]?.ToString() == "process")
                            {
                                var processName = element["name"]?.ToString();
                                var dataFlows = element["dataFlows"] as JArray;

                                if (dataFlows != null)
                                {
                                    foreach (var dataFlow in dataFlows)
                                    {
                                        var target = dataFlow["target"]?.ToString();
                                        var label = dataFlow["label"]?.ToString();

                                        if (string.IsNullOrEmpty(target) || string.IsNullOrEmpty(label))
                                        {
                                            textBox4.AppendText($"Syntax error 49: Data flow in process '{processName}' must have a target and a label.\r\n");
                                            return false;
                                        }

                                        // Assuming the label should contain attributes read or written by the process
                                        bool validLabel = false;
                                        foreach (var elementModel in model)
                                        {
                                            if (elementModel["type"]?.ToString() == "class" &&
                                                elementModel["class_name"]?.ToString() == target)
                                            {
                                                var attributes = elementModel["attributes"] as JArray;
                                                if (attributes != null)
                                                {
                                                    foreach (var attribute in attributes)
                                                    {
                                                        var attributeName = attribute["attribute_name"]?.ToString();
                                                        if (label.Contains(attributeName))
                                                        {
                                                            validLabel = true;
                                                            break;
                                                        }
                                                    }
                                                }
                                                break;
                                            }
                                        }

                                        if (!validLabel)
                                        {
                                            textBox4.AppendText($"Syntax error 49: Data flow from process '{processName}' to '{target}' has an invalid label '{label}'.\r\n");
                                            return false;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // If all checks pass, append success message
                textBox4.AppendText("Success 49: All data flows between processes and object data stores are correctly labeled.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 49: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool Point50(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                foreach (var subsystem in jsonArray)
                {
                    if (subsystem["model"] is JArray model)
                    {
                        foreach (var element in model)
                        {
                            if (element["type"]?.ToString() == "process")
                            {
                                var processName = element["name"]?.ToString();
                                var dataFlows = element["dataFlows"] as JArray;

                                if (dataFlows != null)
                                {
                                    foreach (var dataFlow in dataFlows)
                                    {
                                        var dataFlowName = dataFlow["name"]?.ToString();
                                        var attributes = dataFlow["attributes"] as JArray;

                                        // Validate each data flow
                                        bool isValidDataFlow = false;

                                        if (attributes != null)
                                        {
                                            foreach (var attribute in attributes)
                                            {
                                                var attributeName = attribute["name"]?.ToString();
                                                var attributeType = attribute["type"]?.ToString();

                                                if (attributeName != null && attributeType != null)
                                                {
                                                    // Check if the attribute represents object attributes or transient data
                                                    if (attributeType == "object_attribute" || attributeType == "transient_data")
                                                    {
                                                        isValidDataFlow = true;
                                                        break;
                                                    }
                                                }
                                            }
                                        }

                                        if (!isValidDataFlow)
                                        {
                                            textBox4.AppendText($"Syntax error 50: The data flow '{dataFlowName}' in process '{processName}' does not represent valid object attributes or transient data.\r\n");
                                            return false;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // If all checks pass, append success message
                textBox4.AppendText("Success 50: All data flows between processes represent valid object attributes or transient data.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 50: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool Point51(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                foreach (var subsystem in jsonArray)
                {
                    if (subsystem["model"] is JArray model)
                    {
                        foreach (var element in model)
                        {
                            if (element["type"]?.ToString() == "process")
                            {
                                var processName = element["name"]?.ToString();
                                var dataFlows = element["dataFlows"] as JArray;

                                if (dataFlows != null)
                                {
                                    foreach (var dataFlow in dataFlows)
                                    {
                                        var dataFlowName = dataFlow["name"]?.ToString();
                                        var attributes = dataFlow["attributes"] as JArray;

                                        if (attributes != null)
                                        {
                                            foreach (var attribute in attributes)
                                            {
                                                var attributeName = attribute["name"]?.ToString();
                                                var attributeType = attribute["type"]?.ToString();

                                                // Check if the attribute represents transient data
                                                if (attributeType == "transient_data")
                                                {
                                                    // Check if the data flow is labelled correctly
                                                    if (dataFlowName == null || !dataFlowName.Contains("(transient)"))
                                                    {
                                                        textBox4.AppendText($"Syntax error 51: The transient data flow '{dataFlowName}' in process '{processName}' is not correctly labelled with (transient).\r\n");
                                                        return false;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // If all checks pass, append success message
                textBox4.AppendText("Success 51: All transient data flows between processes are correctly labelled with an appropriate variable name and (transient).\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 51: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool Point52(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                foreach (var subsystem in jsonArray)
                {
                    if (subsystem["model"] is JArray model)
                    {
                        foreach (var element in model)
                        {
                            if (element["type"]?.ToString() == "process")
                            {
                                var processName = element["name"]?.ToString();
                                var dataFlows = element["dataFlows"] as JArray;

                                if (dataFlows != null)
                                {
                                    foreach (var dataFlow in dataFlows)
                                    {
                                        var dataFlowName = dataFlow["name"]?.ToString();
                                        var attributes = dataFlow["attributes"] as JArray;

                                        if (attributes != null)
                                        {
                                            foreach (var attribute in attributes)
                                            {
                                                var attributeName = attribute["name"]?.ToString();
                                                var attributeType = attribute["type"]?.ToString();

                                                // Check if the attribute represents persistent data
                                                if (attributeType == "persistent_data")
                                                {
                                                    // Ensure the data flow is labelled correctly
                                                    if (dataFlowName == null)
                                                    {
                                                        textBox4.AppendText($"Syntax error 52: The persistent data flow in process '{processName}' is not labelled.\r\n");
                                                        return false;
                                                    }

                                                    var expectedLabel = $"{attribute["objectP"]}.{attribute["attribute1"]}={attribute["objectC"]}.{attribute["attribute2"]}";

                                                    if (dataFlowName != expectedLabel && dataFlowName != attributeName)
                                                    {
                                                        textBox4.AppendText($"Syntax error 52: The persistent data flow '{dataFlowName}' in process '{processName}' is not labelled correctly. Expected label: '{expectedLabel}' or '{attributeName}'.\r\n");
                                                        return false;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // If all checks pass, append success message
                textBox4.AppendText("Success 52: All persistent data flows between processes are correctly labelled with the appropriate attribute names.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 52: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool Point53(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                foreach (var subsystem in jsonArray)
                {
                    if (subsystem["model"] is JArray model)
                    {
                        foreach (var element in model)
                        {
                            if (element["type"]?.ToString() == "process")
                            {
                                var processName = element["name"]?.ToString();
                                var transitions = element["transitions"] as JArray;

                                if (transitions != null)
                                {
                                    foreach (var transition in transitions)
                                    {
                                        var eventData = transition["eventData"] as JArray;

                                        if (eventData != null)
                                        {
                                            foreach (var data in eventData)
                                            {
                                                var dataFlowName = data["name"]?.ToString();
                                                var requiredAttributes = data["requiredAttributes"] as JArray;

                                                if (requiredAttributes != null)
                                                {
                                                    var requiredAttributesList = requiredAttributes.Select(attr => attr.ToString()).ToList();

                                                    // Ensure the data flow is labelled correctly
                                                    if (dataFlowName == null || !requiredAttributesList.All(attr => dataFlowName.Contains(attr)))
                                                    {
                                                        textBox4.AppendText($"Syntax error 53: The event data flow into process '{processName}' is not labelled correctly with the required attributes.\r\n");
                                                        return false;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // If all checks pass, append success message
                textBox4.AppendText("Success 53: All event data flows into processes are correctly labelled with the required attributes.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 53: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool Point54(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                foreach (var subsystem in jsonArray)
                {
                    if (subsystem["model"] is JArray model)
                    {
                        foreach (var element in model)
                        {
                            if (element["type"]?.ToString() == "process")
                            {
                                var processName = element["name"]?.ToString();
                                var eventsGenerated = element["eventsGenerated"] as JArray;

                                if (eventsGenerated != null)
                                {
                                    foreach (var eventGenerated in eventsGenerated)
                                    {
                                        var eventName = eventGenerated["name"]?.ToString();
                                        var eventMeaning = eventGenerated["meaning"]?.ToString();
                                        var eventData = eventGenerated["eventData"]?.ToString();

                                        if (string.IsNullOrEmpty(eventName) || string.IsNullOrEmpty(eventMeaning) || string.IsNullOrEmpty(eventData))
                                        {
                                            textBox4.AppendText($"Syntax error 54: The event generated by process '{processName}' is not correctly labelled with the event's label, meaning, and event data.\r\n");
                                            return false;
                                        }
                                        else
                                        {
                                            // This represents the data flow directed away from the process
                                            textBox4.AppendText($"Event '{eventName}' generated by process '{processName}' with meaning '{eventMeaning}' and event data '{eventData}' is correctly labelled and directed away from the process.\r\n");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // If all checks pass, append success message
                textBox4.AppendText("Success 54: All events generated by processes are correctly labelled and represented as data flows directed away from the process.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 54: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool Point55(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                foreach (var subsystem in jsonArray)
                {
                    if (subsystem["processes"] is JArray processes)
                    {
                        foreach (var process in processes)
                        {
                            var processType = process["type"]?.ToString();
                            var processName = process["name"]?.ToString();

                            if (processType == "delete" && process["dataFlows"] is JArray dataFlows)
                            {
                                foreach (var dataFlow in dataFlows)
                                {
                                    var dataFlowLabel = dataFlow["label"]?.ToString();

                                    if (dataFlowLabel != "(delete)")
                                    {
                                        textBox4.AppendText($"Syntax error 55: The data flow from the delete process '{processName}' is not labeled with (delete).\r\n");
                                        return false;
                                    }

                                    // Ensure no attribute names are shown on the data flow
                                    if (dataFlow["attributes"] != null)
                                    {
                                        textBox4.AppendText($"Syntax error 55: The data flow from the delete process '{processName}' should not show any attribute names.\r\n");
                                        return false;
                                    }
                                }
                            }
                        }
                    }
                }

                // If all checks pass, append success message
                textBox4.AppendText("Success 55 : All delete processes are correctly labeled with (delete) and have no attribute names.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 55: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool Point56(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                var dataStores = new HashSet<string>();
                var controlStores = new HashSet<string>();

                // Collect all available data and control stores
                foreach (var subsystem in jsonArray)
                {
                    foreach (var item in subsystem["model"])
                    {
                        var itemType = item["type"]?.ToString();

                        if (itemType == "data_store")
                        {
                            var storeName = item["store_name"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(storeName))
                            {
                                dataStores.Add(storeName);
                            }
                        }
                        else if (itemType == "control_store")
                        {
                            var controlName = item["control_name"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(controlName))
                            {
                                controlStores.Add(controlName);
                            }
                        }
                    }
                }

                // Check each process for required inputs
                foreach (var subsystem in jsonArray)
                {
                    foreach (var item in subsystem["model"])
                    {
                        var itemType = item["type"]?.ToString();

                        if (itemType == "process")
                        {
                            var processName = item["process_name"]?.ToString();
                            var dataInputs = item["data_inputs"] as JArray;
                            var controlInputs = item["control_inputs"] as JArray;
                            bool allInputsAvailable = true;

                            if (dataInputs != null)
                            {
                                foreach (var input in dataInputs)
                                {
                                    var inputName = input.ToString();
                                    if (!dataStores.Contains(inputName))
                                    {
                                        textBox4.AppendText($"Syntax error 56: Data input {inputName} for process {processName} is not available. \r\n");
                                        allInputsAvailable = false;
                                    }
                                }
                            }

                            if (controlInputs != null)
                            {
                                foreach (var input in controlInputs)
                                {
                                    var inputName = input.ToString();
                                    if (!controlStores.Contains(inputName))
                                    {
                                        textBox4.AppendText($"Syntax error 56: Control input {inputName} for process {processName} is not available. \r\n");
                                        allInputsAvailable = false;
                                    }
                                }
                            }

                            if (!allInputsAvailable)
                            {
                                return false;
                            }
                        }
                    }
                }
                textBox4.AppendText("Success 56: All processes have their required data and control inputs available.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 56: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool Point57(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                var dataStores = new HashSet<string>();
                var controlStores = new HashSet<string>();

                // Collect all available data and control stores
                foreach (var subsystem in jsonArray)
                {
                    foreach (var item in subsystem["model"])
                    {
                        var itemType = item["type"]?.ToString();

                        if (itemType == "data_store")
                        {
                            var storeName = item["store_name"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(storeName))
                            {
                                dataStores.Add(storeName);
                            }
                        }
                        else if (itemType == "control_store")
                        {
                            var controlName = item["control_name"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(controlName))
                            {
                                controlStores.Add(controlName);
                            }
                        }
                    }
                }

                // Check each process for required outputs
                foreach (var subsystem in jsonArray)
                {
                    foreach (var item in subsystem["model"])
                    {
                        var itemType = item["type"]?.ToString();

                        if (itemType == "process")
                        {
                            var processName = item["process_name"]?.ToString();
                            var dataOutputs = item["data_outputs"] as JArray;
                            var controlOutputs = item["control_outputs"] as JArray;
                            bool allOutputsAvailable = true;

                            if (dataOutputs != null)
                            {
                                foreach (var output in dataOutputs)
                                {
                                    var outputName = output.ToString();
                                    if (!dataStores.Contains(outputName))
                                    {
                                        textBox4.AppendText($"Syntax error 57: Data output {outputName} from process {processName} is not available in data stores. \r\n");
                                        allOutputsAvailable = false;
                                    }
                                }
                            }

                            if (controlOutputs != null)
                            {
                                foreach (var output in controlOutputs)
                                {
                                    var outputName = output.ToString();
                                    if (!controlStores.Contains(outputName))
                                    {
                                        textBox4.AppendText($"Syntax error 57: Control output {outputName} from process {processName} is not available in control stores. \r\n");
                                        allOutputsAvailable = false;
                                    }
                                }
                            }

                            if (!allOutputsAvailable)
                            {
                                return false;
                            }
                        }
                    }
                }
                textBox4.AppendText("Success 57: All processes checked successfully and all outputs are available.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 57: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool Point58(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();

            try
            {
                var dataStores = new HashSet<string>();
                var controlStores = new HashSet<string>();

                // Mengumpulkan semua data store dan control store yang tersedia
                foreach (var subsystem in jsonArray)
                {
                    foreach (var item in subsystem["model"])
                    {
                        var itemType = item["typedata"]?.ToString();

                        if (itemType == "dataStore")
                        {
                            var storeName = item["store_name"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(storeName))
                            {
                                dataStores.Add(storeName);
                                Debug.WriteLine($"DataStore : {storeName}");
                            }
                        }
                        else if (itemType == "control_store")
                        {
                            var controlName = item["control_name"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(controlName))
                            {
                                controlStores.Add(controlName);
                            }
                        }
                    }
                }

                // Memeriksa setiap state untuk event data yang diperlukan
                foreach (var subsystem in jsonArray)
                {
                    foreach (var item in subsystem["model"])
                    {
                        var itemType = item["type"]?.ToString();

                        if (itemType == "class")
                        {
                            var className = item["class_name"]?.ToString();
                            var states = item["states"] as JArray;

                            if (states != null)
                            {
                                foreach (var state in states)
                                {
                                    var stateName = state["state_name"]?.ToString();
                                    var stateId = state["state_id"]?.ToString();
                                    var stateEvents = state["state_event"] as JArray;
                                    var eventData = state["event_data"] as JArray;

                                    if (stateEvents != null)
                                    {

                                        if (eventData == null)
                                        {
                                            textBox4.AppendText($"Syntax error 58: State {stateName} with id {stateId} of class {className} has events but no event data defined. \r\n");
                                            return false;
                                        }

                                        foreach (var stateEvent in stateEvents)
                                        {
                                            var eventName = stateEvent?.ToString();


                                            foreach (var data in eventData)
                                            {
                                                var dataName = data.ToString();
                                                Debug.WriteLine($"dn : {dataName}");
                                                controlStores.ToList<String>().ForEach(x => Debug.WriteLine($"dns : {x}"));

                                                if (!dataStores.First().Contains(dataName) && !controlStores.Contains(dataName))
                                                {
                                                    textBox4.AppendText($"Syntax error 58: Event data {dataName} for event {eventName} in state {stateName} of class {className} is not available on {String.Join(",", dataStores)} or on .{String.Join(",", controlStores)} \r\n");
                                                    return false;
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        textBox4.AppendText($"Syntax error 58: State {stateName} of class {className} has no events defined. 2 \r\n");
                                        return false;
                                    }
                                }
                            }
                        }
                    }
                }

                textBox4.AppendText("Success 58: All event data are available.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 58: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool Point59(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                var availableDataStores = new HashSet<string>();

                // Mengumpulkan semua data store yang tersedia
                foreach (var item in jsonArray)
                {
                    var itemType = item["type"]?.ToString();

                    if (itemType == "data_store")
                    {
                        var storeName = item["store_name"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(storeName))
                        {
                            availableDataStores.Add(storeName);
                        }
                    }
                }

                // Memeriksa setiap proses, state, dan event untuk data store yang diperlukan
                foreach (var item in jsonArray)
                {
                    var itemType = item["type"]?.ToString();

                    if (itemType == "process")
                    {
                        var dataInputs = item["data_inputs"] as JArray;
                        var dataOutputs = item["data_outputs"] as JArray;

                        CheckDataStoreList(dataInputs, "data_inputs", availableDataStores, textBox4);
                        CheckDataStoreList(dataOutputs, "data_outputs", availableDataStores, textBox4);
                    }
                    else if (itemType == "class")
                    {
                        var states = item["states"] as JArray;
                        if (states != null)
                        {
                            foreach (var state in states)
                            {
                                var stateEvents = state["state_event"] as JArray;
                                if (stateEvents != null)
                                {
                                    foreach (var stateEvent in stateEvents)
                                    {
                                        var eventData = stateEvent["event_data"] as JArray;
                                        CheckDataStoreList(eventData, "event_data", availableDataStores, textBox4);
                                    }
                                }
                            }
                        }
                    }
                }

                textBox4.AppendText("Success 59: All data stores are available.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 59: " + ex.Message + "\r\n");
                return false;
            }
        }
        private static void CheckDataStoreList(JArray dataStores, string listName, HashSet<string> availableDataStores, RichTextBox textBox4)
        {
            if (dataStores != null)
            {
                foreach (var dataStore in dataStores)
                {
                    var dataStoreName = dataStore.ToString();
                    if (!availableDataStores.Contains(dataStoreName))
                    {
                        textBox4.AppendText($"Syntax error 59: Data store {dataStoreName} in {listName} is not available. \r\n");
                    }
                }
            }
        }
        public static bool Point60(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                HashSet<string> processIDs = new HashSet<string>();
                HashSet<string> processNames = new HashSet<string>();

                foreach (var item in jsonArray)
                {
                    var itemType = item["type"]?.ToString();

                    if (itemType == "process")
                    {
                        var processID = item["process_id"]?.ToString();
                        var processName = item["process_name"]?.ToString();

                        // Check for unique process IDs
                        if (string.IsNullOrWhiteSpace(processID) || processIDs.Contains(processID))
                        {
                            textBox4.AppendText($"Syntax error 60: Process ID \"{processID}\" is not unique or empty. \r\n");
                            return false;
                        }
                        else
                        {
                            processIDs.Add(processID);
                        }

                        // Check for meaningful process names
                        if (string.IsNullOrWhiteSpace(processName))
                        {
                            textBox4.AppendText($"Syntax error 60: Process \"{processID}\" has an empty name. \r\n");
                            return false;
                        }
                        else
                        {
                            processNames.Add(processName);
                        }
                    }
                }

                textBox4.AppendText("Success 60: All process IDs and names are unique and meaningful.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 60: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool Point61(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                foreach (var subsystem in jsonArray)
                {
                    foreach (var item in subsystem["model"])
                    {
                        if (item["processes"] is JArray processes)
                        {
                            foreach (var process in processes)
                            {
                                var processType = process["process_type"]?.ToString();
                                if (string.IsNullOrWhiteSpace(processType))
                                {
                                    textBox4.AppendText("Syntax error 61: Process type is missing.\r\n");
                                    continue;
                                }

                                switch (processType)
                                {
                                    case "accessor":
                                        if (process["data_store"] == null)
                                        {
                                            textBox4.AppendText($"Syntax error 61: Accessor process {process["process_name"]} must specify a data store.\r\n");
                                        }
                                        break;
                                    case "transformation":
                                        if (process["input_data"] == null || process["output_data"] == null)
                                        {
                                            textBox4.AppendText($"Syntax error 61: Transformation process {process["process_name"]} must specify input and output data.\r\n");
                                        }
                                        break;
                                    case "event_generator":
                                        if (process["event"] == null)
                                        {
                                            textBox4.AppendText($"Syntax error 61: Event generator process {process["process_name"]} must specify an event.\r\n");
                                        }
                                        break;
                                    case "test":
                                        if (process["condition"] == null || process["true_output"] == null || process["false_output"] == null)
                                        {
                                            textBox4.AppendText($"Syntax error 61: Test process {process["process_name"]} must specify a condition and true/false outputs.\r\n");
                                        }
                                        break;
                                    default:
                                        textBox4.AppendText($"Syntax error 61: Unknown process type {processType} in process {process["process_name"]}.\r\n");
                                        break;
                                }
                            }
                        }
                    }
                }
                textBox4.AppendText("Success 61: All processes checked successfully. No syntax errors found.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 61: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool Point62(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                foreach (var subsystem in jsonArray)
                {
                    if (subsystem["model"] is JArray model)
                    {
                        var processDict = new Dictionary<string, JObject>();

                        foreach (var item in model)
                        {
                            if (item["processes"] is JArray processes)
                            {
                                foreach (var process in processes)
                                {
                                    var processType = process["process_type"]?.ToString();
                                    var dataStore = process["data_store"]?.ToString();
                                    var inputData = process["input_data"]?.ToString();
                                    var outputData = process["output_data"]?.ToString();
                                    var eventGenerated = process["event"]?.ToString();
                                    var condition = process["condition"]?.ToString();
                                    var trueOutput = process["true_output"]?.ToString();
                                    var falseOutput = process["false_output"]?.ToString();

                                    var processKey = $"{processType}-{dataStore}-{inputData}-{outputData}-{eventGenerated}-{condition}-{trueOutput}-{falseOutput}";

                                    if (processDict.ContainsKey(processKey))
                                    {
                                        var existingProcess = processDict[processKey];
                                        var existingProcessId = existingProcess["process_id"]?.ToString();
                                        var existingProcessName = existingProcess["process_name"]?.ToString();
                                        var currentProcessId = process["process_id"]?.ToString();
                                        var currentProcessName = process["process_name"]?.ToString();

                                        if (existingProcessId != currentProcessId || existingProcessName != currentProcessName)
                                        {
                                            textBox4.AppendText($"Syntax error 62: Duplicate process with different identifiers or names. Existing: {existingProcessName} ({existingProcessId}), Current: {currentProcessName} ({currentProcessId})\r\n");
                                        }
                                    }
                                    else
                                    {
                                        processDict[processKey] = (JObject)process;
                                    }
                                }
                            }
                        }
                    }
                }

                // Menambahkan pesan "Success" jika tidak ada kesalahan
                textBox4.AppendText("Success 62: No duplicate processes found.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 62: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool Point63(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                var keyLetterDict = new Dictionary<string, HashSet<int>>();

                foreach (var subsystem in jsonArray)
                {
                    if (subsystem["model"] is JArray model)
                    {
                        foreach (var item in model)
                        {
                            if (item["processes"] is JArray processes)
                            {
                                foreach (var process in processes)
                                {
                                    var processId = process["process_id"]?.ToString();
                                    if (string.IsNullOrWhiteSpace(processId))
                                    {
                                        textBox4.AppendText("Syntax error 63: Process identifier is missing.\r\n");
                                        continue;
                                    }

                                    // Regex to match process identifier format KL.i
                                    var match = Regex.Match(processId, @"^(?<KL>[A-Za-z]+)\.(?<i>\d+)$");
                                    if (!match.Success)
                                    {
                                        textBox4.AppendText($"Syntax error 63: Process identifier {processId} does not match the format KL.i.\r\n");
                                        continue;
                                    }

                                    var keyLetter = match.Groups["KL"].Value;
                                    var index = int.Parse(match.Groups["i"].Value);

                                    if (!keyLetterDict.ContainsKey(keyLetter))
                                    {
                                        keyLetterDict[keyLetter] = new HashSet<int>();
                                    }

                                    if (keyLetterDict[keyLetter].Contains(index))
                                    {
                                        textBox4.AppendText($"Syntax error 63: Process identifier {processId} has a duplicate integer for KeyLetter {keyLetter}.\r\n");
                                    }
                                    else
                                    {
                                        keyLetterDict[keyLetter].Add(index);
                                    }
                                }
                            }
                        }
                    }
                }

                // Menambahkan pesan "Success" jika tidak ada kesalahan
                textBox4.AppendText("Success 63: All process identifiers are valid.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 63: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool Point64(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                var dataStoreToKeyLetter = new Dictionary<string, string>();

                // Map data stores to KeyLetters
                foreach (var subsystem in jsonArray)
                {
                    if (subsystem["model"] is JArray model)
                    {
                        foreach (var item in model)
                        {
                            var dataStore = item["data_store"]?.ToString();
                            var keyLetter = item["KL"]?.ToString();

                            if (!string.IsNullOrWhiteSpace(dataStore) && !string.IsNullOrWhiteSpace(keyLetter))
                            {
                                dataStoreToKeyLetter[dataStore] = keyLetter;
                            }
                        }
                    }
                }

                // Check each accessor process
                foreach (var subsystem in jsonArray)
                {
                    if (subsystem["model"] is JArray model)
                    {
                        foreach (var item in model)
                        {
                            if (item["processes"] is JArray processes)
                            {
                                foreach (var process in processes)
                                {
                                    var processType = process["process_type"]?.ToString();
                                    if (processType == "accessor")
                                    {
                                        var processId = process["process_id"]?.ToString();
                                        var dataStore = process["data_store"]?.ToString();

                                        if (string.IsNullOrWhiteSpace(processId) || string.IsNullOrWhiteSpace(dataStore))
                                        {
                                            textBox4.AppendText("Syntax error 64: Accessor process identifier or data store is missing.\r\n");
                                            continue;
                                        }

                                        var match = Regex.Match(processId, @"^(?<KL>[A-Za-z]+)\.\d+$");
                                        if (!match.Success)
                                        {
                                            textBox4.AppendText($"Syntax error 64: Process identifier {processId} does not match the format KL.i.\r\n");
                                            continue;
                                        }

                                        var processKeyLetter = match.Groups["KL"].Value;

                                        if (dataStoreToKeyLetter.TryGetValue(dataStore, out var expectedKeyLetter))
                                        {
                                            if (processKeyLetter != expectedKeyLetter)
                                            {
                                                textBox4.AppendText($"Syntax error 64: Accessor process {processId} has KeyLetter {processKeyLetter} which does not match the expected KeyLetter {expectedKeyLetter} for data store {dataStore}.\r\n");
                                            }
                                        }
                                        else
                                        {
                                            textBox4.AppendText($"Syntax error 64: Data store {dataStore} is not mapped to any KeyLetter.\r\n");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Menambahkan pesan "Success" jika tidak ada kesalahan
                textBox4.AppendText("Success 64: All accessor processes have valid KeyLetters.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 64: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool Point65(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                foreach (var subsystem in jsonArray)
                {
                    if (subsystem["model"] is JArray model)
                    {
                        foreach (var item in model)
                        {
                            if (item["processes"] is JArray processes)
                            {
                                foreach (var process in processes)
                                {
                                    var processType = process["process_type"]?.ToString();
                                    var dataStore = process["data_store"]?.ToString();
                                    var processId = process["process_id"]?.ToString();
                                    var processName = process["process_name"]?.ToString();

                                    // Check if it's an accessor for timer data store
                                    if (processType == "accessor" && dataStore == "timer")
                                    {
                                        if (processId == "Tim.3" && processName != "createTimer")
                                        {
                                            textBox4.AppendText("Syntax error 65: Accessor Tim.3 should be named createTimer.\r\n");
                                        }
                                        else if (processId == "Tim.4" && processName != "deleteTimer")
                                        {
                                            textBox4.AppendText("Syntax error 65: Accessor Tim.4 should be named deleteTimer.\r\n");
                                        }
                                        else if (processId == "Tim.5" && processName != "getRemainingTime")
                                        {
                                            textBox4.AppendText("Syntax error 65: Accessor Tim.5 should be named getRemainingTime.\r\n");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Menambahkan pesan "Success" jika tidak ada kesalahan
                textBox4.AppendText("Success 65: All timer accessor names are correct.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 65: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool Point66(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                var stateModelToKeyLetter = new Dictionary<string, string>();
                var dataStoreToKeyLetter = new Dictionary<string, string>();

                // Map state models to KeyLetters
                foreach (var subsystem in jsonArray)
                {
                    if (subsystem["model"] is JArray model)
                    {
                        foreach (var item in model)
                        {
                            var itemType = item["type"]?.ToString();
                            var classId = item["class_id"]?.ToString();
                            var keyLetter = item["KL"]?.ToString();

                            if (itemType == "class" && !string.IsNullOrWhiteSpace(classId) && !string.IsNullOrWhiteSpace(keyLetter))
                            {
                                stateModelToKeyLetter[classId] = keyLetter;

                                // If the class has a data store, map it to KeyLetter as well
                                var dataStore = item["data_store"]?.ToString();
                                if (!string.IsNullOrWhiteSpace(dataStore))
                                {
                                    dataStoreToKeyLetter[dataStore] = keyLetter;
                                }
                            }
                        }
                    }
                }

                // Check each transformation process
                foreach (var subsystem in jsonArray)
                {
                    if (subsystem["model"] is JArray model)
                    {
                        foreach (var item in model)
                        {
                            if (item["processes"] is JArray processes)
                            {
                                foreach (var process in processes)
                                {
                                    var processType = process["process_type"]?.ToString();
                                    var processId = process["process_id"]?.ToString();
                                    var processName = process["process_name"]?.ToString();

                                    // Check if it's a transformation process
                                    if (processType == "transformation")
                                    {
                                        var stateModelId = process["state_model_id"]?.ToString();
                                        if (string.IsNullOrWhiteSpace(stateModelId))
                                        {
                                            textBox4.AppendText($"Syntax error 66: Transformation process {processId} is missing state_model_id.\r\n");
                                            continue;
                                        }

                                        if (!stateModelToKeyLetter.TryGetValue(stateModelId, out var expectedKeyLetter))
                                        {
                                            textBox4.AppendText($"Syntax error 66: State model with ID {stateModelId} is not mapped to any KeyLetter.\r\n");
                                            continue;
                                        }

                                        var processKeyLetter = processId?.Split('.')[0];
                                        if (processKeyLetter != expectedKeyLetter)
                                        {
                                            textBox4.AppendText($"Syntax error 66: Transformation process {processId} has KeyLetter {processKeyLetter} which does not match the expected KeyLetter {expectedKeyLetter} for state model {stateModelId}.\r\n");
                                        }

                                        // Check if it accesses data store
                                        var dataStore = process["data_store"]?.ToString();
                                        if (!string.IsNullOrWhiteSpace(dataStore))
                                        {
                                            if (dataStoreToKeyLetter.TryGetValue(dataStore, out var dataStoreKeyLetter) && dataStoreKeyLetter != expectedKeyLetter)
                                            {
                                                textBox4.AppendText($"Syntax error 66: Transformation process {processId} accesses data store {dataStore} with KeyLetter {dataStoreKeyLetter} which does not match the expected KeyLetter {expectedKeyLetter} for state model {stateModelId}.\r\n");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                textBox4.AppendText("Success 66: All transformation processes have correct KeyLetters.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 66: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool Point67(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                var objectToKeyLetter = new Dictionary<string, string>();

                // Map objects and external entities to KeyLetters
                foreach (var subsystem in jsonArray)
                {
                    if (subsystem["model"] is JArray model)
                    {
                        foreach (var item in model)
                        {
                            var itemType = item["type"]?.ToString();
                            var objectId = item["class_id"]?.ToString();
                            var keyLetter = item["KL"]?.ToString();

                            if (itemType == "class" && !string.IsNullOrWhiteSpace(objectId) && !string.IsNullOrWhiteSpace(keyLetter))
                            {
                                objectToKeyLetter[objectId] = keyLetter;
                            }
                        }
                    }
                }

                // Check each event generator process
                foreach (var subsystem in jsonArray)
                {
                    if (subsystem["model"] is JArray model)
                    {
                        foreach (var item in model)
                        {
                            if (item["processes"] is JArray processes)
                            {
                                foreach (var process in processes)
                                {
                                    var processType = process["process_type"]?.ToString();
                                    var processId = process["process_id"]?.ToString();
                                    var processName = process["process_name"]?.ToString();

                                    // Check if it's an event generator
                                    if (processType == "event_generator")
                                    {
                                        var targetObjectId = process["class_id"]?.ToString();
                                        if (string.IsNullOrWhiteSpace(targetObjectId))
                                        {
                                            textBox4.AppendText($"Syntax error 67: Event generator process {processId} is missing class_id.\r\n");
                                            continue;
                                        }

                                        if (!objectToKeyLetter.TryGetValue(targetObjectId, out var expectedKeyLetter))
                                        {
                                            textBox4.AppendText($"Syntax error 67: Object or external entity with ID {targetObjectId} is not mapped to any KeyLetter.\r\n");
                                            continue;
                                        }

                                        var processKeyLetter = processId?.Split('.')[0];
                                        if (processKeyLetter != expectedKeyLetter)
                                        {
                                            textBox4.AppendText($"Syntax error 67: Event generator process {processId} has KeyLetter {processKeyLetter} which does not match the expected KeyLetter {expectedKeyLetter} for object or external entity {targetObjectId}.\r\n");
                                        }

                                        // Check if it accesses data store
                                        var dataStore = process["data_store"]?.ToString();
                                        if (!string.IsNullOrWhiteSpace(dataStore))
                                        {
                                            textBox4.AppendText($"Syntax error 67: Event generator process {processId} should not access any data stores, but it accesses data store {dataStore}.\r\n");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Menambahkan pesan "Success" jika tidak ada kesalahan
                textBox4.AppendText("Success 67: All event generator processes have correct KeyLetters.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 67: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool Point68(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                bool hasError = false;

                foreach (var subsystem in jsonArray)
                {
                    if (subsystem["model"] is JArray model)
                    {
                        foreach (var item in model)
                        {
                            if (item["processes"] is JArray processes)
                            {
                                foreach (var process in processes)
                                {
                                    var processType = process["process_type"]?.ToString();
                                    var processId = process["process_id"]?.ToString();
                                    var dataStore = process["data_store"]?.ToString();

                                    // Check if it's an event generator involving TIMER data store
                                    if (processType == "event_generator" && dataStore == "TIMER")
                                    {
                                        if (processId == "TIM.1")
                                        {
                                            if (process["process_name"]?.ToString() != "setTimer")
                                            {
                                                textBox4.AppendText($"Syntax error 68: Event generator process {processId} should be named 'setTimer'.\r\n");
                                                hasError = true;
                                            }
                                        }
                                        else if (processId == "TIM.2")
                                        {
                                            if (process["process_name"]?.ToString() != "resetTimer")
                                            {
                                                textBox4.AppendText($"Syntax error 68: Event generator process {processId} should be named 'resetTimer'.\r\n");
                                                hasError = true;
                                            }
                                        }
                                        else
                                        {
                                            textBox4.AppendText($"Syntax error 68: Invalid event generator process ID {processId} for TIMER data store. Expected 'TIM.1' or 'TIM.2'.\r\n");
                                            hasError = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (!hasError)
                {
                    textBox4.AppendText("Success 68: All timer event generator processes have correct names.\r\n");
                }

                return !hasError;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 68: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool Point69(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                var stateModelToKeyLetter = new Dictionary<string, string>();
                var dataStoreToKeyLetter = new Dictionary<string, string>();
                bool hasError = false;

                // Map state models to KeyLetters
                foreach (var subsystem in jsonArray)
                {
                    if (subsystem["model"] is JArray model)
                    {
                        foreach (var item in model)
                        {
                            var itemType = item["type"]?.ToString();
                            var classId = item["class_id"]?.ToString();
                            var keyLetter = item["KL"]?.ToString();

                            if (itemType == "class" && !string.IsNullOrWhiteSpace(classId) && !string.IsNullOrWhiteSpace(keyLetter))
                            {
                                stateModelToKeyLetter[classId] = keyLetter;

                                // If the class has a data store, map it to KeyLetter as well
                                var dataStore = item["data_store"]?.ToString();
                                if (!string.IsNullOrWhiteSpace(dataStore))
                                {
                                    dataStoreToKeyLetter[dataStore] = keyLetter;
                                }
                            }
                        }
                    }
                }

                // Check each test process
                foreach (var subsystem in jsonArray)
                {
                    if (subsystem["model"] is JArray model)
                    {
                        foreach (var item in model)
                        {
                            if (item["processes"] is JArray processes)
                            {
                                foreach (var process in processes)
                                {
                                    var processType = process["process_type"]?.ToString();
                                    var processId = process["process_id"]?.ToString();

                                    // Check if it's a test process
                                    if (processType == "test")
                                    {
                                        var stateModelId = process["state_model_id"]?.ToString();
                                        if (string.IsNullOrWhiteSpace(stateModelId))
                                        {
                                            textBox4.AppendText($"Syntax error 69: Test process {processId} is missing state_model_id.\r\n");
                                            hasError = true;
                                            continue;
                                        }

                                        if (!stateModelToKeyLetter.TryGetValue(stateModelId, out var expectedKeyLetter))
                                        {
                                            textBox4.AppendText($"Syntax error 69: State model with ID {stateModelId} is not mapped to any KeyLetter.\r\n");
                                            hasError = true;
                                            continue;
                                        }

                                        var processKeyLetter = processId?.Split('.')[0];
                                        if (processKeyLetter != expectedKeyLetter)
                                        {
                                            textBox4.AppendText($"Syntax error 69: Test process {processId} has KeyLetter {processKeyLetter} which does not match the expected KeyLetter {expectedKeyLetter} for state model {stateModelId}.\r\n");
                                            hasError = true;
                                        }

                                        // Check if it accesses data store
                                        var dataStore = process["data_store"]?.ToString();
                                        if (!string.IsNullOrWhiteSpace(dataStore) && dataStoreToKeyLetter.TryGetValue(dataStore, out var dataStoreKeyLetter))
                                        {
                                            if (dataStoreKeyLetter != expectedKeyLetter)
                                            {
                                                textBox4.AppendText($"Syntax error 69: Test process {processId} accesses data store {dataStore} with KeyLetter {dataStoreKeyLetter} which does not match the expected KeyLetter {expectedKeyLetter} for state model {stateModelId}.\r\n");
                                                hasError = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (!hasError)
                {
                    textBox4.AppendText("Success 69: All test processes have correct KeyLetters.\r\n");
                }

                return !hasError;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 69: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool Point70(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                var objectToKeyLetter = new Dictionary<string, string>();
                var dataStoreToObject = new Dictionary<string, string>();
                var objectAccessModel = new List<Tuple<string, string, string>>(); // Tuple<SourceObject, TargetObject, ProcessId>

                // Map objects to KeyLetters and data stores to objects
                foreach (var subsystem in jsonArray)
                {
                    if (subsystem["model"] is JArray model)
                    {
                        foreach (var item in model)
                        {
                            var itemType = item["type"]?.ToString();
                            var objectId = item["class_id"]?.ToString();
                            var keyLetter = item["KL"]?.ToString();
                            var dataStore = item["data_store"]?.ToString();

                            if (itemType == "class" && !string.IsNullOrWhiteSpace(objectId) && !string.IsNullOrWhiteSpace(keyLetter))
                            {
                                objectToKeyLetter[objectId] = keyLetter;

                                if (!string.IsNullOrWhiteSpace(dataStore))
                                {
                                    dataStoreToObject[dataStore] = objectId;
                                }
                            }
                        }
                    }
                }

                // Check each process in state models
                foreach (var subsystem in jsonArray)
                {
                    if (subsystem["model"] is JArray model)
                    {
                        foreach (var item in model)
                        {
                            if (item["state_diagram"] is JArray stateDiagram)
                            {
                                foreach (var state in stateDiagram)
                                {
                                    if (state["actions"] is JArray actions)
                                    {
                                        foreach (var action in actions)
                                        {
                                            var actionType = action["action_type"]?.ToString();
                                            var actionId = action["action_id"]?.ToString();
                                            var processId = action["process_id"]?.ToString();
                                            var dataStore = action["data_store"]?.ToString();

                                            if (actionType == "access" && !string.IsNullOrWhiteSpace(dataStore))
                                            {
                                                if (dataStoreToObject.TryGetValue(dataStore, out var targetObjectId))
                                                {
                                                    var sourceObjectId = action["class_id"]?.ToString();

                                                    if (!string.IsNullOrWhiteSpace(sourceObjectId) && !string.IsNullOrWhiteSpace(targetObjectId))
                                                    {
                                                        objectAccessModel.Add(new Tuple<string, string, string>(sourceObjectId, targetObjectId, processId));
                                                    }
                                                    else
                                                    {
                                                        textBox4.AppendText($"Syntax error 70: Missing source or target object ID for action {actionId} accessing data store {dataStore}.\r\n");
                                                    }
                                                }
                                                else
                                                {
                                                    textBox4.AppendText($"Syntax error 70: Data store {dataStore} is not mapped to any object.\r\n");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Validate object access model
                foreach (var access in objectAccessModel)
                {
                    var sourceObject = access.Item1;
                    var targetObject = access.Item2;
                    var processId = access.Item3;

                    if (objectToKeyLetter.TryGetValue(sourceObject, out var sourceKeyLetter) &&
                        objectToKeyLetter.TryGetValue(targetObject, out var targetKeyLetter))
                    {
                        var expectedProcessId = $"{sourceKeyLetter}.{processId.Split('.')[1]}";

                        if (processId != expectedProcessId)
                        {
                            textBox4.AppendText($"Syntax error 70: Process ID {processId} for action accessing from {sourceKeyLetter} to {targetKeyLetter} should be {expectedProcessId}.\r\n");
                            return false;

                        }
                    }
                    else
                    {
                        textBox4.AppendText($"Syntax error 70: Source or target object not found in key letter mapping.\r\n");
                        return false;

                    }
                }
                textBox4.AppendText("Success 70: All access actions have correct process IDs.\r\n");
                return true;

            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 70: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool Point71(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();   
            try
            {
                HashSet<string> domainSubsystems = new HashSet<string>();
                HashSet<string> communicationModelSubsystems = new HashSet<string>();

                // Identify all subsystems in the domain
                foreach (var subsystem in jsonArray)
                {
                    var subName = subsystem["sub_name"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(subName))
                    {
                        domainSubsystems.Add(subName);
                    }
                }

                // Find communication model and check subsystems
                var communicationModel = jsonArray[0]["model"].FirstOrDefault(x => x["type"]?.ToString() == "subsystem_communication_model");
                if (communicationModel != null)
                {
                    Debug.WriteLine(communicationModel.ToString()); // Tanda 1: Menambahkan pencarian untuk communication_model
                    var subsystemsArray = communicationModel["subsystems"] as JArray; // Tanda 2: Mengambil array subsystems dari communication_model
                    if (subsystemsArray != null)
                    {
                        foreach (var subsystem in subsystemsArray)
                        {
                            var communicationModelSubName = subsystem["sub_name"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(communicationModelSubName))
                            {
                                communicationModelSubsystems.Add(communicationModelSubName);
                            }
                        }
                    }
                }

                // Check if all domain subsystems appear in the Subsystem Communication Model
                foreach (var subName in domainSubsystems)
                {
                    if (!communicationModelSubsystems.Contains(subName))
                    {
                        textBox4.AppendText($"Syntax error 71: Subsystem '{subName}' does not appear in the Subsystem Communication Model.\r\n");
                        return false;
                    }
                }

                textBox4.AppendText("Success 71: All domain subsystems appear in the Subsystem Communication Model.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 71: " + ex.Message + "\r\n");
                return false;
            }
        }
        public static bool Point72(Form1 form1, JArray jsonArray)
        {
            RichTextBox textBox4 = form1.GetMessageBox();
            try
            {
                Dictionary<string, HashSet<string>> subsystemGeneratedEvents = new Dictionary<string, HashSet<string>>();
                Dictionary<string, HashSet<string>> subsystemReceivedEvents = new Dictionary<string, HashSet<string>>();

                // Identify events generated and received by state models in each subsystem
                foreach (var subsystem in jsonArray)
                {
                    var subName = subsystem["sub_name"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(subName))
                    {
                        var generatedEvents = new HashSet<string>();
                        var receivedEvents = new HashSet<string>();

                        if (subsystem["model"] is JArray model)
                        {
                            foreach (var item in model)
                            {
                                if (item["state_diagram"] is JArray stateDiagram)
                                {
                                    foreach (var state in stateDiagram)
                                    {
                                        if (state["actions"] is JArray actions)
                                        {
                                            foreach (var action in actions)
                                            {
                                                var actionType = action["action_type"]?.ToString();
                                                var eventName = action["event_name"]?.ToString();

                                                if (actionType == "generate" && !string.IsNullOrWhiteSpace(eventName))
                                                {
                                                    generatedEvents.Add(eventName);
                                                }
                                                else if (actionType == "receive" && !string.IsNullOrWhiteSpace(eventName))
                                                {
                                                    receivedEvents.Add(eventName);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        subsystemGeneratedEvents[subName] = generatedEvents;
                        subsystemReceivedEvents[subName] = receivedEvents;
                    }
                }

                // Check if events generated and received between subsystems appear in the Subsystem Communication Model
                foreach (var senderEntry in subsystemGeneratedEvents)
                {
                    var senderSub = senderEntry.Key;
                    var senderEvents = senderEntry.Value;

                    foreach (var receiverEntry in subsystemReceivedEvents)
                    {
                        var receiverSub = receiverEntry.Key;
                        var receiverEvents = receiverEntry.Value;

                        if (senderSub != receiverSub)
                        {
                            foreach (var evt in senderEvents)
                            {
                                if (receiverEvents.Contains(evt))
                                {
                                    textBox4.AppendText($"Syntax error 72: Event '{evt}' generated by state model in '{senderSub}' and received by state model in '{receiverSub}' must appear in the Subsystem Communication Model.\r\n");
                                    return false;
                                }
                            }
                        }
                    }
                }
                textBox4.AppendText("Success 72: All events are correctly placed in the Subsystem Communication Model.\r\n");
                return true;
            }
            catch (Exception ex)
            {
                textBox4.AppendText("Syntax error 72: " + ex.Message + "\r\n");
                return false;
            }

        }



    }
}
