using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xtUML1
{

    public class JsonData
    {
        public string type { get; set; }
        public string sub_id { get; set; }
        public string sub_name { get; set; }
        public Model[] model { get; set; }
    }

    public class Model
    {
        public string type { get; set; }
        public string class_id { get; set; }
        public string class_name { get; set; }
        public string KL { get; set; }
        public Attribute1[] attributes { get; set; }
        public State[] states { get; set; }
        public string name { get; set; }
        public Class1[] _class { get; set; }
        public Model1 model { get; set; }
        public string sub_id { get; set; }
    }

    public class Model1
    {
        public string type { get; set; }
        public string class_id { get; set; }
        public string class_name { get; set; }
        public string KL { get; set; }
        public Attribute[] attributes { get; set; }
    }

    public class Attribute
    {
        public string attribute_type { get; set; }
        public string attribute_name { get; set; }
        public string data_type { get; set; }
    }

    public class Attribute1
    {
        public string attribute_type { get; set; }
        public string attribute_name { get; set; }
        public string data_type { get; set; }
        public string default_value { get; set; }
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
        public string[] state_event { get; set; }
        public Transition[] transitions { get; set; }
        public string action { get; set; }
    }

    public class Transition
    {
        public string target_state_id { get; set; }
        public string target_state { get; set; }
    }

    public class Class1
    {
        public string class_id { get; set; }
        public string class_name { get; set; }
        public string class_multiplicity { get; set; }
    }

}
