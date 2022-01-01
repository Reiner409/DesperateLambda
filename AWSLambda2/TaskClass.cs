using System;


namespace desperate_houseworks_project.Models
{
    public class TaskClass
    {
        public int ID { get; set; }
        public string name { get; set; }
        public string category { get; set; }
        public string description { get; set; }
        public string icon { get; set; }
        public string user { get; set; }
        public DateTime date { get; set; }
        public Boolean verified { get; set; }
        public Boolean custom;
    }
}
