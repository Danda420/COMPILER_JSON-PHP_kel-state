using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Xml.Linq;  
using System.Runtime.InteropServices;
using static System.Windows.Forms.AxHost;

namespace xtUML1
{
    public partial class Form1 : Form
    {
        private OpenFileDialog openFileDialog;
        private SaveFileDialog saveFileDialog;
        private string selectedFilePath;
        private string[] fileNames;
        private bool isJsonFileSelected = false;
        private Translate translator;
        private bool parsed = false; 
        public Form1()
        {
            InitializeComponent();

            openFileDialog = new OpenFileDialog();
            saveFileDialog = new SaveFileDialog();
            translator = new Translate();
        }

        private void btnSelect_Click_1(object sender, EventArgs e)
        {
            // Menampilkan dialog untuk memilih file JSON
            openFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
            openFileDialog.Title = "Select JSON File";
            DialogResult result = openFileDialog.ShowDialog();

            if (result == DialogResult.OK)
            {

                // ubah semua sesuai kebutuhan

                string jsonContent = File.ReadAllText(openFileDialog.FileName);

                dynamic jsonObj = JObject.Parse(jsonContent);

                selectedFilePath = openFileDialog.FileName;

                textBox1.Text = selectedFilePath;

                // tampilkan sintax error parsing di textBox4 jika tidak lolos parsing

                /*textBox4.Text = jsonContent;*/ // untuk menampilkan isi file json (setelah lolos parsing)

                isJsonFileSelected = true;

                string folderPath = Path.GetDirectoryName(openFileDialog.FileName);

                ProcessFilesInFolder(folderPath);
            }
        }
        private void ProcessFilesInFolder(string folderPath)
        {
            try
            {
                this.fileNames = Directory.GetFiles(folderPath, "*.json");

                if (this.fileNames.Length == 0)
                {
                    MessageBox.Show("No JSON files found in this folder.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
                this.ProcessJson(this.fileNames);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        public RichTextBox GetMessageBox()
        {
            return textBox4;
        }
        public void HandleError(string errorMessage)
        {
            textBox4.Text += $"{errorMessage}{Environment.NewLine}";
            Console.WriteLine(errorMessage);
        }
        private JArray ProcessJson(string[] fileNames)
        {
            List<string> jsonArrayList = new List<string>();

            foreach (var fileName in fileNames)
            {
                try
                {
                    string jsonContent = File.ReadAllText(fileName);
                    jsonArrayList.Add(jsonContent);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error reading the file {Path.GetFileName(fileName)}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                }
            }

            JArray jsonArray = new JArray(jsonArrayList.Select(JToken.Parse));

            textBox4.Text = jsonArray.ToString();
            return jsonArray;
        }
        private void btnParse_Click(object sender, EventArgs e)
        {
            // parsing
            // tampilkan sintax error parsing di textBox4 jika tidak lolos parsing
            // tampilkan isi file json di textBox4 jika lolos parsing

            if (fileNames == null || fileNames.Length == 0)
            {
                MessageBox.Show("Please select a folder containing JSON files first.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);

                return;
            }
            JArray jsonArray = this.ProcessJson(fileNames);
            string isiFileJson = textBox4.Text;

            textBox4.Clear();
            Parsing.Point1(this, jsonArray);
            Parsing.Point2(this, jsonArray);
            Parsing.Point3(this, jsonArray);
            Parsing.Point4(this, jsonArray);
            Parsing.Point5(this, jsonArray);
            Parsing.Point6(this, jsonArray);
            Parsing.Point7(this, jsonArray);
            Parsing.Point8(this, jsonArray);
            Parsing.Point9(this, jsonArray);
            Parsing.Point10(this, jsonArray);
            Parsing.Point11(this, jsonArray);
            Parsing.Point13(this, jsonArray);
            Parsing.Point14(this, jsonArray);
            Parsing.Point15(this, jsonArray);

            btnCheck_Click1(sender, e);

            Parsing.Point25(this, jsonArray);
            Parsing.Point27(this, jsonArray);
            Parsing.Point28(this, jsonArray);
            Parsing.Point29(this, jsonArray);
            Parsing.Point30(this, jsonArray);
            Parsing.Point34(this, jsonArray);
            Parsing.Point35(this, jsonArray);
            Parsing.Point99(this, jsonArray);

            if (string.IsNullOrWhiteSpace(textBox4.Text))
            {
                MessageBox.Show("Model has successfully passed parsing", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                parsed = true;

                textBox4.Text = isiFileJson;
            }
            else
            {
                parsed = false;
            }
        }


        private void btnTranslate_Click(object sender, EventArgs e)
        {
            // translate jika lolos parsing
            // tampilkan hasil translate di textBox3

            // Translate if JSON file is selected
            if (!isJsonFileSelected)
            {
                MessageBox.Show("Select JSON file as an input first!", "Alert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Check if JSON content is available
            if (string.IsNullOrWhiteSpace(textBox4.Text))
            {
                MessageBox.Show("No JSON content available for translation.", "Alert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Call the method in Translate to generate PHP codes
            string phpCode = translator.GeneratePHPCode(selectedFilePath);


            // Display the generated PHP code in textBox3
            if (parsed == true)
            {
                textBox3.Text = phpCode;
            }
            else
            {
                MessageBox.Show("JSON model is not successfully parsed", "Alert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            
        }


        private void button3_Click(object sender, EventArgs e)
        {
            // tulis method untuk menghapus nilai textBox1 (selected file)
            textBox1.Clear();
            textBox3.Clear();
            textBox4.Clear();
            isJsonFileSelected = false; // Reset the flag indicating whether a JSON file is selected
            selectedFilePath = null; // Reset the selected file path
            fileNames = null;
            parsed = true; // Reset the parsed variable
        }

        private void btnHelp_Click(object sender, EventArgs e)
        {
            string helpMessage = OpenHelp();
            MessageBox.Show(helpMessage, "Help", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private string OpenHelp()
        {
            StringBuilder helpMessage = new StringBuilder();

            helpMessage.AppendLine("This is xtUML Model Compiler from xtUML JSON Model to PHP");
            helpMessage.AppendLine();
            helpMessage.AppendLine("1. Click 'Select File' to select a JSON formatted file as an input.");
            helpMessage.AppendLine();
            helpMessage.AppendLine("2. The application will automatically read the content of selected file and display the results in the JSON column.");
            helpMessage.AppendLine();
            helpMessage.AppendLine("3. Click 'Parse' to parse the input of xtUML JSON Model in order to meet xtUML standard rules, if the input does not meet the rules then there will be an alert. The input can not be visualized, translated, or simulated if it does not meet the rules.");
            helpMessage.AppendLine();
            helpMessage.AppendLine("4. Click 'Visualize' to visualize the xtUML Model into diagram model.");
            helpMessage.AppendLine();
            helpMessage.AppendLine("5. Click 'Translate' to translate the selected file into PHP code.");
            helpMessage.AppendLine();
            helpMessage.AppendLine("6. Click 'Simulate' to simulate PHP code as a program.");
            helpMessage.AppendLine();
            helpMessage.AppendLine("7. Click 'Copy' to copy the PHP code.");
            helpMessage.AppendLine();
            helpMessage.AppendLine("8. Click 'Save' to save the PHP code as an output into a php formatted file.");
            helpMessage.AppendLine();
            helpMessage.AppendLine("9. Click 'Reset' to clear the displayed data and selected file.");

            return helpMessage.ToString();
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            if (textBox3.TextLength > 0)
            {
                textBox3.SelectAll();
                textBox3.Copy();
                MessageBox.Show("Successfully Copied!", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            else
            {
                MessageBox.Show("Please Translate First!", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (!isJsonFileSelected)
            {
                MessageBox.Show("Select JSON file as an input first!", "Alert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (textBox3.TextLength > 0)
            {
                saveFileDialog.Filter = "C++ files (*.cpp)|*.cpp|All files (*.*)|*.*"; // ubah ekstensi output save file
                saveFileDialog.Title = "Save C++ File"; // ubah C++

                DialogResult result = saveFileDialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    string cppCode = textBox3.Text;

                    File.WriteAllText(saveFileDialog.FileName, cppCode);

                    selectedFilePath = saveFileDialog.FileName;

                    MessageBox.Show("Successfully Saved!", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }
            else
            {
                MessageBox.Show("Please Translate First!", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

        private void btnVisualize_Click(object sender, EventArgs e)
        {
            MessageBox.Show("We are sorry, this feature is not available right now.", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        private void btnSimulate_Click(object sender, EventArgs e)
        {
            MessageBox.Show("We are sorry, this feature is not available right now.", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        private void btnCheck_Click1(object sender, EventArgs e)
        {
            try
            {
                if (fileNames == null || fileNames.Length == 0)
                {
                    MessageBox.Show("Please select a folder containing JSON files first.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    return;
                }

                foreach (var fileName in this.fileNames)
                {
                    string jsonContent = File.ReadAllText(fileName);
                    CheckJsonCompliance(jsonContent);
                }
            }
            catch (Exception ex)
            {
                HandleError($"Error: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void CheckJsonCompliance(string jsonContent)
        {
            try
            {
                JObject jsonObj = JObject.Parse(jsonContent);

                // Dictionary to store state model information
                Dictionary<string, string> stateModels = new Dictionary<string, string>();
                HashSet<string> usedKeyLetters = new HashSet<string>();
                HashSet<int> stateNumbers = new HashSet<int>();

                JToken subsystemsToken = jsonObj["subsystems"];
                if (subsystemsToken != null && subsystemsToken.Type == JTokenType.Array)
                {
                    // Iterasi untuk setiap subsystem dalam subsystemsToken
                    foreach (var subsystem in subsystemsToken)
                    {
                        JToken modelToken = subsystem["model"];
                        if (modelToken != null && modelToken.Type == JTokenType.Array)
                        {
                            foreach (var model in modelToken)
                            {
                                ValidateClassModel(model, stateModels, usedKeyLetters, stateNumbers);
                            }
                        }
                    }

                    // Setelah memvalidasi semua model, panggil ValidateEventDirectedToStateModelHelper untuk setiap subsystem
                    foreach (var subsystem in subsystemsToken)
                    {
                        ValidateEventDirectedToStateModelHelper(subsystem["model"], stateModels, null);
                    }
                }

                ValidateTimerModel(jsonObj, usedKeyLetters);
            }
            catch (Exception ex)
            {
                HandleError($"Error: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ValidateClassModel(JToken model, Dictionary<string, string> stateModels, HashSet<string> usedKeyLetters, HashSet<int> stateNumbers)
        {
            string objectType = model["type"]?.ToString();
            string objectName = model["class_name"]?.ToString();
            Console.WriteLine($"Running CheckKeyLetterUniqueness for {objectName}");

            if (objectType == "class")
            {
                Console.WriteLine($"Checking class: {objectName}");

                string assignerStateModelName = $"{objectName}_ASSIGNER";
                JToken assignerStateModelToken = model[assignerStateModelName];

                if (assignerStateModelToken == null || assignerStateModelToken.Type != JTokenType.Object)
                {
                    HandleError($"Syntax error 16: Assigner state model not found for {objectName}.");
                    return;
                }

                string keyLetter = model["KL"]?.ToString();

                // Pemanggilan CheckKeyLetterUniqueness
                CheckKeyLetterUniqueness(usedKeyLetters, keyLetter, objectName);

                // Check if KeyLetter is correct
                JToken keyLetterToken = assignerStateModelToken?["KeyLetter"];
                if (keyLetterToken != null && keyLetterToken.ToString() != keyLetter)
                {
                    HandleError($"Syntax error 17: KeyLetter for {objectName} does not match the rules.");
                }

                // Check uniqueness of states
                CheckStateUniqueness(stateModels, assignerStateModelToken?["states"], objectName, assignerStateModelName);

                // Check uniqueness of state numbers
                CheckStateNumberUniqueness(stateNumbers, assignerStateModelToken?["states"], objectName);

                // Store state model information
                string stateModelKey = $"{objectName}.{assignerStateModelName}";
                stateModels[stateModelKey] = objectName;
            }
        }

        private void CheckStateUniqueness(Dictionary<string, string> stateModels, JToken statesToken, string objectName, string assignerStateModelName)
        {
            if (statesToken is JArray states)
            {
                HashSet<string> uniqueStates = new HashSet<string>();

                foreach (var state in states)
                {
                    string stateName = state["state_name"]?.ToString();
                    string stateModelName = $"{objectName}.{stateName}";

                    // Check uniqueness of state model
                    if (!uniqueStates.Add(stateModelName))
                    {
                        HandleError($"Syntax error 18: State {stateModelName} is not unique in {assignerStateModelName}.");
                    }
                }
            }
        }

        private void CheckStateNumberUniqueness(HashSet<int> stateNumbers, JToken statesToken, string objectName)
        {
            if (statesToken is JArray stateArray)
            {
                foreach (var state in stateArray)
                {
                    int stateNumber = state["state_number"]?.ToObject<int>() ?? 0;

                    if (!stateNumbers.Add(stateNumber))
                    {
                        HandleError($"Syntax error 19: State number {stateNumber} is not unique.");
                    }
                }
            }
        }

        private void CheckKeyLetterUniqueness(HashSet<string> usedKeyLetters, string keyLetter, string objectName)
        {
            string expectedKeyLetter = $"{keyLetter}_A";
            Console.WriteLine("Running ValidateClassModel");
            Console.WriteLine($"Checking KeyLetter uniqueness: {expectedKeyLetter} for {objectName}");

            if (!usedKeyLetters.Add(expectedKeyLetter))
            {
                HandleError($"Syntax error 20: KeyLetter for {objectName} is not unique.");
            }
        }

        private void ValidateTimerModel(JObject jsonObj, HashSet<string> usedKeyLetters)
        {
            string timerKeyLetter = jsonObj["subsystems"]?[0]?["model"]?[0]?["KL"]?.ToString();
            string timerStateModelName = $"{timerKeyLetter}_ASSIGNER";

            JToken timerModelToken = jsonObj["subsystems"]?[0]?["model"]?[0];
            JToken timerStateModelToken = jsonObj["subsystems"]?[0]?["model"]?[0]?[timerStateModelName];

            // Check if Timer state model exists
            if (timerStateModelToken == null || timerStateModelToken.Type != JTokenType.Object)
            {
                HandleError($"Syntax error 21: Timer state model not found for TIMER.");
                return;
            }

            // Check KeyLetter of Timer state model
            JToken keyLetterToken = timerStateModelToken?["KeyLetter"];
            if (keyLetterToken == null || keyLetterToken.ToString() != timerKeyLetter)
            {
                HandleError($"Syntax error 21: KeyLetter for TIMER does not match the rules.");
            }
        }

        private void ValidateEventDirectedToStateModelHelper(JToken modelsToken, Dictionary<string, string> stateModels, string modelName)
        {
            if (modelsToken != null && modelsToken.Type == JTokenType.Array)
            {
                foreach (var model in modelsToken)
                {
                    string modelType = model["type"]?.ToString();
                    string className = model["class_name"]?.ToString();

                    if (modelType == "class")
                    {
                        JToken assignerToken = model[$"{className}_ASSIGNER"];

                        if (assignerToken != null)
                        {
                            Console.WriteLine($"assignerToken.Type: {assignerToken.Type}");

                            if (assignerToken.Type == JTokenType.Object)
                            {
                                JToken statesToken = assignerToken["states"];

                                if (statesToken != null && statesToken.Type == JTokenType.Array)
                                {
                                    JArray statesArray = (JArray)statesToken;

                                    foreach (var stateItem in statesArray)
                                    {
                                        string stateName = stateItem["state_name"]?.ToString();
                                        string stateModelName = $"{modelName}.{stateName}";

                                        JToken eventsToken = stateItem["events"];
                                        if (eventsToken is JArray events)
                                        {
                                            foreach (var evt in events)
                                            {
                                                string eventName = evt["event_name"]?.ToString();
                                                JToken targetsToken = evt["targets"];

                                                if (targetsToken is JArray targets)
                                                {
                                                    foreach (var target in targets)
                                                    {
                                                        string targetStateModel = target?.ToString();

                                                        // Check if target state model is in the state models dictionary
                                                        if (!stateModels.ContainsKey(targetStateModel))
                                                        {
                                                            HandleError($"Syntax error 24: Event '{eventName}' in state '{stateModelName}' targets non-existent state model '{targetStateModel}'.");
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
        }
    }
}
