using System;

namespace classi
{
    public class FamilyMember
    {
        public string Username { get; set; }
        public int Picture { get; set; }
    }

    public class User
    {
        public FamilyMember Utente { get; set; }
        public string token { get; set; }
    }

    public class Log
    {
        public FamilyMember User;
        public DateTime date { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }
        public bool verified { get; set; }
    }

    class MedalClass
    {
        public string user { get; set; }
        public string name { get; set; }
        public int quantity { get; set; }
    }

    class RequestClass
    {
        public FamilyMember User { get; set; }
        public string familyName { get; set; }
        public int familyCode { get; set; }
    }

    public class TaskClass
    {
        public int ID { get; set; }
        public string name { get; set; }
        public string category { get; set; }
        public string description { get; set; }
        public string user { get; set; }
        public DateTime date { get; set; }
        public Boolean verified { get; set; }
    }
}